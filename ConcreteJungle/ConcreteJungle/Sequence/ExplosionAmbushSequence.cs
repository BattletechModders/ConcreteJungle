using BattleTech;
using ConcreteJungle.Extensions;
using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Sequence
{
    public class ExplosionAmbushSequence : MultiSequence
    {
        private List<ICombatant> Targets { get; set; }

        private List<Vector3> AmbushPositions { get; set; }
        
        private List<Weapon> AmbushWeapons { get; set; }

        private Team AmbushTeam { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        

        public override bool IsComplete { get { return this.state == AmbushExplosionSequenceState.Finished; } }

        public ExplosionAmbushSequence(CombatGameState combat, List<Weapon> ambushWeapons, Team team, 
            List<Vector3> ambushOrigins, List<ICombatant> targets) : base(combat)
        {
            this.AmbushWeapons = ambushWeapons;
            this.AmbushTeam = team;
            this.AmbushPositions = ambushOrigins;
            this.Targets = targets;
            Mod.Log.Debug($"Positions count: {AmbushPositions.Count}  weapons count: {AmbushWeapons.Count}");
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
                    if (this.originIdxForFX >= this.AmbushPositions.Count)
                    {
                        this.SetState(AmbushExplosionSequenceState.Damaging);
                    }
                    break;
                case AmbushExplosionSequenceState.Damaging:
                    this.ResolveNextBlast();
                    if (this.originIdxForAttack >= this.AmbushPositions.Count)
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
                    this.AmbushTeam, "IED Ambush", Mod.Config.Qips.ExplosiveAmbush, this.timeToTaunt * 3f);
                hasTaunted = true;
            }
        }


        private void PlayNextFX()
        {
            this.timeSinceLastExplosion += Time.deltaTime;
            if (this.timeSinceLastExplosion > this.timeBetweenExplosions)
            {
                if (this.originIdxForFX < this.AmbushPositions.Count)
                {
                    Vector3 origin = this.AmbushPositions[this.originIdxForFX];
                    Weapon weapon = this.AmbushWeapons[this.originIdxForFX];

                    Mod.Log.Debug($"Spawning explosion type {base.Combat.Constants.VFXNames.artillery_explosion} at position {origin}");
                    this.osd = new ObjectSpawnData(base.Combat.Constants.VFXNames.artillery_explosion,
                        origin, true, true);
                    this.osd.Spawn(base.Combat);

                    CameraControl.Instance.AddCameraShake(
                        10f * (weapon.DamagePerShot) *
                        base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageRelativeMod +
                        base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageAbsoluteMod,
                        2f, origin
                        );

                    // TODO: Speed up the animations?

                    GameObject spawnedObject = this.osd.spawnedObject;
                    if (spawnedObject != null)
                    {
                        Mod.Log.Debug("Playing sound.");
                        AkGameObj akGameObj = spawnedObject.GetComponent<AkGameObj>();
                        if (akGameObj == null) akGameObj = spawnedObject.AddComponent<AkGameObj>();

                        //WwiseManager.PostEvent<AudioEventList_explosion>(AudioEventList_explosion.explosion_large, akGameObj, null, null);
                        WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_thumper, akGameObj, null, null);
                        //WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_mortar, akGameObj, null, null);
                    }
                    else
                    {
                        Mod.Log.Debug("OSD WAS NULL - WTF?");
                    }

                    this.originIdxForFX++;

                }

                this.timeSinceLastExplosion = 0f;
            }

        }

        private void ResolveNextBlast()
        {
            if (this.originIdxForAttack < this.AmbushPositions.Count)
            {
                // If we're out of targets, return to update
                if (currentTargetIdx == this.Targets.Count)
                {
                    originIdxForAttack++;
                    return;
                }

                // else attack another target
                Vector3 position = this.AmbushPositions[this.originIdxForAttack];
                Weapon weapon = this.AmbushWeapons[this.originIdxForAttack];
                ICombatant target = this.Targets[this.currentTargetIdx];
                DamageNextTarget(position, weapon, target);
            }
        }

        private void DamageNextTarget(Vector3 ambushPosition, Weapon ambushWeapon, ICombatant target)
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > this.timeBetweenTargets)
            {
                if (this.currentTargetIdx < this.Targets.Count)
                {
                    BattleTech.Building targetBuilding = target as BattleTech.Building;
                    Mech targetMech = target as Mech;

                    Mod.Log.Debug($" Damaging target: {CombatantUtils.Label(target)} with weapon: {ambushWeapon} from position: {ambushPosition}");

                    WeaponHitInfo weaponHitInfo = default;
                    weaponHitInfo.attackerId = "Artillery";
                    weaponHitInfo.targetId = target.GUID;
                    weaponHitInfo.numberOfShots = ambushWeapon.ShotsWhenFired;
                    weaponHitInfo.stackItemUID = base.SequenceGUID;
                    weaponHitInfo.locationRolls = new float[ambushWeapon.ShotsWhenFired];
                    weaponHitInfo.hitLocations = new int[ambushWeapon.ShotsWhenFired];
                    weaponHitInfo.hitPositions = new Vector3[ambushWeapon.ShotsWhenFired];
                    weaponHitInfo.hitQualities = new AttackImpactQuality[ambushWeapon.ShotsWhenFired];

                    // TODO: Attacks should come from each of the source positions
                    AttackDirection attackDirection = base.Combat.HitLocation.GetAttackDirection(ambushPosition, target);
                    weaponHitInfo.attackDirections = new AttackDirection[ambushWeapon.ShotsWhenFired];

                    for (int i = 0; i < ambushWeapon.ShotsWhenFired; i++)
                    {
                        weaponHitInfo.attackDirections[i] = attackDirection;
                        weaponHitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                        weaponHitInfo.hitPositions[i] = ambushPosition;
                    }

                    this.GetIndividualHits(ref weaponHitInfo, ambushWeapon, target, ambushPosition);
                    for (int j = 0; j < ambushWeapon.ShotsWhenFired; j++)
                    {
                        Mod.Log.Debug($"  -- target takes: {ambushWeapon.DamagePerShot} to location: {weaponHitInfo.hitLocations[j]}");
                        if (targetBuilding != null)
                        {
                            targetBuilding.DamageBuilding(ambushPosition, weaponHitInfo, ambushWeapon, ambushWeapon.DamagePerShot, 0f, j);
                        }
                        else
                        {
                            target.TakeWeaponDamage(weaponHitInfo, weaponHitInfo.hitLocations[j], ambushWeapon, ambushWeapon.DamagePerShot, 0f, j, DamageType.Artillery);
                        }
                    }

                    if (targetMech != null)
                    {
                        targetMech.ResolveSourcelessWeaponDamage(weaponHitInfo, ambushWeapon, MeleeAttackType.NotSet);
                    }
                    else
                    {
                        target.ResolveWeaponDamage(weaponHitInfo, ambushWeapon, MeleeAttackType.NotSet);
                    }

                    target.HandleDeath("Artillery");

                }

                this.timeSinceLastAttack = 0f;
                this.currentTargetIdx++;
            }
        }

        private void GetIndividualHits(ref WeaponHitInfo hitInfo, Weapon weapon, ICombatant target, Vector3 ambushPosition)
        {
            hitInfo.locationRolls = base.Combat.AttackDirector.GetRandomFromCache(hitInfo, hitInfo.numberOfShots);
            hitInfo.hitVariance = base.Combat.AttackDirector.GetVarianceSumsFromCache(hitInfo, hitInfo.numberOfShots, weapon);
            for (int i = 0; i < hitInfo.numberOfShots; i++)
            {
                hitInfo.hitLocations[i] = target.GetHitLocation(null, ambushPosition, hitInfo.locationRolls[i], 0, 1f);
            }
        }

        private float timeInCurrentState;

        private bool hasTaunted = false;
        private readonly float timeToTaunt = 1f;

        private float timeSinceLastExplosion = 0f;
        private readonly float timeBetweenExplosions = 0.5f;
        private int originIdxForFX = 0;

        private float timeSinceLastAttack;
        private readonly float timeBetweenTargets = 0.25f;
        private int originIdxForAttack = 0;

        private int currentTargetIdx = 0;

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
