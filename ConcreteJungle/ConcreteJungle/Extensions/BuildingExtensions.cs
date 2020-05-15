using BattleTech;
using UnityEngine;

namespace ConcreteJungle.Extensions
{
    public static class BuildingExtensions
    {
        // This method works around issues with the HBS code requiring an AbstractActor as the source of an attack.
        public static void DamageBuilding(this BattleTech.Building target, Vector3 attackOrigin, WeaponHitInfo hitInfo,
            Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType)
        {

            float totalDamage = damageAmount + directStructureDamage;
            ModState.Combat.MessageCenter.PublishMessage(new TakeDamageMessage(hitInfo.attackerId, target.GUID, totalDamage));
            target.StatCollection.ModifyStat<float>(hitInfo.attackerId, hitInfo.stackItemUID, "Structure", StatCollection.StatOperation.Float_Subtract, totalDamage, -1, true);
            Vector3 vector = hitInfo.hitPositions[0] - attackOrigin;
            vector.Normalize();

            if (target.DestructibleObjectGroup != null)
            {
                target.DestructibleObjectGroup.TakeDamage(hitInfo.hitPositions[hitIndex], vector,
                    totalDamage + ModState.Combat.Constants.ResolutionConstants.BuildingDestructionForceMultiplier, totalDamage);
            }

            if (target.UrbanDestructible != null)
            {
                target.UrbanDestructible.TakeDamage(hitInfo, hitIndex, weapon, vector, totalDamage);
            }

            target.ResolveWeaponDamage(hitInfo);
        }
    }
}
