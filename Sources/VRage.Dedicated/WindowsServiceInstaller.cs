using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace VRage.Dedicated
{
    public class WindowsServiceInstallerBase : Installer
    {
        protected ServiceInstaller m_serviceInstaller;

        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsServiceInstallerBase()
        {
            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            m_serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

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