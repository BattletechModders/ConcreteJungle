using BattleTech;
using BattleTech.Data;
using Harmony;
using System.Linq;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    //[HarmonyPatch(typeof(CombatGameState), "_Init")]
    //public static class CombatGameState__Init
    //{
    //    public static void Postfix(CombatGameState __instance)
    //    {
    //        Mod.Log.Trace("CGS:_I - entered.");

    //        // Look for target buildings?
    //        ModState.TargetableBuildings = __instance.GetAllCombatants()
    //            .Where(x => x is BattleTech.Building)
    //            .Select(x => x as BattleTech.Building)
    //            .Where(x => x.BuildingDef != null 
    //                && x.BuildingDef.Destructible
    //                // && !x.IsTabTarget // ! isTabTarget should avoid buildings that are mission objectives
    //                && x.UrbanDestructible != null  
    //                //&& x.UrbanDestructible.CanBeDesolation // x.UrbanDestructible.CanBeDesolation ensures the building can become rubble
    //                )
    //            .ToList();
    //        Mod.Log.Debug($"Map has: {ModState.TargetableBuildings.Count} buildings");

    //        // Load the necessary turret defs
    //        Mod.Log.Debug($"DM TurretDefs are: {__instance.DataManager.TurretDefs.Count}");
    //        LoadRequest asyncSpawnReq = __instance.DataManager.CreateLoadRequest(delegate (LoadRequest loadRequest)
    //        {
    //            OnLoadComplete(__instance);
    //        }, false);
    //        asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, "turretdef_Light_Shredder", new bool?(false));
    //        asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_d7_turret", new bool?(false));
    //        asyncSpawnReq.ProcessRequests(1000U);
    //    }

    //    private static void OnLoadComplete(CombatGameState cgs)
    //    {
    //        Mod.Log.Debug($"TurretDef load complete!");
    //        Mod.Log.Debug($"DM TurretDefs are: {cgs.DataManager.TurretDefs.Count}");
    //    }
    //}

    // TODO: Similar patch for FirstTimeInit, for the same reason
    // Invoke after _Init() methods, as that is where the Destructible objects are associated to buildings
    [HarmonyPatch(typeof(CombatGameState), "InitFromSave")]
    public static class CombatGameState_InitFromSave
    {
        public static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:IFS - entered.");

            //// Look for target buildings?
            //ModState.TargetableBuildings = __instance.GetAllCombatants()
            //    .Where(x => x is BattleTech.Building)
            //    .Select(x => x as BattleTech.Building)
            //    .Where(x => x.BuildingDef != null
            //        && x.BuildingDef.Destructible
            //        // && !x.IsTabTarget // ! isTabTarget should avoid buildings that are mission objectives
            //        && x.UrbanDestructible != null
            //        && x.UrbanDestructible.CanBeDesolation // x.UrbanDestructible.CanBeDesolation ensures the building can become rubble
            //        )
            //    .ToList();
            //Mod.Log.Debug($"Map has: {ModState.TargetableBuildings.Count} buildings");

            //// Load the necessary turret defs
            //Mod.Log.Debug($"DM TurretDefs are: {__instance.DataManager.TurretDefs.Count}");
            //LoadRequest asyncSpawnReq = __instance.DataManager.CreateLoadRequest(delegate (LoadRequest loadRequest)
            //{
            //    OnLoadComplete(__instance);
            //}, false);
            //asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, "turretdef_Light_Shredder", new bool?(false));
            //asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_d7_turret", new bool?(false));
            //asyncSpawnReq.ProcessRequests(1000U);
        }

        private static void OnLoadComplete(CombatGameState cgs)
        {
            Mod.Log.Debug($"TurretDef load complete!");
            Mod.Log.Debug($"DM TurretDefs are: {cgs.DataManager.TurretDefs.Count}");
        }
    }

    // TODO: Similar patch for FirstTimeInit, for the same reason
    // Invoke after _Init() methods, as that is where the Destructible objects are associated to buildings
    [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
    public static class TurnDirector_OnInitializeContractComplete
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {
            Mod.Log.Trace("ELP:IC - entered.");

            InitializeContractCompleteMessage initializeContractMessage = message as InitializeContractCompleteMessage;
            CombatGameState combat = __instance.Combat;
            Contract activeContract = combat.ActiveContract;

            foreach (ICombatant combatant in combat.GetAllCombatants())
            {
                if (combatant is BattleTech.Building building) {
                    Mod.Log.Debug($" Found building {CombatantUtils.Label(building)}");
                    Mod.Log.Debug($"  -- isTabTarget: {building.IsTabTarget}");

                    if (building.BuildingDef != null)
                    {
                        Mod.Log.Debug($"   -- BuildingDef: " +
                            $"description: '{building.BuildingDef.Description}' " +
                            $"isDestructible: {building.BuildingDef.Destructible} " +
                            $"structurePoints: {building.BuildingDef.StructurePoints} ");
                    }
                    if (building.UrbanDestructible != null)
                    {
                        Mod.Log.Debug($"   -- UrbanDestructible: " +
                            $"name: {building.UrbanDestructible.name} " +
                            $"canBeDesolation: {building.UrbanDestructible.CanBeDesolation} " +
                            $"currentDesolationState: {building.UrbanDestructible.CurDesolationState}"
                            );
                    }

                }
            }
            // Look for target buildings
            ModState.TargetableBuildings = combat.GetAllCombatants()
                .Where(x => x is BattleTech.Building)
                .Cast<BattleTech.Building>()
                //.Select(x => x as BattleTech.Building)
                .Where(b =>
                //    b.BuildingDef != null
                    //b.BuildingDef.Destructible
                //      && !b.IsTabTarget // ! isTabTarget should avoid buildings that are mission objectives
                    b.UrbanDestructible != null
                    && b.UrbanDestructible.CanBeDesolation // x.UrbanDestructible.CanBeDesolation ensures the building can become rubble
                    )

                .ToList();
            Mod.Log.Debug($"Map has: {ModState.TargetableBuildings.Count} buildings"); 

            // Load the necessary turret defs
            Mod.Log.Debug($"DM TurretDefs are: {combat.DataManager.TurretDefs.Count}");
            LoadRequest asyncSpawnReq = combat.DataManager.CreateLoadRequest(delegate (LoadRequest loadRequest)
            {
                OnLoadComplete(combat);
            }, false);
            asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, "turretdef_Light_Shredder", new bool?(false));
            asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_d7_turret", new bool?(false));
            asyncSpawnReq.ProcessRequests(1000U);
        }

        private static void OnLoadComplete(CombatGameState cgs)
        {
            Mod.Log.Debug($"TurretDef load complete!");
            Mod.Log.Debug($"DM TurretDefs are: {cgs.DataManager.TurretDefs.Count}");
        }
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed
    {
        //public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:OCGD - entered.");
      
        }
    }
}
