using BattleTech;
using Harmony;
using System.Linq;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(PilotableActorRepresentation), "OnPlayerVisibilityChanged")]
    [HarmonyBefore("us.frostraptor.LowVisibility")]
    public static class PilotableActorRepresentation_OnPlayerVisibilityChanged
    {
        public static void Postfix(PilotableActorRepresentation __instance, VisibilityLevel newLevel)
        {
            Mod.Log.Trace("PAR:OPVC entered.");

            Traverse parentT = Traverse.Create(__instance).Property("parentActor");
            AbstractActor parentActor = parentT.GetValue<AbstractActor>();
            if (ModState.TrapTurretToBuildingIds.Keys.Contains(parentActor.GUID))
            {
                Turret turret = parentActor as Turret;
                if (newLevel == VisibilityLevel.LOSFull)
                {
                    __instance.VisibleObject.SetActive(false);
                }
            }

        }
    }
}
