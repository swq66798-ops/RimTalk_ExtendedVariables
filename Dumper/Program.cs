using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static void Main()
    {
        try
        {
            var module = ModuleDefinition.ReadModule(@"d:\dev\3551203752\1.5\Assemblies\Assembly-CSharp.dll");
            var type = module.Types.FirstOrDefault(t => t.Name == "HealthCardUtility");
            if (type != null)
            {
                foreach (var method in type.Methods.Where(m => m.Name.Contains("VisibleHediffs")))
                {
                    Console.WriteLine($"Method: {method.Name}, IsPublic: {method.IsPublic}, IsStatic: {method.IsStatic}");
                    foreach (var p in method.Parameters)
                    {
                        Console.WriteLine($"  Param: {p.Name} ({p.ParameterType.Name})");
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