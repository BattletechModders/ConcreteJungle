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
        static void Postfix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, VisibilityLevel __result)
        {
            if (source != null && ModState.TrapTurretToBuildingIds.ContainsKey(source.GUID))
            {
                Mod.Log.Debug($"VISIBILITY: SOURCE {CombatantUtils.Label(source)} HAS VISIBILITY {__result} TO TARGET {CombatantUtils.Label(target)}");
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

    //[HarmonyPatch(typeof(LineOfSight), "GetLineOfFireUncached")]
    //static class LineOfSight_GetLineOfFireUncached
    //{
    //    static void Postfix(LineOfSight __instance, AbstractActor source, ICombatant target)
    //    {
    //        Mod.Log.Debug($"CALCULATING LOF FROM {CombatantUtils.Label(source)} TO TARGET: {CombatantUtils.Label(target)}");
    //    }
    //}

    [HarmonyPatch(typeof(LineOfSight), "bresenhamHeightTest")]
    static class LineOfSight_bresenhamHeightTest
    {

        static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, string targetedBuildingGuid, ref Point collisionWorldPos, 
            ref bool __result, CombatGameState ___Combat)
        {

            if (ModState.CurrentTurretForLOF != null)
            {
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
}
