namespace ConcreteJungle.Patches
{
    // Attempt to make turrets that are trap turrets invulnerable
    [HarmonyPatch(typeof(Turret), "HandleDeath")]
    static class Turret_HandleDeath
    {
        static void Prefix(ref bool __runOriginal, Turret __instance)
        {
            if (!__runOriginal) return;

            if (__instance != null &&
                ModState.AmbushTurretGUIDtoBuilding.ContainsKey(__instance.GUID) &&
                ModState.AmbushTurretGUIDtoBuilding[__instance.GUID].GUID != ModState.KillingLinkedUnitsSource)
            {
                __runOriginal = false;
            }
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "FlagForDeath")]
    static class Turret_FlagForDeath
    {
        static void Prefix(ref bool __runOriginal, AbstractActor __instance)
        {
            if (!__runOriginal) return;

            Turret turret = __instance as Turret;
            if (turret != null && 
                ModState.AmbushTurretGUIDtoBuilding.ContainsKey(__instance.GUID) &&
                ModState.AmbushTurretGUIDtoBuilding[__instance.GUID].GUID != ModState.KillingLinkedUnitsSource)
            {
                __runOriginal = false;
            }
        }
    }
}
