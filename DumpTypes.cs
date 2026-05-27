using System;
using System.IO;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var assembly = Assembly.LoadFrom(@"d:\dev\3551203752\1.5\Assemblies\RimTalk.dll");
            using (var writer = new StreamWriter(@"d:\dev\RimTalk_ExtendedVariables\RimTalk_Types.txt"))
            {
                foreach (var type in assembly.GetTypes())
                {
                    writer.WriteLine($"Type: {type.FullName}");
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        writer.WriteLine($"  Method: {method.Name}");
                    }
                    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        writer.WriteLine($"  Property: {prop.Name}");
                    }
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        writer.WriteLine($"  Field: {field.Name}");
                    }
                }
            }
            Console.WriteLine("Done");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}