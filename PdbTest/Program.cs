using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace PdbTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var mypath = typeof(Program).Assembly.Location;
            
            var assembly = Assembly.LoadFile(mypath);
            var assemblyAttributes = assembly.GetCustomAttributes(false);

            foreach (var attribute in assemblyAttributes)
            {
                if (attribute is AssemblyMetadataAttribute)
                {
                    var metadataAttribute = (AssemblyMetadataAttribute)attribute;

                    Console.WriteLine($"Key: {metadataAttribute.Key}, Value: {metadataAttribute.Value}");
                }
            }

            var tem123 = "Hello there!";
            Console.WriteLine(tem123);
            Console.ReadLine();
        }


    }
}
