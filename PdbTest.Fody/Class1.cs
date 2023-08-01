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

namespace PdbTest.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        public override void Execute()
        {
            Debugger.Launch();
            var mys = new MySReader(ModuleDefinition.SymbolReader);

            var type = ModuleDefinition.GetType();
            // Get the PropertyInfo for the property "symbol_reader"
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
            return existing.GetWriterProvider();
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
            return existing.GetDebugHeader();
        }
        public ISymbolReaderProvider GetReaderProvider()
        {
            return existing.GetReaderProvider();
        }
        public void Write(MethodDebugInformation info)
        {

        }
        public void Write()
        {

        }
    }
}
