using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using HarmonyLib;
using RimTalk.API;

namespace RimTalk_ExtendedVariables
{
    [StaticConstructorOnStartup]
    public static class RimTalk_ExtendedVariables_Mod
    {
        private static System.Reflection.MethodInfo visibleHediffsMethod;

        static RimTalk_ExtendedVariables_Mod()
        {
            Log.Message("[RimTalk Extended Variables] Initializing...");
            visibleHediffsMethod = AccessTools.Method(typeof(HealthCardUtility), "VisibleHediffs");
            
            try
            {
                RimTalkPromptAPI.RegisterPawnVariable(
                    "cj.rimtalk.extendedvariables",
                    "extended_health_details",
                    GetPawnHealthDetails,
                    "Detailed health information including injury causes and instigators.",
                    0
                );
                Log.Message("[RimTalk Extended Variables] Successfully registered 'extended_health_details' variable.");

                RimTalkPromptAPI.RegisterPawnVariable(
                    "cj.rimtalk.extendedvariables",
                    "extended_surroundings",
                    GetPawnSurroundings,
                    "Detailed surroundings including items on the ground and specific corpse names.",
                    0
                );
                Log.Message("[RimTalk Extended Variables] Successfully registered 'extended_surroundings' variable.");

                RimTalkPromptAPI.RegisterPawnVariable(
                    "cj.rimtalk.extendedvariables",
                    "extended_captive_status",
                    GetExtendedCaptiveStatus,
                    "Detailed captive status including resistance, will, and unwavering loyalty.",
                    0
                );
                Log.Message("[RimTalk Extended Variables] Successfully registered 'extended_captive_status' variable.");

                RimTalkPromptAPI.RegisterPawnVariable(
                    "cj.rimtalk.extendedvariables",
                    "extended_social_relations",
                    GetExtendedSocialRelations,
                    "Detailed social relations including opinion and custom relation labels.",
                    0
                );
                Log.Message("[RimTalk Extended Variables] Successfully registered 'extended_social_relations' variable.");

                RimTalkPromptAPI.RegisterPawnVariable(
                    "cj.rimtalk.extendedvariables",
                    "extended_pain_level",
                    GetPawnPainLevel,
                    "Current pain level of the pawn.",
                    0
                );
                Log.Message("[RimTalk Extended Variables] Successfully registered 'extended_pain_level' variable.");
            }
            catch (Exception ex)
            {
                Log.Error("[RimTalk Extended Variables] Failed to register variable: " + ex.ToString());
            }
        }

        private static string GetPawnHealthDetails(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
                return "";

            StringBuilder sb = new StringBuilder();
            
            // Use RimWorld's VisibleHediffs to properly hide replaced parts
            IEnumerable<Hediff> hediffs = null;
            if (visibleHediffsMethod != null)
            {
                try
                {
                    hediffs = (IEnumerable<Hediff>)visibleHediffsMethod.Invoke(null, new object[] { pawn, false });
                }
                catch { }
            }
            
            if (hediffs == null)
            {
                hediffs = pawn.health.hediffSet.hediffs.Where(h => h.Visible);
            }
            
            bool hasHediffs = false;
            foreach (var hediff in hediffs)
            {
                hasHediffs = true;
                string part = hediff.Part != null ? $"({hediff.Part.Label})" : "";
                string severity = $"Severity:{(int)(hediff.Severity * 100)}%";
                string pain = hediff.PainOffset > 0 ? $", Pain:{hediff.PainOffset.ToStringPercent()}" : "";
                
                string cause = "";
                if (hediff is Hediff_Injury injury)
                {
                    string injuryCause = GetInjuryCause(injury);
                    if (!string.IsNullOrEmpty(injuryCause))
                    {
                        cause = $"，{injuryCause}";
                    }
                }
                else if (hediff is Hediff_MissingPart missingPart)
                {
                    string missingCause = GetMissingPartCause(missingPart);
                    if (!string.IsNullOrEmpty(missingCause))
                    {
                        cause = $"，{missingCause}";
                    }
                }
                
                sb.AppendLine($"- {hediff.Label}{part}, {severity}{pain}{cause}");
            }

            if (hasHediffs)
            {
                return "Health:\n" + sb.ToString().TrimEnd();
            }
            
            return "";
        }

        private static string GetInjuryCause(Hediff_Injury injury)
        {
            LogEntry logEntry = injury.combatLogEntry?.Target;
            if (logEntry != null)
            {
                return logEntry.ToGameStringFromPOV(null);
            }
            return null;
        }

        private static string GetMissingPartCause(Hediff_MissingPart missingPart)
        {
            LogEntry logEntry = missingPart.combatLogEntry?.Target;
            if (logEntry != null)
            {
                return logEntry.ToGameStringFromPOV(null);
            }
            return null;
        }

        private static string GetPawnSurroundings(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return "";

            int radius = 4;
            var cells = GenRadial.RadialCellsAround(pawn.Position, radius, true).ToList();
            
            Dictionary<string, int> buildings = new Dictionary<string, int>();
            Dictionary<string, int> items = new Dictionary<string, int>();
            Dictionary<string, int> plants = new Dictionary<string, int>();
            Dictionary<string, int> animals = new Dictionary<string, int>();
            Dictionary<string, int> pawns = new Dictionary<string, int>();
            Dictionary<string, int> corpses = new Dictionary<string, int>();
            Dictionary<string, int> filth = new Dictionary<string, int>();

            HashSet<int> seenThings = new HashSet<int>();

            Room pawnRoom = pawn.GetRoom();
            bool isOutdoors = pawnRoom == null || pawnRoom.TouchesMapEdge;

            foreach (var cell in cells)
            {
                if (!cell.InBounds(pawn.Map)) continue;
                if (!isOutdoors && cell.GetRoom(pawn.Map) != pawnRoom) continue;
                if (!GenSight.LineOfSight(pawn.Position, cell, pawn.Map, true)) continue;

                var thingList = cell.GetThingList(pawn.Map);
                foreach (var thing in thingList)
                {
                    if (thing == pawn) continue;
                    if (thing.def == null) continue;
                    if (thing.Destroyed) continue;
                    if (Find.HiddenItemsManager != null && Find.HiddenItemsManager.Hidden(thing.def)) continue;

                    if (!seenThings.Add(thing.thingIDNumber)) continue;

                    if (thing is Corpse corpse)
                    {
                        string name = corpse.InnerPawn?.Name?.ToStringShort ?? corpse.InnerPawn?.LabelShort ?? corpse.def.LabelCap;
                        AddCount(corpses, name, 1);
                        continue;
                    }

                    if (thing is Pawn p)
                    {
                        if (p.Dead) continue;
                        if (p.RaceProps.Animal)
                        {
                            AddCount(animals, p.def.LabelCap, 1);
                        }
                        else
                        {
                            AddCount(pawns, p.Name?.ToStringShort ?? p.LabelShort, 1);
                        }
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Building)
                    {
                        if (thing.def.defName == "Wall" || thing.def.defName.Contains("Wall")) continue;
                        if (thing.def.altitudeLayer == AltitudeLayer.Conduits) continue;
                        if (thing.def.defName.ToLower().Contains("conduit") || thing.def.defName.ToLower().Contains("pipe") || thing.def.defName.ToLower().Contains("wire")) continue;
                        
                        AddCount(buildings, thing.def.LabelCap, 1);
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        AddCount(items, thing.def.LabelCap, thing.stackCount);
                    }
                    else if (thing.def.category == ThingCategory.Plant)
                    {
                        AddCount(plants, thing.def.LabelCap, 1);
                    }
                    else if (thing.def.IsFilth)
                    {
                        AddCount(filth, thing.def.LabelCap, 1);
                    }
                }
            }

            List<string> groups = new List<string>();
            AddGroup(groups, "Buildings", buildings);
            AddGroup(groups, "Items", items);
            AddGroup(groups, "Plants", plants);
            AddGroup(groups, "Animals", animals);
            AddGroup(groups, "Pawns", pawns);
            AddGroup(groups, "Corpses", corpses);
            AddGroup(groups, "Filth", filth);

            if (groups.Count > 0)
            {
                return "Surroundings:\n" + string.Join("\n", groups);
            }

            return "";
        }

        private static void AddCount(Dictionary<string, int> dict, string key, int count)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (dict.ContainsKey(key))
                dict[key] += count;
            else
                dict[key] = count;
        }

        private static void AddGroup(List<string> groups, string label, Dictionary<string, int> dict)
        {
            if (dict.Count == 0) return;
            var items = dict.OrderByDescending(kv => kv.Value).Select(kv => kv.Value > 1 ? $"{kv.Key} ×{kv.Value}" : kv.Key);
            groups.Add($"{label}: {string.Join(", ", items)}");
        }

        private static string GetExtendedCaptiveStatus(Pawn pawn)
        {
            if (pawn == null || pawn.guest == null || !pawn.IsPrisoner)
                return "";

            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Resistance: {pawn.guest.Resistance}");
            
            if (ModsConfig.IdeologyActive)
            {
                sb.AppendLine($"Will: {pawn.guest.will}");
            }
            
            if (pawn.guest.Recruitable == false)
            {
                sb.AppendLine("Unwaveringly Loyal: Yes (Cannot be recruited)");
            }
            else
            {
                sb.AppendLine("Unwaveringly Loyal: No");
            }

            return sb.ToString().TrimEnd();
        }

        private static string GetPawnPainLevel(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
                return "";

            return pawn.health.hediffSet.PainTotal.ToStringPercent();
        }

        private static string GetExtendedSocialRelations(Pawn pawn)
        {
            if (pawn == null || pawn.relations == null || pawn.Map == null)
                return "";

            StringBuilder sb = new StringBuilder();
            bool hasRelations = false;

            // Get pawns currently in conversation
            HashSet<Pawn> conversationPawns = new HashSet<Pawn>();
            try
            {
                var context = RimTalk.Prompt.PromptManager.LastContext;
                if (context != null && context.Pawns != null)
                {
                    foreach (var p in context.Pawns)
                    {
                        if (p != pawn && p is Pawn pawnObj)
                        {
                            conversationPawns.Add(pawnObj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[RimTalk Extended Variables] Failed to get active conversations: " + ex.Message);
            }

            foreach (Pawn other in pawn.Map.mapPawns.AllPawns)
            {
                if (other == pawn || !other.RaceProps.Humanlike || other.Dead) continue;

                bool isKin = pawn.relations.FamilyByBlood.Contains(other);
                bool inConversation = conversationPawns.Contains(other);

                // Only show if they are kin or in the same conversation
                if (!isKin && !inConversation) continue;

                int opinion = pawn.relations.OpinionOf(other);

                string relationLabel = "";
                if (!isKin)
                {
                    if (opinion > 20) relationLabel = "朋友";
                    else if (opinion < -20) relationLabel = "仇人";
                    else relationLabel = "相识";
                }
                else
                {
                    if (opinion > 20) relationLabel = "喜爱";
                    else if (opinion < -20) relationLabel = "厌恶";
                    else relationLabel = "平淡";
                }

                sb.AppendLine($"- {other.Name?.ToStringShort ?? other.LabelShort}: {relationLabel} ({opinion})");
                hasRelations = true;
            }

            if (hasRelations)
            {
                return "Social Relations:\n" + sb.ToString().TrimEnd();
            }

            return "";
        }
    }
}
