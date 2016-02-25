using Sandbox;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VRage.Dedicated.Configurator;
using VRage.FileSystem;
using VRage.Game;

namespace VRage.Dedicated
{
    public static class MyConfigurator
    {
        //internal static readonly string AppName = "SpaceEngineersDedicated";


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        public static void Start<T>() where T : MyObjectBuilder_SessionSettings, new()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SelectInstanceForm selectionForm;
            ConfigForm<T> configForm;

            var isService = false;
            var serviceName = "";
            var serviceData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), MyPerServerSettings.GameDSName);
            var contentPath = Path.Combine(new FileInfo(MyFileSystem.ExePath).Directory.FullName, "Content");

            do
            {
                selectionForm = new SelectInstanceForm(serviceData, MyPerServerSettings.GameDSName + ".exe");
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

                        MyFileSystem.Init(contentPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MyPerServerSettings.GameDSName));
                        MyFileSystem.InitUserSpecific(null);
                    }
                }
                else
                {
                    break;
                }

                MySandboxGame.Config = new MyConfig(MyPerServerSettings.GameNameSafe + ".cfg");
                MySandboxGame.ConfigDedicated = new MyConfigDedicated<T>(MyPerServerSettings.GameNameSafe + "-Dedicated.cfg");

                configForm = new ConfigForm<T>(isService, serviceName);
                Application.Run(configForm);
            }
            while (configForm.HasToExit);
        }
    }
}
