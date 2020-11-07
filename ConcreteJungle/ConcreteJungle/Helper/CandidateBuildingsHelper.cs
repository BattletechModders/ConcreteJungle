using BattleTech;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class CandidateBuildingsHelper
    {
        public static List<BattleTech.Building> ClosestCandidatesToPosition(Vector3 originPos, float searchRadius)
        {
            List<BattleTech.Building> candidates = new List<BattleTech.Building>();
            foreach (BattleTech.Building building in ModState.CandidateBuildings)
            {
                if (!building.IsDead && (building.CurrentPosition - originPos).magnitude < searchRadius)
                {
                    Mod.Log.Debug?.Write($" -- Candidate building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is within search range.");
                    candidates.Add(building);
                }
                else
                {
                    Mod.Log.Trace?.Write($" -- Candidate building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is beyond search range");
                }
            }

            // Remove any candidates that already have a trap in them
            candidates.RemoveAll(x => ModState.AmbushBuildingGUIDToTurrets.ContainsKey(x.GUID));

            // Sort candidates by distance from the origin
            candidates.Sort((b1, b2) =>
                (b1.CurrentPosition - originPos).magnitude.CompareTo((b2.CurrentPosition - originPos).magnitude)
            );

            return candidates;
        }

        // Invoke when the player has finished moving all of their units during non-interleaved mode
        public static void FilterOnTurnActorIncrement(CombatGameState combat)
        {
            List<string> guidsToRemove = new List<string>();
            foreach (BattleTech.Building building in ModState.CandidateBuildings)
            {
                // Remove any that have become objective targets
                if (building.objectiveGUIDS.Contains(combat.GUID))
                {
                    Mod.Log.Debug?.Write($"   -- Building is an objective, needs to be removed.");
                    guidsToRemove.Add(building.GUID);
                }

                // Remove any that are dead
                if (building.IsDead || building.IsFlaggedForDeath)
                {
                    Mod.Log.Debug?.Write($"   -- Building is an dead or dying, must be removed.");
                    guidsToRemove.Add(building.GUID);
                }

                // Sanity check infantry spawn
                if (ModState.AmbushBuildingGUIDToTurrets.ContainsKey(building.GUID))
                {
                    Mod.Log.Debug?.Write($"   -- didn't clean up after myself in infantry ambush, removing trap shell.");
                    guidsToRemove.Add(building.GUID);
                }
            }

            ModState.CandidateBuildings.RemoveAll(x => guidsToRemove.Contains(x.GUID));
            Mod.Log.Debug?.Write($"Cleanup - removed {guidsToRemove.Count} buildings.");

        }

        // Invoke when the contract is initialized, but after all Destructible assets have been created
        public static void DoInitialFilter(CombatGameState combat)
        {
            Mod.Log.Debug?.Write("Filtering candidate buildings:");
            ModState.CandidateBuildings.Clear();
            foreach (ICombatant combatant in combat.GetAllCombatants())
            {
                if (combatant is BattleTech.Building building)
                {
                    Mod.Log.Debug?.Write($" Found building {CombatantUtils.Label(building)}");
                    Mod.Log.Trace?.Write($"  -- isTabTarget: {building.IsTabTarget}");

                    if (building.BuildingDef != null)
                    {
                        Mod.Log.Trace?.Write($"   -- BuildingDef:");
                        Mod.Log.Trace?.Write($"     description: '{building.BuildingDef.Description}' ");
                        Mod.Log.Trace?.Write($"     isDestructible: {building.BuildingDef.Destructible} " +
                            $"structurePoints: {building.BuildingDef.StructurePoints} ");
                    }
                    else { continue; }

                    if (building.UrbanDestructible != null)
                    {
                        Mod.Log.Trace?.Write($"   -- UrbanDestructible: " +
                            $"name: {building.UrbanDestructible.name} " +
                            $"canBeDesolation: {building.UrbanDestructible.CanBeDesolation} " +
                            $"currentDesolationState: {building.UrbanDestructible.CurDesolationState}"
                            );
                    }
                    else { continue; }

                    if (building.objectiveGUIDS.Contains(combat.GUID))
                    {
                        Mod.Log.Info?.Write($"   -- Building is an objective, skipping.");
                        continue;
                    }

                    if (building.BuildingDef != null && building.BuildingDef.Destructible &&
                        building.UrbanDestructible != null && building.UrbanDestructible.CanBeDesolation &&
                        !building.IsTabTarget)
                    {
                        Mod.Log.Info?.Write($"  -- Building {CombatantUtils.Label(building)} meets criteria, adding as trap candidate.");
                        ModState.CandidateBuildings.Add(building);
                    }

                }
            }
        }
    }
}
