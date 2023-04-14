using System;
using System.Collections.Generic;

namespace ConcreteJungle.Extensions
{
    public static class MechExtensions
    {

        // The default method assumes an absractActor exists, and tries to draw a line of fire. We don't have that, so skip it.
        public static void ResolveSourcelessWeaponDamage(this Mech mech, WeaponHitInfo hitInfo, Weapon weapon, MeleeAttackType meleeAttackType)
        {
            AttackDirector.AttackSequence attackSequence = ModState.Combat.AttackDirector.GetAttackSequence(hitInfo.attackSequenceId);
            float damagePerShot = weapon.DamagePerShot;
            float structureDamagePerShot = weapon.StructureDamagePerShot;

            LineOfFireLevel lineOfFireLevel = LineOfFireLevel.LOFClear;
            damagePerShot = mech.GetAdjustedDamage(damagePerShot, weapon.WeaponCategoryValue, mech.occupiedDesignMask, lineOfFireLevel, false);
            structureDamagePerShot = mech.GetAdjustedDamage(structureDamagePerShot, weapon.WeaponCategoryValue, mech.occupiedDesignMask, lineOfFireLevel, false);
            foreach (KeyValuePair<int, float> keyValuePair in hitInfo.ConsolidateCriticalHitInfo(mech.GUID, damagePerShot))
            {
                if (keyValuePair.Key != 0 && keyValuePair.Key != 65536 && (mech.ArmorForLocation(keyValuePair.Key) <= 0f || structureDamagePerShot > 0f))
                {
                    ChassisLocations chassisLocationFromArmorLocation = MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)keyValuePair.Key);
                    if (!mech.IsLocationDestroyed(chassisLocationFromArmorLocation))
                    {
                        Traverse checkForCritT = Traverse.Create(mech).Method("CheckForCrit", new Type[] { typeof(WeaponHitInfo), typeof(ChassisLocations), typeof(Weapon) });
                        checkForCritT.GetValue(new object[] { hitInfo, chassisLocationFromArmorLocation, weapon });
                    }
                }
            }
            if (weapon.HeatDamagePerShot > 0f)
            {
                bool flag = false;
                for (int i = 0; i < hitInfo.numberOfShots; i++)
                {
                    if (hitInfo.DidShotHitTarget(mech.GUID, i) && hitInfo.ShotHitLocation(i) != 0 && hitInfo.ShotHitLocation(i) != 65536)
                    {
                        flag = true;
                        mech.AddExternalHeat(string.Format("Heat Damage from {0}", weapon.Description.Name), (int)weapon.HeatDamagePerShotAdjusted(hitInfo.hitQualities[i]));
                    }
                }
                if (flag && attackSequence != null)
                {
                    attackSequence.FlagAttackDidHeatDamage(mech.GUID);
                }
            }
            float num3 = hitInfo.ConsolidateInstability(mech.GUID, weapon.Instability(), mech.Combat.Constants.ResolutionConstants.GlancingBlowDamageMultiplier,
                mech.Combat.Constants.ResolutionConstants.NormalBlowDamageMultiplier, mech.Combat.Constants.ResolutionConstants.SolidBlowDamageMultiplier);
            num3 *= mech.StatCollection.GetValue<float>("ReceivedInstabilityMultiplier");
            num3 *= mech.EntrenchedMultiplier;
            mech.AddAbsoluteInstability(num3, StabilityChangeSource.Attack, hitInfo.attackerId);
        }
    }
}
