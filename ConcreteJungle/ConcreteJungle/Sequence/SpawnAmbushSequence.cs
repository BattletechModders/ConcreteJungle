using BattleTech;
using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Sequence
{
    public class SpawnAmbushSequence : MultiSequence
    {
        private Vector3 AmbushPosition { get; set; }

        private List<AbstractActor> AttackingActors { get; set; }

        private List<BattleTech.Building> BuildingsToCollapse { get; set; }

        private List<ICombatant> AllTargets { get; set; }

        private bool ApplyAttacks { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        
        public override bool IsComplete { get { return this.state == SpawnAmbushSequenceState.Finished; } }

        public SpawnAmbushSequence(CombatGameState combat, Vector3 ambushPos, List<AbstractActor> spawnedActors, 
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
            this.SetState(SpawnAmbushSequenceState.Taunting);
            Mod.Log.Debug($"Starting new SpawnAmbushSequence in state: {this.state}");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            this.timeInCurrentState += Time.deltaTime;
            switch (this.state)
            {
                case SpawnAmbushSequenceState.Taunting:
                    this.Taunt();
                    if (this.timeInCurrentState > this.timeToTaunt)
                    {
                        this.SetState(SpawnAmbushSequenceState.Collapsing);
                    }
                    break;
                case SpawnAmbushSequenceState.Collapsing:
                    this.CollapseNextBuilding();
                    if (this.BuildingsToCollapse.Count < 1)
                    {
                        this.SetState(SpawnAmbushSequenceState.Attacking);
                    }
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
                    break;
                default:
                    return;
            }
        }

        private void CollapseNextBuilding()
        {
            this.timeSinceLastCollapse += Time.deltaTime;
            if (this.timeSinceLastCollapse> this.timeBetweenBuildingCollapses)
            {
                if (this.BuildingsToCollapse.Count > 0)
                {
                    BattleTech.Building buildingToCollapse = this.BuildingsToCollapse[0];
                    this.BuildingsToCollapse.RemoveAt(0);

                    Mod.Log.Debug($"Collapsing ambush building: {CombatantUtils.Label(buildingToCollapse)}");
                    buildingToCollapse.FlagForDeath("Ambush Collapse", DeathMethod.Unknown, DamageType.Artillery, 0, -1, "0", false);
                    buildingToCollapse.HandleDeath("0");
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
                    AbstractActor actor = this.AttackingActors[0];
                    this.AttackingActors.RemoveAt(0);

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
                    List<Weapon> selectedWeapons = new List<Weapon>();
                    foreach (Weapon weapon in actor.Weapons)
                    {
                        if (weapon.CanFire && weapon.MinRange < currentRange)
                        {
                            selectedWeapons.Add(weapon);
                        }
                    }
                    
                    Mod.Log.Debug($"Ambush attack from actor: {CombatantUtils.Label(actor)}");
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
            switch(newState)
            {
                case SpawnAmbushSequenceState.Collapsing:
                    Mod.Log.Debug("Destroying ambush buildings");
                    return;
                case SpawnAmbushSequenceState.Attacking:
                    Mod.Log.Debug("Actors are attacking targets");
                    break;
                case SpawnAmbushSequenceState.Finished:
                    Mod.Log.Debug("Finished with SpawnAmbushSequence");
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
                Mod.Log.Debug("Taunting player.");
                // Create a quip
                Guid g = Guid.NewGuid();
                QuipHelper.PlayQuip(ModState.Combat, g.ToString(), 
                    AttackingActors[0].team,
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
            Taunting,
            Collapsing,
            Attacking,
            Finished
        }
    }
}
