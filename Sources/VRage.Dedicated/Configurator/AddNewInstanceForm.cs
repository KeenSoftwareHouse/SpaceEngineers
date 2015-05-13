using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VRage.Dedicated.Configurator
{
    public partial class AddNewInstanceForm : Form
    {
        /// <summary>
        /// Name of a service given by the user.
        /// </summary>
        public string NameOfService { get; private set; }

        public AddNewInstanceForm()
        {
            InitializeComponent();
        }

        private void AddNewInstanceForm_Load(object sender, EventArgs e)
        {
            textBox1.Focus();
            textBox1.Select();
        }

        private void insert_Click(object sender, EventArgs e)
        {
            string nameOfService = textBox1.Text;

            if (nameOfService.Length == 0
                || nameOfService.Contains(':') 
                || nameOfService.Contains('/') 
                || nameOfService.Contains('\\') || nameOfService.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                MessageBox.Show(
                    "Service name contains invalid characters!",
                    "Invalid value",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                DialogResult = System.Windows.Forms.DialogResult.None;
            }
            else
            {
                NameOfService = nameOfService;
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
    }
}
