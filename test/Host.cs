using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
namespace test
{
    class Host : HostApplicationServices
    {
        private List<string> mUresolved = new List<string>();
        public Host()
        {
            RuntimeSystem.Initialize(this, 1033);
        }

        public override string FindFile(string fileName, Database database, FindFileHint hint)
        {
            throw new NotImplementedException();
        }
    }
}
