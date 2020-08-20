using EasyHook;
using GLHook;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Principal;
using System.Windows.Forms;

namespace Cheater
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var TargetProcess = Process.GetProcessesByName("dnplayer").First();
            var libPath = Application.StartupPath + "\\GLHook.dll";

            var ret = InjectPayload(TargetProcess, libPath);

            Add_Log("Init = " + ret);

            InjectorInterface.isInstalled += InjectorInterface_isInstalled;
            InjectorInterface.Error += InjectorInterface_Error;
            InjectorInterface.recvFrame += InjectorInterface_recvFrame;
        }

        private void InjectorInterface_recvFrame(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var bmp = new Bitmap(ms);

                pic.BeginInvoke(new Action(() =>
                {
                    pic.Image?.Dispose();
                    pic.Image = bmp;
                }));
            }
        }

        private void InjectorInterface_Error(Exception obj)
        {
            Add_Log("Error : " + obj.Message);
        }

        private void InjectorInterface_isInstalled(int obj)
        {
            Add_Log("Injected Moudle");
        }

        void Add_Log(string msg)
        {
            lst_Log.BeginInvoke(new Action(() => { lst_Log.Items.Add(msg); }));
        }

        bool InjectPayload(Process proc, string injectionLibrary)
        {
            string channelName = null;

            RemoteHooking.IpcCreateServer<InjectorInterface>(ref channelName, WellKnownObjectMode.Singleton);

            var parameter = new EntryPointParameters
            {
                Message = "Test Message",
                HostProcessId = RemoteHooking.GetCurrentProcessId(),
                ScreenWidth = 640,
                ScreenHeight = 480
            };

            try
            {
                RemoteHooking.Inject(
                    proc.Id,
                    InjectionOptions.Default | InjectionOptions.DoNotRequireStrongName,
                    injectionLibrary,
                    injectionLibrary,
                    channelName,
                    parameter
                );
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
