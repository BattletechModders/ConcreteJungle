using BattleTech;
using Harmony;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    public static class CombatGameState__Init
    {
        public static void Postfix(CombatGameState __instance)
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
                        Lance newLance = new Lance(team);
                        team.lances.Add(newLance);
                    }
                }
            }

            ModState.Combat = __instance;
        }
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed
    {

        public static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:OCGD - entered.");

            ModState.Reset();
        }
    }



}
