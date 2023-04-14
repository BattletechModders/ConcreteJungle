namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CritChanceRules), "GetCritMultiplier")]
    static class CritChanceRules_GetCritMultiplier
    {
        public static bool Prefix(CritChanceRules __instance, ICombatant target, Weapon weapon, bool shouldLog, ref float __result)
        {
            // We are in a weapon without parent - so we're coming from one of our sourceless attacks. Don't break.
            if (weapon != null && weapon.parent == null)
            {
                float baseCritChance = 1f;
                float weaponMultiplier = __instance.GetWeaponMultiplier(weapon);
                float targetEffectMutliplier = __instance.GetTargetEffectMutliplier(target);
                float totalCritChance = baseCritChance * weaponMultiplier * targetEffectMutliplier;

                if (CritChanceRules.attackLogger.IsDebugEnabled && shouldLog)
                {
                    CritChanceRules.attackLogger.LogDebug(string.Format("[GetCritMultiplier]baseMultiplier = {0}, weaponModier = {1}, targetEffectModifier = {2}", baseCritChance, weaponMultiplier, targetEffectMutliplier));
                }

                if (totalCritChance < 0f)
                {
                    totalCritChance = 0f;
                }

                __result = totalCritChance;
                return false;
            }

            return true;
        }
    }
}
