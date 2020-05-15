using BattleTech;
using ConcreteJungle.Extensions;
using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;
using static BattleTech.AttackDirector;

namespace ConcreteJungle.Sequence
{
    public class SpawnAmbushSequence : MultiSequence
    {
        private Vector3 AmbushPosition { get; set; }

        private List<AbstractActor> AttackingActors { get; set; }

        private List<BattleTech.Building> BuildingsToCollapse { get; set; }

        private List<ICombatant> AllTargets { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        

        public override bool IsComplete { get { return this.state == AmbushExplosionSequenceState.Finished; } }

        public SpawnAmbushSequence(CombatGameState combat, Vector3 ambushPos, Dictionary<AbstractActor, BattleTech.Building> actorToSpawnBuildings, List<ICombatant> targets) : base(combat)
        {
            Mod.Log.Debug($"Creating SAS for position: {ambushPos}");
            this.AmbushPosition = ambushPos;
            this.AttackingActors = actorToSpawnBuildings.Keys.ToList();
            this.BuildingsToCollapse = actorToSpawnBuildings.Values.ToList();
            this.AllTargets = targets;
        }

        public override void OnAdded()
        {
            base.OnAdded();
            Mod.Log.Debug("Starting new SpawnAmbushSequence.");
            this.SetState(AmbushExplosionSequenceState.Taunting);
        }

        public override void OnUpdate()
        {
            Mod.Log.Trace($"Updating AmbushExplosionSequence in state: {this.state}");
            base.OnUpdate();
            this.timeInCurrentState += Time.deltaTime;
            switch (this.state)
            {
                case AmbushExplosionSequenceState.Taunting:
                    this.Taunt();
                    if (this.timeInCurrentState > this.timeToTaunt)
                    {
                        this.SetState(AmbushExplosionSequenceState.Collapsing);
                    }
                    break;
                case AmbushExplosionSequenceState.Collapsing:
                    this.CollapseNextBuilding();
                    if (this.BuildingsToCollapse.Count < 1)
                    {
                        this.SetState(AmbushExplosionSequenceState.Attacking);
                    }
                    break;
                case AmbushExplosionSequenceState.Attacking:
                    this.ResolveNextAttack();
                    if (this.AttackingActors.Count < 1)
                    {
                        this.SetState(AmbushExplosionSequenceState.Finished);
                    }
                    break;
                case AmbushExplosionSequenceState.Finished:
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

                    Mod.Log.Debug($"Collapsing shell building: {CombatantUtils.Label(buildingToCollapse)}");
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
                this.timeSinceLastCollapse = 0f;
            }
        }

        private void SetState(AmbushExplosionSequenceState newState)
        {
            if (this.state == newState) return;

            this.state = newState;
            this.timeInCurrentState = 0f;
            switch(newState)
            {
                case AmbushExplosionSequenceState.Collapsing:
                    Mod.Log.Debug("Destroying shelter buildings");
                    return;
                case AmbushExplosionSequenceState.Attacking:
                    Mod.Log.Debug("Actors are attacking targets");
                    break;
                case AmbushExplosionSequenceState.Finished:
                    Mod.Log.Debug("Finished with AmbushExplosionSequence");
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
                // Create a quip
                Guid g = Guid.NewGuid();
                QuipHelper.PlayQuip(ModState.Combat, g.ToString(),
                    ModState.CandidateTeams.ElementAt(0), "Vehicle Ambush", Mod.Config.Qips.SpawnAmbush, this.timeToTaunt * 3f);
                hasTaunted = true;
                Mod.Log.Debug("Taunted player.");
            }
        }


        private float timeInCurrentState;

        private bool hasTaunted = false;
        private float timeToTaunt = 1f;

        private float timeSinceLastCollapse = 0f;
        private float timeBetweenBuildingCollapses = 0.25f;

        private float timeSinceLastAttack = 0f;
        private float timeBetweenAttacks = 0.25f;

        private AmbushExplosionSequenceState state;

        private enum AmbushExplosionSequenceState
        {
            NotSet,
            Taunting,
            Collapsing,
            Attacking,
            Finished
        }
    }
}
