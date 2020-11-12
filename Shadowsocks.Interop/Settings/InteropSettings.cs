using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Interop.Settings
{
    public class InteropSettings
    {
        public string SsRustPath { get; set; }
        public string V2RayCorePath { get; set; }

        public InteropSettings()
        {
            SsRustPath = "";
            V2RayCorePath = "";
        }
    }
}
