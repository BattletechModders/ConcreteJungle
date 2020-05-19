using BattleTech;
using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Sequence
{
    public class InfantryAmbushSequence : MultiSequence
    {
        private Vector3 AmbushPos { get; set; }

        private List<AbstractActor> AttackingActors { get; set; }

        private List<BattleTech.Building> ShellBuildings { get; set; }

        private List<ICombatant> AllTargets { get; set; }

        private bool ApplyAttacks { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        
        public override bool IsComplete { get { return this.state == InfantryAmbushSequenceState.Finished; } }

        public InfantryAmbushSequence(CombatGameState combat, Vector3 ambushPos, List<AbstractActor> spawnedActors, 
            List<BattleTech.Building> shellBuildings, List<ICombatant> targets, bool applyAttacks) : base(combat)
        {
            this.AmbushPos = ambushPos;
            this.AttackingActors = spawnedActors;
            this.ShellBuildings = shellBuildings;
            this.AllTargets = targets;
            this.ApplyAttacks = applyAttacks;
        }

        public override void OnAdded()
        {
            base.OnAdded();
            this.SetState(InfantryAmbushSequenceState.Taunting);
            Mod.Log.Debug($"Starting new SpawnAmbushSequence in state: {this.state}");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            this.timeInCurrentState += Time.deltaTime;
            switch (this.state)
            {
                case InfantryAmbushSequenceState.Taunting:
                    this.Taunt();
                    if (this.timeInCurrentState > this.timeToTaunt)
                    {
                        this.SetState(InfantryAmbushSequenceState.Attacking);
                    }
                    break;
                case InfantryAmbushSequenceState.Attacking:
                    if (!ApplyAttacks)
                    {
                        this.SetState(InfantryAmbushSequenceState.Finished);
                        return;
                    }

                    this.ResolveNextAttack();
                    if (this.AttackingActors.Count < 1)
                    {
                        this.SetState(InfantryAmbushSequenceState.Finished);
                    }
                    break;
                case InfantryAmbushSequenceState.Finished:
                    break;
                default:
                    return;
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
            }
        }

        private void SetState(InfantryAmbushSequenceState newState)
        {
            if (this.state == newState) return;

            this.state = newState;
            this.timeInCurrentState = 0f;
            switch(newState)
            {
                case InfantryAmbushSequenceState.Taunting:
                    Mod.Log.Debug("Actors are taunting targets");
                    break;
                case InfantryAmbushSequenceState.Attacking:
                    Mod.Log.Debug("Actors are attacking targets");
                    break;
                case InfantryAmbushSequenceState.Finished:
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
                QuipHelper.PlayQuip(ModState.Combat, g.ToString(),AttackingActors[0].team,
                    "Infantry Ambush", Mod.Config.Quips.InfantryAmbush, this.timeToTaunt * 3f);
                hasTaunted = true;
            }
        }

        private float timeInCurrentState;

        private bool hasTaunted = false;
        private readonly float timeToTaunt = 1f;

        private float timeSinceLastAttack = 0f;
        private readonly float timeBetweenAttacks = 0.25f;

        private InfantryAmbushSequenceState state;

        private enum InfantryAmbushSequenceState
        {
            NotSet,
            Taunting,
            Attacking,
            Finished
        }
    }
}
