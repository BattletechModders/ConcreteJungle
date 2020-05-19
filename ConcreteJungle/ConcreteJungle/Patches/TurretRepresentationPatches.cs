using BattleTech;
using Harmony;
using Localize;

namespace ConcreteJungle.Patches
{
    // Replace the floatie text with the display name of the destroyed linked turret
    [HarmonyPatch(typeof(TurretRepresentation), "PlayDeathFloatie")]
    static class TurretRepresentation_PlayDeathFloatie
    {
        static bool Prefix(TurretRepresentation __instance, DeathMethod deathMethod)
        {

            // We're a linked turret, replace the kill floatie with a custom one.
            if (ModState.KillingLinkedUnitsSource != null)
            {
                if (__instance.parentActor.WasDespawned)
                {
                    return false;
                }

                string localText = new Text(Mod.Config.LocalizedText[ModConfig.FT_Turret_Death], new object[] { __instance.parentActor.DisplayName }).ToString();
                FloatieMessage message = new FloatieMessage(__instance.parentCombatant.GUID, __instance.parentCombatant.GUID, localText, FloatieMessage.MessageNature.Death);
                __instance.parentCombatant.Combat.MessageCenter.PublishMessage(message);

                return false;
            }
            else
            {
                return true;
            }

        }
    }
}
