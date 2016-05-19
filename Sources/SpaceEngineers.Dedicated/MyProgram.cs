#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using SpaceEngineers.Game;
using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using VRage.Dedicated;
using VRage.Game;
using VRage.Game.SessionComponents;

#endregion

namespace SpaceEngineersDedicated
{
    static class MyProgram
    {
        //  Main method
        [STAThread]
        static void Main(string[] args)
        {
            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();

            MyPerGameSettings.SendLogToKeen = DedicatedServer.SendLogToKeen;

            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;
            MyPerServerSettings.GameDSName = MyPerServerSettings.GameNameSafe + "Dedicated";
            MyPerServerSettings.GameDSDescription = "Your place for space engineering, destruction and exploring.";

            MySessionComponentExtDebug.ForceDisable = true;

            MyPerServerSettings.AppId = 244850;

            ConfigForm<MyObjectBuilder_SessionSettings>.LogoImage = SpaceEngineersDedicated.Properties.Resources.SpaceEngineersDSLogo;
            ConfigForm<MyObjectBuilder_SessionSettings>.GameAttributes = Game.SpaceEngineers;
            ConfigForm<MyObjectBuilder_SessionSettings>.OnReset = delegate
            {
                SpaceEngineersGame.SetupBasicGameInfo();
                SpaceEngineersGame.SetupPerGameSettings();
            };
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;

            DedicatedServer.Run<MyObjectBuilder_SessionSettings>(args);
        }
    }

    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        protected ServiceInstaller m_serviceInstaller;

        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsServiceInstaller()
        {
            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();

            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            m_serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;



            m_serviceInstaller.DisplayName = MyPerServerSettings.GameName + " dedicated server";
            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            m_serviceInstaller.ServiceName = m_serviceInstaller.DisplayName;
            m_serviceInstaller.Description = MyPerServerSettings.GameDSDescription;

            this.Installers.Add(m_serviceInstaller);

            //# Service Information            
            m_serviceInstaller.StartType = ServiceStartMode.Automatic;
            this.Installers.Add(serviceProcessInstaller);
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            RetrieveServiceName();
            base.Install(stateSaver);
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            RetrieveServiceName();
            base.Uninstall(savedState);
        }

        private void RetrieveServiceName()
        {
            var serviceName = Context.Parameters["servicename"];
            if (!string.IsNullOrEmpty(serviceName))
            {
                this.m_serviceInstaller.ServiceName = serviceName;
                this.m_serviceInstaller.DisplayName = serviceName;
            }
        }
    }
}