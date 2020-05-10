using BattleTech;
using Harmony;
using System.Reflection;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{

    // Unnecessary?
    //[HarmonyPatch(typeof(AIUtil), "UnitHasDirectLOFToTargetFromPosition")]
    //static class AIUtil_UnitHasDirectLOFToTargetFromPosition
    //{
    //    static void Postfix(AbstractActor attacker, ICombatant target, CombatGameState combat, Vector3 attackerPosition, bool __result)
    //    {
    //        Mod.Log.Debug($"UHDLOFTTFP - Attacker {CombatantUtils.Label(attacker)} to target: {CombatantUtils.Label(target)} returns {__result}");
    //    }
    //}

    //// Unnecessary?
    //[HarmonyPatch]
    //static class HasLOSToAnyHostileNode_Tick
    //{
    //    public static MethodInfo TargetMethod()
    //    {
    //        return AccessTools.Method("HasLOSToAnyHostileNode:Tick");
    //    }

    //    static void Postfix(LeafBehaviorNode __instance, BehaviorTree ___tree, AbstractActor ___unit)
    //    {
    //        for (int i = 0; i < ___tree.enemyUnits.Count; i++)
    //        {
    //            AbstractActor abstractActor = ___tree.enemyUnits[i] as AbstractActor;
    //            if (abstractActor != null)
    //            {
    //                Mod.Log.Debug($" Enemy unit: {CombatantUtils.Label(abstractActor)}");
    //                Mod.Log.Debug($"   -- visibility: {___unit.VisibilityCache.VisibilityToTarget(abstractActor).VisibilityLevel}");
    //            }
    //        }
    //    }
    //}
}
