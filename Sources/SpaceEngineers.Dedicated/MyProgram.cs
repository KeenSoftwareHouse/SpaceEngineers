#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using SpaceEngineers.Game;
using System;
using System.ComponentModel;
using VRage.Dedicated;

#endregion

namespace SpaceEngineersDedicated
{
    static class MyProgram
    {
        //  Main method
        [STAThread]
        static void Main(string[] args)
        {
            SpaceEngineersGame.SetupPerGameSettings();

            MyPerGameSettings.SendLogToKeen = DedicatedServer.SendLogToKeen;

            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;
            MyPerServerSettings.GameDSName = MyPerServerSettings.GameNameSafe + "Dedicated";
            MyPerServerSettings.GameDSDescription = "Your place for space engineering, destruction and exploring.";


            MyPerServerSettings.AppId = 244850;

            ConfigForm<MyObjectBuilder_SessionSettings>.LogoImage = SpaceEngineersDedicated.Properties.Resources.SpaceEngineersDSLogo;
            ConfigForm<MyObjectBuilder_SessionSettings>.GameAttributes = Game.SpaceEngineers;
            ConfigForm<MyObjectBuilder_SessionSettings>.OnReset = delegate
            {
                SpaceEngineersGame.SetupPerGameSettings();
            };


            DedicatedServer.Run<MyObjectBuilder_SessionSettings>(args);
        }
    }

    [RunInstaller(true)]
    public class WindowsServiceInstaller : WindowsServiceInstallerBase
    {
        public WindowsServiceInstaller()
        {
            SpaceEngineersGame.SetupPerGameSettings();

            m_serviceInstaller.DisplayName = MyPerServerSettings.GameName + " dedicated server";
            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            m_serviceInstaller.ServiceName = m_serviceInstaller.DisplayName;
            m_serviceInstaller.Description = MyPerServerSettings.GameDSDescription;

            this.Installers.Add(m_serviceInstaller);
        }
    }
}