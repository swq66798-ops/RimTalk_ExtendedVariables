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
            var type = module.Types.FirstOrDefault(t => t.Name == "Pawn_GuestTracker");
            if (type != null)
            {
                foreach (var prop in type.Properties)
                {
                    Console.WriteLine($"Property: {prop.Name} ({prop.PropertyType.Name})");
                }
                foreach (var field in type.Fields)
                {
                    Console.WriteLine($"Field: {field.Name} ({field.FieldType.Name})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}