using BattleTech;
using Harmony;
using System.Collections.Generic;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    static class CombatGameState__Init
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:_I - entered.");

            foreach (Team team in __instance.Teams)
            {
                Mod.Log.Debug($" Found Team with displayName: {team.DisplayName}  name:{team.Name}  unitCount: {team.unitCount}  has_supportTeam: {team.SupportTeam != null}");
                //if (team.IsEnemy(__instance.LocalPlayerTeam) && team.unitCount > 0)
                //{
                //    ModState.CandidateTeams.Add(team);
                //    Mod.Log.Debug($"  - Adding as candidate for traps.");
                //}
                if (team.GUID == TeamDefinition.TargetsAllyTeamDefinitionGuid)
                {
                    ModState.CandidateTeams.Add(team);
                    Mod.Log.Debug($"  team has: {team?.lances?.Count} lances");
                    if (team?.lances?.Count == 0)
                    {
                        Mod.Log.Debug($"  -- adding lance to team.");
                        Lance newLance = new Lance(team);
                        team.lances.Add(newLance);
                    }
                }
            }

            ModState.Combat = __instance;
        }
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    static class CombatGameState_OnCombatGameDestroyed
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:OCGD - entered.");

            ModState.Reset();
        }
    }

    // Remove any trap turrets as possible tab targets
    static class CombatGameState_GetAllTabTargets
    {
        static void Postfix(CombatGameState __instance, List<ICombatant> __result)
        {
            if (__result != null && __result.Count > 0)
            {
                __result.RemoveAll(x => ModState.TrapTurretToBuildingIds.ContainsKey(x.GUID));
            }
        }
    }


}
