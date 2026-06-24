using HarmonyLib;
using RimWorld;
using System;
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
            try
            {
                // Find TalkService
                var talkServiceType = AccessTools.TypeByName("RimTalk.Service.TalkService");
                if (talkServiceType != null)
                {
                    var instanceProp = AccessTools.Property(talkServiceType, "Instance");
                    if (instanceProp != null)
                    {
                        var instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            // Find TalkRequest type
                            var talkRequestType = AccessTools.TypeByName("RimTalk.Data.TalkRequest");
                            if (talkRequestType != null)
                            {
                                // Find TalkType enum
                                var talkTypeEnum = AccessTools.TypeByName("RimTalk.Source.Data.TalkType");
                                if (talkTypeEnum != null)
                                {
                                    // Create TalkRequest instance
                                    // Constructor: .ctor (String, Pawn, Pawn, TalkType)
                                    // We use "User" TalkType (value 8)
                                    object talkTypeValue = Enum.ToObject(talkTypeEnum, 8); // User
                                    
                                    // Find a recipient using RimTalk's PawnSelector
                                    Pawn recipient = null;
                                    var pawnSelectorType = AccessTools.TypeByName("RimTalk.Service.PawnSelector");
                                    if (pawnSelectorType != null)
                                    {
                                        var getNearbyMethod = AccessTools.Method(pawnSelectorType, "GetNearByTalkablePawns");
                                        if (getNearbyMethod != null)
                                        {
                                            // DetectionType enum is likely in RimTalk.Util.NearbyKind or similar, but we can pass null for the second pawn and 0 for DetectionType (usually Default/Any)
                                            // Let's try to find the DetectionType enum
                                            var detectionTypeEnum = AccessTools.TypeByName("RimTalk.Util.NearbyKind");
                                            object detectionTypeValue = detectionTypeEnum != null ? Enum.ToObject(detectionTypeEnum, 0) : 0;
                                            
                                            var nearbyPawns = getNearbyMethod.Invoke(null, new object[] { pawn, null, detectionTypeValue }) as IEnumerable<Pawn>;
                                            if (nearbyPawns != null && nearbyPawns.Any())
                                            {
                                                recipient = nearbyPawns.RandomElement();
                                            }
                                        }
                                    }
                                    
                                    // Fallback if PawnSelector fails or isn't found
                                    if (recipient == null && pawn.Map != null)
                                    {
                                        // Simple distance check (e.g., within 10 cells and in the same room)
                                        Room pawnRoom = pawn.GetRoom();
                                        recipient = pawn.Map.mapPawns.AllPawnsSpawned
                                            .Where(p => p != pawn && p.RaceProps.Humanlike && !p.Dead && !p.Downed && 
                                                        p.Position.DistanceTo(pawn.Position) <= 10f && 
                                                        p.GetRoom() == pawnRoom &&
                                                        GenSight.LineOfSight(pawn.Position, p.Position, pawn.Map))
                                            .RandomElementWithFallback();
                                    }
                                    
                                    if (recipient == null)
                                    {
                                        Messages.Message($"Failed to initiate conversation for {pawn.NameShortColored}. No valid recipient found.", pawn, MessageTypeDefOf.RejectInput);
                                        return;
                                    }

                                    object talkRequest = Activator.CreateInstance(talkRequestType, new object[] { "User initiated talk", pawn, recipient, talkTypeValue });
                                    
                                    if (talkRequest != null)
                                    {
                                        var generateMethod = AccessTools.Method(talkServiceType, "GenerateAndProcessTalkAsync", new[] { talkRequestType });
                                        if (generateMethod != null)
                                        {
                                            generateMethod.Invoke(instance, new object[] { talkRequest });
                                            Messages.Message($"{pawn.NameShortColored} is initiating a conversation with {recipient.NameShortColored}.", pawn, MessageTypeDefOf.NeutralEvent);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                Messages.Message($"Failed to initiate conversation for {pawn.NameShortColored}. RimTalk method not found.", pawn, MessageTypeDefOf.RejectInput);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimTalk Extended Variables] Error initiating talk: {ex}");
                Messages.Message($"Error initiating conversation for {pawn.NameShortColored}. See log for details.", pawn, MessageTypeDefOf.RejectInput);
            }
        }
    }
}
