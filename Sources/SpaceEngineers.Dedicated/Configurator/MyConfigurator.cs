using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VRage.Dedicated.Configurator;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;

namespace DedicatedConfigurator
{
    public static class MyConfigurator
    {
        internal static readonly string AppName = "SpaceEngineersDedicated";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        public static void Start()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SelectInstanceForm selectionForm;
            ConfigForm configForm;

            var isService   = false;
            var serviceName = "";
            var serviceData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName);
            var contentPath = Path.Combine(new FileInfo(MyFileSystem.ExePath).Directory.FullName, "Content");

            do
            {
                selectionForm = new SelectInstanceForm(serviceData, "SpaceEngineersDedicated.exe");
                Application.Run(selectionForm);

                if (selectionForm.DialogResult == DialogResult.OK)
                {
                    if (selectionForm.SelectedInstance != null)
                    {
                        isService = true;
                        serviceName = selectionForm.SelectedInstance.InstanceName;

                        MyFileSystem.Init(contentPath, Path.Combine(serviceData, serviceName));
                        MyFileSystem.InitUserSpecific(null);
                    }
                    else
                    {
                        isService = false;
                        serviceName = "";

                        MyFileSystem.Init(contentPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName));
                        MyFileSystem.InitUserSpecific(null);
                    }
                }
                else
                {
                    break;
                }

                MySandboxGame.Config = new MyConfig("SpaceEngineers.cfg");
                MySandboxGame.ConfigDedicated = new MyConfigDedicated<MyObjectBuilder_SessionSettings>("SpaceEngineers-Dedicated.cfg");
                configForm = new ConfigForm(isService, serviceName);
                Application.Run(configForm);
            }
            while (configForm.HasToExit);
        }
    }
}
