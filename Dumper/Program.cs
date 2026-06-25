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
            string dllPath = args.Length > 0 ? args[0] : @"d:\dev\RimTalk_ExtendedVariables\RimTalk.dll";
            string typeName = args.Length > 1 ? args[1] : "RimTalk.Service.TalkService";
            
            var module = ModuleDefinition.ReadModule(dllPath);
            if (typeName == "ALL_TYPES")
            {
                foreach (var t in module.Types)
                {
                    Console.WriteLine(t.FullName);
                }
            }
            else
            {
                var type = module.Types.FirstOrDefault(t => t.FullName == typeName);
                if (type != null)
                {
                    Console.WriteLine($"Methods in {typeName}:");
                    foreach (var method in type.Methods)
                    {
                        Console.WriteLine($"- {method.Name} ({string.Join(", ", method.Parameters.Select(p => p.ParameterType.Name))})");
                    }
                    if (type.IsEnum)
                    {
                        Console.WriteLine($"\nEnum values in {typeName}:");
                        foreach (var field in type.Fields.Where(f => f.IsStatic))
                        {
                            Console.WriteLine($"- {field.Name} = {field.Constant}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Type {typeName} not found.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}