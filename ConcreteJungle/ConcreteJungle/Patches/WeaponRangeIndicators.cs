﻿using BattleTech.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConcreteJungle.Patches
{

    // HBS draws a line-of-fire to target buildings, but when you target them it goes away. This ensures they persist. 
    [HarmonyPatch(typeof(WeaponRangeIndicators), "UpdateTargetingLines")]
    static class WeaponRangeIndicators_UpdateTargetingLines
    {
        static void Postfix(WeaponRangeIndicators __instance, AbstractActor selectedActor, Vector3 position, Quaternion rotation, bool isPositionLocked,
            ICombatant targetedActor, bool useMultiFire, List<ICombatant> lockedTargets, bool isMelee)
        {
            if (targetedActor != null && !useMultiFire && targetedActor is BattleTech.Building targetedBuilding)
            {
                Mod.Log.Trace?.Write("Drawing line for building-as-target.");
                // Replicate WeaponRangeIndicators.ShowLineToTarget, because it only supports AbstractActors
                __instance.DrawLine(position, rotation, true, selectedActor, targetedActor, false, false, isMelee);

                targetedActor.GameRep.IsTargeted = true;

                //this.HUD.InWorldMgr.ShowAttackDirection(this.HUD.SelectedActor, targetedActor, this.HUD.Combat.HitLocation.GetAttackDirection(position, targetedActor), 0f, 
                //    isMelee ? MeleeAttackType.Punch : MeleeAttackType.NotSet, this.HUD.InWorldMgr.NumWeaponsTargeting(targetedActor));

                __instance.hideLines(1);

                List<AbstractActor> allEnemies = selectedActor.Combat.AllEnemies;
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    if (allEnemies[i] != targetedActor)
                    {
                        __instance.SetEnemyTargetable(allEnemies[i], false);
                    }
                }
            }
        }
    }
}
