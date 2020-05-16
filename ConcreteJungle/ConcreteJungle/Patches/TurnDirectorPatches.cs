using BattleTech;
using BattleTech.Data;
using ConcreteJungle.Helper;
using Harmony;
using us.frostraptor.modUtils;
using static ConcreteJungle.ModConfig;

namespace ConcreteJungle.Patches
{

    [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
    static class TurnDirector_IncrementActiveTurnActor
    {
        static void Prefix(TurnDirector __instance)
        {
            if (!__instance.IsInterleaved && ! __instance.IsInterleavePending &&
                __instance.ActiveTurnActor is Team activeTeam &&
                activeTeam.IsLocalPlayer &&
                ModState.PendingAmbushOrigin.magnitude != 0)
            {
                Mod.Log.Debug($"Resolving pending ambush at position: {ModState.PendingAmbushOrigin}");

                // Determine trap type - infantry ambush, tank ambush, IED
                // TODO: Randomize
                //TrapType trapType = TrapType.TRAP_INFANTRY_AMBUSH;

                //InfantryAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);
                //ExplosionAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);
                SpawnAmbushHelper.SpawnAmbush(ModState.PendingAmbushOrigin);

            }
        }
    }


    // We need to defer until after the buildings are initialized in pass 2, because the UrbanDestructible elements aren't
    //   added until then. 
    [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
    public static class TurnDirector_OnInitializeContractComplete
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {
            Mod.Log.Trace("TD:OICC - entered.");

            InitializeContractCompleteMessage initializeContractMessage = message as InitializeContractCompleteMessage;
            CombatGameState combat = __instance.Combat;
            Contract activeContract = combat.ActiveContract;

            // Find candidate buildings
            Mod.Log.Debug("Filtering candidate buidlings:");
            ModState.CandidateBuildings.Clear();
            foreach (ICombatant combatant in combat.GetAllCombatants())
            {
                if (combatant is BattleTech.Building building)
                {
                    Mod.Log.Trace($" Found building {CombatantUtils.Label(building)}");
                    Mod.Log.Trace($"  -- isTabTarget: {building.IsTabTarget}");

                    if (building.BuildingDef != null)
                    {
                        Mod.Log.Trace($"   -- BuildingDef:");
                        Mod.Log.Trace($"     description: '{building.BuildingDef.Description}' ");
                        Mod.Log.Trace($"     isDestructible: {building.BuildingDef.Destructible} " +
                            $"structurePoints: {building.BuildingDef.StructurePoints} ");
                    }
                    else { continue; }

                    if (building.UrbanDestructible != null)
                    {
                        Mod.Log.Trace($"   -- UrbanDestructible: " +
                            $"name: {building.UrbanDestructible.name} " +
                            $"canBeDesolation: {building.UrbanDestructible.CanBeDesolation} " +
                            $"currentDesolationState: {building.UrbanDestructible.CurDesolationState}"
                            );
                    }
                    else { continue; }

                    if (building.objectiveGUIDS.Contains(combat.GUID))
                    {
                        Mod.Log.Debug($"   -- Building is an objective, skipping.");
                        continue;
                    }

                    if (building.BuildingDef != null && building.BuildingDef.Destructible &&
                        building.UrbanDestructible != null && building.UrbanDestructible.CanBeDesolation &&
                        !building.IsTabTarget)
                    {
                        Mod.Log.Trace($"  -- Building {CombatantUtils.Label(building)} meets criteria, adding as trap candidate.");
                        ModState.CandidateBuildings.Add(building);
                    }

                }
            }
            Mod.Log.Debug($"Map has: {ModState.CandidateBuildings.Count} buildings suitable for traps");

            // Devestate buildings
            DevestationHelper.DevestateBuildings();

            Mod.Log.Debug($"After devestation, map has {ModState.CandidateBuildings.Count} candidate buildings.");
        }

    }
}
