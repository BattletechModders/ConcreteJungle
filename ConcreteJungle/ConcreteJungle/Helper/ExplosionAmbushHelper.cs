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
            // Build list of candidate trap buildings
            List<BattleTech.Building> candidates = CandidateHelper.FilterCandidates(originPos, Mod.Config.ExplosionAmbush.SearchRadius);

			Vector3 originHex = ModState.Combat.HexGrid.GetClosestPointOnGrid(originPos);
			List<Vector3> adjacentHexes = ModState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(originPos, 2);
			Mod.Log.Debug($"Found origin hex: {originHex} with {adjacentHexes.Count} adjacent hexes within {2} hexes");

			int numBlasts = Mod.Random.Next(Mod.Config.ExplosionAmbush.MinExplosions, Mod.Config.ExplosionAmbush.MaxExplosions);
			List<Vector3> blastOrigins = new List<Vector3> { originHex };
			numBlasts--;

			for (int i = 0; i < numBlasts; i++)
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

				blastOrigins.Add(new Vector3(newHexPos.x, melDataCell.relatedTerrainCell.cachedHeight, newHexPos.z));
				adjacentHexes.RemoveAt(randIdx);
			}

			List<ICombatant> targets = new List<ICombatant>();
			List<Collider> overlapedColliders = new List<Collider>(Physics.OverlapSphere(originPos, Mod.Config.ExplosionAmbush.SearchRadius));
			foreach (ICombatant combatant in ModState.Combat.GetAllCombatants())
			{
				if (!combatant.IsDead && !combatant.IsFlaggedForDeath)
				{
					if (combatant.UnitType == UnitType.Building)
					{
						if (Vector3.Distance(originPos, combatant.CurrentPosition) <= 2f * Mod.Config.ExplosionAmbush.SearchRadius)
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
					else if (Vector3.Distance(originPos, combatant.CurrentPosition) <= Mod.Config.ExplosionAmbush.SearchRadius)
					{
						targets.Add(combatant);
					}
				}
			}

			Mod.Log.Debug("Sending AddSequence message for ambush explosion.");
			try
			{
				AmbushExplosionSequence ambushSequence = new AmbushExplosionSequence(ModState.Combat, originPos, blastOrigins, targets);
				ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));

			}
			catch (Exception e)
			{
				Mod.Log.Error("Failed to create AES sequence due to error!", e);
			}

			// Reset the initial state
			Mod.Log.Debug("Resetting mod to original state");
			ModState.PendingAmbushOrigin = Vector3.zero;
		}
    }
}
