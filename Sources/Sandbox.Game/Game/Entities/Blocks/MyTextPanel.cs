using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Game;
using VRageMath;
using VRage.Utils;
using VRage.Game.Entity.UseObject;
using VRage.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI;
using VRage.Sync;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TextPanel))]
    public partial class MyTextPanel : MyFunctionalBlock
    {
        private const int NUM_DECIMALS = 3;
        public const double MAX_DRAW_DISTANCE = 200.0;
        private const int DEFAULT_RESOLUTION = 512;
        private const int MAX_NUMBER_CHARACTERS = 100000;
        private const string DEFAULT_OFFLINE_TEXTURE = "Offline";
        private const string DEFAULT_ONLINE_TEXTURE = "Online";

        private StringBuilder m_publicDescription;
        private StringBuilder m_publicTitle;
        private StringBuilder m_privateDescription;
        private StringBuilder m_privateTitle;
        private MyGuiScreenTextPanel m_textBox;
        private bool m_isOpen;
        private ulong m_userId;

        private static StringBuilder m_helperSB = new StringBuilder();
        int m_currentPos = 0;
        int m_previousUpdateTime = 0;
        bool m_isOutofRange = false;
        string m_previousTextureID = null;
        bool m_forceUpdateText = false;

        bool m_isEditingPublic = false;

        public bool ShowTextOnScreen
        {
            get
            {
                return m_showFlag != ShowTextOnScreenFlag.NONE;
            }
        }

        List<MyLCDTextureDefinition> m_selectedTexturesToDraw = new List<MyLCDTextureDefinition>();
        List<MyLCDTextureDefinition> m_definitions = new List<MyLCDTextureDefinition>();
        List<MyGuiControlListbox.Item> m_selectedTextures = new List<MyGuiControlListbox.Item>();
        List<MyGuiControlListbox.Item> m_selectedTexturesToRemove = new List<MyGuiControlListbox.Item>();

        Sync<Color> m_backgroundColor;
        bool m_backgroundColorChanged = true;
        public Color BackgroundColor
        {
            get { return m_backgroundColor; }
            set { m_backgroundColor.Value = value; }
        }

        void m_backgroundColor_ValueChanged(SyncBase obj)
        {
            m_backgroundColorChanged = true;
            RaisePropertiesChanged();
        }

        Sync<Color> m_fontColor;
        bool m_fontColorChanged = true;
        public Color FontColor
        {
            get { return m_fontColor; }
            set { m_fontColor.Value = value; }
        }

        Sync<string> m_font;
        bool m_fontChanged = true;
        public MyDefinitionId Font
        {
            get { return new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), m_font); }
            set 
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(value.SubtypeName), "Font must always have value. Use Debug font as default.");
                m_font.Value = value.SubtypeName; 
        }
        }


        void m_fontColor_ValueChanged(SyncBase obj)
        {
            m_fontColorChanged = true;
            RaisePropertiesChanged();
        }

        void m_font_ValueChanged(SyncBase obj)
        {
            m_fontChanged = true;
            RaisePropertiesChanged();
        }

        bool m_descriptionChanged = true;
        public StringBuilder PublicDescription
        {
            get { return m_publicDescription; }
            set
            {
                m_descriptionChanged = m_publicDescription.CompareUpdate(value);

                if (m_publicDescriptionHelper != value)
                {
                    m_publicDescriptionHelper.Clear().Append(value);
                }
            }
        }

        public StringBuilder PublicTitle
        {
            get { return m_publicTitle; }
            set
            {
                m_publicTitle.CompareUpdate(value);

                if (m_publicTitleHelper != value)
                {
                    m_publicTitleHelper.Clear().Append(value);
                }
            }
        }

        public StringBuilder PrivateTitle
        {
            get { return m_privateTitle; }
            set
            {
                m_privateTitle.CompareUpdate(value);

                if (m_privateTitleHelper != value)
                {
                    m_privateTitleHelper.Clear().Append(value);
                }
            }
        }

        public StringBuilder PrivateDescription
        {
            get { return m_privateDescription; }
            set
            {
                m_descriptionChanged = m_privateDescription.CompareUpdate(value);

                if (m_privateDescriptionHelper != value)
                {
                    m_privateDescriptionHelper.Clear().Append(value);
                }
            }
        }

        bool m_failedToRenderTexture = false;

        public bool FailedToRenderTexture
        {
            get
            {
                return m_failedToRenderTexture;
            }
            set
            {
                if (value == true)
                {
                    Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
                }
                m_failedToRenderTexture = value;

            }
        }

        private Sync<ShowTextOnScreenFlag> m_showFlag;
        public ShowTextOnScreenFlag ShowTextFlag
        {
            get { return m_showFlag; }
            set { m_showFlag.Value = value; }
        }

        void m_showFlag_ValueChanged(SyncBase obj)
        {
            if (m_showFlag != ShowTextOnScreenFlag.NONE)
            {
                m_forceUpdateText = true;
            }
            else
            {
                ReleaseRenderTexture();
                m_forceUpdateText = false;
                m_previousTextureID = null;
            }
        }

        public bool IsOpen
        {
            get { return m_isOpen; }
            set
            {
                if (m_isOpen != value)
                {
                    m_isOpen = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public ulong UserId
        {
            get { return m_userId; }
            set { m_userId = value; }
        }

        private readonly Sync<float> m_changeInterval;
        public float ChangeInterval
        {
            get { return m_changeInterval; }
            set { m_changeInterval.Value = (float)Math.Round(value, NUM_DECIMALS); }
        }

        void m_changeInterval_ValueChanged(SyncBase obj)
        {
            RaisePropertiesChanged();
        }

        bool m_fontSizeChanged = true;
        private readonly Sync<float> m_fontSize;
        public float FontSize
        {
            get { return m_fontSize; }
            set { m_fontSize.Value = (float)Math.Round(value, NUM_DECIMALS); }
        }

        void m_fontSize_ValueChanged(SyncBase obj)
        {
            m_fontSizeChanged = true;
            RaisePropertiesChanged();
        }

        internal new MyRenderComponentTextPanel Render
        {
            get { return base.Render as MyRenderComponentTextPanel; }
            set { base.Render = value; }
        }

        public new MyTextPanelDefinition BlockDefinition
        {
            get { return (MyTextPanelDefinition)base.BlockDefinition; }
        }

        public void FillListContent(ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems)
        {
            foreach (var texture in m_definitions)
            {
                m_helperSB.Clear().Append(texture.Id.SubtypeName);
                var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: texture.TexturePath);
                listBoxContent.Add(item);
            }
        }

        public void FillSelectedListContent(ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems)
        {
            foreach (var texture in m_selectedTexturesToDraw)
            {
                m_helperSB.Clear().Append(texture.Id.SubtypeName);
                var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: texture.TexturePath);
                listBoxContent.Add(item);
            }
        }

        public void SelectImage(List<MyGuiControlListbox.Item> imageId)
        {
            m_selectedTexturesToRemove.Clear();
            for (int i = 0; i < imageId.Count; i++)
            {
                m_selectedTexturesToRemove.Add(imageId[i]);
            }
        }

        public void SelectImageToDraw(List<MyGuiControlListbox.Item> imageIds)
        {
            m_selectedTextures.Clear();
            for (int i = 0; i < imageIds.Count; i++)
            {
                m_selectedTextures.Add(imageIds[i]);
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (IsBeingHacked)
            {
                PrivateDescription.Clear();
                SendChangeDescriptionMessage(PrivateDescription, false);
            }
            ResourceSink.Update();
            if (IsFunctional && IsWorking)
            {
                if (ShowTextOnScreen && IsInRange() == false)
                {
                    if (!m_isOutofRange)
                    {
                        m_isOutofRange = true;
                        ReleaseRenderTexture();
                    }
                    return;
                }

                if (ShowTextOnScreen && (NeedsToDrawText() || m_isOutofRange || m_forceUpdateText))
                {
                    m_descriptionChanged = false;
                    m_forceUpdateText = false;
                    m_fontColorChanged = false;
                    m_fontSizeChanged = false;
                    m_backgroundColorChanged = false;
                    m_fontChanged = false;
                    Render.RenderTextToTexture(ShowTextFlag == ShowTextOnScreenFlag.PUBLIC ? m_publicDescription.ToString() : m_privateDescription.ToString(),
                        FontSize * BlockDefinition.TextureResolution / DEFAULT_RESOLUTION, FontColor, BackgroundColor,
                        BlockDefinition.TextureResolution, BlockDefinition.TextureAspectRadio);
                    FailedToRenderTexture = false;
                }

                m_isOutofRange = false;

                if (ShowTextOnScreen == false)
                {
                    UpdateTexture();
                }
            }
            else if (IsOpen)
            {
                SendChangeOpenMessage(false);
                if (m_textBox != null)
                {
                    m_textBox.CloseScreen();
                    //m_textBox = null;
                }
                MyScreenManager.CloseScreen(typeof(MyGuiScreenTerminal));
            }
        }

        private void UpdateTexture()
        {
            if (m_selectedTexturesToDraw.Count > 0) // At least 1ms
            {
                int imageIntervalMiliseconds = (int)(ChangeInterval * 1000.0f);
                if (imageIntervalMiliseconds > 0)
                {
                    int elapsedTimeMs = (int)(MySession.Static.ElapsedGameTime.TotalMilliseconds) % imageIntervalMiliseconds;
                    if (m_previousUpdateTime - elapsedTimeMs > 0)
                    {
                        m_currentPos++;
                    }
                    m_previousUpdateTime = elapsedTimeMs;
                }
                if (m_currentPos >= m_selectedTexturesToDraw.Count)
                {
                    m_currentPos = 0;
                }
                if (m_previousTextureID != m_selectedTexturesToDraw[m_currentPos].Id.SubtypeName)
                {
                    m_previousTextureID = m_selectedTexturesToDraw[m_currentPos].Id.SubtypeName;
                    Render.ChangeTexture(m_selectedTexturesToDraw[m_currentPos].TexturePath);
                }
            }
        }

        bool NeedsToDrawText()
        {
            return ShowTextOnScreen && (m_descriptionChanged || m_fontSizeChanged || m_fontColorChanged || m_fontChanged || m_backgroundColorChanged);
        }

        public void AddImagesToSelection()
        {
            if (m_selectedTextures == null)
            {
                return;
            }
            if (m_selectedTextures.Count == 0)
            {
                return;
            }
            int[] selection = new int[m_selectedTextures.Count];

            for (int i = 0; i < m_selectedTextures.Count; ++i)
            {
                for (int j = 0; j < m_definitions.Count; ++j)
                {
                    if (m_selectedTextures[i].Text.ToString() == m_definitions[j].Id.SubtypeName)
                    {
                        selection[i] = j;
                        break;
                    }
                }
            }
            SendAddImagesToSelectionRequest(selection);
        }

        public void RemoveImagesFromSelection()
        {
            if (m_selectedTexturesToRemove == null)
            {
                return;
            }
            if (m_selectedTexturesToRemove.Count == 0)
            {
                return;
            }
            m_previousTextureID = null;
            int[] selection = new int[m_selectedTexturesToRemove.Count];

            for (int i = 0; i < m_selectedTexturesToRemove.Count; ++i)
            {
                for (int j = 0; j < m_definitions.Count; ++j)
                {
                    if (m_selectedTexturesToRemove[i].Text.ToString() == m_definitions[j].Id.SubtypeName)
                    {
                        selection[i] = j;
                        break;
                    }
                }
            }
            SendRemoveSelectedImageRequest(selection);
        }

        public void SelectItems(int[] selection)
        {
            for (int j = 0; j < selection.Length; ++j)
            {
                if (selection[j] >= m_definitions.Count)
                {
                    continue;
                }
                m_selectedTexturesToDraw.Add(m_definitions[selection[j]]);
            }
            m_currentPos = 0;
            RaisePropertiesChanged();
        }

        public void RemoveItems(int[] selection)
        {
            for (int j = 0; j < selection.Length; ++j)
            {
                if (selection[j] >= m_definitions.Count)
                {
                    continue;
                }
                m_selectedTexturesToDraw.Remove(m_definitions[selection[j]]);
            }
            m_currentPos = 0;

            if (m_selectedTexturesToDraw.Count == 0)
            {
                if (CheckIsWorking() == false)
                {
                    Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
                }
                else
                {
                    if (ShowTextOnScreen == false)
                    {
                        Render.ChangeTexture(GetPathForID(DEFAULT_ONLINE_TEXTURE));
                    }
                    m_previousTextureID = null;
                    m_forceUpdateText = ShowTextOnScreen;
                }
            }

            RaisePropertiesChanged();
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            UpdateText();
            UpdateIsWorking();
            if (Render == null)
            {
                Debug.Fail("Closed entity!");
                return;
            }
            if (CheckIsWorking() == false)
            {
                Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
            }
            else
            {
                if (ShowTextOnScreen == false)
                {
                    Render.ChangeTexture(GetPathForID(DEFAULT_ONLINE_TEXTURE));
                }
                m_previousTextureID = null;
                m_forceUpdateText = ShowTextOnScreen;
            }
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            ResourceSink.Update();
            if (IsFunctional)
            {
                if (ShowTextOnScreen == false)
                {
                    Render.ChangeTexture(GetPathForID(DEFAULT_ONLINE_TEXTURE));
                }
                m_previousTextureID = null;
                m_forceUpdateText = ShowTextOnScreen;
            }
            else
            {
                Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            ComponentStack_IsFunctionalChanged();//after merging grids...
            if (!IsWorking && ShowTextOnScreen)
                Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
        }

        public MyTextPanel()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_backgroundColor = SyncType.CreateAndAddProp<Color>();
            m_fontColor = SyncType.CreateAndAddProp<Color>();
            m_accessFlag = SyncType.CreateAndAddProp<TextPanelAccessFlag>();
            m_showFlag = SyncType.CreateAndAddProp<ShowTextOnScreenFlag>();
            m_changeInterval = SyncType.CreateAndAddProp<float>();
            m_fontSize = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            m_publicDescription = new StringBuilder();
            m_textBox = null;
            m_publicTitle = new StringBuilder();
            m_isOpen = false;

            m_privateDescription = new StringBuilder();
            m_privateTitle = new StringBuilder();

            Render = new MyRenderComponentTextPanel(this);
            m_definitions.Clear();
            foreach (var textureDefinition in MyDefinitionManager.Static.GetLCDTexturesDefinitions())
            {
                m_definitions.Add(textureDefinition);
            }

            m_backgroundColor.Value = Color.Black;
            m_fontColor.Value = Color.White;
            m_changeInterval.Value = 0;
            m_fontSize.Value = 1.0f;
            m_font.Value = "Debug";

            m_backgroundColor.ValueChanged += m_backgroundColor_ValueChanged;
            m_font.ValueChanged += m_font_ValueChanged;
            m_fontColor.ValueChanged += m_fontColor_ValueChanged;
            m_showFlag.ValueChanged += m_showFlag_ValueChanged;
            m_changeInterval.ValueChanged += m_changeInterval_ValueChanged;
            m_fontSize.ValueChanged += m_fontSize_ValueChanged;
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyTextPanel>())
                return;
            base.CreateTerminalControls();
            var publicTitleField = new MyTerminalControlTextbox<MyTextPanel>("Title", MySpaceTexts.BlockPropertyTitle_TextPanelPublicTitle, MySpaceTexts.Blank);
            publicTitleField.Getter = (x) => x.PublicTitle;
            publicTitleField.Setter = (x, v) => x.SendChangeTitleMessage(v, true);
            publicTitleField.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(publicTitleField);

            var showPublicButton = new MyTerminalControlButton<MyTextPanel>("ShowTextPanel", MySpaceTexts.BlockPropertyTitle_TextPanelShowPublicTextPanel, MySpaceTexts.Blank, (x) => x.OpenWindow(true, true, true));
            showPublicButton.Enabled = (x) => !x.IsOpen;
            showPublicButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(showPublicButton);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTextPanel>());

            var showTextOnScreen = new MyTerminalControlOnOffSwitch<MyTextPanel>("ShowTextOnScreen", MySpaceTexts.BlockPropertyTitle_ShowTextOnScreen, MySpaceTexts.Blank);
            showTextOnScreen.Getter = (x) => x.ShowTextFlag != ShowTextOnScreenFlag.NONE;
            showTextOnScreen.Setter = (x, y) => x.ShowTextFlag = y ? ShowTextOnScreenFlag.PUBLIC : ShowTextOnScreenFlag.NONE;

            MyTerminalControlFactory.AddControl(showTextOnScreen);


            var comboFont = new MyTerminalControlCombobox<MyTextPanel>("Font", MySpaceTexts.BlockPropertyTitle_Font, MySpaceTexts.Blank);
            comboFont.ComboBoxContent = (x) => FillFontComboBoxContent(x);
            comboFont.Getter = (x) => (long)x.Font.SubtypeId;
            comboFont.Setter = (x, y) => x.Font = new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), MyStringHash.TryGet((int)y));
            MyTerminalControlFactory.AddControl(comboFont);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTextPanel>());

            var changeFontSlider = new MyTerminalControlSlider<MyTextPanel>("FontSize", MySpaceTexts.BlockPropertyTitle_LCDScreenTextSize, MySpaceTexts.Blank);
            changeFontSlider.SetLimits(0.1f, 10.0f);
            changeFontSlider.DefaultValue = 1.0f;
            changeFontSlider.Getter = (x) => x.FontSize;
            changeFontSlider.Setter = (x, v) => x.FontSize = v;
            changeFontSlider.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.FontSize, NUM_DECIMALS));
            changeFontSlider.EnableActions();
            MyTerminalControlFactory.AddControl(changeFontSlider);

            var fontColor = new MyTerminalControlColor<MyTextPanel>("FontColor", MySpaceTexts.BlockPropertyTitle_FontColor);
            fontColor.Getter = (x) => x.FontColor;
            fontColor.Setter = (x, v) => x.FontColor = v;
            MyTerminalControlFactory.AddControl(fontColor);

            var backgroundColor = new MyTerminalControlColor<MyTextPanel>("BackgroundColor", MySpaceTexts.BlockPropertyTitle_BackgroundColor);
            backgroundColor.Getter = (x) => x.BackgroundColor;
            backgroundColor.Setter = (x, v) => x.BackgroundColor = v;
            MyTerminalControlFactory.AddControl(backgroundColor);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTextPanel>());

            var imagesList = new MyTerminalControlListbox<MyTextPanel>("ImageList", MySpaceTexts.BlockPropertyTitle_LCDScreenDefinitionsTextures, MySpaceTexts.Blank, true);
            imagesList.ListContent = (x, list1, list2) => x.FillListContent(list1, list2);
            imagesList.ItemSelected = (x, y) => x.SelectImageToDraw(y);
            MyTerminalControlFactory.AddControl(imagesList);

            var addToSelectionButton = new MyTerminalControlButton<MyTextPanel>("SelectTextures", MySpaceTexts.BlockPropertyTitle_LCDScreenSelectTextures, MySpaceTexts.Blank, (x) => x.AddImagesToSelection());
            MyTerminalControlFactory.AddControl(addToSelectionButton);

            var changeIntervalSlider = new MyTerminalControlSlider<MyTextPanel>("ChangeIntervalSlider", MySpaceTexts.BlockPropertyTitle_LCDScreenRefreshInterval, MySpaceTexts.Blank);
            changeIntervalSlider.SetLimits(0, 30.0f);
            changeIntervalSlider.DefaultValue = 0;
            changeIntervalSlider.Getter = (x) => x.ChangeInterval;
            changeIntervalSlider.Setter = (x, v) => x.ChangeInterval = v;
            changeIntervalSlider.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.ChangeInterval, NUM_DECIMALS)).Append(" s");
            changeIntervalSlider.EnableActions();
            MyTerminalControlFactory.AddControl(changeIntervalSlider);

            var selectedImagesList = new MyTerminalControlListbox<MyTextPanel>("SelectedImageList", MySpaceTexts.BlockPropertyTitle_LCDScreenSelectedTextures, MySpaceTexts.Blank, true);
            selectedImagesList.ListContent = (x, list1, list2) => x.FillSelectedListContent(list1, list2);
            selectedImagesList.ItemSelected = (x, y) => x.SelectImage(y);
            MyTerminalControlFactory.AddControl(selectedImagesList);

            var removeSelectedButton = new MyTerminalControlButton<MyTextPanel>("RemoveSelectedTextures", MySpaceTexts.BlockPropertyTitle_LCDScreenRemoveSelectedTextures, MySpaceTexts.Blank, (x) => x.RemoveImagesFromSelection());
            MyTerminalControlFactory.AddControl(removeSelectedButton);

        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
             BlockDefinition.ResourceSinkGroup,
             BlockDefinition.RequiredPowerInput,
             () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_TextPanel ob = (MyObjectBuilder_TextPanel)objectBuilder;

            PrivateTitle.Append(ob.Title);
            PrivateDescription.Append(ob.Description);
            PublicDescription.Append(ob.PublicDescription);
            PublicTitle.Append(ob.PublicTitle);

            m_currentPos = ob.CurrentShownTexture;

            if (Sync.IsServer && Sync.Clients != null)
            {
                Sync.Clients.ClientRemoved += TextPanel_ClientRemoved;
            }

            FontColor = ob.FontColor;
            BackgroundColor = ob.BackgroundColor;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            Render.NeedsDrawFromParent = true;
            ChangeInterval = ob.ChangeInterval;
            if (!ob.Font.IsNull())
            {
                Font = ob.Font;
            }
            FontSize = ob.FontSize;
            ShowTextFlag = ob.ShowText;
            if (ob.SelectedImages != null)
            {
                foreach (var savedTextureName in ob.SelectedImages)
                {
                    foreach (var textureDefinition in m_definitions)
                    {
                        if (textureDefinition.Id.SubtypeName == savedTextureName)
                        {
                            m_selectedTexturesToDraw.Add(textureDefinition);
                            break;
                        }
                    }
                }
                m_currentPos = Math.Min(m_currentPos, m_selectedTexturesToDraw.Count);
                RaisePropertiesChanged();
            }


            ResourceSink.Update();
            ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_TextPanel)base.GetObjectBuilderCubeBlock(copy);
            ob.Description = m_privateDescription.ToString();
            ob.Title = m_privateTitle.ToString();

            ob.PublicDescription = m_publicDescription.ToString();
            ob.PublicTitle = m_publicTitle.ToString();

            ob.ChangeInterval = ChangeInterval;
            ob.Font = Font;
            ob.FontSize = FontSize;
            ob.ShowText = ShowTextFlag;
            ob.FontColor = FontColor;
            ob.BackgroundColor = BackgroundColor;
            ob.CurrentShownTexture = m_currentPos;

            if (m_selectedTexturesToDraw.Count > 0)
            {
                ob.SelectedImages = new List<string>();

                foreach (var texture in m_selectedTexturesToDraw)
                {
                    ob.SelectedImages.Add(texture.Id.SubtypeName);
                }
            }
            return ob;
        }

        private void CreateTextBox(bool isEditable, StringBuilder description, bool isPublic)
        {
            m_textBox = new MyGuiScreenTextPanel(missionTitle: isPublic ? m_publicTitle.ToString() : m_privateTitle.ToString(),
                           currentObjectivePrefix: "",
                           currentObjective: "",
                           description: description.ToString(),
                           editable: isEditable,
                           resultCallback: OnClosedTextBox);
        }

        public void OpenWindow(bool isEditable, bool sync, bool isPublic)
        {
            if (sync)
            {
                SendChangeOpenMessage(true, isEditable, Sync.MyId, isPublic);
                return;
            }
            m_isEditingPublic = isPublic;
            CreateTextBox(isEditable, isPublic ? PublicDescription : PrivateDescription, isPublic);
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = MyGuiScreenGamePlay.ActiveGameplayScreen;
            MyScreenManager.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = m_textBox);
        }

        public void OnClosedTextBox(ResultEnum result)
        {
            if (m_textBox == null)
                return;
            if (m_textBox.Description.Text.Length > MAX_NUMBER_CHARACTERS)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                     styleEnum: MyMessageBoxStyleEnum.Info,
                                     callback: OnClosedMessageBox,
                                     buttonType: MyMessageBoxButtonsType.YES_NO,
                                     messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextTooLongText)));
            }
            else
            {
                CloseWindow(m_isEditingPublic);
            }
        }

        public void OnClosedMessageBox(MyGuiScreenMessageBox.ResultEnum result)
        {
            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                m_textBox.Description.Text.Remove(MAX_NUMBER_CHARACTERS, m_textBox.Description.Text.Length - (MAX_NUMBER_CHARACTERS));
                CloseWindow(m_isEditingPublic);
            }
            else
            {
                CreateTextBox(true, m_textBox.Description.Text, m_isEditingPublic);
                MyScreenManager.AddScreen(m_textBox);
            }
        }

        private void CloseWindow(bool isPublic)
        {
            MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiScreenGamePlay.TmpGameplayScreenHolder;
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = null;
            MySession.Static.Gpss.ScanText(m_textBox.Description.Text.ToString(), PublicTitle);

            foreach (var block in CubeGrid.CubeBlocks)
            {
                if (block.FatBlock != null && block.FatBlock.EntityId == EntityId)
                {
                    SendChangeDescriptionMessage(m_textBox.Description.Text, isPublic);
                    SendChangeOpenMessage(false);
                    return;
                }
            }
        }

        public void Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            if (m_isOpen)
                return;

            var user = entity as MyCharacter;
            var relation = GetUserRelationToOwner(user.ControllerInfo.Controller.Player.Identity.IdentityId);

            if (OwnerId == 0)
            {
                OnOwnerUse(actionEnum, user);
            }
            else
            {
                switch (relation)
                {
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                    case MyRelationsBetweenPlayerAndBlock.Neutral:	// HACK: relation is neutral if sharing is set to none and we would like to access a faction text panel text field
                        if (MySession.Static.Factions.TryGetPlayerFaction(user.ControllerInfo.Controller.Player.Identity.IdentityId) == MySession.Static.Factions.TryGetPlayerFaction(IDModule.Owner) &&
                            actionEnum == UseActionEnum.Manipulate)
                            OnFactionUse(actionEnum, user);
                        else
                            OnEnemyUse(actionEnum, user);
                        break;
                    case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    case MyRelationsBetweenPlayerAndBlock.FactionShare:
                        if (OwnerId == 0)
                            OnOwnerUse(actionEnum, user);
                        else
                            OnFactionUse(actionEnum, user);
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        OnOwnerUse(actionEnum, user);
                        break;
                }
            }
        }

        private void OnEnemyUse(UseActionEnum actionEnum, MyCharacter user)
        {
            if (actionEnum == UseActionEnum.Manipulate)
            {
                OpenWindow(false, true, true);
            }
            else if (actionEnum == UseActionEnum.OpenTerminal)
            {
                MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
            }
        }

        private void OnFactionUse(UseActionEnum actionEnum, MyCharacter user)
        {
            bool readOnlyNotification = false;

            if (actionEnum == UseActionEnum.Manipulate)
            {
                var relation = GetUserRelationToOwner(user.GetPlayerIdentityId());

                if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                    OpenWindow(true, true, true);
                else
                    OpenWindow(false, true, true);
            }
            else if (actionEnum == UseActionEnum.OpenTerminal)
            {
                var relation = GetUserRelationToOwner(user.GetPlayerIdentityId());

                if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, this);
                }
                else
                {
                    readOnlyNotification = true;
                }
            }

            if (user.ControllerInfo.Controller.Player == MySession.Static.LocalHumanPlayer)
            {   
                if (readOnlyNotification)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.TextPanelReadOnly);
                }
            }
        }

        private void OnOwnerUse(UseActionEnum actionEnum, MyCharacter user)
        {
            if (actionEnum == UseActionEnum.Manipulate)
            {
                OpenWindow(true, true, true);
            }
            else if (actionEnum == UseActionEnum.OpenTerminal)
            {
                MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, this);
            }
        }

        protected override void Closing()
        {
            base.Closing();

            if (Sync.IsServer && Sync.Clients != null)
            {
                Sync.Clients.ClientRemoved -= TextPanel_ClientRemoved;
            }
            ReleaseRenderTexture();
        }

        public static void FillComboBoxContent(List<MyTerminalControlComboBoxItem> items)
        {
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)TextPanelAccessFlag.NONE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessOnlyOwner });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_FACTION, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadFaction });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_AND_WRITE_FACTION, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadWriteFaction });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_ALL, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadAll });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_AND_WRITE_ALL, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadWriteAll });
        }

        public static void FillShowOnScreenComboBoxContent(List<MyTerminalControlComboBoxItem> items)
        {
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)ShowTextOnScreenFlag.NONE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextNone });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)ShowTextOnScreenFlag.PUBLIC, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextPublic });
            items.Add(new MyTerminalControlComboBoxItem() { Key = (long)ShowTextOnScreenFlag.PRIVATE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextPrivate });
        }

        public static void FillFontComboBoxContent(List<MyTerminalControlComboBoxItem> items)
        {
            foreach (var font in MyDefinitionManager.Static.GetFontDefinitions())
            {
                items.Add(new MyTerminalControlComboBoxItem() { Key = (long)(font.Id.SubtypeId), Value = MyStringId.GetOrCompute(font.Id.SubtypeName) });
            }
        }

        private void TextPanel_ClientRemoved(ulong playerId)
        {
            if (playerId == m_userId)
            {
                SendChangeOpenMessage(false);
            }
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        public bool IsInRange()
        {
            MyCharacter player = MySession.Static.LocalCharacter;
            if (player == null)
            {
                return false;
            }

            var camera = MySector.MainCamera;
            if (camera == null)
            {
                return false;
            }

            MatrixD localOffset = MatrixD.CreateTranslation( PositionComp.LocalVolume.Center);
            MatrixD matrix = localOffset * WorldMatrix;

            Vector3D position = matrix.Translation;
            Vector3D dirToCamera = Vector3D.Normalize(position - camera.Position);
            double dot = Vector3D.Dot(camera.ForwardVector, dirToCamera);
            if (dot <= 0.0f)
            {
                return false;
            }
            return Vector3D.Distance(position, camera.Position) < MAX_DRAW_DISTANCE;
        }

        public void RefreshRenderText(int freeResources = int.MaxValue)
        {
            string currentDescription = m_publicDescription.ToString();
            if (BlockDefinition.TextureResolution * BlockDefinition.TextureResolution * BlockDefinition.TextureAspectRadio <= freeResources && currentDescription.Length > 0)
            {
                Render.RenderTextToTexture(currentDescription, FontSize * BlockDefinition.TextureResolution / DEFAULT_RESOLUTION,
                    FontColor, BackgroundColor, BlockDefinition.TextureResolution, BlockDefinition.TextureAspectRadio);
                FailedToRenderTexture = false;
            }
        }

        public void ReleaseRenderTexture()
        {
            m_descriptionChanged = true;
            Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
        }

        string GetPathForID(string id)
        {
            foreach (var definition in m_definitions)
            {
                if (definition.Id.SubtypeName == id)
                {
                    return definition.TexturePath;
                }
            }
            return null;
        }

        public void OnColorChanged()
        {
            if (ShowTextOnScreen)
            {
                m_forceUpdateText = true;
            }
            else
            {
                m_forceUpdateText = false;
                m_previousTextureID = null;
            }
        }
        public override void OnModelChange()
        {
            base.OnModelChange();
            if (ResourceSink != null)
                if (CheckIsWorking() == false)
                {
                    Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
                }
                else
                {
                    if (ShowTextOnScreen == false)
                    {
                        Render.ChangeTexture(GetPathForID(DEFAULT_ONLINE_TEXTURE));
                    }
                    m_previousTextureID = null;
                    m_forceUpdateText = ShowTextOnScreen;
                }
        }

        #region Sync

        private void SendRemoveSelectedImageRequest(int[] selection)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRemoveSelectedImageRequest, selection);
        }

        [Event, Reliable, Server, Broadcast]
        void OnRemoveSelectedImageRequest(int[] selection)
        {
            RemoveItems(selection);
        }

        private void SendAddImagesToSelectionRequest(int[] selection)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnSelectImageRequest, selection);
        }

        [Event, Reliable, Server, Broadcast]
        void OnSelectImageRequest(int[] selection)
        {
            SelectItems(selection);
        }

        [Event, Reliable, Server, Broadcast]
        void OnChangeDescription(string description, bool isPublic)
        {
            m_helperSB.Clear().Append(description);
            if (isPublic)
            {
                PublicDescription = m_helperSB;
            }
            else
            {
                PrivateDescription = m_helperSB;
            }
        }

        [Event, Reliable, Server, Broadcast]
        void OnChangeTitle(string title, bool isPublic)
        {
            m_helperSB.Clear().Append(title);
            if (isPublic)
            {
                PublicTitle = m_helperSB;
            }
            else
            {
                PrivateTitle = m_helperSB;
            }
        }

        private void SendChangeOpenMessage(bool isOpen, bool editable = false, ulong user = 0, bool isPublic = false)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnChangeOpenRequest, isOpen, editable, user, isPublic);
        }

        [Event, Reliable, Server]
        void OnChangeOpenRequest(bool isOpen, bool editable, ulong user, bool isPublic)
        {
            if (Sync.IsServer && IsOpen && isOpen)
                return;

            OnChangeOpen(isOpen, editable, user, isPublic);

            MyMultiplayer.RaiseEvent(this, x => x.OnChangeOpenSuccess, isOpen, editable, user, isPublic);
        }

        [Event, Reliable, Broadcast]
        void OnChangeOpenSuccess(bool isOpen, bool editable, ulong user, bool isPublic)
        {
            OnChangeOpen(isOpen, editable, user, isPublic);
        }

        void OnChangeOpen(bool isOpen, bool editable, ulong user, bool isPublic)
        {
            IsOpen = isOpen;
            UserId = user;

            if (!Engine.Platform.Game.IsDedicated && user == Sync.MyId && isOpen)
            {
                OpenWindow(editable, false, isPublic);
            }
        }

        private void SendChangeDescriptionMessage(StringBuilder description, bool isPublic)
        {
            if (CubeGrid.IsPreview || !CubeGrid.SyncFlag)
            {
                if (isPublic)
                {
                    PublicDescription = description;
                }
                else
                {
                    PrivateDescription = description;
                }
            }
            else
            {

                if (description.CompareTo(PublicDescription) == 0 && isPublic)
                {
                    return;
                }

                if (description.CompareTo(PrivateDescription) == 0 && isPublic == false)
                {
                    return;
                }
                MyMultiplayer.RaiseEvent(this, x => x.OnChangeDescription, description.ToString(), isPublic);
            }
        }

        private void SendChangeTitleMessage(StringBuilder title, bool isPublic)
        {
            if (CubeGrid.IsPreview || !CubeGrid.SyncFlag)
            {
                if (isPublic)
                {
                    PublicTitle = title;
                }
                else
                {
                    PrivateTitle = title;
                }
            }
            else
            {
                if (title.CompareTo(PublicTitle) == 0 && isPublic)
                {
                    return;
                }

                if (title.CompareTo(PrivateTitle) == 0 && isPublic == false)
                {
                    return;
                }

                if (isPublic)
                {
                    PublicTitle = title;
                }
                else
                {
                    PrivateTitle = title;
                }

                MyMultiplayer.RaiseEvent(this, x => x.OnChangeTitle, title.ToString(), isPublic);
            }
        }

        #endregion
    }
}
