using System;
using System.Windows.Forms;
using Sandbox.Game.Gui;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using System.Reflection;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using VRage.Utils;
using System.Collections.Generic;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Engine.Utils;
using VRage.Plugins;
using Sandbox.Game;
using System.ComponentModel.DataAnnotations;
using Sandbox.Game;

namespace VRage.Dedicated
{
    public partial class ConfigForm<T> : Form where T : MyObjectBuilder_SessionSettings, new()
    {
        public static Action OnReset;
        public static Sandbox.Common.ObjectBuilders.Game GameAttributes;
        public static System.Drawing.Image LogoImage;

        IMyAsyncResult m_loadWorldsAsync;
        MyObjectBuilder_SessionSettings m_selectedSessionSettings;
        bool m_canChangeStartType = false;

        public bool HasToExit { get; private set; }

        bool m_isService;
        string m_serviceName;

        ServiceController m_serviceController;

        public ConfigForm(bool isService, string serviceName)
        {
            m_isService = isService;
            m_serviceName = serviceName;

            if (isService) // if it's service get its handler
            {
                m_serviceController = new ServiceController(serviceName);
            }

            InitializeComponent();

            this.logoPictureBox.Image = LogoImage;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            saveConfigButton_Click(sender, e);

            if (m_isService) // Service
            {
                startService();
            }
            else // Local / Console
            {
                string[] cmdLine = Environment.GetCommandLineArgs();
                Process.Start(cmdLine[0], ((cmdLine.Length > 1) ? cmdLine[1] : "" ) + " -console -ignorelastsession");
                Close();
            }
        }

        struct ScenarioItem
        {
            public MyScenarioDefinition Definition;

            public override string ToString()
            {
                return Definition.DisplayNameEnum.HasValue ? Definition.DisplayNameEnum.ToString() : Definition.DisplayNameString;
            }
        }


        private void ConfigForm_Load(object sender, EventArgs e)
        {
            // hide everything related to service
            HasToExit = false;
            serviceStatusLabel.Hide();
            serviceStatusValueLabel.Hide();
            restartServiceButton.Hide();
            stopServiceButton.Hide();

            Text = MyPerServerSettings.GameName + " - Dedicated server configurator";

            if (m_isService)
            {
                // show everything that is related to service
                restartServiceButton.Show();
                stopServiceButton.Show();
                serviceStatusLabel.Show();
                serviceStatusValueLabel.Show();
                serviceStatusValueLabel.Text = "";

                // update service label
                updateServiceStatus();
            }

            MyLoadListResult worldList = new MyLoadListResult();

            m_loadWorldsAsync = new MyLoadListResult();
            worldListTimer.Enabled = true;
            MySandboxGame.ConfigDedicated.Load();

            UpdateLoadedData();
        }

        void UpdateLoadedData()
        {

            IPTextBox.Text = MySandboxGame.ConfigDedicated.IP;
            QueryPortUD.Value = MySandboxGame.ConfigDedicated.ServerPort;
            serverNameTextBox.Text = MySandboxGame.ConfigDedicated.ServerName;
            worldNameTextBox.Text = MySandboxGame.ConfigDedicated.WorldName;
            SteamGroupID.Text = MySandboxGame.ConfigDedicated.GroupID.ToString();
            adminIDs.Text = string.Join(Environment.NewLine, MySandboxGame.ConfigDedicated.Administrators.ToArray());
            bannedIDs.Text = string.Join(Environment.NewLine, MySandboxGame.ConfigDedicated.Banned.ToArray());
            modIdsTextBox.Text = string.Join(Environment.NewLine, MySandboxGame.ConfigDedicated.Mods.ToArray());
            pauseWhenEmptyCHB.Checked = MySandboxGame.ConfigDedicated.PauseGameWhenEmpty;
            ignoreLastSessionCHB.Checked = MySandboxGame.ConfigDedicated.IgnoreLastSession;

            scenarioCB.Items.Clear();


            MyDefinitionManager.Static.LoadScenarios();

            var scenarioId = MySandboxGame.ConfigDedicated.Scenario;

            foreach (var scenario in MyDefinitionManager.Static.GetScenarioDefinitions())
            {
                if (scenario.Public)
                {
                    ScenarioItem item = new ScenarioItem() { Definition = scenario };
                    scenarioCB.Items.Add(item);
                    if (scenario.Id == scenarioId)
                        scenarioCB.SelectedItem = item;
                }

            }
            if (scenarioCB.Items.Count > 0 && scenarioCB.SelectedIndex == -1)
                scenarioCB.SelectedIndex = 0;
        }

        struct WorldItem
        {
            public string SessionName;
            public string SessionPath;
            public string SessionDir;

            public override string ToString()
            {
                return SessionName;
            }
        }

        void FillWorldsList()
        {
            var loadListRes = (MyLoadListResult)m_loadWorldsAsync;
            var availableSaves = loadListRes.AvailableSaves;

            availableSaves.Sort((x, y) =>
            {
                return x.Item1.CompareTo(y.Item1);
            });

            gamesListBox.Items.Clear();
            if (availableSaves.Count != 0)
            {
                foreach (var save in availableSaves)
                {
                    WorldItem worldItem = new WorldItem()
                    {
                        SessionName = save.Item2.SessionName,
                        SessionPath = save.Item1,
                        SessionDir = System.IO.Path.GetFileName(save.Item1)
                    };
                    gamesListBox.Items.Add(worldItem);

                    if (MySandboxGame.ConfigDedicated.LoadWorld == worldItem.SessionPath)
                        gamesListBox.SelectedIndex = gamesListBox.Items.Count - 1;
                }
            }

            gamesListBox.Sorted = true;
        }

        private void worldListTimer_Tick(object sender, EventArgs e)
        {
            if (m_loadWorldsAsync.IsCompleted)
            {
                worldListTimer.Enabled = false;

                FillWorldsList();

                loadGameButton.Checked = !string.IsNullOrEmpty(MySandboxGame.ConfigDedicated.LoadWorld);
                startGameButton.Checked = !loadGameButton.Checked;

                m_canChangeStartType = true;
                startTypeRadio_CheckedChanged(null, null);
            }
        }

        private void gamesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (gamesListBox.SelectedIndex != -1)
            {
                ulong sizeInBytes;

                WorldItem worldItem = (WorldItem)gamesListBox.Items[gamesListBox.SelectedIndex];
                ((MyConfigDedicated<T>)MySandboxGame.ConfigDedicated).LoadWorld = worldItem.SessionPath;

                var loadPath = worldItem.SessionPath;
                MyObjectBuilder_Checkpoint checkpoint = MyLocalCache.LoadCheckpoint(loadPath, out sizeInBytes);

                m_selectedSessionSettings = checkpoint.Settings;

                MySandboxGame.ConfigDedicated.Mods.Clear();
                foreach (var mod in checkpoint.Mods)
                {
                    if (mod.PublishedFileId != 0)
                    {
                        MySandboxGame.ConfigDedicated.Mods.Add(mod.PublishedFileId);
                    }
                }

                modIdsTextBox.Text = string.Join(Environment.NewLine, MySandboxGame.ConfigDedicated.Mods.ToArray());

                FillSessionSettingsItems();
            }
        }

        private void startTypeRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_canChangeStartType)
                return;

            if (loadGameButton.Checked)
            {
                if (gamesListBox.Items.Count > 0)
                {
                    gamesListBox.Enabled = true;
                    //tableLayoutPanel1.Enabled = false;
                    newGameSettingsPanel.Enabled = false;
                    if (gamesListBox.SelectedIndex == -1)
                        gamesListBox.SelectedIndex = 0;
                }
                else
                {
                    startGameButton.Checked = true;
                    gamesListBox.Enabled = false;
                    //tableLayoutPanel1.Enabled = true;
                    newGameSettingsPanel.Enabled = true;

                    MySandboxGame.ConfigDedicated.Load();
                    m_selectedSessionSettings = MySandboxGame.ConfigDedicated.SessionSettings;
                }
            }
            else
            {
                gamesListBox.SelectedIndex = -1;
                gamesListBox.Enabled = false;
                //tableLayoutPanel1.Enabled = true;
                newGameSettingsPanel.Enabled = true;

                MySandboxGame.ConfigDedicated.Load();
                m_selectedSessionSettings = MySandboxGame.ConfigDedicated.SessionSettings;

                //enable tool shake needs to be true for new world, but false for old saved worlds.                                
                m_selectedSessionSettings.EnableToolShake = true;

                m_selectedSessionSettings.EnablePlanets = MyFakes.ENABLE_PLANETS;
                m_selectedSessionSettings.EnableSunRotation = true;
                m_selectedSessionSettings.EnableStationVoxelSupport = true;

            }

            FillSessionSettingsItems();
        }

        void FillSessionSettingsItems()
        {
            tableLayoutPanel1.RowCount = 0;
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.Controls.Clear();

            Type sessionSettingsType = typeof(T);
            foreach (var sessionField in sessionSettingsType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.SetField))
            {
                if (sessionField.FieldType == typeof(MyOnlineModeEnum))
                    continue;

                if (sessionField.Name.Contains("Procedural"))
                {
                    if (Sandbox.Engine.Utils.MyFakes.ENABLE_ASTEROID_FIELDS)
                    {
                        if (loadGameButton.Checked)
                        {
                            continue; // skip procedural stuff when editing existing world
                        }
                    }
                    else
                    {
                        continue; // skip procredural stuff when disabled
                    }
                }

                string displayName = sessionField.Name;

                var nameAttr = sessionField.GetCustomAttributes(typeof(DisplayAttribute), true);
                if (nameAttr.Length > 0)
                    displayName = ((DisplayAttribute)nameAttr[0]).Name;

                if (string.IsNullOrEmpty(displayName))
                    continue;

                var gameAttr = sessionField.GetCustomAttributes(typeof(GameRelationAttribute), true);
                if (gameAttr.Length > 0)
                {
                    var gameRel = ((GameRelationAttribute)gameAttr[0]).RelatedTo;
                    if (gameRel != Sandbox.Common.ObjectBuilders.Game.Shared && gameRel != GameAttributes)
                        continue;
                }

                var rangeAttr = sessionField.GetCustomAttributes(typeof(RangeAttribute), true);
                decimal minValue = decimal.MinValue;
                decimal maxValue = decimal.MaxValue;
                if (rangeAttr.Length > 0)
                {
                    minValue = Convert.ToDecimal(((RangeAttribute)rangeAttr[0]).Minimum);
                    maxValue = Convert.ToDecimal(((RangeAttribute)rangeAttr[0]).Maximum);
                }

                if (sessionField.FieldType == typeof(float))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    NumericUpDown nup = new NumericUpDown();
                    nup.Minimum = minValue;
                    nup.Maximum = maxValue;
                    nup.Value = (decimal)(float)sessionField.GetValue(m_selectedSessionSettings);
                    nup.Tag = sessionField;
                    nup.ValueChanged += nup_ValueChanged2;
                    nup.Name = sessionField.Name;
                    nup.DecimalPlaces = 2;
                    fieldPanel.Controls.Add(nup);

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType.BaseType == typeof(Enum))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    ComboBox combo = new ComboBox();
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    combo.Name = sessionField.Name;

                    int i = 0;
                    foreach (var value in Enum.GetValues(sessionField.FieldType))
                    {
                        combo.Items.Add(value);
                        var val = sessionField.GetValue(m_selectedSessionSettings);
                        if (value.Equals(val))
                        {
                            combo.SelectedIndex = i;
                        }
                        i++;
                    }
                    combo.Tag = sessionField;
                    fieldPanel.Controls.Add(combo);


                    combo.SelectedIndexChanged += combo_SelectedIndexChanged;

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType == typeof(short))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    NumericUpDown nup = new NumericUpDown();
                    nup.Minimum = minValue;
                    nup.Maximum = maxValue;
                    nup.Value = (decimal)System.Convert.ToInt16(sessionField.GetValue(m_selectedSessionSettings));
                    nup.DecimalPlaces = 0;
                    nup.Increment = 1;
                    nup.Tag = sessionField;
                    nup.ValueChanged += nup_ValueChanged;
                    nup.Name = sessionField.Name;
                    fieldPanel.Controls.Add(nup);

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType == typeof(int))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    NumericUpDown nup = new NumericUpDown();
                    nup.Minimum = minValue;
                    nup.Maximum = maxValue;
                    nup.Value = (decimal)System.Convert.ToInt32(sessionField.GetValue(m_selectedSessionSettings));
                    nup.DecimalPlaces = 0;
                    nup.Increment = 1;
                    nup.Tag = sessionField;
                    nup.ValueChanged += nup_ValueChanged3;
                    nup.Name = sessionField.Name;
                    fieldPanel.Controls.Add(nup);

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType == typeof(uint))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    NumericUpDown nup = new NumericUpDown();
                    nup.Minimum = minValue;
                    nup.Maximum = maxValue;
                    nup.Value = (decimal)System.Convert.ToUInt32(sessionField.GetValue(m_selectedSessionSettings));
                    nup.DecimalPlaces = 0;
                    nup.Increment = 1;
                    nup.Tag = sessionField;
                    nup.ValueChanged += nup_ValueChanged4;
                    nup.Name = sessionField.Name;
                    fieldPanel.Controls.Add(nup);

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType == typeof(bool))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    CheckBox checkBox = new CheckBox();
                    checkBox.Checked = (bool)sessionField.GetValue(m_selectedSessionSettings);
                    checkBox.Tag = sessionField;
                    checkBox.CheckedChanged += checkBox_CheckedChanged;
                    checkBox.Name = sessionField.Name;
                    fieldPanel.Controls.Add(checkBox);

                    AddNewRow(fieldPanel);
                }

                if (sessionField.FieldType.IsGenericType && sessionField.FieldType.GetGenericArguments()[0] == typeof(bool))
                {
                    FlowLayoutPanel fieldPanel = AddFieldLabel(sessionField, displayName);

                    CheckBox checkBox = new CheckBox();
                    var pops = sessionField.GetType().GetProperties();
                    var nulBool = (bool?)sessionField.GetValue(m_selectedSessionSettings);
                    checkBox.Checked = nulBool.HasValue ? nulBool.Value : true;
                    
                    checkBox.Tag = sessionField;
                    checkBox.CheckedChanged += checkBox_CheckedChanged;
                    checkBox.Name = sessionField.Name;
                    fieldPanel.Controls.Add(checkBox);

                    AddNewRow(fieldPanel);
                }

            }
            EnableCopyPaste(null);
            EnableOxygen();
        }

        void EnableCopyPaste(ComboBox sender)
        {
            var enabled = false;

            if (sender != null)
                enabled = sender.SelectedIndex == 0;
            else
                enabled = (tableLayoutPanel1.Controls.Find("GameMode", true)[0] as ComboBox).SelectedIndex == 0;

            var checkLabel = tableLayoutPanel1.Controls.Find("EnableCopyPasteLabel", true);
            var checkBox = tableLayoutPanel1.Controls.Find("EnableCopyPaste", true)[0] as CheckBox;

            checkBox.Enabled = enabled;
            checkBox.Checked = enabled;
            checkLabel[0].Enabled = enabled;
            m_selectedSessionSettings.EnableCopyPaste = enabled;

            checkLabel = tableLayoutPanel1.Controls.Find("PermanentDeathLabel", true);
            var foundControls = tableLayoutPanel1.Controls.Find("PermanentDeath", true);

            if (foundControls.Length > 0)
            {
                checkBox = foundControls[0] as CheckBox;
                checkBox.Enabled = !enabled;
                checkBox.Checked = !enabled;
                checkLabel[0].Enabled = !enabled;
                m_selectedSessionSettings.PermanentDeath &= !enabled;
            }
        }

        void EnableOxygen()
        {
            var foundControls = tableLayoutPanel1.Controls.Find("VoxelGeneratorVersion", true);
            if (foundControls.Length > 0)
            {
                var voxelGeneratorControl = foundControls[0] as NumericUpDown;
                voxelGeneratorControl.Minimum = 0;
                voxelGeneratorControl.Maximum = VRage.Voxels.MyVoxelConstants.VOXEL_GENERATOR_VERSION;

                var oxygenControl = tableLayoutPanel1.Controls.Find("EnableOxygen", true)[0] as CheckBox;
                oxygenControl.CheckedChanged += oxygenCheckBox_CheckedChanged;

                if (newGameSettingsPanel.Enabled)
                {
                    oxygenControl.Checked = true;
                    voxelGeneratorControl.Value = VRage.Voxels.MyVoxelConstants.VOXEL_GENERATOR_VERSION;
                }
            }
        }

        void oxygenCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var voxelGeneratorControl = tableLayoutPanel1.Controls.Find("VoxelGeneratorVersion", true)[0] as NumericUpDown;
            var oxygenControl = (CheckBox)sender;
            if (oxygenControl.Checked)
            {
                if (voxelGeneratorControl.Value < VRage.Voxels.MyVoxelConstants.VOXEL_GENERATOR_MIN_ICE_VERSION)
                {
                    voxelGeneratorControl.Value = VRage.Voxels.MyVoxelConstants.VOXEL_GENERATOR_MIN_ICE_VERSION;
                }
            }
        }

        void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            FieldInfo fieldInfo = (FieldInfo)((CheckBox)sender).Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, ((CheckBox)sender).Checked);
        }

        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            FieldInfo fieldInfo = (FieldInfo)cb.Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, cb.SelectedItem);

            if ((sender as ComboBox).Name == "GameMode")
                EnableCopyPaste(sender as ComboBox);
        }

        void nup_ValueChanged4(object sender, EventArgs e)
        {
            FieldInfo fieldInfo = (FieldInfo)((NumericUpDown)sender).Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, (uint)((NumericUpDown)sender).Value);
        }

        void nup_ValueChanged3(object sender, EventArgs e)
        {
            FieldInfo fieldInfo = (FieldInfo)((NumericUpDown)sender).Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, (int)((NumericUpDown)sender).Value);
        }

        void nup_ValueChanged2(object sender, EventArgs e)
        {
            FieldInfo fieldInfo = (FieldInfo)((NumericUpDown)sender).Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, (float)((NumericUpDown)sender).Value);
        }

        void nup_ValueChanged(object sender, EventArgs e)
        {
            FieldInfo fieldInfo = (FieldInfo)((NumericUpDown)sender).Tag;
            fieldInfo.SetValue(m_selectedSessionSettings, (short)((NumericUpDown)sender).Value);
        }

        private static FlowLayoutPanel AddFieldLabel(System.Reflection.FieldInfo sessionField, string displayName)
        {
            FlowLayoutPanel fieldPanel = CreateFieldPanel();

            Label label = new Label();
            label.Size = new System.Drawing.Size(190, 22);
            label.Text = displayName;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label.Name = sessionField.Name + "Label";
            fieldPanel.Controls.Add(label);
            fieldPanel.Name = sessionField.Name + "Row";

            return fieldPanel;
        }

        private static FlowLayoutPanel CreateFieldPanel()
        {
            FlowLayoutPanel fieldPanel = new FlowLayoutPanel();
            fieldPanel.FlowDirection = FlowDirection.LeftToRight;
            fieldPanel.AutoSize = true;
            fieldPanel.Margin = new Padding(0);
            return fieldPanel;
        }

        private void AddNewRow(FlowLayoutPanel fieldPanel)
        {
            tableLayoutPanel1.RowCount++;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            tableLayoutPanel1.Controls.Add(fieldPanel);
            tableLayoutPanel1.SetRow(fieldPanel, tableLayoutPanel1.RowCount - 1);
        }

        void SaveConfiguration(string file = null)
        {
            MySandboxGame.ConfigDedicated.IP = IPTextBox.Text;
            MySandboxGame.ConfigDedicated.ServerPort = (int)QueryPortUD.Value;
            MySandboxGame.ConfigDedicated.ServerName = serverNameTextBox.Text;
            MySandboxGame.ConfigDedicated.WorldName = worldNameTextBox.Text;
            MySandboxGame.ConfigDedicated.PauseGameWhenEmpty = pauseWhenEmptyCHB.Checked;
            MySandboxGame.ConfigDedicated.IgnoreLastSession = ignoreLastSessionCHB.Checked;

            if (!string.IsNullOrEmpty(SteamGroupID.Text))
            {
                try
                {
                    MySandboxGame.ConfigDedicated.GroupID = Convert.ToUInt64(SteamGroupID.Text);
                }
                catch
                {
                    MessageBox.Show(SteamGroupID.Text + " is not valid group ID!");
                    MySandboxGame.ConfigDedicated.GroupID = 0;
                }
            }
            else
                MySandboxGame.ConfigDedicated.GroupID = 0;

            MySandboxGame.ConfigDedicated.Administrators.Clear();
            foreach (var id in adminIDs.Lines)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        MySandboxGame.ConfigDedicated.Administrators.Add(id);
                    }
                    catch
                    {
                        MessageBox.Show(id + " is not valid admin ID!");
                    }
                }
            }

            MySandboxGame.ConfigDedicated.Banned.Clear();
            foreach (var id in bannedIDs.Lines)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        MySandboxGame.ConfigDedicated.Banned.Add(Convert.ToUInt64(id.ToString()));
                    }
                    catch
                    {
                        MessageBox.Show(id + " is not valid ID for ban!");
                    }
                }
            }


            MySandboxGame.ConfigDedicated.Mods.Clear();
            foreach (var id in modIdsTextBox.Lines)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        MySandboxGame.ConfigDedicated.Mods.Add(Convert.ToUInt64(id.ToString()));
                    }
                    catch
                    {
                        MessageBox.Show(id + " is not valid ID for mod!");
                    }
                }
            }

            if (scenarioCB.SelectedItem != null)
                MySandboxGame.ConfigDedicated.Scenario = ((ScenarioItem)scenarioCB.SelectedItem).Definition.Id;

            MySandboxGame.ConfigDedicated.AsteroidAmount = 0;

            if (startGameButton.Checked)
                ((MyConfigDedicated<T>)MySandboxGame.ConfigDedicated).LoadWorld = "";

            m_selectedSessionSettings.OnlineMode = MyOnlineModeEnum.PUBLIC;
            MySandboxGame.ConfigDedicated.SessionSettings = m_selectedSessionSettings;
            MySandboxGame.ConfigDedicated.Save(file);
        }

        private void saveConfigButton_Click(object sender, EventArgs e)
        {
            SaveConfiguration();

            if (!string.IsNullOrEmpty(MySandboxGame.ConfigDedicated.LoadWorld))
            {
                ulong sizeInBytes;
                var path = MySandboxGame.ConfigDedicated.LoadWorld;

                MyObjectBuilder_Checkpoint checkpoint = MyLocalCache.LoadCheckpoint(path, out sizeInBytes);

                if (!string.IsNullOrWhiteSpace(MySandboxGame.ConfigDedicated.WorldName))
                {
                    checkpoint.SessionName = MySandboxGame.ConfigDedicated.WorldName;
                }

                checkpoint.Settings = m_selectedSessionSettings;

                checkpoint.Mods.Clear();
                foreach (ulong publishedFileId in MySandboxGame.ConfigDedicated.Mods)
                {
                    checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(publishedFileId));
                }

                MyLocalCache.SaveCheckpoint(checkpoint, path);
            }
        }


        private void saveAsButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Configuration file (*.xml)|*.xml|All files (*.*)|*.*";;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SaveConfiguration(sfd.FileName);
            }
        }

        private void loadConfigButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Configuration file (*.xml)|*.xml|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                MySandboxGame.ConfigDedicated.Load(ofd.FileName);

                m_selectedSessionSettings = MySandboxGame.ConfigDedicated.SessionSettings;

                UpdateLoadedData();

                FillSessionSettingsItems();
            }
        }


        private void resetButton_Click(object sender, EventArgs e)
        {
            OnReset();

            m_selectedSessionSettings = MySandboxGame.ConfigDedicated.SessionSettings;

            UpdateLoadedData();

            FillSessionSettingsItems();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            MySandboxGame.ConfigDedicated.Load();

            m_selectedSessionSettings = MySandboxGame.ConfigDedicated.SessionSettings;

            UpdateLoadedData();

            FillSessionSettingsItems();
        }


        private void editConfigButton_Click(object sender, EventArgs e)
        {
            string path = MySandboxGame.ConfigDedicated.GetFilePath();
            Process.Start("notepad.exe", path);
        }

        // Get back to selection of instance
        private void getBackButton_Click(object sender, EventArgs e)
        {
            HasToExit = true;
            MyFileSystem.Reset();
            Close();
        }
        
        #region Service Controls
        
        private void restartServiceButton_Click(object sender, EventArgs e)
        {
            if (m_serviceController.Status != ServiceControllerStatus.Stopped)
            {
                try
                {
                    m_serviceController.Stop();
                    updateServiceStatus();

                    m_serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                    updateServiceStatus();

                    m_serviceController.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        GetAllExceptionMessages(ex),
                        "Invalid operation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Service is not running.",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void stopServiceButton_Click(object sender, EventArgs e)
        {
            stopService();
        }

        private void updateServiceStatus()
        {
            if (m_isService)
            {
                m_serviceController.Refresh();
                switch (m_serviceController.Status)
                {
                    case ServiceControllerStatus.Running: { serviceStatusValueLabel.Text = "running"; break; }
                    case ServiceControllerStatus.Stopped: { serviceStatusValueLabel.Text = "stopped"; break; }
                    case ServiceControllerStatus.ContinuePending: { serviceStatusValueLabel.Text = "continue pending"; break; }
                    case ServiceControllerStatus.Paused: { serviceStatusValueLabel.Text = "paused"; break; }
                    case ServiceControllerStatus.PausePending: { serviceStatusValueLabel.Text = "pause pending"; break; }
                    case ServiceControllerStatus.StartPending: { serviceStatusValueLabel.Text = "start pending"; break; }
                    case ServiceControllerStatus.StopPending: { serviceStatusValueLabel.Text = "stop pending"; break; }
                }

                restartServiceButton.Enabled = m_serviceController.Status == ServiceControllerStatus.Running;
                stopServiceButton.Enabled = m_serviceController.Status == ServiceControllerStatus.Running;

                if (m_isService)
                    startButton.Enabled = m_serviceController.Status == ServiceControllerStatus.Stopped;
            }

        }
        
        private void startService()
        {
            if (m_serviceController.Status != ServiceControllerStatus.Running)
            {
                try
                {
                    m_serviceController.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        GetAllExceptionMessages(ex),
                        "Invalid operation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Service is already running.",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void stopService()
        {
            if (m_serviceController.Status == ServiceControllerStatus.Running)
            {
                try
                {
                    m_serviceController.Stop();
                    updateServiceStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        GetAllExceptionMessages(ex),
                        "Invalid operation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Service is not running.",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

#endregion

        /// <summary>
        /// Gets all exception messages (includoing innerExceptions) of given Exception.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>String that contains all exception messages seperated by new line.</returns>
        internal static string GetAllExceptionMessages(Exception ex)
        {
            if (ex == null)
                return "";

            StringBuilder sb = new StringBuilder();

            while (ex != null)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(Environment.NewLine);
                    }
                    sb.Append(ex.Message);
                }
                ex = ex.InnerException;
            }
            return sb.ToString();
        }

        private void serviceUpdateTimer_Tick(object sender, EventArgs e)
        {
            updateServiceStatus();
        }
    }
}
