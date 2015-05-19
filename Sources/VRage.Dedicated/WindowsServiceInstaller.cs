using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace VRage.Dedicated
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        ServiceInstaller m_serviceInstaller;

        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            m_serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            m_serviceInstaller.DisplayName = MyPerServerSettings.GameName + " dedicated server";
            m_serviceInstaller.StartType = ServiceStartMode.Automatic;

            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            m_serviceInstaller.ServiceName = m_serviceInstaller.DisplayName;
            m_serviceInstaller.Description = MyPerServerSettings.GameDSDescription;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(m_serviceInstaller);
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