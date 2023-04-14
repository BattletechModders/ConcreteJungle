using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{

    // Prevent stray shots from hitting the shell buildings in an ambush
    [HarmonyPatch(typeof(LineOfSight), "FindSecondaryImpactTarget")]
    static class LineOfSight_FindSecondaryImpactTarget
    {
        static void Postfix(LineOfSight __instance, ref bool __result, AbstractActor attacker, ref string impactTargetId, ref int impactHitLocation, ref AttackDirection attackDirection)
        {
            if (attacker != null && ModState.AmbushTurretGUIDtoBuilding.ContainsKey(attacker.GUID))
            {
                // We are an ambush turret - check to see if the secondary target is our shell building
                if (ModState.AmbushTurretGUIDtoBuilding[attacker.GUID].GUID == impactTargetId)
                {
                    Mod.Log.Debug?.Write($"Attack from ambush turret would hit it's shell building - preventing!");
                    __result = false;
                    impactTargetId = null;
                    impactHitLocation = 0;
                    attackDirection = AttackDirection.FromFront;
                }
            }
        }
    }

    // TODO: The bresenham tests should probably be prefixes

    // When a trap turret's line of sight is calculated, give it 'x-ray' vision to see through the shell building.
    [HarmonyPatch(typeof(LineOfSight), "GetVisibilityToTargetWithPositionsAndRotations")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
    static class LineOfSight_GetVisibilityToTargetWithPositionsAndRotations
    {
        static void Prefix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (source != null && ModState.IsUrbanBiome && ModState.AmbushTurretGUIDtoBuilding.ContainsKey(source.GUID) && !(target is BattleTech.Building))
            {
                Mod.Log.Trace?.Write($"___VISIBILITY: SOURCE {CombatantUtils.Label(source)} TO TARGET {CombatantUtils.Label(target)}");
            }

            if (source is Turret turret && ModState.AmbushTurretGUIDtoBuilding.Keys.Contains(turret.GUID))
            {
                Mod.Log.Trace?.Write($"Turret {CombatantUtils.Label(turret)} is calculating it's LOS");
                ModState.CurrentTurretForLOS = turret;
            }

        }

        static void Postfix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, VisibilityLevel __result)
        {
            if (source != null && ModState.IsUrbanBiome && ModState.AmbushTurretGUIDtoBuilding.ContainsKey(source.GUID) && !(target is BattleTech.Building))
            {
                Mod.Log.Trace?.Write($"___VISIBILITY RESULT: {__result}");
            }

            if (ModState.CurrentTurretForLOS != null)
            {
                ModState.CurrentTurretForLOS = null;
            }
        }
    }

    // Modify the vision test to allow 'x-ray' vision through the shell building for trap turrets
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
            string shellBuildingGUID = ModState.AmbushTurretGUIDtoBuilding[ModState.CurrentTurretForLOS.GUID].GUID;
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
                    Mod.Log.Trace?.Write($" Point x={list[i].X} z={list[i].Z} is inside the shell building, adding vision cost normally.");
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

    // When a trap turret's line of fire is calculated, give it 'x-ray' vision to see through the shell building.
    [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
    static class LOFCache_GetLineOfFire
    {

        static void Prefix(AbstractActor source, ICombatant target, LineOfFireLevel __result)
        {
            if (!source.team.IsLocalPlayer && !(target is BattleTech.Building building))
            {
                Mod.Log.Trace?.Write($"== CALCULATING LOF FROM {CombatantUtils.Label(source)} TO TARGET: {CombatantUtils.Label(source)}");
            }

            if (source is Turret turret && ModState.IsUrbanBiome && ModState.AmbushTurretGUIDtoBuilding.Keys.Contains(turret.GUID))
            {
                Mod.Log.Trace?.Write($"Turret {CombatantUtils.Label(turret)} is calculating it's LOF");
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
                Mod.Log.Trace?.Write($"== LOF RESULT: {__result}");
            }
        }
    }

    // Modify the vision test to allow 'x-ray' vision through the shell building for trap turrets
    [HarmonyPatch(typeof(LineOfSight), "bresenhamHeightTest")]
    static class LineOfSight_bresenhamHeightTest
    {

        static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, string targetedBuildingGuid, ref Point collisionWorldPos,
            ref bool __result, CombatGameState ___Combat)
        {

            if (ModState.CurrentTurretForLOF == null) return;

            Mod.Log.Trace?.Write($"Recalculating LOF from {CombatantUtils.Label(ModState.CurrentTurretForLOF)} due to collision on building shell. " +
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
            string shellBuildingGUID = ModState.AmbushTurretGUIDtoBuilding[ModState.CurrentTurretForLOF.GUID].GUID;

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
                    Mod.Log.Trace?.Write($" Point x={point.X} z={point.Z} is inside the shell building, continuing.");
                    continue;
                }

                if (targetIsABuilding && encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(targetedBuildingGuid))
                {
                    Mod.Log.Trace?.Write($" Building {targetedBuildingGuid} conflicts with the LoS, collision at x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                    collisionWorldPos = bresenhamLinePoints[i];
                    __result = true;
                    return;
                }

                if (mapMetaData.mapTerrainDataCells[point.Z, point.X].cachedHeight > collisionPointHeight)
                {
                    Mod.Log.Trace?.Write($" Collision on terrain at position x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                    collisionWorldPos = bresenhamLinePoints[i];
                    __result = false;
                    return;
                }
            }

            Mod.Log.Trace?.Write($"No collision detected, changing LoF to true. CollisonWorldPos => x ={collisionWorldPos.X} z ={collisionWorldPos.Z}");

            __result = true;
            return;

        }
    }
}
