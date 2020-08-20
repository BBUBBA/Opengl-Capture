using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLHook
{
    [Serializable]
    public sealed class EntryPointParameters
    {
        public string Message;
        public int HostProcessId;

        public int ScreenWidth;
        public int ScreenHeight;
    }
}
