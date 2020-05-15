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
    public class AmbushExplosionSequence : MultiSequence
    {
        private List<ICombatant> Targets { get; set; }

        private List<Vector3> FXOrigins { get; set; }
        
        private Vector3 AmbushPosition { get; set; }

        private Weapon AmbushWeapon { get; set; }

        private int AmbushHits { get; set; }

        public override bool IsValidMultiSequenceChild {  get { return false;  } }

        public override bool IsParallelInterruptable { get { return false; } }
        
        public override bool IsCancelable { get { return false; } }
        

        public override bool IsComplete { get { return this.state == AmbushExplosionSequenceState.Finished; } }

        public AmbushExplosionSequence(CombatGameState combat, Vector3 ambushPos, List<Vector3> blastOrigins, List<ICombatant> targets) : base(combat)
        {
            Mod.Log.Debug($"Creating AES for position: {ambushPos}");
            AmbushPosition = ambushPos;
            AmbushHits = blastOrigins.Count;

            WeaponDef weaponDef = ModState.Combat.DataManager.WeaponDefs.Get("Weapon_Ambush_Explosion");
            Mod.Log.Debug($"Loaded weapon def: {weaponDef} with Damage: {weaponDef.Damage}");
            MechComponentRef mechComponentRef = new MechComponentRef(
                weaponDef.Description.Id, weaponDef.Description.Id + "_ReferenceWeapon", 
                ComponentType.Weapon, ChassisLocations.None, -1, ComponentDamageLevel.Functional,
                true);
            Mod.Log.Debug($"Created mechComponentRef: {mechComponentRef}");
            mechComponentRef.SetComponentDef(weaponDef);
            mechComponentRef.DataManager = this.Combat.DataManager;
            mechComponentRef.RefreshComponentDef();
            Mod.Log.Debug($"Refreshed componentDef. Def after refresh is: {mechComponentRef.Def}");
            AmbushWeapon = new Weapon(null, ModState.Combat, mechComponentRef, this.Combat.GUID);
            AmbushWeapon.InitStats();
            Mod.Log.Debug($"Created ambush weapon {AmbushWeapon.Description.Id} with " +
                $"Name: {AmbushWeapon.Name} " +
                $"DamagePerShot: {AmbushWeapon.DamagePerShot} and " +
                $"baseComponentDef: {AmbushWeapon.baseComponentRef}");

            this.FXOrigins = blastOrigins;
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
                    if (this.FXOrigins.Count < 1)
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
                    ModState.CandidateTeams.ElementAt(0), "IED Ambush", Mod.Config.Qips.ExplosiveAmbush, this.timeToTaunt * 3f);
                hasTaunted = true;
            }
        }


        private void PlayNextFX()
        {
            this.timeSinceLastExplosion += Time.deltaTime;
            if (this.timeSinceLastExplosion > this.timeBetweenExplosions)
            {
                if (this.FXOrigins.Count > 0)
                {
                    Vector3 origin = this.FXOrigins[0];
                    this.FXOrigins.RemoveAt(0);

                    Mod.Log.Debug($"Spawning explosion type {base.Combat.Constants.VFXNames.artillery_explosion} at position {origin}");
                    this.osd = new ObjectSpawnData(base.Combat.Constants.VFXNames.artillery_explosion,
                        origin, true, true);
                    this.osd.Spawn(base.Combat);

                    CameraControl.Instance.AddCameraShake(
                        10f * (Mod.Config.ExplosionAmbush.DamagePerShot) *
                            base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageRelativeMod +
                            base.Combat.Constants.CombatUIConstants.ScreenShakeRangedDamageAbsoluteMod,
                        2f, origin);

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

                }
                this.timeSinceLastExplosion = 0f;
            }

        }

        private void DamageNextTarget()
        {
            this.timeSinceLastAttack += Time.deltaTime;
            if (this.timeSinceLastAttack > this.timeBetweenTargets)
            {
                if (this.Targets.Count > 0)
                {
                    ICombatant target = this.Targets[0];
                    BattleTech.Building targetBuilding = target as BattleTech.Building;
                    Mech targetMech = target as Mech;

                    this.Targets.Remove(target);
                    Mod.Log.Debug($" Damaging target: {CombatantUtils.Label(target)}");

                    WeaponHitInfo weaponHitInfo = default(WeaponHitInfo);
                    weaponHitInfo.attackerId = "Artillery";
                    weaponHitInfo.targetId = target.GUID;
                    weaponHitInfo.numberOfShots = this.AmbushHits;
                    weaponHitInfo.stackItemUID = base.SequenceGUID;
                    weaponHitInfo.locationRolls = new float[this.AmbushHits];
                    weaponHitInfo.hitLocations = new int[this.AmbushHits];
                    weaponHitInfo.hitPositions = new Vector3[this.AmbushHits];
                    weaponHitInfo.hitQualities = new AttackImpactQuality[this.AmbushHits];

                    // TODO: Attacks should come from each of the source positions
                    AttackDirection attackDirection = base.Combat.HitLocation.GetAttackDirection(this.AmbushPosition, target);
                    weaponHitInfo.attackDirections = new AttackDirection[this.AmbushHits];

                    for (int i = 0; i < this.AmbushHits; i++)
                    {
                        weaponHitInfo.attackDirections[i] = attackDirection;
                        weaponHitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                        weaponHitInfo.hitPositions[i] = this.AmbushPosition;
                    }

                    this.GetIndividualHits(ref weaponHitInfo, AmbushWeapon, target);
                    for (int j = 0; j < this.AmbushHits; j++)
                    {
                        Mod.Log.Debug($"  -- target takes: {this.AmbushWeapon.DamagePerShot} to location: {weaponHitInfo.hitLocations[j]}");
                        if (targetBuilding != null)
                        {
                            targetBuilding.DamageBuilding(this.AmbushPosition, weaponHitInfo, this.AmbushWeapon, this.AmbushWeapon.DamagePerShot, 0f, j);
                        }
                        else
                        {
                            target.TakeWeaponDamage(weaponHitInfo, weaponHitInfo.hitLocations[j], this.AmbushWeapon, this.AmbushWeapon.DamagePerShot, 0f, j, DamageType.Artillery);
                        }
                    }

                    if (targetMech != null)
                    {
                        targetMech.ResolveSourcelessWeaponDamage(weaponHitInfo, this.AmbushWeapon, MeleeAttackType.NotSet);
                    }
                    else
                    {
                        target.ResolveWeaponDamage(weaponHitInfo, this.AmbushWeapon, MeleeAttackType.NotSet);
                    }

                    target.HandleDeath("Artillery");
                }
                this.timeSinceLastAttack = 0f;
            }
        }

        private void GetIndividualHits(ref WeaponHitInfo hitInfo, Weapon weapon, ICombatant target)
        {
            hitInfo.locationRolls = base.Combat.AttackDirector.GetRandomFromCache(hitInfo, hitInfo.numberOfShots);
            hitInfo.hitVariance = base.Combat.AttackDirector.GetVarianceSumsFromCache(hitInfo, hitInfo.numberOfShots, weapon);
            for (int i = 0; i < hitInfo.numberOfShots; i++)
            {
                hitInfo.hitLocations[i] = target.GetHitLocation(null, this.AmbushPosition, hitInfo.locationRolls[i], 0, 1f);
            }
        }

        private float timeInCurrentState;

        private bool hasTaunted = false;
        private float timeToTaunt = 1f;

        private float timeSinceLastExplosion = 0f;
        private float timeBetweenExplosions = 0.5f;

        private float timeSinceLastAttack;
        private float timeBetweenTargets = 0.25f;

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
