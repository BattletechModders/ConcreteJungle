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

			// Load the weapons we'll use in the blast
			List<AOEBlastDef> blastsForAttack = RandomizeBlastDefs(numBlasts);

			Mod.Log.Debug($"Sending AddSequence message for ambush explosion with {blastsForAttack.Count} weapons and {blastPositions.Count} blasts");
			try
			{
				ExplosionAmbushSequence ambushSequence = new ExplosionAmbushSequence(ModState.Combat, blastPositions, blastsForAttack, ModState.AmbushTeam);
				ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));

			}
			catch (Exception e)
			{
				Mod.Log.Error("Failed to create AES sequence due to error!", e);
			}
		}

		private static List<AOEBlastDef> RandomizeBlastDefs(int count)
		{

			// Shuffle the weaponDefs over and over again, and add the weapon to the list.
			List<AOEBlastDef> randomizedBlasts = new List<AOEBlastDef>();
			List<AOEBlastDef> shuffledAOEDefs = new List<AOEBlastDef>();
			shuffledAOEDefs.AddRange(ModState.ExplosionAmbushDefForContract.SpawnPool);

			Mod.Log.Debug($"Shuffling AOEBlasts for explosive ambush");
			for (int i = 0; i < count; i++)
			{
				shuffledAOEDefs.Shuffle();
				AOEBlastDef blastDef = shuffledAOEDefs[0];
				Mod.Log.Debug($"  [{i}] = blastDef.label: {blastDef.FloatieTextKey}");
				randomizedBlasts.Add(blastDef);
			}

			return randomizedBlasts;
		}
    }
}
