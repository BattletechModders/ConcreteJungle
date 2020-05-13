using BattleTech;
using ConcreteJungle.Sequence;
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

            // The ambush requires at least one building within the explosion radius of the target
            if (candidates.Count < 1)
            {
                Mod.Log.Debug($"Insufficient candidate buildings to spawn an explosion ambush. Skipping.");
                return;
            }

			List<ICombatant> list = new List<ICombatant>();
			List<Collider> list2 = new List<Collider>(Physics.OverlapSphere(originPos, Mod.Config.ExplosionAmbush.SearchRadius));
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
								if (list2.Contains(item))
								{
									list.Add(combatant);
									break;
								}
							}
						}
					}
					else if (Vector3.Distance(originPos, combatant.CurrentPosition) <= Mod.Config.ExplosionAmbush.SearchRadius)
					{
						list.Add(combatant);
					}
				}
			}

			Mod.Log.Debug("Sending AddSequence message for ambush explosion.");
			AmbushExplosionSequence ambushSequence = new AmbushExplosionSequence(ModState.Combat, originPos, list);
			ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));

			//List<Vector3> targetPositions = new List<Vector3> { originPos };
			//MessageCenterMessage message = new MechMortarInvocation(creator, targetPositions, list, weapon, this);
			//ModState.Combat.MessageCenter.PublishMessage(message);
		}
    }
}
