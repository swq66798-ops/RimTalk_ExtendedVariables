using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.Reflection;

namespace RimTalk_ExtendedVariables.Patches
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            if (__instance.Faction != null && (__instance.IsColonist || __instance.IsPrisoner || __instance.IsSlave || __instance.Faction != Faction.OfPlayer))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Initiate RimTalk",
                    defaultDesc = "Force this pawn to initiate a natural RimTalk conversation.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true), // Placeholder icon
                    action = delegate
                    {
                        InitiateTalk(__instance);
                    }
                };
            }
        }

        private static void InitiateTalk(Pawn pawn)
        {
            // Try to find RimTalk's talk generation method
            var rimTalkType = AccessTools.TypeByName("RimTalk.RimTalk");
            if (rimTalkType != null)
            {
                // Look for a method that generates talk
                var generateTalkMethod = AccessTools.Method(rimTalkType, "GenerateTalk", new[] { typeof(Pawn) });
                if (generateTalkMethod != null)
                {
                    generateTalkMethod.Invoke(null, new object[] { pawn });
                    Messages.Message($"{pawn.NameShortColored} is initiating a conversation.", pawn, MessageTypeDefOf.NeutralEvent);
                    return;
                }
                
                // Alternative method name check
                var tryGenerateTalkMethod = AccessTools.Method(rimTalkType, "TryGenerateTalkFromPool", new[] { typeof(Pawn) });
                if (tryGenerateTalkMethod != null)
                {
                    tryGenerateTalkMethod.Invoke(null, new object[] { pawn });
                    Messages.Message($"{pawn.NameShortColored} is initiating a conversation.", pawn, MessageTypeDefOf.NeutralEvent);
                    return;
                }
            }

            // If we can't find the direct method, we might need to use the TalkService or similar
            var talkServiceType = AccessTools.TypeByName("RimTalk.Service.TalkService");
            if (talkServiceType != null)
            {
                var instanceProp = AccessTools.Property(talkServiceType, "Instance");
                if (instanceProp != null)
                {
                    var instance = instanceProp.GetValue(null);
                    if (instance != null)
                    {
                        var generateMethod = AccessTools.Method(talkServiceType, "GenerateAndProcessTalkAsync", new[] { typeof(Pawn) });
                        if (generateMethod != null)
                        {
                            generateMethod.Invoke(instance, new object[] { pawn });
                            Messages.Message($"{pawn.NameShortColored} is initiating a conversation.", pawn, MessageTypeDefOf.NeutralEvent);
                            return;
                        }
                    }
                }
            }

            Messages.Message($"Failed to initiate conversation for {pawn.NameShortColored}. RimTalk method not found.", pawn, MessageTypeDefOf.RejectInput);
        }
    }
}
