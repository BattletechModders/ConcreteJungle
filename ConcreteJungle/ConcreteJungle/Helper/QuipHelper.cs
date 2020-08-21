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
            DialogueContent content = BuildContent(castDef, quips);
            source.Combat.MessageCenter.PublishMessage(new CustomDialogMessage(source, content, showDuration));
        }

        public static void PlayQuip(CombatGameState combat, string sourceGUID, Team team, 
            string employerFactionName, List<string> quips, float showDuration = 3)
        {
            CastDef castDef = Coordinator.CreateCast(combat, sourceGUID, team, employerFactionName);
            DialogueContent content = BuildContent( castDef, quips);
            combat.MessageCenter.PublishMessage(new CustomDialogMessage(sourceGUID, content, showDuration));
        }

        private static DialogueContent BuildContent(CastDef castDef, List<string> quips)
        {
            string quip = quips[Mod.Random.Next(0, quips.Count)];
            string localizedQuip = new Localize.Text(quip).ToString();
            return Coordinator.BuildDialogueContent(castDef, localizedQuip, Color.white);
        }
    }
}
