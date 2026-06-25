using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string dllPath = args[0];
            string typeName = args[1];
            var module = ModuleDefinition.ReadModule(dllPath);
            var type = module.Types.FirstOrDefault(t => t.FullName == typeName);
            if (type != null)
            {
                var method = type.Methods.FirstOrDefault(m => m.Name == "Render");
                if (method != null && method.HasBody)
                {
                    Console.WriteLine($"Instructions in {method.Name}:");
                    foreach (var inst in method.Body.Instructions)
                    {
                        if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                        {
                            var methodRef = inst.Operand as MethodReference;
                            if (methodRef != null && methodRef.DeclaringType.FullName == "Scriban.Template" && methodRef.Name == "Render")
                            {
                                Console.WriteLine($"Render call: {inst.Previous.Previous.Previous.Previous.Operand} -> {inst.Previous.Previous.Previous.Operand}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}