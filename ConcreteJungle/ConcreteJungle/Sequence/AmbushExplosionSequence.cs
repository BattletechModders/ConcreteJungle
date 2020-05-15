using BattleTech;
using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Sequence
{
    public class AmbushExplosionSequence : MultiSequence
    {
        private List<ICombatant> Targets { get; set; }

        private List<Vector3> BlastOrigins { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        

        public override bool IsComplete { get { return this.state == AmbushExplosionSequenceState.Finished; } }

        public AmbushExplosionSequence(CombatGameState combat, List<Vector3> blastOrigins, List<ICombatant> targets) : base(combat)
        {
            this.BlastOrigins = blastOrigins;
            this.Targets = targets;
        }

        public override void OnAdded()
        {
            base.OnAdded();
            Mod.Log.Debug("Starting new AmbushExplosionSequence.");
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
                        this.SetState(AmbushExplosionSequenceState.Exploding);
                    }
                    break;
                case AmbushExplosionSequenceState.Exploding:
                    this.PlayNextFX();
                    if (this.BlastOrigins.Count < 1)
                    {
                        this.SetState(AmbushExplosionSequenceState.Damaging);
                    }
                    break;
                case AmbushExplosionSequenceState.Damaging:
                    this.DamageNextTarget();
                    if (this.Targets.Count < 1)
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

        private void SetState(AmbushExplosionSequenceState newState)
        {
            if (this.state == newState) return;

            this.state = newState;
            this.timeInCurrentState = 0f;
            switch(newState)
            {
                case AmbushExplosionSequenceState.Exploding:
                    Mod.Log.Debug("Playing explosion FX.");
                    return;
                case AmbushExplosionSequenceState.Damaging:
                    Mod.Log.Debug("Damaging targets");
                    break;
                case AmbushExplosionSequenceState.Finished:
                    Mod.Log.Debug("Finished with AmbushExplosionSequence");
                    this.osd.Cleanup(ModState.Combat);
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
                    ModState.CandidateTeams.ElementAt(0), "IED Ambush", Mod.Config.Qips.ExplosiveAmbush, 6);
                hasTaunted = true;
            }
        }


        private void PlayNextFX()
        {
            this.timeSinceLastExplosion += Time.deltaTime;
            if (this.timeSinceLastExplosion > this.timeBetweenExplosions)
            {
                if (this.BlastOrigins.Count > 0)
                {
                    Vector3 origin = this.BlastOrigins[0];
                    this.BlastOrigins.RemoveAt(0);

                    Mod.Log.Debug($"Spawning explosion type {base.Combat.Constants.VFXNames.artillery_explosion} at position {origin}");
                    this.osd = new ObjectSpawnData(base.Combat.Constants.VFXNames.artillery_explosion,
                        origin, true, true);
                    this.osd.Spawn(base.Combat);

                    CameraControl.Instance.AddCameraShake(
                        10f * (Mod.Config.ExplosionAmbush.DamagePerShot) *
                            base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageRelativeMod +
                            base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageAbsoluteMod,
                        2f, origin);
                    this.timeSinceLastSound = 0f;
                    this.timeBetweenSounds = UnityEngine.Random.Range(this.minTimeBetweenExplosions, this.maxTimeBetweenExplosions);

                    // TODO: Speed up the animations?

                    GameObject spawnedObject = this.osd.spawnedObject;
                    if (spawnedObject != null)
                    {
                        Mod.Log.Debug("Playing sound.");
                        AkGameObj akGameObj = spawnedObject.GetComponent<AkGameObj>();
                        if (akGameObj == null) akGameObj = spawnedObject.AddComponent<AkGameObj>();

                        WwiseManager.PostEvent<AudioEventList_explosion>(AudioEventList_explosion.explosion_large, akGameObj, null, null);
                        //WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_thumper, this.audioObject, null, null); 
                        //WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_mortar, this.audioObject, null, null);
                    }
                    else
                    {
                        Mod.Log.Debug("OSD WAS NULL - WTF?");
                    }

                }
                this.timeSinceLastExplosion = 0f;
            }

        }

        //private void SpawnScorch()
        //{
        //    float num = this.referenceAbility.Def.FloatParam1 * 0.8f;
        //    Vector3 vector = new Vector3(UnityEngine.Random.Range(0f, 1f), 0f, UnityEngine.Random.Range(0f, 1f));
        //    FootstepManager.Instance.AddScorch(this.TargetPositions[0], vector.normalized, new Vector3(num, num, num), true);
        //}

        //private void DestroyFlimsyObjects()
        //{
        //    foreach (Collider collider in Physics.OverlapSphere(this.TargetPositions[0], 
        //        this.referenceAbility.Def.FloatParam1, -5, QueryTriggerInteraction.Ignore))
        //    {
        //        Vector3 normalized = (collider.transform.position - this.TargetPositions[0]).normalized;
        //        float num = Mod.Config.ExplosionAmbush.DamagePerShot + base.Combat.Constants.ResolutionConstants.FlimsyDestructionForceMultiplier;
        //        DestructibleObject component = collider.gameObject.GetComponent<DestructibleObject>();
        //        DestructibleUrbanFlimsy component2 = collider.gameObject.GetComponent<DestructibleUrbanFlimsy>();
        //        if (component != null && component.isFlimsy)
        //        {
        //            component.TakeDamage(this.TargetPositions[0], normalized, num);
        //            component.Collapse(normalized, num);
        //        }
        //        if (component2 != null)
        //        {
        //            component2.PlayDestruction(normalized, num);
        //        }
        //    }
        //}

        private void DamageNextTarget()
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > this.timeBetweenTargets)
            {
                if (this.Targets.Count > 0)
                {
                    ICombatant combatant = this.Targets[0];
                    Mod.Log.Debug($" Damaging target: {CombatantUtils.Label(combatant)}");

                    this.Targets.Remove(combatant);
                    AbstractActor abstractActor = combatant as AbstractActor;

                    //if (abstractActor != null && abstractActor.BehaviorTree != null && 
                    //    !abstractActor.BehaviorTree.IsTargetIgnored(this.owningActor))
                    //{
                    //    combatant.LastTargetedPhaseNumber = base.Combat.TurnDirector.TotalElapsedPhases;
                    //}

                    //if (Mod.Config.ExplosionAmbush.DamagePerShot > 0f)
                    //{
                    //    Vector2 damageRange = new Vector2(
                    //        Mod.Config.ExplosionAmbush.DamagePerShot - Mod.Config.ExplosionAmbush.DamageVariance,
                    //        Mod.Config.ExplosionAmbush.DamagePerShot + Mod.Config.ExplosionAmbush.DamageVariance
                    //        );
                    //    DamageOrderUtility.ApplyDamageToAllLocations(this.owningActor.GUID, base.SequenceGUID, base.RootSequenceGUID, 
                    //        combatant, (int)damageRange.x, (int)damageRange.y, AttackDirection.FromArtillery, DamageType.Artillery);
                    //}

                    //if (Mod.Config.ExplosionAmbush.DamagePerShot > 0f)
                    //{
                    //    DamageOrderUtility.ApplyHeatDamage(base.SequenceGUID, combatant, (int)Mod.Config.ExplosionAmbush.DamagePerShot);
                    //}

                    //if (Mod.Config.ExplosionAmbush.DamagePerShot > 0f)
                    //{
                    //    DamageOrderUtility.ApplyStabilityDamage(base.SequenceGUID, combatant, Mod.Config.ExplosionAmbush.DamagePerShot);
                    //}

                    //ActorAttackedMessage message = new ActorAttackedMessage("0", combatant.GUID);
                    //base.Combat.MessageCenter.PublishMessage(message);
                }
                this.timeSinceLastAttack = 0f;
            }
        }

        private float timeInCurrentState;
        //private float timeInExplosion;
        //private float timeInDamaging;

        private bool hasTaunted = false;
        private float timeToTaunt = 6f;

        private float timeSinceLastExplosion = 0f;
        private float timeBetweenExplosions = 1f;

        private float timeSinceLastAttack;
        private float timeBetweenTargets = 0.25f;
        //private float timeShowingEffects = 3f;

        private float timeSinceLastSound;
        private float timeBetweenSounds;
        private float minTimeBetweenExplosions = 0.0625f;
        private float maxTimeBetweenExplosions = 0.125f;

        //private float focalDistance = 400f;

        //private float impactLightIntensity = 1000000f;

        //private AkGameObj audioObject;

        private ObjectSpawnData osd;


        private AmbushExplosionSequenceState state;

        private enum AmbushExplosionSequenceState
        {
            NotSet,
            Taunting,
            Exploding,
            Damaging,
            Finished
        }
    }
}
