using EasyHook;
using OpenGL;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;

namespace GLHook
{
    public class EntryPoint : IEntryPoint
    {

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr DwglSwapBuffers(IntPtr hdc);

        [DllImport("opengl32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr wglSwapBuffers(IntPtr hdc);

        readonly InjectorInterface ipc;
        LocalHook CreateBufferHook;

        DateTime OldDateTime = DateTime.Now;
        bool NeedTakeScreen;

        EntryPointParameters parameters;

        public EntryPoint(RemoteHooking.IContext InContext, string InChannelName, EntryPointParameters _parameter)
        {
            ipc = RemoteHooking.IpcConnectClient<InjectorInterface>(InChannelName);
            this.parameters = _parameter;
        }

        public void Run(RemoteHooking.IContext InContext, string inChannelName, EntryPointParameters _parameter)
        {

//#if DEBUG
//            // Instant launch debugger on debug build (does cause crash when the process is not already running)
//            Debugger.Launch();
//#endif

            if (inChannelName == null) throw new ArgumentNullException(nameof(inChannelName));

            try
            {
                BeginHook();
                ipc.IsInstalled(RemoteHooking.GetCurrentProcessId());
                RemoteHooking.WakeUpProcess();
            }
            catch (Exception extInfo)
            {
                ipc.ReportException(extInfo);
                return;
            }

            while (isAlive(_parameter.HostProcessId))
            {
                if (DateTime.Now > OldDateTime + new TimeSpan(0, 0, 2))
                {
                    NeedTakeScreen = true;
                }
                ipc.Ping();

                Thread.Sleep(100);
            }

            EndHook();

        }

        bool isAlive(int id)
        {
            return Process.GetProcesses().Any(x => x.Id == id);
        }

        void BeginHook()
        {
            CreateBufferHook = LocalHook.Create(LocalHook.GetProcAddress("opengl32.dll", "wglSwapBuffers"), new DwglSwapBuffers(SwapBuffers_Hooked), this);
            CreateBufferHook.ThreadACL.SetExclusiveACL(new[] { 0 });
        }

        void EndHook()
        {
            CreateBufferHook?.Dispose();
            LocalHook.Release();
        }

        IntPtr SwapBuffers_Hooked(IntPtr hdc)
        {

            //#if DEBUG
            //            // Instant launch debugger on debug build (does cause crash when the process is not already running)
            //            Debugger.Launch();
            //#endif

            EntryPoint This = (EntryPoint)HookRuntimeInfo.Callback;
            try
            {
                if (NeedTakeScreen)
                {
                    NeedTakeScreen = false;
                    OldDateTime = DateTime.Now;
                }

                var bitmap = new Bitmap(parameters.ScreenWidth, parameters.ScreenHeight);
                Rectangle bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bmpData = bitmap.LockBits(bounds, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                Gl.ReadBuffer(ReadBufferMode.Front);
                Gl.ReadPixels(0, 0, bounds.Width, bounds.Height, OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
                bitmap.UnlockBits(bmpData);
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    ipc.RecvFrame(ms.ToArray());
                }

            }
            catch (Exception ex)
            {
                This.ipc.ReportException(ex);
            }
            return wglSwapBuffers(hdc);
        }
    }
}
