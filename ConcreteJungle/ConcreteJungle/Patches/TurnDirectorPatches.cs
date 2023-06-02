using ConcreteJungle.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcreteJungle.Patches
{

    [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
    static class TurnDirector_IncrementActiveTurnActor
    {
        static void Prefix(ref bool __runOriginal, TurnDirector __instance)
        {
            if (!__runOriginal) return;

            if (
                __instance.CurrentRound >= Mod.Config.Ambush.EnableOnRound &&
                ModState.PotentialAmbushOrigins.Count != 0)
            {
                Mod.Log.Info?.Write("Incremented active turn actor, checking buildings.");
                // Re-Filter the candidates to try to catch buildings that were marked contract objectives
                CandidateBuildingsHelper.FilterOnTurnActorIncrement(__instance.Combat);

                // Determine potential ambush sites based upon the origins
                Dictionary<Vector3, List<BattleTech.Building>> ambushSites = new Dictionary<Vector3, List<BattleTech.Building>>();
                foreach (Vector3 origin in ModState.PotentialAmbushOrigins)
                {
                    // For the given origin, find how many potential buildings there are.
                    List<BattleTech.Building> originCandidates = CandidateBuildingsHelper.ClosestCandidatesToPosition(origin, Mod.Config.Ambush.SearchRadius);
                    Mod.Log.Debug?.Write($" Found {originCandidates.Count} candidate buildings for originPos: {origin}");
                    ambushSites.Add(origin, originCandidates);
                }
                ModState.PotentialAmbushOrigins.Clear(); // reset potential origins for next round

                // Determine if the unit was ambushed this turn
                bool wasAmbushed = false;
                if (ModState.Ambushes < Mod.Config.Ambush.MaxPerMap && ambushSites.Count > 0)
                {
                    float roll = Mod.Random.Next(1, 100);
                    int threshold = (int)Math.Ceiling(ModState.CurrentAmbushChance * 100f);
                    if (roll <= threshold)
                    {
                        Mod.Log.Info?.Write($" Roll: {roll} is under current threshold: {threshold}, enabling possible ambush");
                        wasAmbushed = true;
                    }
                    else
                    {
                        ModState.CurrentAmbushChance += Mod.Config.Ambush.ChancePerActor;
                        Mod.Log.Info?.Write($" Roll: {roll} was over threshold: {threshold}, increasing ambush chance to: {ModState.CurrentAmbushChance} for next position.");
                    }
                }

                if (wasAmbushed)
                {
                    // Sort the ambushSites by number of buildings to maximize ambush success
                    Mod.Log.Debug?.Write("Sorting sites by potential buildings.");
                    List<KeyValuePair<Vector3, List<BattleTech.Building>>> sortedSites = ambushSites.ToList();
                    sortedSites.Sort((x, y) => x.Value.Count.CompareTo(y.Value.Count));

                    Vector3 ambushOrigin = sortedSites[0].Key;
                    Mod.Log.Debug?.Write($"Spawning an ambush at position: {ambushOrigin}");

                    // Randomly determine an ambush type by weight
                    List<AmbushType> shuffledTypes = new List<AmbushType>();
                    shuffledTypes.AddRange(Mod.Config.Ambush.AmbushTypes);
                    shuffledTypes.Shuffle<AmbushType>();
                    AmbushType ambushType = shuffledTypes[0];
                    Mod.Log.Info?.Write($"Ambush type of: {ambushType} will be applied at position: {ambushOrigin}");

                    switch (ambushType)
                    {
                        case AmbushType.Explosion:
                            ExplosionAmbushHelper.SpawnAmbush(ambushOrigin);
                            break;
                        case AmbushType.Infantry:
                            InfantryAmbushHelper.SpawnAmbush(ambushOrigin);
                            break;
                        case AmbushType.BattleArmor:
                            SpawnAmbushHelper.SpawnAmbush(ambushOrigin, AmbushType.BattleArmor);
                            break;
                        case AmbushType.Mech:
                            SpawnAmbushHelper.SpawnAmbush(ambushOrigin, AmbushType.Mech);
                            break;
                        case AmbushType.Vehicle:
                            SpawnAmbushHelper.SpawnAmbush(ambushOrigin, AmbushType.Vehicle);
                            break;
                        default:
                            Mod.Log.Error?.Write($"UNKNOWN AMBUSH TYPE: {ambushType} - CANNOT PROCEED!");
                            break;

                    }

                    // Record a successful ambush and reset the weighting
                    ModState.AmbushOrigins.Add(ambushOrigin);
                    ModState.Ambushes++;
                    ModState.CurrentAmbushChance = Mod.Config.Ambush.BaseChance;
                }

            }
        }
    }


    // We need to defer until after the buildings are initialized in pass 2, because the UrbanDestructible elements aren't
    //   added until then. 
    [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
    public static class TurnDirector_OnInitializeContractComplete
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {
            Mod.Log.Trace?.Write("TD:OICC - entered.");

            ModState.ProcessAmbushes = ModState.Combat.ActiveContract.ContractBiome == Biome.BIOMESKIN.urbanHighTech;
            if (!ModState.ProcessAmbushes)
            {
                Mod.Log.Info?.Write($"Contract has non-urban biome ({ModState.Combat.ActiveContract.ContractBiome}). Skipping processing.");
                ModState.ProcessAmbushes = false;
                return;
            }
            Mod.Log.Info?.Write($"Contract has Urban High Tech biome, enabling mod features.");

            // Check contract exclusions
            foreach (string overrrideId in Mod.Config.Ambush.BlacklistedContracts)
            {
                if (overrrideId.Equals(ModState.Combat.ActiveContract.Override.ID, StringComparison.InvariantCultureIgnoreCase))
                {
                    Mod.Log.Info?.Write($"Contract with override ID '{overrrideId}' is excluded, skipping processing.");
                    ModState.ProcessAmbushes = false;
                    return;
                }
            }
            Mod.Log.Info?.Write($"Contract with override ID: {ModState.Combat.ActiveContract.Override.ID} is not blacklisted, enabling ambushes.");

            ModState.ContractDifficulty = ModState.Combat.ActiveContract.Override.finalDifficulty;
            Mod.Log.Info?.Write($"Using contractOverride finalDifficulty of: {ModState.ContractDifficulty}");

            foreach (Team team in ModState.Combat.Teams)
            {
                if (team.GUID == TeamDefinition.TargetsTeamDefinitionGuid) ModState.TargetTeam = team;
                else if (team.GUID == TeamDefinition.TargetsAllyTeamDefinitionGuid) ModState.TargetAllyTeam = team;
                else if (team.GUID == TeamDefinition.HostileToAllTeamDefinitionGuid) ModState.HostileToAllTeam = team;
            }
            Mod.Log.Info?.Write($"" +
                $"TargetTeam identified as: {ModState.TargetTeam?.DisplayName}  " +
                $"TargetAllyTeam identified as: {ModState.TargetAllyTeam?.DisplayName}  " +
                $"HostileToAllTeam identified as: {ModState.HostileToAllTeam?.DisplayName}.");

            // TODO: Make this much more powerful and varied.
            ModState.AmbushTeam = ModState.TargetTeam;
            Mod.Log.Info?.Write($"Using team: {ModState.AmbushTeam.DisplayName} as Ambush team.");

            // Check faction exclusions
            foreach (FactionValue faction in Mod.Config.Ambush.BlacklistedFactions)
            {
                if (ModState.AmbushTeam.factionValue.Equals(faction))
                {
                    Mod.Log.Info?.Write($"Ambushing team has blacklisted factionId: {faction.FactionDefID}, skipping ambush processing.");
                    ModState.ProcessAmbushes = false;
                    return;
                }
            }

            // Filter the AmbushDefs by contract difficulty. If we don't have ambushes for our contract difficulty,
            //   there's a configuration error - abort! This has to come before data loading as it sets
            //   the AmbushDefs for the current contract
            bool haveAmbushes = FilterAmbushes();
            if (!haveAmbushes)
            {
                ModState.ProcessAmbushes = false;
                Mod.Log.Warn?.Write("Incorrect filter configuration - disabling ambushes!");
                return;
            }

            // Load any resources necessary for our ambush
            try
            {
                DataLoadHelper.LoadAmbushResources(ModState.Combat);
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Failed to load ambush resources due to exception!");
                ModState.ProcessAmbushes = false;
            }

            // Find candidate buildings
            CandidateBuildingsHelper.DoInitialFilter(__instance.Combat);
            Mod.Log.Info?.Write($"Contract initially has: {ModState.CandidateBuildings.Count} candidate buildings");

            // Devestate buildings
            DevestationHelper.DevestateBuildings();

            Mod.Log.Info?.Write($"After devestation, map has {ModState.CandidateBuildings.Count} candidate buildings.");
        }

        static private bool FilterAmbushes()
        {
            bool success = true;
            Func<AmbushDef, bool> filterByDifficulty = x => x.MinDifficulty <= ModState.ContractDifficulty && x.MaxDifficulty >= ModState.ContractDifficulty;


            Mod.Log.Info?.Write($"Filtering ambushes for contract difficulty: {ModState.ContractDifficulty}");
            List<ExplosionAmbushDef> filteredExpAmbushes = Mod.Config.ExplosionAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredExpAmbushes.Count != 1)
            {
                Mod.Log.Error?.Write("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error?.Write("  Error in ExplosionAmbush.Ambushes.SpawnPool!");
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
                Mod.Log.Error?.Write("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error?.Write("  Error in InfantryAmbushes.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.InfantryAmbushDefForContract = filteredInfantryAmbushes[0];
            }

            List<MechAmbushDef> filteredBattleArmorAmbushes = Mod.Config.BattleArmorAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredBattleArmorAmbushes.Count != 1)
            {
                Mod.Log.Error?.Write("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error?.Write("  Error in BattleArmorAmbush.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.BattleArmorAmbushDefForContract = filteredBattleArmorAmbushes[0];
            }

            List<MechAmbushDef> filteredMechAmbushes = Mod.Config.MechAmbush.Ambushes
                .Where(x => filterByDifficulty(x))
                .ToList();
            if (filteredMechAmbushes.Count != 1)
            {
                Mod.Log.Error?.Write("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error?.Write("  Error in MechAmbush.Ambushes.SpawnPool!");
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
                Mod.Log.Error?.Write("Mod is misconfigured! Ambush defs cannot have overlapping or missing difficulty ranges!  Disabling mod.");
                Mod.Log.Error?.Write("  Error in VehicleAmbush.Ambushes.SpawnPool!");
                success = false;
            }
            else
            {
                ModState.VehicleAmbushDefForContract = filteredVehicleAmbushes[0];
            }

            return success;
        }

    }
}
