using BattleTech;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils.CustomDialog;

namespace ConcreteJungle.Helper
{
    public static class QuipHelper
    {

        // Generates a random quip and publishes it 
        public static void PlayQuip(AbstractActor source, List<string> quips, float showDuration=3)
        {
            CastDef castDef = Coordinator.CreateCast(source);
            DialogueContent content = BuildContent(source.Combat, castDef, quips);
            source.Combat.MessageCenter.PublishMessage(new CustomDialogMessage(source, content, showDuration));
        }

        public static void PlayQuip(CombatGameState combat, string sourceGUID, Team team, 
            string employerFactionName, List<string> quips, float showDuration = 3)
        {
            CastDef castDef = Coordinator.CreateCast(combat, sourceGUID, team, employerFactionName);
            DialogueContent content = BuildContent(combat, castDef, quips);
            combat.MessageCenter.PublishMessage(new CustomDialogMessage(sourceGUID, content, showDuration));
        }

        private static DialogueContent BuildContent(CombatGameState combat, CastDef castDef, List<string> quips)
        {
            string quip = quips[Mod.Random.Next(0, quips.Count)];
            string localizedQuip = new Localize.Text(quip).ToString();

            DialogueContent content = new DialogueContent(
                localizedQuip, Color.white, castDef.id, null, null, DialogCameraDistance.Medium, DialogCameraHeight.Default, 0
                );
            content.ContractInitialize(combat);
            return content;
        }
    }
}
