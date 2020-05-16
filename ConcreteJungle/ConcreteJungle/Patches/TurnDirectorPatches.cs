using BattleTech;
using ConcreteJungle.Helper;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcreteJungle.Patches
{

    [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
    static class TurnDirector_IncrementActiveTurnActor
    {
        static void Prefix(TurnDirector __instance)
        {
            if (!__instance.IsInterleaved && ! __instance.IsInterleavePending &&
                __instance.ActiveTurnActor is Team activeTeam && activeTeam.IsLocalPlayer &&
                ModState.PotentialAmbushOrigins.Count != 0)
            {
                // Re-Filter the candidates to try to catch buildings that were marked contract objectives
                CandidateBuildingsHelper.FilterOnTurnActorIncrement(__instance.Combat);

                bool wasAmbushed = false;
                Dictionary<Vector3, List<BattleTech.Building>> ambushSites = new Dictionary<Vector3, List<BattleTech.Building>>();
                foreach (Vector3 origin in ModState.PotentialAmbushOrigins)
                {
                    float roll = Mod.Random.Next(1, 100);
                    int threshold = (int)Math.Ceiling(ModState.CurrentAmbushChance * 100f);
                    if (roll <= threshold)
                    {                        
                        Mod.Log.Info($" Roll: {roll} is under current threshold: {threshold}, enabling possible ambush");
                        wasAmbushed = true;
                    }
                    else
                    {
                        ModState.CurrentAmbushChance += Mod.Config.Ambush.ChancePerActor;
                        Mod.Log.Info($" Roll: {roll} was over threshold: {threshold}, increasing ambush chance to: {ModState.CurrentAmbushChance} for next position.");
                    }

                    // For the given origin, find how many potential buildings there are.
                    List<BattleTech.Building> originCandidates = CandidateBuildingsHelper.FilterCandidates(origin, Mod.Config.Ambush.SearchRadius);
                    Mod.Log.Debug($" Found {originCandidates.Count} candidate buildings for originPos: {origin}");
                    ambushSites.Add(origin, originCandidates);

                }
                ModState.PotentialAmbushOrigins.Clear(); // reset potential origins for next round

                if (wasAmbushed)
                {
                    // Sort the ambushSites by number of buildings to maximize ambush success
                    Mod.Log.Debug("Sorting sites by potential buildings.");
                    List<KeyValuePair<Vector3, List<BattleTech.Building>>> sortedSites = ambushSites.ToList();
                    sortedSites.Sort((x, y) => x.Value.Count.CompareTo(y.Value.Count));

                    Vector3 ambushOrigin = sortedSites[0].Key;
                    Mod.Log.Debug($"Spawning an ambush at position: {ambushOrigin}");

                    // Randomly determine an ambush type by weight
                }



                Mod.Log.Debug($"Resolving pending ambush at position: {ModState.PendingAmbushOrigin}");

                // Determine trap type - infantry ambush, tank ambush, IED
                // TODO: Randomize
                //TrapType trapType = TrapType.TRAP_INFANTRY_AMBUSH;

                //InfantryAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);
                //ExplosionAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);
                SpawnAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);

                // Record a successful ambush and reset the weighting
                ModState.AmbushOrigins.Add(ModState.PendingAmbushOrigin);
                ModState.CurrentAmbushChance = Mod.Config.Ambush.BaseChance;
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
            Mod.Log.Trace("TD:OICC - entered.");

            if (!ModState.IsUrbanBiome) return;

            // Find candidate buildings
            CandidateBuildingsHelper.DoInitialFilter(__instance.Combat);
            Mod.Log.Info($"Contract initially has: {ModState.CandidateBuildings.Count} candidate buildings");

            // Devestate buildings
            DevestationHelper.DevestateBuildings();

            Mod.Log.Info($"After devestation, map has {ModState.CandidateBuildings.Count} candidate buildings.");
        }

    }
}
