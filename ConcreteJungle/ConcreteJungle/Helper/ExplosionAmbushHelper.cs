using BattleTech;
using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConcreteJungle.Helper
{
    public static class ExplosionAmbushHelper
    {
        public static void SpawnAmbush(Vector3 originPos)
        {
			if (!Mod.Config.ExplosionAmbush.Enabled) return;

			// Determine the number of blasts that will occurs
			int numBlasts = Mod.Random.Next(ModState.ExplosionAmbushDefForContract.MinSpawns, ModState.ExplosionAmbushDefForContract.MaxSpawns);
			Mod.Log.Info($"Explosion ambush will apply {numBlasts} blasts.");

			// Load the weapons we'll use in the blast
			List<Weapon> attackWeapons = BuildRandomizedWeaponList(numBlasts);

			// Determine the positions for the blasts
			Vector3 originHex = ModState.Combat.HexGrid.GetClosestPointOnGrid(originPos);
			List<Vector3> blastPositions = new List<Vector3> { originHex };
			List<Vector3> adjacentHexes = ModState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(originPos, 2);
			Mod.Log.Debug($"Found origin hex: {originHex} with {adjacentHexes.Count} adjacent hexes within {2} hexes");
			for (int i = 0; i < numBlasts - 1; i++) 
			{
				if (adjacentHexes.Count == 0) break;

				int randIdx = Mod.Random.Next(0, adjacentHexes.Count);
				Vector3 newHexPos = adjacentHexes[randIdx];
				Mod.Log.Debug($" Adding additional blast position: {newHexPos}");

				EncounterLayerData encounterLayerData = ModState.Combat.EncounterLayerData;
				Point cellPoint = new Point(
					ModState.Combat.MapMetaData.GetXIndex(newHexPos.x),
					ModState.Combat.MapMetaData.GetZIndex(newHexPos.z));
				MapEncounterLayerDataCell melDataCell =
					encounterLayerData.mapEncounterLayerDataCells[cellPoint.Z, cellPoint.X];
				Mod.Log.Debug($" TerrainCell cached height: {melDataCell.relatedTerrainCell.cachedHeight}");

				blastPositions.Add(new Vector3(newHexPos.x, melDataCell.relatedTerrainCell.cachedHeight, newHexPos.z));
				adjacentHexes.RemoveAt(randIdx);
			}

			// Find all targets and building within the explosion range to supply as potential targets.
			List<ICombatant> targets = new List<ICombatant>();
			List<Collider> overlapedColliders = new List<Collider>(Physics.OverlapSphere(originPos, Mod.Config.Ambush.SearchRadius));
			foreach (ICombatant combatant in ModState.Combat.GetAllCombatants())
			{
				if (!combatant.IsDead && !combatant.IsFlaggedForDeath)
				{
					if (combatant.UnitType == UnitType.Building)
					{
						if (Vector3.Distance(originPos, combatant.CurrentPosition) <= 2f * Mod.Config.Ambush.SearchRadius)
						{
							for (int i = 0; i < combatant.GameRep.AllRaycastColliders.Length; i++)
							{
								Collider item = combatant.GameRep.AllRaycastColliders[i];
								if (overlapedColliders.Contains(item))
								{
									targets.Add(combatant);
									break;
								}
							}
						}
					}
					else if (Vector3.Distance(originPos, combatant.CurrentPosition) <= Mod.Config.Ambush.SearchRadius)
					{
						targets.Add(combatant);
					}
				}
			}

			Mod.Log.Debug($"Sending AddSequence message for ambush explosion with {attackWeapons.Count} weapons and {blastPositions.Count} blasts");
			try
			{
				ExplosionAmbushSequence ambushSequence = new ExplosionAmbushSequence(ModState.Combat, attackWeapons, ModState.TargetAllyTeam, blastPositions, targets);
				ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));

			}
			catch (Exception e)
			{
				Mod.Log.Error("Failed to create AES sequence due to error!", e);
			}
		}

		private static List<Weapon> BuildRandomizedWeaponList(int count)
		{
			// Create weapons for the defs first
			Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>();
			foreach (WeaponDefRef wdr in ModState.ExplosionAmbushDefForContract.SpawnPool)
			{
				if (!weapons.ContainsKey(wdr.WeaponDefId))
				{
					WeaponDef weaponDef = ModState.Combat.DataManager.WeaponDefs.Get(wdr.WeaponDefId);
					Mod.Log.Debug($"Loaded weapon def: {weaponDef} with Damage: {weaponDef.Damage}");

					MechComponentRef mechComponentRef = new MechComponentRef(
						weaponDef.Description.Id, weaponDef.Description.Id + "_ReferenceWeapon",
						ComponentType.Weapon, ChassisLocations.None, -1, ComponentDamageLevel.Functional,
						true);
					Mod.Log.Debug($"Created mechComponentRef: {mechComponentRef}");

					mechComponentRef.SetComponentDef(weaponDef);
					mechComponentRef.DataManager = ModState.Combat.DataManager;
					mechComponentRef.RefreshComponentDef();
					Mod.Log.Debug($"Refreshed componentDef. Def after refresh is: {mechComponentRef.Def}");

					Weapon weapon = new Weapon(null, ModState.Combat, mechComponentRef, ModState.Combat.GUID);
					weapon.InitStats();
					Mod.Log.Debug($"Created ambush weapon {weapon.Description.Id} with " +
						$"Name: {weapon.Name} " +
						$"DamagePerShot: {weapon.DamagePerShot} and " +
						$"baseComponentDef: {weapon.baseComponentRef}");

					weapons.Add(wdr.WeaponDefId, weapon);
				}
			}

			// Shuffle the weaponDefs over and over again, and add the weapon to the list.
			List<Weapon> randomizedWeapons = new List<Weapon>();
			List<WeaponDefRef> shuffledWepDefRefs = new List<WeaponDefRef>();
			shuffledWepDefRefs.AddRange(ModState.ExplosionAmbushDefForContract.SpawnPool);

			Mod.Log.Debug($"Shuffling weapons for explosive ambush");
			for (int i = 0; i < count; i++)
			{
				shuffledWepDefRefs.Shuffle();
				WeaponDefRef wepDefRef = shuffledWepDefRefs[0];
				Mod.Log.Debug($"  [{i}] = weaponDefId: {wepDefRef.WeaponDefId}");
				randomizedWeapons.Add(weapons[wepDefRef.WeaponDefId]);
			}

			return randomizedWeapons;
		}
    }
}
