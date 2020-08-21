using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
	// Patch the targeting HUD popup to show buildings with weapons. 
	[HarmonyPatch(typeof(CombatHUDTargetingComputer), "RefreshActorInfo")]
	[HarmonyBefore("us.frostraptor.LowVisibility")]
	public static class CombatHUDTargetingComputer_RefreshActorInfo
	{
		public static void Postfix(CombatHUDTargetingComputer __instance, List<LocalizableText> ___weaponNames, UIManager ___uiManager)
		{

			// Skip processing if we're not initialized properly
			if (__instance == null || __instance?.ActivelyShownCombatant?.Combat == null || __instance.WeaponList == null) return;

			if (__instance.ActivelyShownCombatant is BattleTech.Building building && ModState.AmbushBuildingGUIDToTurrets.ContainsKey(building.GUID))
			{
				Mod.Log.Trace?.Write($"TargetingHUD target {CombatantUtils.Label(__instance.ActivelyShownCombatant)} is trapped enemy building");

				// Replicate RefreshWeaponList here as it expects an AbstractActor
				Turret turret = ModState.AmbushBuildingGUIDToTurrets[building.GUID];
				for (int i = 0; i < ___weaponNames.Count; i++)
				{
					if (turret != null && i < turret.Weapons.Count)
					{
						Weapon weapon = turret.Weapons[i];
						___weaponNames[i].SetText(weapon.UIName);
						if (!weapon.HasAmmo)
						{
							___weaponNames[i].color = ___uiManager.UIColorRefs.qualityD;
						}
						else
						{
							switch (weapon.DamageLevel)
							{
								case ComponentDamageLevel.Functional:
									___weaponNames[i].color = ___uiManager.UIColorRefs.qualityA;
									break;
								case ComponentDamageLevel.Misaligned:
								case ComponentDamageLevel.Penalized:
									___weaponNames[i].color = ___uiManager.UIColorRefs.structureUndamaged;
									break;
								case ComponentDamageLevel.NonFunctional:
								case ComponentDamageLevel.Destroyed:
									___weaponNames[i].color = ___uiManager.UIColorRefs.qualityD;
									break;
							}
						}
						if (!___weaponNames[i].transform.parent.gameObject.activeSelf)
						{
							___weaponNames[i].transform.parent.gameObject.SetActive(true);
						}
					}
					else if (___weaponNames[i].transform.parent.gameObject.activeSelf)
					{
						___weaponNames[i].transform.parent.gameObject.SetActive(false);
					}
				}

				__instance.WeaponList.SetActive(true);
				Transform weaponListT = __instance.WeaponList?.transform?.parent?.Find("tgtWeaponsLabel");
				GameObject weaponsLabel = weaponListT.gameObject;
				weaponsLabel.SetActive(true);
			}

		}

	}

	[HarmonyPatch(typeof(CombatHUDActorNameDisplay), "RefreshInfo")]
	[HarmonyPatch(new Type[] { typeof(VisibilityLevel) } )]
	static class CombatHUDActorNameDisplay_RefreshInfo
	{
		static void Postfix(CombatHUDActorNameDisplay __instance, LocalizableText ___MechNameText)
		{
			if (__instance.DisplayedCombatant != null && 
				__instance.DisplayedCombatant is BattleTech.Building building && 
				ModState.AmbushBuildingGUIDToTurrets.ContainsKey(building.GUID))
			{
				Turret turret = ModState.AmbushBuildingGUIDToTurrets[building.GUID];

				Text localText = new Text(turret.DisplayName);
				___MechNameText.SetText(localText.ToString());
			}
		}
	}
}
