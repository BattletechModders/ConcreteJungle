using BattleTech;
using ConcreteJungle.Helper;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    static class CombatGameState__Init
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:_I - entered.");

            // Re-initialize everything to give us a clean slate.
            ModState.Reset();

            ModState.IsUrbanBiome = __instance.ActiveContract.ContractBiome == Biome.BIOMESKIN.urbanHighTech;
            if (!ModState.IsUrbanBiome)
            {
                Mod.Log.Info("Contract has non-urban biome. Skipping processing.");
                return;
            }
            Mod.Log.Info($"Contract has Urban High Tech biome, enabling mod features.");

            foreach (Team team in __instance.Teams)
            {
                if (team.GUID == TeamDefinition.TargetsAllyTeamDefinitionGuid) ModState.TargetAllyTeam = team;
                else if (team.GUID == TeamDefinition.HostileToAllTeamDefinitionGuid) ModState.HostileToAllTeam = team;
            }
            Mod.Log.Info($"TargetAllyTeam identified as: {ModState.TargetAllyTeam}  " +
                $"HostileToAllTeam identified as: {ModState.HostileToAllTeam}");

            ModState.Combat = __instance;
            ModState.ContractDifficulty = __instance.ActiveContract.Override.finalDifficulty;
            Mod.Log.Info($"Using contractOverride finalDifficulty of: {ModState.ContractDifficulty}");

            // Filter the AmbushDefs by contract difficulty. If we don't have ambushes for our contract difficulty,
            //   there's a configuration error - abort!
            bool haveAmbushes = FilterAmbushes();
            if (!haveAmbushes)
            {
                ModState.IsUrbanBiome = false; 
                return;
            }

            // Load any resources necessary for our ambush
            try
            {
                DataLoadHelper.LoadAmbushResources(__instance);
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to load ambush resources due to exception!", e);
                ModState.IsUrbanBiome = false;
            }

        }

        static private bool FilterAmbushes()
        {
            bool success = true;
            Func<AmbushDef, bool> filterByDifficulty = x => x.MinDifficulty <= ModState.ContractDifficulty && x.MaxDifficulty >= ModState.ContractDifficulty;

            List<ExplosionAmbushDef> filteredExpAmbushes = Mod.Config.ExplosionAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredExpAmbushes.Count != 1)
            {
                Mod.Log.Error("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error("  Error in ExplosionAmbush.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.ExplosionAmbushDefForContract = filteredExpAmbushes[0];
            }

            List<InfantryAmbushDef> filteredInfantryAmbushes = Mod.Config.InfantryAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredInfantryAmbushes.Count != 1)
            {
                Mod.Log.Error("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error("  Error in InfantryAmbushes.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.InfantryAmbushDefForContract = filteredInfantryAmbushes[0];
            }

            List<MechAmbushDef> filteredMechAmbushes = Mod.Config.MechAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredMechAmbushes.Count != 1)
            {
                Mod.Log.Error("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error("  Error in MechAmbush.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.MechAmbushDefForContract = filteredMechAmbushes[0];
            }

            List<VehicleAmbushDef> filteredVehicleAmbushes = Mod.Config.VehicleAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredVehicleAmbushes.Count != 1)
            {
                Mod.Log.Error("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error("  Error in VehicleAmbush.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.VehicleAmbushDefForContract = filteredVehicleAmbushes[0];
            }

            return success;
        }
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    static class CombatGameState_OnCombatGameDestroyed
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:OCGD - entered.");
            
            try
            {
                DataLoadHelper.UnloadAmbushResources(__instance);
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to unload ambush resources due to exception!", e);
                ModState.IsUrbanBiome = false;
            }

            ModState.Reset();
        }
    }

    // Remove any trap turrets as possible tab targets
    //static class CombatGameState_GetAllTabTargets
    //{
    //    static void Postfix(CombatGameState __instance, List<ICombatant> __result)
    //    {
    //        if (__result != null && __result.Count > 0)
    //        {
    //            __result.RemoveAll(x => ModState.TrapTurretToBuildingIds.ContainsKey(x.GUID));
    //        }
    //    }
    //}


}
