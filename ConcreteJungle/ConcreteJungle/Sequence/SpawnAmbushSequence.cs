using ConcreteJungle.Helper;
using IRBTModUtils.Extension;
using MonsterMashup.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Sequence
{
    public class SpawnAmbushSequence : MultiSequence
    {
        private Vector3 AmbushPosition { get; set; }

        private OriginVisualization OriginVisualization { get; set; }

        private List<(AbstractActor actor, Vector3 spawnPos, Quaternion spawnRot)> AttackingActors { get; set; }

        private List<BattleTech.Building> BuildingsToCollapse { get; set; }

        private List<ICombatant> AllTargets { get; set; }

        private bool ApplyAttacks { get; set; }

        public override bool IsValidMultiSequenceChild { get { return false; } }

        public override bool IsParallelInterruptable { get { return false; } }

        public override bool IsCancelable { get { return false; } }

        public override bool IsComplete { get { return this.state == SpawnAmbushSequenceState.Finished; } }

        public SpawnAmbushSequence(CombatGameState combat, Vector3 ambushPos, List<(AbstractActor, Vector3, Quaternion)> spawnedActors,
            List<BattleTech.Building> buildingsToLevel, List<ICombatant> targets, bool applyAttacks) : base(combat)
        {
            this.AmbushPosition = ambushPos;
            this.AttackingActors = spawnedActors;
            this.BuildingsToCollapse = buildingsToLevel;
            this.AllTargets = targets;
            this.ApplyAttacks = applyAttacks;
        }

        public override void OnAdded()
        {
            base.OnAdded();
            this.SetState(SpawnAmbushSequenceState.Collapsing);
            Mod.Log.Debug?.Write($"Starting new SpawnAmbushSequence in state: {this.state}");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            this.timeInCurrentState += Time.deltaTime;
            switch (this.state)
            {
                case SpawnAmbushSequenceState.DebugVisuals:
                    this.CreateOriginVisuals();
                    this.SetState(SpawnAmbushSequenceState.Collapsing);
                    break;
                case SpawnAmbushSequenceState.Collapsing:
                    this.CollapseNextBuilding();
                    if (this.BuildingsToCollapse.Count < 1)
                    {
                        this.SetState(SpawnAmbushSequenceState.Taunting);
                    }
                    break;
                case SpawnAmbushSequenceState.Taunting:
                    this.Taunt();
                    if (this.timeInCurrentState > this.timeToTaunt)
                    {
                        this.SetState(SpawnAmbushSequenceState.Spawning);
                    }
                    break;
                case SpawnAmbushSequenceState.Spawning:
                    this.UpdateSpawnedActors();
                    this.SetState(SpawnAmbushSequenceState.Attacking);
                    break;
                case SpawnAmbushSequenceState.Attacking:
                    if (!ApplyAttacks)
                    {
                        this.SetState(SpawnAmbushSequenceState.Finished);
                        return;
                    }

                    this.ResolveNextAttack();
                    if (this.AttackingActors.Count < 1)
                    {
                        this.SetState(SpawnAmbushSequenceState.Finished);
                    }
                    break;
                case SpawnAmbushSequenceState.Finished:
                    ModState.CurrentSpawningLance = null;
                    //if (originVisualization != null) originVisualization.Destroy();
                    break;
                default:
                    return;
            }
        }

        private void CreateOriginVisuals()
        {
            Mod.Log.Debug?.Write($"Creating origin visualization at vector: {this.AmbushPosition}");
            this.OriginVisualization = new OriginVisualization(this.AmbushPosition);
            this.OriginVisualization.Init();
            this.OriginVisualization.Show();
        }

        private void UpdateSpawnedActors()
        {
            // Attempt to solve the underground issues
            foreach ((AbstractActor actor, Vector3 spawnPos, Quaternion spawnRot) t in this.AttackingActors)
            {
                Mod.Log.Debug?.Write($"Aligning spawn position to nearest hex for actor: {t.actor.DistinctId()}");
                Vector3 hexPosition = Combat.HexGrid.GetClosestPointOnGrid(t.spawnPos);
                RaycastHit[] array = Physics.RaycastAll(hexPosition + SnapToTerrain.aboveTerrainOffset, Vector3.down, SnapToTerrain.terrainDistance * 2f);
                Vector3? vector = null;
                for (int i = 0; i < array.Length; i++)
                {
                    RaycastHit raycastHit = array[i];
                    if ((raycastHit.collider.GetComponent<ObstructionGameLogic>() != null || raycastHit.collider.GetComponent<Terrain>() != null) && (!vector.HasValue || vector.Value.y < raycastHit.point.y))
                    {
                        vector = raycastHit.point;
                    }
                }
                if (vector.HasValue)
                {
                    hexPosition.y = vector.Value.y;
                }
                hexPosition.y = Combat.MapMetaData.GetLerpedHeightAt(hexPosition);

                t.actor.TeleportActor(hexPosition);
                Mod.Log.Debug?.Write($"  Teleported actor to position: {hexPosition}");

                //t.actor.CurrentRotation.SetLookRotation(targetDir, Vector3.up);
                t.actor.GameRep.transform.LookAt(this.AmbushPosition);
                t.actor.CurrentRotation = t.actor.GameRep.transform.localRotation;
                t.actor.OnPositionUpdate(t.actor.CurrentPosition, t.actor.CurrentRotation, -1, updateDesignMask: true, null);

                Mod.Log.Debug?.Write($"  Set rotation {t.actor.CurrentRotation} to face position {this.AmbushPosition}");

                t.actor.GameRep.FadeIn(1f);
            }
        }

        private void CollapseNextBuilding()
        {
            Mod.Log.Debug?.Write($"Collapsing ambush buildings: {this.BuildingsToCollapse.Count}");
            this.timeSinceLastCollapse += Time.deltaTime;
            if (this.timeSinceLastCollapse > this.timeBetweenBuildingCollapses)
            {
                if (this.BuildingsToCollapse.Count > 0)
                {
                    BattleTech.Building toCollapse = this.BuildingsToCollapse.First();

                    Mod.Log.Debug?.Write($"Collapsing ambush building: {CombatantUtils.Label(toCollapse)}");
                    toCollapse.FlagForDeath("Ambush Collapse", DeathMethod.Unknown, DamageType.Artillery, 0, -1, "0", false);
                    toCollapse.HandleDeath("0");

                    this.BuildingsToCollapse.RemoveAt(0);

                }
                this.timeSinceLastCollapse = 0f;
            }
        }

        private void ResolveNextAttack()
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > this.timeBetweenAttacks)
            {
                if (this.AttackingActors.Count > 0)
                {
                    AbstractActor actor = this.AttackingActors.First().actor;
                    this.AttackingActors.RemoveAt(0);
                    Mod.Log.Info?.Write($"Ambush attack from actor: {actor.DistinctId()}");

                    // Find the closest target
                    ICombatant closestTarget = AllTargets[0];
                    foreach (ICombatant target in AllTargets)
                    {
                        if ((target.CurrentPosition - actor.CurrentPosition).magnitude <
                            (closestTarget.CurrentPosition - actor.CurrentPosition).magnitude)
                        {
                            closestTarget = target;
                        }
                    }

                    float currentRange = (closestTarget.CurrentPosition - actor.CurrentPosition).magnitude;
                    Mod.Log.Info?.Write($" -- ambush attack targets closest actor: {closestTarget.DistinctId()} at distance: {currentRange}");

                    List<Weapon> selectedWeapons = new List<Weapon>();
                    foreach (Weapon weapon in actor.Weapons)
                    {
                        if (weapon.CanFire && weapon.MinRange < currentRange)
                        {
                            selectedWeapons.Add(weapon);
                            Mod.Log.Info?.Write($" -- ambush weapon: {weapon.UIName}");
                        }
                    }

                    AttackStackSequence attackSequence = new AttackStackSequence(actor, closestTarget, actor.CurrentPosition, actor.CurrentRotation,
                        selectedWeapons);
                    ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackSequence));
                }
                this.timeSinceLastAttack = 0f;
            }
        }

        private void SetState(SpawnAmbushSequenceState newState)
        {
            if (this.state == newState) return;

            this.state = newState;
            this.timeInCurrentState = 0f;
            switch (newState)
            {
                case SpawnAmbushSequenceState.Collapsing:
                    Mod.Log.Debug?.Write("Destroying ambush buildings");
                    return;
                case SpawnAmbushSequenceState.Taunting:
                    Mod.Log.Debug?.Write("Playing taunt for player");
                    break;
                case SpawnAmbushSequenceState.Spawning:
                    Mod.Log.Debug?.Write("Creating attacking units");
                    break;
                case SpawnAmbushSequenceState.Attacking:
                    Mod.Log.Debug?.Write("Actors are attacking targets");
                    break;
                case SpawnAmbushSequenceState.Finished:
                    Mod.Log.Debug?.Write("Finished with SpawnAmbushSequence");
                    base.ClearCamera();
                    return;
                default:
                    return;

            }
        }

        private void Taunt()
        {
            if (!hasTaunted)
            {
                Mod.Log.Debug?.Write("Taunting player.");
                // Create a quip
                Guid g = Guid.NewGuid();
                Team t = this.AttackingActors.First().actor.team;
                QuipHelper.PlayQuip(ModState.Combat, g.ToString(), t,
                    "Vehicle Ambush", Mod.Config.Quips.SpawnAmbush, this.timeToTaunt * 3f);
                hasTaunted = true;
            }
        }


        private float timeInCurrentState;

        private bool hasTaunted = false;
        private float timeToTaunt = 1f;

        private float timeSinceLastCollapse = 0f;
        private float timeBetweenBuildingCollapses = 0.25f;

        private float timeSinceLastAttack = 0f;
        private float timeBetweenAttacks = 0.25f;

        private SpawnAmbushSequenceState state;

        private enum SpawnAmbushSequenceState
        {
            NotSet,
            DebugVisuals,
            Collapsing,
            Taunting,
            Spawning,
            Attacking,
            Finished
        }
    }
}
