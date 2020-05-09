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
            if (ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
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
            if (ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(BattleTech.Building), "HandleDeath")]
    static class Building_HandleDeath
    {
        static void Prefix(BattleTech.Building __instance)
        {
            if (ModState.TrapBuildingsToTurrets.ContainsKey(__instance.GUID))
            {
                // Despawn the associated turret
                Turret turret = ModState.TrapBuildingsToTurrets[__instance.GUID];
                turret.FlagForDeath("Shell Building Destroyed", DeathMethod.VitalComponentDestroyed, DamageType.Enemy, -1, -1, "", false);
                turret.HandleDeath(__instance.GUID);
            }
        }
    }
}
