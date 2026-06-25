using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimTalk.Prompt;

namespace RimTalk_ExtendedVariables.Patches
{
    [HarmonyPatch(typeof(ScribanParser), "Render")]
    public static class ScribanParser_Render_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var pushGlobalMethod = AccessTools.Method(AccessTools.TypeByName("Scriban.TemplateContext"), "PushGlobal");
            
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is System.Reflection.MethodInfo mi && mi == pushGlobalMethod)
                {
                    // Stack before PushGlobal: [TemplateContext] [IScriptObject]
                    // We insert a call to ModifyScriptObject which takes IScriptObject and returns IScriptObject
                    codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScribanParser_Render_Patch), nameof(ModifyScriptObject))));
                    i++;
                }
            }
            return codes;
        }

        public static object ModifyScriptObject(object scriptObject)
        {
            try
            {
                var importMethod = AccessTools.Method(AccessTools.TypeByName("Scriban.Runtime.ScriptObjectExtensions"), "Import", new Type[] { AccessTools.TypeByName("Scriban.Runtime.IScriptObject"), typeof(string), typeof(Delegate) });
                if (importMethod != null)
                {
                    importMethod.Invoke(null, new object[] { scriptObject, "count_by_def", new Func<string, int>(RimTalk_ExtendedVariables_Mod.GetCountByDef) });
                }
            }
            catch (Exception ex)
            {
                Verse.Log.Error("[RimTalk Extended Variables] Error injecting count_by_def: " + ex.ToString());
            }
            return scriptObject;
        }
    }
}
