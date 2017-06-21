using VRage.Game;

namespace VRage.Dedicated
{
    partial class ConfigForm<T> where T : MyObjectBuilder_SessionSettings, new()
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
            this.components = new System.ComponentModel.Container();
            this.gamesListBox = new System.Windows.Forms.ListBox();
            this.startGameButton = new System.Windows.Forms.RadioButton();
            this.loadGameButton = new System.Windows.Forms.RadioButton();
            this.startButton = new System.Windows.Forms.Button();
            this.worldListTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.newGameSettingsPanel = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.scenarioCB = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.saveConfigButton = new System.Windows.Forms.Button();
            this.editConfigButton = new System.Windows.Forms.Button();
            this.reloadButton = new System.Windows.Forms.Button();
            this.resetButton = new System.Windows.Forms.Button();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.stopServiceButton = new System.Windows.Forms.Button();
            this.getBackButton = new System.Windows.Forms.Button();
            this.restartServiceButton = new System.Windows.Forms.Button();
            this.serviceStatusLabel = new System.Windows.Forms.Label();
            this.serviceStatusValueLabel = new System.Windows.Forms.Label();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.serviceUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageMods = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.modIdsTextBox = new System.Windows.Forms.TextBox();
            this.tabPageBannedUsers = new System.Windows.Forms.TabPage();
            this.bannedIDs = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPageAdmins = new System.Windows.Forms.TabPage();
            this.adminIDs = new System.Windows.Forms.TextBox();
            this.steamAdminsLabel = new System.Windows.Forms.Label();
            this.tabPageServerSettings = new System.Windows.Forms.TabPage();
            this.serverNameTextBox = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.QueryPortUD = new System.Windows.Forms.NumericUpDown();
            this.IPTextBox = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.worldNameTextBox = new System.Windows.Forms.TextBox();
            this.SteamGroupID = new System.Windows.Forms.TextBox();
            this.pauseWhenEmptyCHB = new System.Windows.Forms.CheckBox();
            this.steamGroupLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ignoreLastSessionCHB = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.newGameSettingsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageMods.SuspendLayout();
            this.tabPageBannedUsers.SuspendLayout();
            this.tabPageAdmins.SuspendLayout();
            this.tabPageServerSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.QueryPortUD)).BeginInit();
            this.SuspendLayout();
            // 
            // gamesListBox
            // 
            this.gamesListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gamesListBox.FormattingEnabled = true;
            this.gamesListBox.Location = new System.Drawing.Point(0, 0);
            this.gamesListBox.Name = "gamesListBox";
            this.gamesListBox.Size = new System.Drawing.Size(337, 124);
            this.gamesListBox.TabIndex = 0;
            this.gamesListBox.SelectedIndexChanged += new System.EventHandler(this.gamesListBox_SelectedIndexChanged);
            // 
            // startGameButton
            // 
            this.startGameButton.AutoSize = true;
            this.startGameButton.Location = new System.Drawing.Point(10, 18);
            this.startGameButton.Name = "startGameButton";
            this.startGameButton.Size = new System.Drawing.Size(76, 17);
            this.startGameButton.TabIndex = 1;
            this.startGameButton.Text = "New game";
            this.startGameButton.UseVisualStyleBackColor = true;
            this.startGameButton.CheckedChanged += new System.EventHandler(this.startTypeRadio_CheckedChanged);
            // 
            // loadGameButton
            // 
            this.loadGameButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.loadGameButton.AutoSize = true;
            this.loadGameButton.Checked = true;
            this.loadGameButton.Location = new System.Drawing.Point(10, 36);
            this.loadGameButton.Name = "loadGameButton";
            this.loadGameButton.Size = new System.Drawing.Size(89, 17);
            this.loadGameButton.TabIndex = 2;
            this.loadGameButton.TabStop = true;
            this.loadGameButton.Text = "Saved worlds";
            this.loadGameButton.UseVisualStyleBackColor = true;
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.startButton.Location = new System.Drawing.Point(551, 533);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(149, 48);
            this.startButton.TabIndex = 3;
            this.startButton.Text = "&Save && start";
            this.toolTip1.SetToolTip(this.startButton, "Save current configuration and start dedicated server");
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // worldListTimer
            // 
            this.worldListTimer.Enabled = true;
            this.worldListTimer.Tick += new System.EventHandler(this.worldListTimer_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoScroll = true;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(349, 76);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(350, 330);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.splitContainer1);
            this.groupBox1.Controls.Add(this.newGameSettingsPanel);
            this.groupBox1.Controls.Add(this.startGameButton);
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Controls.Add(this.loadGameButton);
            this.groupBox1.Location = new System.Drawing.Point(1, 92);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(705, 412);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Game settings";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.splitContainer1.Location = new System.Drawing.Point(6, 76);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.gamesListBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.AutoScroll = true;
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(337, 330);
            this.splitContainer1.SplitterDistance = 124;
            this.splitContainer1.SplitterWidth = 2;
            this.splitContainer1.TabIndex = 11;
            // 
            // newGameSettingsPanel
            // 
            this.newGameSettingsPanel.Controls.Add(this.label5);
            this.newGameSettingsPanel.Controls.Add(this.scenarioCB);
            this.newGameSettingsPanel.Location = new System.Drawing.Point(349, 14);
            this.newGameSettingsPanel.Name = "newGameSettingsPanel";
            this.newGameSettingsPanel.Size = new System.Drawing.Size(350, 57);
            this.newGameSettingsPanel.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Scenario:";
            // 
            // scenarioCB
            // 
            this.scenarioCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.scenarioCB.FormattingEnabled = true;
            this.scenarioCB.Location = new System.Drawing.Point(104, 7);
            this.scenarioCB.Name = "scenarioCB";
            this.scenarioCB.Size = new System.Drawing.Size(215, 21);
            this.scenarioCB.TabIndex = 8;
            // 
            // saveConfigButton
            // 
            this.saveConfigButton.Location = new System.Drawing.Point(102, 18);
            this.saveConfigButton.Name = "saveConfigButton";
            this.saveConfigButton.Size = new System.Drawing.Size(90, 23);
            this.saveConfigButton.TabIndex = 9;
            this.saveConfigButton.Text = "&Save";
            this.toolTip1.SetToolTip(this.saveConfigButton, "Save configuration data to current config file");
            this.saveConfigButton.UseVisualStyleBackColor = true;
            this.saveConfigButton.Click += new System.EventHandler(this.saveConfigButton_Click);
            // 
            // editConfigButton
            // 
            this.editConfigButton.Location = new System.Drawing.Point(197, 19);
            this.editConfigButton.Name = "editConfigButton";
            this.editConfigButton.Size = new System.Drawing.Size(90, 23);
            this.editConfigButton.TabIndex = 17;
            this.editConfigButton.Text = "&Edit...";
            this.toolTip1.SetToolTip(this.editConfigButton, "Open current config file in Notepad");
            this.editConfigButton.UseVisualStyleBackColor = true;
            this.editConfigButton.Click += new System.EventHandler(this.editConfigButton_Click);
            // 
            // reloadButton
            // 
            this.reloadButton.Location = new System.Drawing.Point(102, 43);
            this.reloadButton.Name = "reloadButton";
            this.reloadButton.Size = new System.Drawing.Size(90, 23);
            this.reloadButton.TabIndex = 22;
            this.reloadButton.Text = "&Reload";
            this.toolTip1.SetToolTip(this.reloadButton, "Reload data from current config file");
            this.reloadButton.UseVisualStyleBackColor = true;
            this.reloadButton.Click += new System.EventHandler(this.reloadButton_Click);
            // 
            // resetButton
            // 
            this.resetButton.Location = new System.Drawing.Point(197, 44);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(90, 23);
            this.resetButton.TabIndex = 21;
            this.resetButton.Text = "&Reset";
            this.toolTip1.SetToolTip(this.resetButton, "Reset current configuration to default data");
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            // 
            // saveAsButton
            // 
            this.saveAsButton.Location = new System.Drawing.Point(7, 44);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(90, 23);
            this.saveAsButton.TabIndex = 20;
            this.saveAsButton.Text = "S&ave as...";
            this.toolTip1.SetToolTip(this.saveAsButton, "Save configuration data to external file");
            this.saveAsButton.UseVisualStyleBackColor = true;
            this.saveAsButton.Click += new System.EventHandler(this.saveAsButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(7, 18);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 23);
            this.button1.TabIndex = 19;
            this.button1.Text = "&Load from...";
            this.toolTip1.SetToolTip(this.button1, "Load configuration data from external file");
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.loadConfigButton_Click);
            // 
            // stopServiceButton
            // 
            this.stopServiceButton.Location = new System.Drawing.Point(101, 19);
            this.stopServiceButton.Name = "stopServiceButton";
            this.stopServiceButton.Size = new System.Drawing.Size(90, 23);
            this.stopServiceButton.TabIndex = 11;
            this.stopServiceButton.Text = "Stop";
            this.toolTip1.SetToolTip(this.stopServiceButton, "Stop currently running service");
            this.stopServiceButton.UseVisualStyleBackColor = true;
            this.stopServiceButton.Click += new System.EventHandler(this.stopServiceButton_Click);
            // 
            // getBackButton
            // 
            this.getBackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.getBackButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.getBackButton.Location = new System.Drawing.Point(551, 508);
            this.getBackButton.Name = "getBackButton";
            this.getBackButton.Size = new System.Drawing.Size(149, 23);
            this.getBackButton.TabIndex = 12;
            this.getBackButton.Text = "Back to instances";
            this.toolTip1.SetToolTip(this.getBackButton, "Return to list of dedicated server instances");
            this.getBackButton.UseVisualStyleBackColor = true;
            this.getBackButton.Click += new System.EventHandler(this.getBackButton_Click);
            // 
            // restartServiceButton
            // 
            this.restartServiceButton.Location = new System.Drawing.Point(6, 19);
            this.restartServiceButton.Name = "restartServiceButton";
            this.restartServiceButton.Size = new System.Drawing.Size(90, 23);
            this.restartServiceButton.TabIndex = 15;
            this.restartServiceButton.Text = "Restart";
            this.toolTip1.SetToolTip(this.restartServiceButton, "Restart currently running service");
            this.restartServiceButton.UseVisualStyleBackColor = true;
            this.restartServiceButton.Click += new System.EventHandler(this.restartServiceButton_Click);
            // 
            // serviceStatusLabel
            // 
            this.serviceStatusLabel.AutoSize = true;
            this.serviceStatusLabel.Location = new System.Drawing.Point(24, 48);
            this.serviceStatusLabel.Name = "serviceStatusLabel";
            this.serviceStatusLabel.Size = new System.Drawing.Size(77, 13);
            this.serviceStatusLabel.TabIndex = 13;
            this.serviceStatusLabel.Text = "Service status:";
            // 
            // serviceStatusValueLabel
            // 
            this.serviceStatusValueLabel.AutoSize = true;
            this.serviceStatusValueLabel.Location = new System.Drawing.Point(103, 48);
            this.serviceStatusValueLabel.Name = "serviceStatusValueLabel";
            this.serviceStatusValueLabel.Size = new System.Drawing.Size(63, 13);
            this.serviceStatusValueLabel.TabIndex = 14;
            this.serviceStatusValueLabel.Text = "Service info";
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logoPictureBox.BackColor = System.Drawing.Color.Black;
            this.logoPictureBox.Location = new System.Drawing.Point(1, 2);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(699, 98);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.logoPictureBox.TabIndex = 5;
            this.logoPictureBox.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox3.Controls.Add(this.reloadButton);
            this.groupBox3.Controls.Add(this.resetButton);
            this.groupBox3.Controls.Add(this.saveAsButton);
            this.groupBox3.Controls.Add(this.button1);
            this.groupBox3.Controls.Add(this.editConfigButton);
            this.groupBox3.Controls.Add(this.saveConfigButton);
            this.groupBox3.Location = new System.Drawing.Point(7, 510);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(293, 73);
            this.groupBox3.TabIndex = 18;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Configuration";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.restartServiceButton);
            this.groupBox4.Controls.Add(this.stopServiceButton);
            this.groupBox4.Controls.Add(this.serviceStatusLabel);
            this.groupBox4.Controls.Add(this.serviceStatusValueLabel);
            this.groupBox4.Location = new System.Drawing.Point(343, 510);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(198, 72);
            this.groupBox4.TabIndex = 19;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Service";
            // 
            // serviceUpdateTimer
            // 
            this.serviceUpdateTimer.Enabled = true;
            this.serviceUpdateTimer.Interval = 1000;
            this.serviceUpdateTimer.Tick += new System.EventHandler(this.serviceUpdateTimer_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageServerSettings);
            this.tabControl1.Controls.Add(this.tabPageAdmins);
            this.tabControl1.Controls.Add(this.tabPageBannedUsers);
            this.tabControl1.Controls.Add(this.tabPageMods);
            this.tabControl1.Location = new System.Drawing.Point(3, 3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(334, 200);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageMods
            // 
            this.tabPageMods.Controls.Add(this.modIdsTextBox);
            this.tabPageMods.Controls.Add(this.label8);
            this.tabPageMods.Location = new System.Drawing.Point(4, 22);
            this.tabPageMods.Name = "tabPageMods";
            this.tabPageMods.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageMods.Size = new System.Drawing.Size(326, 182);
            this.tabPageMods.TabIndex = 3;
            this.tabPageMods.Text = "Mods";
            this.tabPageMods.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 3);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(36, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Mods:";
            // 
            // modIdsTextBox
            // 
            this.modIdsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.modIdsTextBox.Location = new System.Drawing.Point(6, 18);
            this.modIdsTextBox.Multiline = true;
            this.modIdsTextBox.Name = "modIdsTextBox";
            this.modIdsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.modIdsTextBox.Size = new System.Drawing.Size(314, 158);
            this.modIdsTextBox.TabIndex = 20;
            this.toolTip1.SetToolTip(this.modIdsTextBox, "Workshop ID of mods");
            // 
            // tabPageBannedUsers
            // 
            this.tabPageBannedUsers.Controls.Add(this.label2);
            this.tabPageBannedUsers.Controls.Add(this.bannedIDs);
            this.tabPageBannedUsers.Location = new System.Drawing.Point(4, 22);
            this.tabPageBannedUsers.Name = "tabPageBannedUsers";
            this.tabPageBannedUsers.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageBannedUsers.Size = new System.Drawing.Size(326, 182);
            this.tabPageBannedUsers.TabIndex = 2;
            this.tabPageBannedUsers.Text = "Banned users";
            this.tabPageBannedUsers.UseVisualStyleBackColor = true;
            // 
            // bannedIDs
            // 
            this.bannedIDs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bannedIDs.Location = new System.Drawing.Point(6, 18);
            this.bannedIDs.Multiline = true;
            this.bannedIDs.Name = "bannedIDs";
            this.bannedIDs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.bannedIDs.Size = new System.Drawing.Size(314, 158);
            this.bannedIDs.TabIndex = 15;
            this.toolTip1.SetToolTip(this.bannedIDs, "Steam ID of banned players");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Banned users:";
            // 
            // tabPageAdmins
            // 
            this.tabPageAdmins.Controls.Add(this.steamAdminsLabel);
            this.tabPageAdmins.Controls.Add(this.adminIDs);
            this.tabPageAdmins.Location = new System.Drawing.Point(4, 22);
            this.tabPageAdmins.Name = "tabPageAdmins";
            this.tabPageAdmins.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAdmins.Size = new System.Drawing.Size(326, 182);
            this.tabPageAdmins.TabIndex = 1;
            this.tabPageAdmins.Text = "Admins";
            this.tabPageAdmins.UseVisualStyleBackColor = true;
            // 
            // adminIDs
            // 
            this.adminIDs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.adminIDs.Location = new System.Drawing.Point(6, 18);
            this.adminIDs.Multiline = true;
            this.adminIDs.Name = "adminIDs";
            this.adminIDs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.adminIDs.Size = new System.Drawing.Size(314, 158);
            this.adminIDs.TabIndex = 15;
            this.toolTip1.SetToolTip(this.adminIDs, "Steam ID of server admins (can kick people)\r\nInsert one Steam ID per line");
            // 
            // steamAdminsLabel
            // 
            this.steamAdminsLabel.AutoSize = true;
            this.steamAdminsLabel.Location = new System.Drawing.Point(3, 3);
            this.steamAdminsLabel.Name = "steamAdminsLabel";
            this.steamAdminsLabel.Size = new System.Drawing.Size(77, 13);
            this.steamAdminsLabel.TabIndex = 14;
            this.steamAdminsLabel.Text = "Server admins:";
            // 
            // tabPageServerSettings
            // 
            this.tabPageServerSettings.Controls.Add(this.label9);
            this.tabPageServerSettings.Controls.Add(this.ignoreLastSessionCHB);
            this.tabPageServerSettings.Controls.Add(this.label3);
            this.tabPageServerSettings.Controls.Add(this.steamGroupLabel);
            this.tabPageServerSettings.Controls.Add(this.pauseWhenEmptyCHB);
            this.tabPageServerSettings.Controls.Add(this.SteamGroupID);
            this.tabPageServerSettings.Controls.Add(this.worldNameTextBox);
            this.tabPageServerSettings.Controls.Add(this.label6);
            this.tabPageServerSettings.Controls.Add(this.label11);
            this.tabPageServerSettings.Controls.Add(this.label12);
            this.tabPageServerSettings.Controls.Add(this.IPTextBox);
            this.tabPageServerSettings.Controls.Add(this.QueryPortUD);
            this.tabPageServerSettings.Controls.Add(this.label13);
            this.tabPageServerSettings.Controls.Add(this.serverNameTextBox);
            this.tabPageServerSettings.Location = new System.Drawing.Point(4, 22);
            this.tabPageServerSettings.Name = "tabPageServerSettings";
            this.tabPageServerSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageServerSettings.Size = new System.Drawing.Size(326, 174);
            this.tabPageServerSettings.TabIndex = 0;
            this.tabPageServerSettings.Text = "Server settings";
            this.tabPageServerSettings.UseVisualStyleBackColor = true;
            // 
            // serverNameTextBox
            // 
            this.serverNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.serverNameTextBox.Location = new System.Drawing.Point(111, 53);
            this.serverNameTextBox.Name = "serverNameTextBox";
            this.serverNameTextBox.Size = new System.Drawing.Size(212, 20);
            this.serverNameTextBox.TabIndex = 30;
            this.serverNameTextBox.Text = "Medieval Engineers Dedicated Server";
            this.toolTip1.SetToolTip(this.serverNameTextBox, "A name of the server");
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 56);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(70, 13);
            this.label13.TabIndex = 29;
            this.label13.Text = "Server name:";
            // 
            // QueryPortUD
            // 
            this.QueryPortUD.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryPortUD.Location = new System.Drawing.Point(203, 28);
            this.QueryPortUD.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.QueryPortUD.Name = "QueryPortUD";
            this.QueryPortUD.Size = new System.Drawing.Size(120, 20);
            this.QueryPortUD.TabIndex = 28;
            this.QueryPortUD.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.QueryPortUD, "Port that will manage server browser related duties and info");
            this.QueryPortUD.Value = new decimal(new int[] {
            27015,
            0,
            0,
            0});
            // 
            // IPTextBox
            // 
            this.IPTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.IPTextBox.Location = new System.Drawing.Point(203, 3);
            this.IPTextBox.Name = "IPTextBox";
            this.IPTextBox.Size = new System.Drawing.Size(120, 20);
            this.IPTextBox.TabIndex = 27;
            this.IPTextBox.Text = "0.0.0.0";
            this.IPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.IPTextBox, "The IP Address the server is listening for client connections on.\r\nUse 0.0.0.0 to" +
        " listen on all local interfaces.");
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 30);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(62, 13);
            this.label12.TabIndex = 26;
            this.label12.Text = "Server port:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 81);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(67, 13);
            this.label11.TabIndex = 31;
            this.label11.Text = "World name:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 6);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 13);
            this.label6.TabIndex = 25;
            this.label6.Text = "Listen IP:";
            // 
            // worldNameTextBox
            // 
            this.worldNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.worldNameTextBox.Location = new System.Drawing.Point(111, 78);
            this.worldNameTextBox.Name = "worldNameTextBox";
            this.worldNameTextBox.Size = new System.Drawing.Size(212, 20);
            this.worldNameTextBox.TabIndex = 32;
            this.toolTip1.SetToolTip(this.worldNameTextBox, "A name of the world. Empty name will generate unique world name.");
            // 
            // SteamGroupID
            // 
            this.SteamGroupID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SteamGroupID.Location = new System.Drawing.Point(111, 103);
            this.SteamGroupID.Name = "SteamGroupID";
            this.SteamGroupID.Size = new System.Drawing.Size(212, 20);
            this.SteamGroupID.TabIndex = 34;
            this.toolTip1.SetToolTip(this.SteamGroupID, "ID of the Steam group\r\nOnly users in this group will be allowed to connect to ser" +
        "ver\r\nUse 0 or empty to allow everyone to connect");
            // 
            // pauseWhenEmptyCHB
            // 
            this.pauseWhenEmptyCHB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pauseWhenEmptyCHB.AutoSize = true;
            this.pauseWhenEmptyCHB.Location = new System.Drawing.Point(305, 131);
            this.pauseWhenEmptyCHB.Name = "pauseWhenEmptyCHB";
            this.pauseWhenEmptyCHB.Size = new System.Drawing.Size(15, 14);
            this.pauseWhenEmptyCHB.TabIndex = 35;
            this.pauseWhenEmptyCHB.UseVisualStyleBackColor = true;
            // 
            // steamGroupLabel
            // 
            this.steamGroupLabel.AutoSize = true;
            this.steamGroupLabel.Location = new System.Drawing.Point(6, 106);
            this.steamGroupLabel.Name = "steamGroupLabel";
            this.steamGroupLabel.Size = new System.Drawing.Size(86, 13);
            this.steamGroupLabel.TabIndex = 33;
            this.steamGroupLabel.Text = "Steam Group ID:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 131);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(129, 13);
            this.label3.TabIndex = 36;
            this.label3.Text = "Pause game when empty:";
            // 
            // ignoreLastSessionCHB
            // 
            this.ignoreLastSessionCHB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ignoreLastSessionCHB.AutoSize = true;
            this.ignoreLastSessionCHB.Location = new System.Drawing.Point(305, 156);
            this.ignoreLastSessionCHB.Name = "ignoreLastSessionCHB";
            this.ignoreLastSessionCHB.Size = new System.Drawing.Size(15, 14);
            this.ignoreLastSessionCHB.TabIndex = 37;
            this.ignoreLastSessionCHB.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 156);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(97, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "Ignore last session:";
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 586);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.getBackButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.logoPictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(720, 16384);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 620);
            this.Name = "ConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Medieval engineers - Dedicated server configurator";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.newGameSettingsPanel.ResumeLayout(false);
            this.newGameSettingsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageMods.ResumeLayout(false);
            this.tabPageMods.PerformLayout();
            this.tabPageBannedUsers.ResumeLayout(false);
            this.tabPageBannedUsers.PerformLayout();
            this.tabPageAdmins.ResumeLayout(false);
            this.tabPageAdmins.PerformLayout();
            this.tabPageServerSettings.ResumeLayout(false);
            this.tabPageServerSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.QueryPortUD)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox gamesListBox;
        private System.Windows.Forms.RadioButton startGameButton;
        private System.Windows.Forms.RadioButton loadGameButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Timer worldListTimer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox scenarioCB;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel newGameSettingsPanel;
        private System.Windows.Forms.Button saveConfigButton;
        private System.Windows.Forms.Button stopServiceButton;
        private System.Windows.Forms.Button getBackButton;
        private System.Windows.Forms.Label serviceStatusLabel;
        private System.Windows.Forms.Label serviceStatusValueLabel;
        private System.Windows.Forms.Button restartServiceButton;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Button editConfigButton;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button saveAsButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.Button reloadButton;
        private System.Windows.Forms.Timer serviceUpdateTimer;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageServerSettings;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox ignoreLastSessionCHB;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label steamGroupLabel;
        private System.Windows.Forms.CheckBox pauseWhenEmptyCHB;
        private System.Windows.Forms.TextBox SteamGroupID;
        private System.Windows.Forms.TextBox worldNameTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox IPTextBox;
        private System.Windows.Forms.NumericUpDown QueryPortUD;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox serverNameTextBox;
        private System.Windows.Forms.TabPage tabPageAdmins;
        private System.Windows.Forms.Label steamAdminsLabel;
        private System.Windows.Forms.TextBox adminIDs;
        private System.Windows.Forms.TabPage tabPageBannedUsers;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox bannedIDs;
        private System.Windows.Forms.TabPage tabPageMods;
        private System.Windows.Forms.TextBox modIdsTextBox;
        private System.Windows.Forms.Label label8;
    }
}

