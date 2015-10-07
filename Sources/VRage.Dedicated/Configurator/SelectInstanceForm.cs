using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration.Install;
using System.Reflection;
using System.Diagnostics;
//using Sandbox.AppCode;
using System.Runtime.InteropServices;
using VRage.Win32;
//using VRage.Win32;
//using Sandbox;

namespace VRage.Dedicated.Configurator
{
    public partial class SelectInstanceForm : Form
    {
        public class Instance
        {
            public ServiceController Controller;
            public string InstanceName;
            public string InstancePath;

            public override string ToString()
            {
                return InstanceName;
            }
        }

        public Instance SelectedInstance { get; private set; }

        readonly string LocalConsoleName = "Local / Console";
        
        string m_serviceRootPath;
        string m_serviceExeName;

        bool m_isAdmin;

        AddNewInstanceForm m_addNewInstanceForm;

        public SelectInstanceForm(string servicePath, string serviceExe)
        {
            m_serviceRootPath = servicePath;
            m_serviceExeName = serviceExe;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectedInstance = (Instance)listBox1.SelectedItem;
            if (SelectedInstance != null && SelectedInstance.Controller == null)
                SelectedInstance = null;

            if (SelectedInstance != null && !m_isAdmin)
            {
                System.Windows.Forms.MessageBox.Show(
                    "You do not have the administrator rights, please run this app as admin!",
                    "Invalid operation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        private string GetServicePath(string serviceName)
        {
            try
            {
                string registryPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;
                RegistryKey keyHKLM = Registry.LocalMachine;

                RegistryKey key;
                using (key = keyHKLM.OpenSubKey(registryPath))
                {
                    string value = key.GetValue("ImagePath").ToString();
                    value = value.Replace("\"", "");
                    return Environment.ExpandEnvironmentVariables(value);
                }
            }
            catch
            {
                return String.Empty;
            }
        }

        private void SelectInstanceForm_Load(object sender, EventArgs e)
        {
            Text = MyPerServerSettings.GameName + " - Select Instance of Dedicated server";

            addDateToFilename.Checked = DedicatedServer.AddDateToLog;
            sendLogFiles.Checked = DedicatedServer.SendLogToKeen;

            ReloadServices();

            // hide run as admin button
            runAsAdmin.Image = GetShieldIcon();
            runAsAdmin.Hide();

            // hide text of labels
            globalInfo.Text = "";
            info.Text = "";
            
            // check whether user is admin or not
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            m_isAdmin = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!m_isAdmin)
            {
                info.Text = "Service management not available, please run as admin!";

                addDateToFilename.Enabled = false;
                sendLogFiles.Enabled = false;

                saveConfig.Enabled = false;

                addNewInstance.Enabled = false;
                remove.Enabled = false;

                runAsAdmin.Show();
            }
        }

        private void ReloadServices()
        {
            listBox1.Items.Clear();

            Instance local = new Instance();
            local.InstanceName = LocalConsoleName;
            listBox1.Items.Add(local);

            foreach (var s in ServiceController.GetServices())
            {
                try
                {
                    string path = GetServicePath(s.ServiceName);
                    if (path.IndexOf(m_serviceExeName, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        var i = new Instance();
                        i.InstanceName = s.ServiceName;
                        i.InstancePath = Path.Combine(m_serviceRootPath, s.ServiceName);
                        i.Controller = s;

                        listBox1.Items.Add(i);
                    }
                }
                catch
                {
                }
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = this.listBox1.IndexFromPoint(e.Location);
            if (index != System.Windows.Forms.ListBox.NoMatches)
            {
                button1_Click(this, EventArgs.Empty);
            }
        }

        private void addNewInstance_Click(object sender, EventArgs e)
        {
            using (m_addNewInstanceForm = new AddNewInstanceForm())
            {
                if (m_addNewInstanceForm.ShowDialog() == DialogResult.OK)
                {
                    // install a service
                    ManagedInstallerClass.InstallHelper(new string[] { "/servicename=" + m_addNewInstanceForm.NameOfService, m_serviceExeName });

                    // reload service controls
                    ReloadServices();
                }
            }
        }

        private void remove_Click(object sender, EventArgs e)
        {
            // get id of the service
            if (listBox1.SelectedItem != null)
            {
                string serviceName = listBox1.SelectedItem.ToString();

                if (serviceName != LocalConsoleName)
                {
                    // uninstall service
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", "/servicename=" + serviceName, m_serviceExeName });

                    // reload service controls
                    ReloadServices();
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(
                        "This instance cannot be deleted!",
                        "Invalid operation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void runAsAdmin_Click(object sender, EventArgs e)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Application.ExecutablePath;
            proc.Verb = "runas";

            try
            {
                Process.Start(proc);
            }
            catch
            {
                // The user refused the elevation. Do nothing and return directly ...
                return;
            }
            Application.Exit(); // Quit itself
        }

        private void saveConfig_Click(object sender, EventArgs e)
        {
            if (m_isAdmin)
            {
                try
                {
                    DedicatedServer.AddDateToLog = addDateToFilename.Checked;
                    DedicatedServer.SendLogToKeen = sendLogFiles.Checked;

                    globalInfo.Text = "Configuration saved.";
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Cannot save log file settings!",
                        "Invalid operation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void sendLogFiles_CheckedChanged(object sender, EventArgs e)
        {
            globalInfo.Text = "";
        }

        private void addDateToFilename_CheckedChanged(object sender, EventArgs e)
        {
            globalInfo.Text = "";
        }

        internal static Bitmap GetShieldIcon()
        {
            var size = SystemInformation.SmallIconSize;
            var image = WinApi.LoadImage(IntPtr.Zero, "#106", 1, size.Width, size.Height, 0);

            if (image == IntPtr.Zero)
            {
                return null;
            }

            using (var icon = Icon.FromHandle(image))
            {
                var bitmap = new Bitmap(size.Width, size.Height);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawIcon(icon, new Rectangle(0, 0, size.Width, size.Height));
                }

                return bitmap;
            }
        }
    }
}
