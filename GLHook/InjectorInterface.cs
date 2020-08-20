using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLHook
{
    public class InjectorInterface : MarshalByRefObject
    {
        public static event Action<int> isInstalled;
        public static event Action isPing;
        public static event Action<Exception> Error;
        public static event Action<byte[]> recvFrame;

        public void IsInstalled(int clientPid)
        {
            isInstalled?.Invoke(clientPid);
        }

        public void Ping()
        {
            isPing?.Invoke();
        }

        public void ReportException(Exception ex)
        {
            Error?.Invoke(ex);
        }

        public void RecvFrame(byte[] bytes)
        {
            recvFrame?.Invoke(bytes);
        }

    }
}
