using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class CandidateHelper
    {
        public static List<BattleTech.Building> FilterCandidates(Vector3 originPos, float searchRadius)
        {
            List<BattleTech.Building> candidates = new List<BattleTech.Building>();
            foreach (BattleTech.Building building in ModState.CandidateBuildings)
            {
                if (!building.IsDead && (building.CurrentPosition - originPos).magnitude < searchRadius)
                {
                    Mod.Log.Debug($" -- Candidate building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is within search range.");
                    candidates.Add(building);
                }
                else
                {
                    Mod.Log.Trace($" -- Candidate building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is beyond search range");
                }
            }

            // Remove any candidates that already have a trap in them
            candidates.RemoveAll(x => ModState.TrapBuildingsToTurrets.ContainsKey(x.GUID));

            // Sort candidates by distance from the origin
            candidates.Sort((b1, b2) =>
                (b1.CurrentPosition - originPos).magnitude.CompareTo((b2.CurrentPosition - originPos).magnitude)
            );

            return candidates;
        }
    }
}
