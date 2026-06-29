using System;
using System.Reflection;
using System.Linq;

class DumpWhoShotMyLegOffProgram
{
    static void Main2()
    {
        try
        {
            var assembly = Assembly.LoadFrom(@"E:\SteamLibrary\steamapps\workshop\content\294100\3491552121\Assemblies\WhoShotMyLegOff.dll");
            foreach (var type in assembly.GetTypes())
            {
                Console.WriteLine($"Type: {type.FullName}");
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine($"  Method: {method.Name}");
                }
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine($"  Field: {field.Name}");
                }
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Console.WriteLine($"  Property: {prop.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
