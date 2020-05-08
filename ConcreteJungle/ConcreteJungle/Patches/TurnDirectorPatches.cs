using BattleTech;
using BattleTech.Data;
using Harmony;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    // We need to defer until after the buildings are initialized in pass 2, because the UrbanDestrible elements aren't
    //   added until then. 
    [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
    public static class TurnDirector_OnInitializeContractComplete
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {
            Mod.Log.Trace("TD:OICC - entered.");

            InitializeContractCompleteMessage initializeContractMessage = message as InitializeContractCompleteMessage;
            CombatGameState combat = __instance.Combat;
            Contract activeContract = combat.ActiveContract;

            // Find candidate buildings
            Mod.Log.Debug("Filtering candidate buidlings:");
            ModState.CandidateBuildings.Clear();
            foreach (ICombatant combatant in combat.GetAllCombatants())
            {
                if (combatant is BattleTech.Building building)
                {
                    Mod.Log.Trace($" Found building {CombatantUtils.Label(building)}");
                    Mod.Log.Trace($"  -- isTabTarget: {building.IsTabTarget}");

                    if (building.BuildingDef != null)
                    {
                        Mod.Log.Trace($"   -- BuildingDef:");
                        Mod.Log.Trace($"     description: '{building.BuildingDef.Description}' ");
                        Mod.Log.Trace($"     isDestructible: {building.BuildingDef.Destructible} " +
                            $"structurePoints: {building.BuildingDef.StructurePoints} ");
                    }
                    if (building.UrbanDestructible != null)
                    {
                        Mod.Log.Trace($"   -- UrbanDestructible: " +
                            $"name: {building.UrbanDestructible.name} " +
                            $"canBeDesolation: {building.UrbanDestructible.CanBeDesolation} " +
                            $"currentDesolationState: {building.UrbanDestructible.CurDesolationState}"
                            );
                    }

                    if (building.BuildingDef != null && building.BuildingDef.Destructible &&
                        building.UrbanDestructible != null && building.UrbanDestructible.CanBeDesolation &&
                        !building.IsTabTarget)
                    {
                        Mod.Log.Trace($"  -- Building {CombatantUtils.Label(building)} meets criteria, adding as trap candidate.");
                        ModState.CandidateBuildings.Add(building);
                    }

                }
            }
            Mod.Log.Debug($"Map has: {ModState.CandidateBuildings.Count} buildings suitable for traps");

            // Load the necessary turret defs
            Mod.Log.Debug($"DM TurretDefs are: {combat.DataManager.TurretDefs.Count}");
            LoadRequest asyncSpawnReq = combat.DataManager.CreateLoadRequest(delegate (LoadRequest loadRequest)
            {
                OnLoadComplete(combat);
            }, false);
            asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, Mod.Config.TurretDef, new bool?(false));
            asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, Mod.Config.TurretPilotDef, new bool?(false));
            asyncSpawnReq.ProcessRequests(1000U);
        }

        private static void OnLoadComplete(CombatGameState cgs)
        {
            Mod.Log.Debug($"TurretDef load complete!");
            Mod.Log.Debug($"DM TurretDefs are: {cgs.DataManager.TurretDefs.Count}");
        }
    }
}
