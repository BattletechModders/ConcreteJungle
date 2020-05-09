using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{

    [HarmonyPatch(typeof(LineOfSight), "GetVisibilityToTargetWithPositionsAndRotations")]
    [HarmonyPatch(new Type[] {  typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion)})]
    static class LineOfSight_GetVisibilityToTargetWithPositionsAndRotations
    {
        static void Prefix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (source != null && ModState.TrapTurretToBuildingIds.ContainsKey(source.GUID) && !(target is BattleTech.Building))
            {
                Mod.Log.Debug($"___VISIBILITY: SOURCE {CombatantUtils.Label(source)} TO TARGET {CombatantUtils.Label(target)}");
            }

            if (source is Turret turret && ModState.TrapTurretToBuildingIds.Keys.Contains(turret.GUID))
            {
                Mod.Log.Trace($"Turret {CombatantUtils.Label(turret)} is calculating it's LOS");
                ModState.CurrentTurretForLOS = turret;
            }

        }

        static void Postfix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, VisibilityLevel __result)
        {
            if (source != null && ModState.TrapTurretToBuildingIds.ContainsKey(source.GUID) && !(target is BattleTech.Building))
            {
                Mod.Log.Debug($"___VISIBILITY RESULT: {__result}");
            }

            if (ModState.CurrentTurretForLOS != null)
            {
                ModState.CurrentTurretForLOS = null;
            }
        }
    }

    [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
    static class LOFCache_GetLineOfFire { 

        static void Prefix(AbstractActor source, ICombatant target, LineOfFireLevel __result)
        {
            if (!source.team.IsLocalPlayer && !(target is BattleTech.Building building))
            {
                Mod.Log.Debug($"== CALCULATING LOF FROM {CombatantUtils.Label(source)} TO TARGET: {CombatantUtils.Label(target)}");
            }

            if (source is Turret turret && ModState.TrapTurretToBuildingIds.Keys.Contains(turret.GUID))
            {
                Mod.Log.Trace($"Turret {CombatantUtils.Label(turret)} is calculating it's LOF");
                ModState.CurrentTurretForLOF = turret;
            }
        }

        static void Postfix(AbstractActor source, ICombatant target, LineOfFireLevel __result)
        {
            if (ModState.CurrentTurretForLOF != null)
            {
                ModState.CurrentTurretForLOF = null;
            }

            if (!source.team.IsLocalPlayer && !(target is BattleTech.Building building))
            {
                Mod.Log.Debug($"== LOF RESULT: {__result}");
            }
        }
    }

    [HarmonyPatch(typeof(LineOfSight), "bresenhamVisionTest")]
    static class LineOfSight_bresenhamVisionTest
    {
        static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, Vector3 unitDelta, string targetGuid,
            ref float __result, CombatGameState ___Combat)
        {
            if (ModState.CurrentTurretForLOS == null) return;

            if (p0.X == p1.X && p0.Z == p1.Z)
            {
                __result = 0f;
                return;
            }

            if (!___Combat.MapMetaData.IsWithinBounds(p0) || !___Combat.MapMetaData.IsWithinBounds(p1))
            {
                __result = float.MaxValue;
                return;
            }

            float numCellsX = Mathf.Abs(unitDelta.x) * (float)MapMetaDataExporter.cellSize;
            float numCellsY = Mathf.Abs(unitDelta.z) * (float)MapMetaDataExporter.cellSize;
            float lineDeltaX = (float)(p1.X - p0.X);
            float lineDeltaZ = (float)(p1.Z - p0.Z);
            float greatestDivisor = Mathf.Max(Mathf.Abs(lineDeltaX), Mathf.Abs(lineDeltaZ));
            float stepHeight = (height1 - height0) / greatestDivisor;
            float sumVisionCost = 0f;

            Traverse projectedHeightAtT = Traverse.Create(__instance).Method("getProjectedHeightAt", new Type[] { typeof(Point), typeof(float), typeof(Point), typeof(float) });
            Traverse visCostOfCellT = Traverse.Create(__instance).Method("visCostOfCell", new Type[] { typeof(MapTerrainDataCell), typeof(float) });
            string shellBuildingGUID = ModState.TrapTurretToBuildingIds[ModState.CurrentTurretForLOS.GUID];
            EncounterLayerData encounterLayerData = ___Combat.EncounterLayerData;

            List<Point> list = BresenhamLineUtil.BresenhamLine(p0, p1);
            for (int i = 1; i < list.Count; i++)
            {
                float stepDelta;
                if (list[i].X != list[i - 1].X)
                {
                    stepDelta = numCellsX;
                }
                else
                {
                    stepDelta = numCellsY;
                }

                // Increment vision cost only slightly if it's inside our shell building
                if (encounterLayerData.mapEncounterLayerDataCells[list[i].Z, list[i].X].HasSpecifiedBuilding(shellBuildingGUID))
                {
                    Mod.Log.Trace($" Point x={list[i].X} z={list[i].Z} is inside the shell building, adding vision cost normally.");
                    sumVisionCost += stepDelta;
                }
                else
                {
                    float projectedHeightAt = projectedHeightAtT.GetValue<float>(new object[] { p0, height0, list[i], stepHeight });
                    MapTerrainDataCell mapTerrainDataCell = ___Combat.MapMetaData.mapTerrainDataCells[list[i].Z, list[i].X];
                    if (mapTerrainDataCell.cachedHeight > projectedHeightAt)
                    {
                        if (mapTerrainDataCell.MapEncounterLayerDataCell.HasBuilding)
                        {
                            for (int j = 0; j < mapTerrainDataCell.MapEncounterLayerDataCell.buildingList.Count; j++)
                            {
                                if (ObstructionGameLogic.GuidsMatchObjectOrRep(mapTerrainDataCell.MapEncounterLayerDataCell.buildingList[j].buildingGuid, targetGuid))
                                {
                                    __result = sumVisionCost;
                                    return;
                                }
                            }
                        }

                        __result = float.MaxValue;
                        return;
                    }

                    sumVisionCost += visCostOfCellT.GetValue<float>(new object[] { mapTerrainDataCell, projectedHeightAt }) * stepDelta;
                }
            }

            __result = sumVisionCost;
            return;
        }
    }

    [HarmonyPatch(typeof(LineOfSight), "bresenhamHeightTest")]
    static class LineOfSight_bresenhamHeightTest
    {

        static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, string targetedBuildingGuid, ref Point collisionWorldPos, 
            ref bool __result, CombatGameState ___Combat)
        {

            if (ModState.CurrentTurretForLOF == null) return;

            Mod.Log.Debug($"Recalculating LOF from {CombatantUtils.Label(ModState.CurrentTurretForLOF)} due to collision on building shell. " +
                $"CollisonWorldPos=> x={collisionWorldPos.X} z={collisionWorldPos.Z}");

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
            string shellBuildingGUID = ModState.TrapTurretToBuildingIds[ModState.CurrentTurretForLOF.GUID];

            List<Point> bresenhamLinePoints = BresenhamLineUtil.BresenhamLine(p0, p1);
            float heightDeltaPerPoint = (height1 - height0) / (float)bresenhamLinePoints.Count;
            float collisionPointHeight = height0;
            // Walk the bresenham Line, evaluation collision at a speciifc height as we go.
            for (int i = 0; i < bresenhamLinePoints.Count; i++)
            {
                collisionPointHeight += heightDeltaPerPoint;
                Point point = bresenhamLinePoints[i];

                if (encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(shellBuildingGUID))
                {
                    Mod.Log.Trace($" Point x={point.X} z={point.Z} is inside the shell building, continuing.");
                    continue;
                }

                if (targetIsABuilding && encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(targetedBuildingGuid))
                {
                    Mod.Log.Debug($" Building {targetedBuildingGuid} conflicts with the LoS, collision at x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                    collisionWorldPos = bresenhamLinePoints[i];
                    __result = true;
                    return;
                }

                if (mapMetaData.mapTerrainDataCells[point.Z, point.X].cachedHeight > collisionPointHeight)
                {
                    Mod.Log.Debug($" Collision on terrain at position x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                    collisionWorldPos = bresenhamLinePoints[i];
                    __result = false;
                    return;
                }
            }

            Mod.Log.Debug($"No collision detected, changing LoF to true. CollisonWorldPos => x ={ collisionWorldPos.X} z ={ collisionWorldPos.Z}");

            __result = true;
            return;

        }
    }
}
