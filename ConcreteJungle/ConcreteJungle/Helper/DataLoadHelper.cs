﻿using BattleTech.Data;
using System.Collections.Generic;

namespace ConcreteJungle.Helper
{
    public static class DataLoadHelper
    {
        public static void LoadAmbushResources(CombatGameState combat)
        {
            // Load the necessary turret defs
            Mod.Log.Info?.Write($"== BEGIN load request for all possible ambush spawns");
            LoadRequest asyncSpawnReq = combat.DataManager.CreateLoadRequest(
                delegate (LoadRequest loadRequest) { OnLoadComplete(combat); }, false
                );
            Mod.Log.Info?.Write($" -- Pre-load counts => weaponDefs: {combat.DataManager.WeaponDefs.Count}  " +
                $"pilotDefs: {combat.DataManager.PilotDefs.Count}  mechDefs: {combat.DataManager.MechDefs.Count}" +
                $"turretDefs: {combat.DataManager.TurretDefs.Count}  vehicleDefs: {combat.DataManager.VehicleDefs.Count}");

            // Filter requests so we don't load multiple times
            HashSet<string> turretsToLoad = new HashSet<string>();
            HashSet<string> pilotsToLoad = new HashSet<string>();
            foreach (TurretAndPilotDef ambushDef in ModState.InfantryAmbushDefForContract.SpawnPool)
            {
                turretsToLoad.Add(ambushDef.TurretDefId);
                pilotsToLoad.Add(ambushDef.PilotDefId);
            }

            HashSet<string> battleArmorToLoad = new HashSet<string>();
            foreach (MechAndPilotDef ambushDef in ModState.BattleArmorAmbushDefForContract.SpawnPool)
            {
                battleArmorToLoad.Add(ambushDef.MechDefId);
                pilotsToLoad.Add(ambushDef.PilotDefId);
            }

            HashSet<string> mechToLoad = new HashSet<string>();
            foreach (MechAndPilotDef ambushDef in ModState.MechAmbushDefForContract.SpawnPool)
            {
                mechToLoad.Add(ambushDef.MechDefId);
                pilotsToLoad.Add(ambushDef.PilotDefId);
            }

            HashSet<string> vehiclesToLoad = new HashSet<string>();
            foreach (VehicleAndPilotDef ambushDef in ModState.VehicleAmbushDefForContract.SpawnPool)
            {
                vehiclesToLoad.Add(ambushDef.VehicleDefId);
                pilotsToLoad.Add(ambushDef.PilotDefId);
            }

            // Add the filtered requests to the async load
            foreach (string defId in turretsToLoad)
            {
                Mod.Log.Info?.Write($"  - TurretDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, defId, new bool?(false));
            }
            foreach (string defId in pilotsToLoad)
            {
                Mod.Log.Info?.Write($"  - PilotDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, defId, new bool?(false));
            }
            foreach (string defId in battleArmorToLoad)
            {
                Mod.Log.Info?.Write($"  - MechDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.MechDef, defId, new bool?(false));
            }
            foreach (string defId in mechToLoad)
            {
                Mod.Log.Info?.Write($"  - MechDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.MechDef, defId, new bool?(false));
            }
            foreach (string defId in vehiclesToLoad)
            {
                Mod.Log.Info?.Write($"  - VehicleDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, defId, new bool?(false));
            }

            // Fire the load request
            asyncSpawnReq.ProcessRequests(1000U);
        }

        private static void OnLoadComplete(CombatGameState combat)
        {
            Mod.Log.Info?.Write($"== END load request for all possible ambush spawns");
            Mod.Log.Info?.Write($" -- Post-load counts => weaponDefs: {combat.DataManager.WeaponDefs.Count}  " +
                $"pilotDefs: {combat.DataManager.PilotDefs.Count}  mechDefs: {combat.DataManager.MechDefs.Count}" +
                $"turretDefs: {combat.DataManager.TurretDefs.Count}  vehicleDefs: {combat.DataManager.VehicleDefs.Count}");
        }

        public static void UnloadAmbushResources(CombatGameState combat)
        {
            // TODO: Looks like data manager has no unload function, just a 'clear' function?
            // Possibly just set defs=true for clear... but would that screw up say salvage?
        }

        public class DataloadResourcePair
        {
            public BattleTechResourceType type;
            public string id;
        }

    }
}
