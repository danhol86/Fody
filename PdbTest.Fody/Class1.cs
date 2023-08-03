using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static InnerWeaver;

namespace PdbTest.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        public ModuleWeaver()
        {
        }

        public override void Execute()
        {
            var myp = ModuleDefinition.FileName;
            var csum = CalculateMD5Checksum(myp);
            var newstr = @"C:\CS\" + csum + @"\";

            var dictmap = new Dictionary<string, string> { { ProjectDirectoryPath, newstr } };

            var mys = new MySReader(ModuleDefinition.SymbolReader, dictmap);

            var type = ModuleDefinition.GetType();
            var propertyInfo = type.GetField("symbol_reader", BindingFlags.NonPublic | BindingFlags.Instance);
            //propertyInfo.SetValue(ModuleDefinition, mys);

            var assembly = (AssemblyDefinition)type.GetField("assembly", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ModuleDefinition);
            var stringType = assembly.MainModule.TypeSystem.String;
            var attribute = new CustomAttribute(assembly.MainModule.ImportReference(typeof(System.Reflection.AssemblyMetadataAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) })));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, "PreFodyHash"));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, csum));
            assembly.CustomAttributes.Add(attribute);
            //assembly.Write();
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        public static string CalculateMD5Checksum(byte[] filebyte)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(filebyte);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string CalculateMD5Checksum(string filePath)
        {
            using (var sha256 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }
    }

    public class MySReader : ISymbolReader
    {
        ISymbolReader existing;
        Dictionary<string, string> ProjectDirectoryPath;
        public MySReader(ISymbolReader existing, Dictionary<string, string> ProjectDirectoryPath)
        {
            this.existing = existing;
            this.ProjectDirectoryPath = ProjectDirectoryPath;
        }

        public void Dispose()
        {
            existing.Dispose();
        }

        public ISymbolWriterProvider GetWriterProvider()
        {
            return new MyWriterProvider(existing.GetWriterProvider(), ProjectDirectoryPath);
        }
        public bool ProcessDebugHeader(ImageDebugHeader header)
        {
            return existing.ProcessDebugHeader(header);
        }

        public MethodDebugInformation Read(MethodDefinition method)
        {
            return existing.Read(method);
        }
    }

    public class MyWriterProvider : ISymbolWriterProvider
    {
        ISymbolWriterProvider existing;
        Dictionary<string, string> ProjectDirectoryPath;
        public MyWriterProvider(ISymbolWriterProvider existing, Dictionary<string, string> ProjectDirectoryPath)
        {
            this.ProjectDirectoryPath = ProjectDirectoryPath;
            this.existing = existing;
        }

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
        {
            return new MyWriter(existing.GetSymbolWriter(module, fileName), ProjectDirectoryPath);
        }
        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
        {
            return new MyWriter(existing.GetSymbolWriter(module, symbolStream), ProjectDirectoryPath);
        }
    }

    public class MyWriter : ISymbolWriter
    {
        ISymbolWriter existing;
        Dictionary<string, string> ProjectDirectoryPath;
        
        public MyWriter(ISymbolWriter existing, Dictionary<string, string> ProjectDirectoryPath)
        {
            this.existing = existing;
            this.ProjectDirectoryPath = ProjectDirectoryPath;
        }

        public void Dispose()
        {
            existing.Dispose();
        }
        public ImageDebugHeader GetDebugHeader()
        {
            var dheader = existing.GetDebugHeader();

            var mydata = dheader.Entries.First();
            var d = mydata.Data;

            var str = Encoding.Default.GetString(d);

            foreach (var item in ProjectDirectoryPath)
            {
                var find = Encoding.UTF8.GetBytes(item.Key);
                var replace = Encoding.UTF8.GetBytes(item.Value);
                d = ReplaceInByteArray(d, find, replace);
            }            

            var header = new ImageDebugHeader(new ImageDebugHeaderEntry(mydata.Directory, d));

            return header;
        }

        public static byte[] ReplaceInByteArray(byte[] source, byte[] find, byte[] replace)
        {
            var result = new List<byte>();
            for (var i = 0; i < source.Length; ++i)
            {
                if (IsMatch(source, i, find))
                {
                    foreach (var b in replace)
                    {
                        result.Add(b);
                    }
                    i += find.Length - 1;
                }
                else
                {
                    result.Add(source[i]);
                }
            }
            return result.ToArray();
        }

        public static bool IsMatch(byte[] source, int position, byte[] find)
        {
            if (find.Length > (source.Length - position))
                return false;

            for (var i = 0; i < find.Length; i++)
                if (source[position + i] != find[i])
                    return false;

            return true;
        }

        public ISymbolReaderProvider GetReaderProvider()
        {
            return existing.GetReaderProvider();
        }
        public void Write(MethodDebugInformation info)
        {
            existing.Write(info);
        }
        public void Write()
        {
            existing.Write();
        }
    }

}
