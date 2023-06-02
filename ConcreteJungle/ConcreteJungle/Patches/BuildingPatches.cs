using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    // Ensure that buildings with trap turrets in them can be tab-targeted
    [HarmonyPatch(typeof(BattleTech.Building), "IsTabTarget", MethodType.Getter)]
    static class Building_IsTabTaget_Getter
    {
        static void Postfix(BattleTech.Building __instance, ref bool __result)
        {
            if (ModState.ProcessAmbushes && ModState.AmbushBuildingGUIDToTurrets.ContainsKey(__instance.GUID))
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
            if (ModState.ProcessAmbushes && ModState.AmbushBuildingGUIDToTurrets.ContainsKey(__instance.GUID))
            {
                __result = true;
            }
        }
    }

    // Kill the associated turrets on death.
    [HarmonyPatch(typeof(BattleTech.Building), "KillLinkedUnits")]
    static class Building_KillLinkedUnits
    {
        static void Prefix(ref bool __runOriginal, BattleTech.Building __instance)
        {
            if (!__runOriginal) return;

            // Skip if we aren't enabled or we're not placed on a building
            if (__instance == null || !ModState.ProcessAmbushes) return;

            Mod.Log.Debug?.Write("Current ambush building shells");
            foreach (string guid in ModState.AmbushBuildingGUIDToTurrets.Keys)
            {
                Mod.Log.Debug?.Write($" -- Building GUID: {guid}");
            }

            // If we contain a linked turret, destroy it before we die.
            if (ModState.AmbushBuildingGUIDToTurrets.ContainsKey(__instance.GUID))
            {
                ModState.KillingLinkedUnitsSource = __instance.GUID;

                // Despawn the associated turret
                Turret linkedTurret = ModState.AmbushBuildingGUIDToTurrets[__instance.GUID];
                Mod.Log.Info?.Write($"Building {CombatantUtils.Label(__instance)} is destroyed, destroying associated turret: {CombatantUtils.Label(linkedTurret)}");
                DespawnActorMessage despawnMessage = new DespawnActorMessage(__instance.GUID, linkedTurret.GUID, DeathMethod.VitalComponentDestroyed);
                __instance.Combat.MessageCenter.PublishMessage(despawnMessage);

                ModState.KillingLinkedUnitsSource = null;
                ModState.AmbushBuildingGUIDToTurrets.Remove(__instance.GUID);
                ModState.AmbushTurretGUIDtoBuilding.Remove(linkedTurret.GUID);
            }

            // If the building is in candidates, remove it.
            if (ModState.CandidateBuildings.Contains(__instance))
            {
                Mod.Log.Debug?.Write($"Removing building as a candidate for ambushes: {__instance.GUID}");
                ModState.CandidateBuildings.Remove(__instance);
            }
        }
    }

}
