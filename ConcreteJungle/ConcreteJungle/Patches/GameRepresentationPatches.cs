﻿using BattleTech.Rendering;
using System.Linq;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    // Hide the turret object that's within a building
    [HarmonyPatch(typeof(PilotableActorRepresentation), "OnPlayerVisibilityChanged")]
    [HarmonyBefore("us.frostraptor.LowVisibility")]
    static class PilotableActorRepresentation_OnPlayerVisibilityChanged
    {
        static bool Prepare() { return !Mod.Config.InfantryAmbush.VisibleTrapTurrets; }

        static void Postfix(PilotableActorRepresentation __instance, VisibilityLevel newLevel)
        {
            Mod.Log.Trace?.Write("PAR:OPVC entered.");

            AbstractActor parentActor = __instance.parentActor;
            if (ModState.AmbushTurretGUIDtoBuilding.Keys.Contains(parentActor.GUID))
            {
                Turret turret = parentActor as Turret;
                if (newLevel == VisibilityLevel.LOSFull)
                {
                    __instance.VisibleObject.SetActive(false);
                }
            }

        }
    }

    // Change the building highlight color if it contains a trap
    [HarmonyPatch(typeof(GameRepresentation), "SetHighlightColor")]
    static class GameRepresentation_SetHighlightColor
    {
        static void Postfix(GameRepresentation __instance, CombatGameState combat, Team team)
        {
            if (__instance != null && __instance.parentCombatant != null && ModState.AmbushBuildingGUIDToTurrets.Keys.Contains(__instance.parentCombatant.GUID))
            {
                Mod.Log.Debug?.Write($"Building {CombatantUtils.Label(__instance.parentCombatant)} contains a trap, marking it as hostile.");

                MechEdgeSelection edgeHighlight = __instance.edgeHighlight;
                edgeHighlight.SetTeam(3);
            }
        }
    }
}
