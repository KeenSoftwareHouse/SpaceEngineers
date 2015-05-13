namespace VRage.Dedicated.Configurator
{
    partial class SelectInstanceForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.exit = new System.Windows.Forms.Button();
            this.addNewInstance = new System.Windows.Forms.Button();
            this.remove = new System.Windows.Forms.Button();
            this.addDateToFilename = new System.Windows.Forms.CheckBox();
            this.sendLogFiles = new System.Windows.Forms.CheckBox();
            this.info = new System.Windows.Forms.Label();
            this.runAsAdmin = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.globalInfo = new System.Windows.Forms.Label();
            this.saveConfig = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 12);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(257, 225);
            this.listBox1.TabIndex = 0;
            this.listBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDoubleClick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 311);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(257, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Continue to server configuration";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // exit
            // 
            this.exit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.exit.Location = new System.Drawing.Point(385, 282);
            this.exit.Name = "exit";
            this.exit.Size = new System.Drawing.Size(110, 52);
            this.exit.TabIndex = 2;
            this.exit.Text = "Exit";
            this.exit.UseVisualStyleBackColor = true;
            this.exit.Click += new System.EventHandler(this.exit_Click);
            // 
            // addNewInstance
            // 
            this.addNewInstance.Location = new System.Drawing.Point(12, 282);
            this.addNewInstance.Name = "addNewInstance";
            this.addNewInstance.Size = new System.Drawing.Size(120, 23);
            this.addNewInstance.TabIndex = 3;
            this.addNewInstance.Text = "Add new instance";
            this.addNewInstance.UseVisualStyleBackColor = true;
            this.addNewInstance.Click += new System.EventHandler(this.addNewInstance_Click);
            // 
            // remove
            // 
            this.remove.Location = new System.Drawing.Point(149, 282);
            this.remove.Name = "remove";
            this.remove.Size = new System.Drawing.Size(120, 23);
            this.remove.TabIndex = 4;
            this.remove.Text = "Remove instance";
            this.remove.UseVisualStyleBackColor = true;
            this.remove.Click += new System.EventHandler(this.remove_Click);
            // 
            // addDateToFilename
            // 
            this.addDateToFilename.AutoSize = true;
            this.addDateToFilename.Location = new System.Drawing.Point(6, 19);
            this.addDateToFilename.Name = "addDateToFilename";
            this.addDateToFilename.Size = new System.Drawing.Size(140, 17);
            this.addDateToFilename.TabIndex = 5;
            this.addDateToFilename.Text = "Add date to log filename";
            this.addDateToFilename.UseVisualStyleBackColor = true;
            this.addDateToFilename.CheckedChanged += new System.EventHandler(this.addDateToFilename_CheckedChanged);
            // 
            // sendLogFiles
            // 
            this.sendLogFiles.AutoSize = true;
            this.sendLogFiles.Location = new System.Drawing.Point(6, 42);
            this.sendLogFiles.Name = "sendLogFiles";
            this.sendLogFiles.Size = new System.Drawing.Size(129, 17);
            this.sendLogFiles.TabIndex = 6;
            this.sendLogFiles.Text = "Send log files to Keen";
            this.sendLogFiles.UseVisualStyleBackColor = true;
            this.sendLogFiles.CheckedChanged += new System.EventHandler(this.sendLogFiles_CheckedChanged);
            // 
            // info
            // 
            this.info.AutoSize = true;
            this.info.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.info.Location = new System.Drawing.Point(12, 248);
            this.info.Name = "info";
            this.info.Size = new System.Drawing.Size(31, 15);
            this.info.TabIndex = 7;
            this.info.Text = "Info";
            // 
            // runAsAdmin
            // 
            this.runAsAdmin.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.runAsAdmin.Location = new System.Drawing.Point(385, 243);
            this.runAsAdmin.Name = "runAsAdmin";
            this.runAsAdmin.Size = new System.Drawing.Size(110, 30);
            this.runAsAdmin.TabIndex = 8;
            this.runAsAdmin.Text = "Run as Admin";
            this.runAsAdmin.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.runAsAdmin.UseVisualStyleBackColor = true;
            this.runAsAdmin.Click += new System.EventHandler(this.runAsAdmin_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.globalInfo);
            this.groupBox1.Controls.Add(this.saveConfig);
            this.groupBox1.Controls.Add(this.addDateToFilename);
            this.groupBox1.Controls.Add(this.sendLogFiles);
            this.groupBox1.Location = new System.Drawing.Point(275, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(220, 225);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Global configuration";
            // 
            // globalInfo
            // 
            this.globalInfo.AutoSize = true;
            this.globalInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.globalInfo.Location = new System.Drawing.Point(88, 70);
            this.globalInfo.Name = "globalInfo";
            this.globalInfo.Size = new System.Drawing.Size(58, 13);
            this.globalInfo.TabIndex = 10;
            this.globalInfo.Text = "Global Info";
            // 
            // saveConfig
            // 
            this.saveConfig.Location = new System.Drawing.Point(6, 65);
            this.saveConfig.Name = "saveConfig";
            this.saveConfig.Size = new System.Drawing.Size(75, 23);
            this.saveConfig.TabIndex = 7;
            this.saveConfig.Text = "Save";
            this.saveConfig.UseVisualStyleBackColor = true;
            this.saveConfig.Click += new System.EventHandler(this.saveConfig_Click);
            // 
            // SelectInstanceForm
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.exit;
            this.ClientSize = new System.Drawing.Size(509, 346);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.runAsAdmin);
            this.Controls.Add(this.info);
            this.Controls.Add(this.remove);
            this.Controls.Add(this.addNewInstance);
            this.Controls.Add(this.exit);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(525, 380);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(525, 380);
            this.Name = "SelectInstanceForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.SelectInstanceForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button exit;
        private System.Windows.Forms.Button addNewInstance;
        private System.Windows.Forms.Button remove;
        private System.Windows.Forms.CheckBox addDateToFilename;
        private System.Windows.Forms.CheckBox sendLogFiles;
        private System.Windows.Forms.Label info;
        private System.Windows.Forms.Button runAsAdmin;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button saveConfig;
        private System.Windows.Forms.Label globalInfo;
    }
}