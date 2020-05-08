using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(WeaponRangeIndicators), "ShowLinesToAllEnemies")]
    static class WeaponRangeIndicators_ShowLinesToAllEnemies
    {
        static void Postfix(WeaponRangeIndicators __instance, AbstractActor selectedActor)
        {
            //Mod.Log.Debug($"Iterating all enemies of selected actor: {CombatantUtils.Label(selectedActor)}");
            //List<AbstractActor> allEnemies = selectedActor.Combat.AllEnemies;
            //foreach(AbstractActor enemyActor in allEnemies)
            //{
            //    Mod.Log.Debug($"  -- enemy: {CombatantUtils.Label(enemyActor)}");
            //}

            //Mod.Log.Debug($"Iterating all possible targets");
            //Traverse HUDT = Traverse.Create(__instance).Property("HUD");
            //CombatHUD HUD = HUDT.GetValue<CombatHUD>();
            //foreach (ICombatant target in HUD.SelectionHandler.ActiveState.FiringPreview.AllPossibleTargets)
            //{
            //    Mod.Log.Debug($"  -- target: {CombatantUtils.Label(target)}");
            //}
        }
    }

    [HarmonyPatch(typeof(WeaponRangeIndicators), "UpdateTargetingLines")]
    static class WeaponRangeIndicators_UpdateTargetingLines
    {
        static void Postfix(WeaponRangeIndicators __instance, AbstractActor selectedActor, Vector3 position, Quaternion rotation, bool isPositionLocked,
            ICombatant targetedActor, bool useMultiFire, List<ICombatant> lockedTargets, bool isMelee)
        {
            if (targetedActor != null && !useMultiFire && targetedActor is BattleTech.Building targetedBuilding)
            {
                Mod.Log.Debug("Drawing line for building-as-target.");
                // Replicate WeaponRangeIndicators.ShowLineToTarget, because it only supports AbstractActors
                Traverse drawLineT = Traverse.Create(__instance).Method("DrawLine", new object[] { position, rotation, true, selectedActor, targetedActor, false, false, isMelee });
                drawLineT.GetValue();

                targetedActor.GameRep.IsTargeted = true;

                //this.HUD.InWorldMgr.ShowAttackDirection(this.HUD.SelectedActor, targetedActor, this.HUD.Combat.HitLocation.GetAttackDirection(position, targetedActor), 0f, 
                //    isMelee ? MeleeAttackType.Punch : MeleeAttackType.NotSet, this.HUD.InWorldMgr.NumWeaponsTargeting(targetedActor));

                Traverse hideLinesT = Traverse.Create(__instance).Method("hideLines", new object[] { 1 });
                hideLinesT.GetValue();

                Traverse setEnemyTargetableT = Traverse.Create(__instance).Method("SetEnemyTargetable", new Type[] { typeof(ICombatant), typeof(bool) });
                List<AbstractActor> allEnemies = selectedActor.Combat.AllEnemies;
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    if (allEnemies[i] != targetedActor)
                    {
                        setEnemyTargetableT.GetValue(new object[] { allEnemies[i], false });
                    }
                }
            }
        }
    }


    [HarmonyPatch(typeof(WeaponRangeIndicators), "DrawLine")]
    static class WeaponRangeIndicator_DrawLine
    {
        static void Postfix(WeaponRangeIndicators __instance, bool isPositionLocked, AbstractActor selectedActor, ICombatant target, bool usingMultifire, bool isLocked, bool isMelee)
        {
            Mod.Log.Debug($" === Drawing line from {CombatantUtils.Label(selectedActor)} to target: {CombatantUtils.Label(target)}");
        }
    }
}
