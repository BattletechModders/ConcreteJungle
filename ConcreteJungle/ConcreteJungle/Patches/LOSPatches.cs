using BattleTech;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
 
    [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
    static class LOFCache_GetLineOfFire { 

        static void Prefix(AbstractActor source)
        {
            if (source is Turret turret && ModState.TrapTurretToBuildingIds.Keys.Contains(turret.GUID))
            {
                Mod.Log.Trace($"Turret {CombatantUtils.Label(turret)} is calculating it's LOF");
                ModState.CurrentTurretForLOF = turret;
            }
        }

        static void Postfix()
        {
            if (ModState.CurrentTurretForLOF != null)
            {
                ModState.CurrentTurretForLOF = null;
            }
        }
    }

    [HarmonyPatch(typeof(LineOfSight), "bresenhamHeightTest")]
    static class LineOfSight_bresenhamHeightTest
    {

        static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, string targetedBuildingGuid, ref Point collisionWorldPos, 
            ref bool __result, CombatGameState ___Combat)
        {
            if (ModState.CurrentTurretForLOF != null)
            {
                Mod.Log.Debug($"Recalculating LOF from {CombatantUtils.Label(ModState.CurrentTurretForLOF)} due to collision on building shell.");

                collisionWorldPos = p1;

                // If the origin and target points are the same, there is a collision
                if (p0.X == p1.X && p0.Z == p1.Z)
                {
                    __result = true;
                    return;
                }

                // If the origin or target points are outsie the bounds of the map, there is no collision (because how could there be)
                if (!___Combat.MapMetaData.IsWithinBounds(p0) || !___Combat.MapMetaData.IsWithinBounds(p1))
                {
                    __result = false;
                    return;
                }

                MapMetaData mapMetaData = ___Combat.MapMetaData;
                EncounterLayerData encounterLayerData = ___Combat.EncounterLayerData;

                bool targetIsABuilding = !string.IsNullOrEmpty(targetedBuildingGuid);
                string shellBuildingGuid = ModState.TrapTurretToBuildingIds[ModState.CurrentTurretForLOF.GUID];

                List<Point> bresenhamLinePoints = BresenhamLineUtil.BresenhamLine(p0, p1);
                float heightDeltaPerPoint = (height1 - height0) / (float)bresenhamLinePoints.Count;
                float collisionPointHeight = height0;
                // Walk the bresenham Line, evaluation collision at a speciifc height as we go.
                for (int i = 0; i < bresenhamLinePoints.Count; i++)
                {
                    collisionPointHeight += heightDeltaPerPoint;
                    Point point = bresenhamLinePoints[i];

                    if (encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(shellBuildingGuid))
                    {
                        Mod.Log.Trace($" Point {point} is inside the shell building, continuing.");
                        continue;
                    }

                    if (targetIsABuilding && encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(targetedBuildingGuid))
                    {
                        collisionWorldPos = bresenhamLinePoints[i];
                        __result = true;
                        return;
                    }

                    if (mapMetaData.mapTerrainDataCells[point.Z, point.X].cachedHeight > collisionPointHeight)
                    {
                        collisionWorldPos = bresenhamLinePoints[i];
                        __result = false;
                        return;
                    }
                }
                __result = true;
                return;
            }
        }
    }
}
