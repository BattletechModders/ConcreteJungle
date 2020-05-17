using BattleTech;
using Harmony;

namespace ConcreteJungle.Patches
{
    // Ensure that buildings with trap turrets in them can be tab-targeted
    [HarmonyPatch(typeof(BattleTech.Building), "IsTabTarget", MethodType.Getter)]
    static class Building_IsTabTaget_Getter
    {
        static void Postfix(BattleTech.Building __instance, ref bool __result)
        {
            if (ModState.IsUrbanBiome && ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(BattleTech.Building), "ShouldShowFlags", MethodType.Getter)]
    static class Building_ShouldShowFlags_Getter
    {
        static void Postfix(BattleTech.Building __instance, ref bool __result)
        {
            if (ModState.IsUrbanBiome && ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
            {
                __result = true;
            }
        }
    }

    // Kill the associated turrets on death.
    [HarmonyPatch(typeof(BattleTech.Building), "HandleDeath")]
    static class Building_HandleDeath
    {
        static void Prefix(BattleTech.Building __instance)
        {
            if (ModState.IsUrbanBiome && ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
            {
                // Despawn the associated turret
                Turret turret = ModState.TrapBuildingsToTurrets[__instance.GUID];
                DespawnActorMessage despawnMessage = new DespawnActorMessage(__instance.GUID, turret.GUID, DeathMethod.VitalComponentDestroyed);
                __instance.Combat.MessageCenter.PublishMessage(despawnMessage);
            }

            // Remove any destroyed building from future candidates
            if ((__instance.IsFlaggedForDeath || __instance.IsDead) && ModState.CandidateBuildings.Contains(__instance))
            {
                ModState.CandidateBuildings.Remove(__instance);
            }
        }
    }
}
