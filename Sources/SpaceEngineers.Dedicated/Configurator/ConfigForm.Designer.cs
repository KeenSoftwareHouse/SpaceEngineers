namespace DedicatedConfigurator
{
    partial class ConfigFormOld
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigFormOld));
            this.gamesListBox = new System.Windows.Forms.ListBox();
            this.startGameButton = new System.Windows.Forms.RadioButton();
            this.loadGameButton = new System.Windows.Forms.RadioButton();
            this.startButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.newGameSettingsPanel = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.asteroidAmountUD = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.scenarioCB = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.worldNameTextBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.ignoreLastSessionCHB = new System.Windows.Forms.CheckBox();
            this.modIdsTextBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.pauseWhenEmptyCHB = new System.Windows.Forms.CheckBox();
            this.bannedIDs = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.adminIDs = new System.Windows.Forms.TextBox();
            this.steamAdminsLabel = new System.Windows.Forms.Label();
            this.SteamGroupID = new System.Windows.Forms.TextBox();
            this.steamGroupLabel = new System.Windows.Forms.Label();
            this.serverNameTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.QueryPortUD = new System.Windows.Forms.NumericUpDown();
            this.IPTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.saveConfigButton = new System.Windows.Forms.Button();
            this.editConfigButton = new System.Windows.Forms.Button();
            this.stopServiceButton = new System.Windows.Forms.Button();
            this.getBackButton = new System.Windows.Forms.Button();
            this.serviceStatusLabel = new System.Windows.Forms.Label();
            this.serviceStatusValueLabel = new System.Windows.Forms.Label();
            this.restartServiceButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.newGameSettingsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.asteroidAmountUD)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.QueryPortUD)).BeginInit();
            this.SuspendLayout();
            // 
            // gamesListBox
            // 
            this.gamesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.gamesListBox.FormattingEnabled = true;
            this.gamesListBox.Location = new System.Drawing.Point(10, 76);
            this.gamesListBox.Name = "gamesListBox";
            this.gamesListBox.Size = new System.Drawing.Size(333, 186);
            this.gamesListBox.TabIndex = 0;
            this.gamesListBox.SelectedIndexChanged += new System.EventHandler(this.gamesListBox_SelectedIndexChanged);
            // 
            // startGameButton
            // 
            this.startGameButton.AutoSize = true;
            this.startGameButton.Location = new System.Drawing.Point(10, 25);
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
            this.loadGameButton.Location = new System.Drawing.Point(10, 54);
            this.loadGameButton.Name = "loadGameButton";
            this.loadGameButton.Size = new System.Drawing.Size(89, 17);
            this.loadGameButton.TabIndex = 2;
            this.loadGameButton.TabStop = true;
            this.loadGameButton.Text = "Saved worlds";
            this.loadGameButton.UseVisualStyleBackColor = true;
            this.loadGameButton.CheckedChanged += new System.EventHandler(this.startTypeRadio_CheckedChanged);
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.startButton.Location = new System.Drawing.Point(133, 688);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(127, 48);
            this.startButton.TabIndex = 3;
            this.startButton.Text = "&Save config and start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // exitButton
            // 
            this.exitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.exitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.exitButton.Location = new System.Drawing.Point(590, 688);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(110, 48);
            this.exitButton.TabIndex = 4;
            this.exitButton.Text = "E&xit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(1, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(705, 84);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoScroll = true;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(349, 76);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(350, 508);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.newGameSettingsPanel);
            this.groupBox1.Controls.Add(this.startGameButton);
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Controls.Add(this.gamesListBox);
            this.groupBox1.Controls.Add(this.loadGameButton);
            this.groupBox1.Location = new System.Drawing.Point(1, 92);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(705, 590);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Game settings";
            // 
            // newGameSettingsPanel
            // 
            this.newGameSettingsPanel.Controls.Add(this.label6);
            this.newGameSettingsPanel.Controls.Add(this.asteroidAmountUD);
            this.newGameSettingsPanel.Controls.Add(this.label5);
            this.newGameSettingsPanel.Controls.Add(this.scenarioCB);
            this.newGameSettingsPanel.Location = new System.Drawing.Point(349, 14);
            this.newGameSettingsPanel.Name = "newGameSettingsPanel";
            this.newGameSettingsPanel.Size = new System.Drawing.Size(350, 57);
            this.newGameSettingsPanel.TabIndex = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 37);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(86, 13);
            this.label6.TabIndex = 9;
            this.label6.Text = "Asteroid amount:";
            // 
            // asteroidAmountUD
            // 
            this.asteroidAmountUD.Location = new System.Drawing.Point(199, 34);
            this.asteroidAmountUD.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.asteroidAmountUD.Name = "asteroidAmountUD";
            this.asteroidAmountUD.Size = new System.Drawing.Size(120, 20);
            this.asteroidAmountUD.TabIndex = 8;
            this.asteroidAmountUD.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.asteroidAmountUD.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
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
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.worldNameTextBox);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.ignoreLastSessionCHB);
            this.groupBox2.Controls.Add(this.modIdsTextBox);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.pauseWhenEmptyCHB);
            this.groupBox2.Controls.Add(this.bannedIDs);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.adminIDs);
            this.groupBox2.Controls.Add(this.steamAdminsLabel);
            this.groupBox2.Controls.Add(this.SteamGroupID);
            this.groupBox2.Controls.Add(this.steamGroupLabel);
            this.groupBox2.Controls.Add(this.serverNameTextBox);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.QueryPortUD);
            this.groupBox2.Controls.Add(this.IPTextBox);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(1, 357);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(343, 319);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Server settings";
            // 
            // worldNameTextBox
            // 
            this.worldNameTextBox.Location = new System.Drawing.Point(98, 97);
            this.worldNameTextBox.Name = "worldNameTextBox";
            this.worldNameTextBox.Size = new System.Drawing.Size(239, 20);
            this.worldNameTextBox.TabIndex = 24;
            this.toolTip1.SetToolTip(this.worldNameTextBox, "A name of the world. Empty name will generate unique world name.");
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 100);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(67, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "World name:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 294);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(97, 13);
            this.label9.TabIndex = 22;
            this.label9.Text = "Ignore last session:";
            // 
            // ignoreLastSessionCHB
            // 
            this.ignoreLastSessionCHB.AutoSize = true;
            this.ignoreLastSessionCHB.Location = new System.Drawing.Point(322, 293);
            this.ignoreLastSessionCHB.Name = "ignoreLastSessionCHB";
            this.ignoreLastSessionCHB.Size = new System.Drawing.Size(15, 14);
            this.ignoreLastSessionCHB.TabIndex = 21;
            this.ignoreLastSessionCHB.UseVisualStyleBackColor = true;
            // 
            // modIdsTextBox
            // 
            this.modIdsTextBox.Location = new System.Drawing.Point(98, 204);
            this.modIdsTextBox.Multiline = true;
            this.modIdsTextBox.Name = "modIdsTextBox";
            this.modIdsTextBox.Size = new System.Drawing.Size(239, 39);
            this.modIdsTextBox.TabIndex = 20;
            this.toolTip1.SetToolTip(this.modIdsTextBox, "Workshop ID of mods");
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 207);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(36, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Mods:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 274);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(129, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Pause game when empty:";
            // 
            // pauseWhenEmptyCHB
            // 
            this.pauseWhenEmptyCHB.AutoSize = true;
            this.pauseWhenEmptyCHB.Location = new System.Drawing.Point(322, 273);
            this.pauseWhenEmptyCHB.Name = "pauseWhenEmptyCHB";
            this.pauseWhenEmptyCHB.Size = new System.Drawing.Size(15, 14);
            this.pauseWhenEmptyCHB.TabIndex = 17;
            this.pauseWhenEmptyCHB.UseVisualStyleBackColor = true;
            // 
            // bannedIDs
            // 
            this.bannedIDs.Location = new System.Drawing.Point(98, 163);
            this.bannedIDs.Multiline = true;
            this.bannedIDs.Name = "bannedIDs";
            this.bannedIDs.Size = new System.Drawing.Size(239, 39);
            this.bannedIDs.TabIndex = 15;
            this.toolTip1.SetToolTip(this.bannedIDs, "Steam ID of banned players");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 166);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Banned users:";
            // 
            // adminIDs
            // 
            this.adminIDs.Location = new System.Drawing.Point(98, 123);
            this.adminIDs.Multiline = true;
            this.adminIDs.Name = "adminIDs";
            this.adminIDs.Size = new System.Drawing.Size(239, 39);
            this.adminIDs.TabIndex = 13;
            this.toolTip1.SetToolTip(this.adminIDs, "Steam ID of server admins (can kick people)\r\nInsert one Steam ID per line");
            // 
            // steamAdminsLabel
            // 
            this.steamAdminsLabel.AutoSize = true;
            this.steamAdminsLabel.Location = new System.Drawing.Point(7, 126);
            this.steamAdminsLabel.Name = "steamAdminsLabel";
            this.steamAdminsLabel.Size = new System.Drawing.Size(77, 13);
            this.steamAdminsLabel.TabIndex = 12;
            this.steamAdminsLabel.Text = "Server admins:";
            // 
            // SteamGroupID
            // 
            this.SteamGroupID.Location = new System.Drawing.Point(98, 244);
            this.SteamGroupID.Name = "SteamGroupID";
            this.SteamGroupID.Size = new System.Drawing.Size(239, 20);
            this.SteamGroupID.TabIndex = 11;
            this.toolTip1.SetToolTip(this.SteamGroupID, "ID of the Steam group\r\nOnly users in this group will be allowed to connect to ser" +
        "ver\r\nUse 0 or empty to allow everyone to connect");
            // 
            // steamGroupLabel
            // 
            this.steamGroupLabel.AutoSize = true;
            this.steamGroupLabel.Location = new System.Drawing.Point(6, 247);
            this.steamGroupLabel.Name = "steamGroupLabel";
            this.steamGroupLabel.Size = new System.Drawing.Size(86, 13);
            this.steamGroupLabel.TabIndex = 10;
            this.steamGroupLabel.Text = "Steam Group ID:";
            // 
            // serverNameTextBox
            // 
            this.serverNameTextBox.Location = new System.Drawing.Point(98, 69);
            this.serverNameTextBox.Name = "serverNameTextBox";
            this.serverNameTextBox.Size = new System.Drawing.Size(239, 20);
            this.serverNameTextBox.TabIndex = 9;
            this.serverNameTextBox.Text = "Space Engineers Dedicated Server";
            this.toolTip1.SetToolTip(this.serverNameTextBox, "A name of the server");
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 72);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Server name:";
            // 
            // QueryPortUD
            // 
            this.QueryPortUD.Location = new System.Drawing.Point(217, 43);
            this.QueryPortUD.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.QueryPortUD.Name = "QueryPortUD";
            this.QueryPortUD.Size = new System.Drawing.Size(120, 20);
            this.QueryPortUD.TabIndex = 7;
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
            this.IPTextBox.Location = new System.Drawing.Point(217, 17);
            this.IPTextBox.Name = "IPTextBox";
            this.IPTextBox.Size = new System.Drawing.Size(120, 20);
            this.IPTextBox.TabIndex = 4;
            this.IPTextBox.Text = "0.0.0.0";
            this.IPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.IPTextBox, "The IP Address the server is listening for client connections on.\r\nUse 0.0.0.0 to" +
        " listen on all local interfaces.");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 45);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Server port:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Listen IP:";
            // 
            // saveConfigButton
            // 
            this.saveConfigButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.saveConfigButton.Location = new System.Drawing.Point(14, 688);
            this.saveConfigButton.Name = "saveConfigButton";
            this.saveConfigButton.Size = new System.Drawing.Size(113, 23);
            this.saveConfigButton.TabIndex = 9;
            this.saveConfigButton.Text = "S&ave config";
            this.saveConfigButton.UseVisualStyleBackColor = true;
            this.saveConfigButton.Click += new System.EventHandler(this.saveConfigButton_Click);
            // 
            // editConfigButton
            // 
            this.editConfigButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.editConfigButton.Location = new System.Drawing.Point(14, 712);
            this.editConfigButton.Name = "editConfigButton";
            this.editConfigButton.Size = new System.Drawing.Size(113, 23);
            this.editConfigButton.TabIndex = 10;
            this.editConfigButton.Text = "&Edit config";
            this.editConfigButton.UseVisualStyleBackColor = true;
            this.editConfigButton.Click += new System.EventHandler(this.editConfigButton_Click);
            // 
            // stopServiceButton
            // 
            this.stopServiceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.stopServiceButton.Location = new System.Drawing.Point(362, 688);
            this.stopServiceButton.Name = "stopServiceButton";
            this.stopServiceButton.Size = new System.Drawing.Size(90, 23);
            this.stopServiceButton.TabIndex = 11;
            this.stopServiceButton.Text = "Stop";
            this.stopServiceButton.UseVisualStyleBackColor = true;
            this.stopServiceButton.Click += new System.EventHandler(this.stopServiceButton_Click);
            // 
            // getBackButton
            // 
            this.getBackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.getBackButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.getBackButton.Location = new System.Drawing.Point(474, 688);
            this.getBackButton.Name = "getBackButton";
            this.getBackButton.Size = new System.Drawing.Size(110, 48);
            this.getBackButton.TabIndex = 12;
            this.getBackButton.Text = "Back to instances";
            this.getBackButton.UseVisualStyleBackColor = true;
            this.getBackButton.Click += new System.EventHandler(this.getBackButton_Click);
            // 
            // serviceStatusLabel
            // 
            this.serviceStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.serviceStatusLabel.AutoSize = true;
            this.serviceStatusLabel.Location = new System.Drawing.Point(267, 717);
            this.serviceStatusLabel.Name = "serviceStatusLabel";
            this.serviceStatusLabel.Size = new System.Drawing.Size(77, 13);
            this.serviceStatusLabel.TabIndex = 13;
            this.serviceStatusLabel.Text = "Service status:";
            // 
            // serviceStatusValueLabel
            // 
            this.serviceStatusValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.serviceStatusValueLabel.AutoSize = true;
            this.serviceStatusValueLabel.Location = new System.Drawing.Point(347, 717);
            this.serviceStatusValueLabel.Name = "serviceStatusValueLabel";
            this.serviceStatusValueLabel.Size = new System.Drawing.Size(63, 13);
            this.serviceStatusValueLabel.TabIndex = 14;
            this.serviceStatusValueLabel.Text = "Service info";
            // 
            // restartServiceButton
            // 
            this.restartServiceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.restartServiceButton.Location = new System.Drawing.Point(266, 688);
            this.restartServiceButton.Name = "restartServiceButton";
            this.restartServiceButton.Size = new System.Drawing.Size(90, 23);
            this.restartServiceButton.TabIndex = 15;
            this.restartServiceButton.Text = "Restart";
            this.restartServiceButton.UseVisualStyleBackColor = true;
            this.restartServiceButton.Click += new System.EventHandler(this.restartServiceButton_Click);
            // 
            // ConfigFormOld
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.exitButton;
            this.ClientSize = new System.Drawing.Size(704, 747);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.restartServiceButton);
            this.Controls.Add(this.serviceStatusValueLabel);
            this.Controls.Add(this.serviceStatusLabel);
            this.Controls.Add(this.getBackButton);
            this.Controls.Add(this.stopServiceButton);
            this.Controls.Add(this.editConfigButton);
            this.Controls.Add(this.saveConfigButton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.exitButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(720, 16384);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 720);
            this.Name = "ConfigFormOld";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Space engineers - Dedicated server configurator";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.newGameSettingsPanel.ResumeLayout(false);
            this.newGameSettingsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.asteroidAmountUD)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.QueryPortUD)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox gamesListBox;
        private System.Windows.Forms.RadioButton startGameButton;
        private System.Windows.Forms.RadioButton loadGameButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown QueryPortUD;
        private System.Windows.Forms.TextBox IPTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox scenarioCB;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown asteroidAmountUD;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel newGameSettingsPanel;
        private System.Windows.Forms.Button saveConfigButton;
        private System.Windows.Forms.Button editConfigButton;
        private System.Windows.Forms.TextBox serverNameTextBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button stopServiceButton;
        private System.Windows.Forms.Button getBackButton;
        private System.Windows.Forms.Label serviceStatusLabel;
        private System.Windows.Forms.Label serviceStatusValueLabel;
        private System.Windows.Forms.Button restartServiceButton;
        private System.Windows.Forms.Label steamAdminsLabel;
        private System.Windows.Forms.TextBox SteamGroupID;
        private System.Windows.Forms.Label steamGroupLabel;
        private System.Windows.Forms.TextBox adminIDs;
        private System.Windows.Forms.TextBox bannedIDs;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox pauseWhenEmptyCHB;
        private System.Windows.Forms.TextBox modIdsTextBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox ignoreLastSessionCHB;
        private System.Windows.Forms.TextBox worldNameTextBox;
        private System.Windows.Forms.Label label10;
    }
}

