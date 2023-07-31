using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fody;

namespace PdbTest.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        public override void Execute()
        {

        }
        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }
    }
}
