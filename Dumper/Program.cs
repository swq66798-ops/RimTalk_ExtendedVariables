using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main()
    {
        try
        {
            var module = ModuleDefinition.ReadModule(@"d:\dev\3551203752\1.5\Assemblies\RimTalk.dll");
            var type = module.Types.FirstOrDefault(t => t.FullName == "RimTalk.Service.ContextBuilder");
            if (type != null)
            {
                var cctor = type.Methods.FirstOrDefault(m => m.Name == ".cctor");
                if (cctor != null && cctor.HasBody)
                {
                    foreach (var instr in cctor.Body.Instructions)
                    {
                        Console.WriteLine(instr.ToString());
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