using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var mys = new MySReader(ModuleDefinition.SymbolReader);

            var type = ModuleDefinition.GetType();
            var propertyInfo = type.GetField("symbol_reader", BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo.SetValue(ModuleDefinition, mys);
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }
    }

    public class MySReader : ISymbolReader
    {
        ISymbolReader existing;
        public MySReader(ISymbolReader existing)
        {
            this.existing = existing;
        }

        public void Dispose()
        {
            existing.Dispose();
        }

        public ISymbolWriterProvider GetWriterProvider()
        {
            return new MyWriterProvider(existing.GetWriterProvider());
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
        public MyWriterProvider(ISymbolWriterProvider existing)
        {
            this.existing = existing;
        }

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
        {
            return new MyWriter(existing.GetSymbolWriter(module, fileName));
        }
        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
        {
            return new MyWriter(existing.GetSymbolWriter(module, symbolStream));
        }
    }

    public class MyWriter : ISymbolWriter
    {
        ISymbolWriter existing;

        public MyWriter(ISymbolWriter existing)
        {
            this.existing = existing;
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

            Debugger.Launch();

            var str = Encoding.Default.GetString(d);
            var find = Encoding.UTF8.GetBytes("D:\\Repos\\repos\\fody\\PdbTest");
            var replace = Encoding.UTF8.GetBytes("C");
            var result = ReplaceInByteArray(d, find, replace);

            var header = new ImageDebugHeader(new ImageDebugHeaderEntry(mydata.Directory, result));

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
