
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRage;
using Sandbox.Engine.Utils;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TextPanel))]
    partial class MyTextPanel : MyFunctionalBlock, IMyPowerConsumer
    {
        private const int NUM_DECIMALS = 1;
        public const double MAX_DRAW_DISTANCE = 100.0;
        private const int DEFAULT_RESOLUTION = 512;
        private const int MAX_NUMBER_CHARACTERS = 4200;
        private const string DEFAULT_OFFLINE_TEXTURE = "Offline";
        private const string DEFAULT_ONLINE_TEXTURE = "Online";

        private StringBuilder m_publicDescription;
        private StringBuilder m_publicTitle;
        private StringBuilder m_privateDescription;
        private StringBuilder m_privateTitle;
        private MyGuiScreenTextPanel m_textBox;
        private TextPanelAccessFlag m_accessFlag;
        private ShowTextOnScreenFlag m_showFlag;
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

        List<Sandbox.Definitions.MyLCDTextureDefinition> m_selectedTexturesToDraw = new List<Sandbox.Definitions.MyLCDTextureDefinition>();
        static List<Sandbox.Definitions.MyLCDTextureDefinition> m_definitions = new List<Sandbox.Definitions.MyLCDTextureDefinition>();
        List<MyGuiControlListbox.Item> m_selectedTextures = null;
        List<MyGuiControlListbox.Item> m_selectedTexturesToRemove = null;

        Color m_backgroundColor = Color.Black;
        bool m_backgroundColorChanged = true;
        public Color BackgroundColor
        {
            get { return m_backgroundColor; }
            set
            {
                if (m_backgroundColor != value)
                {
                    m_backgroundColorChanged = true;
                    m_backgroundColor = value;
                    RaisePropertiesChanged();
                }
            }
        }

        Color m_fontColor = Color.White;
        bool m_fontColorChanged = true;
        public Color FontColor
        {
            get { return m_fontColor; }
            set
            {
                if (m_fontColor != value)
                {
                    m_fontColorChanged = true;
                    m_fontColor = value;
                    RaisePropertiesChanged();
                }
            }
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

                if (m_privateTitle != value)
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

                if (m_privateDescription != value)
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

        public TextPanelAccessFlag AccessFlag
        {
            get { return m_accessFlag; }
            set
            {
                if (m_accessFlag != value)
                {
                    m_accessFlag = value;
                }
            }
        }

        public ShowTextOnScreenFlag ShowTextFlag
        {
            get { return m_showFlag; }
            set
            {
                if (m_showFlag != value)
                {
                    m_showFlag = value;

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
            }
        }

        public bool IsAccessibleForOnlyOwner
        {
            get { return (m_accessFlag == TextPanelAccessFlag.NONE); }
        }

        public bool IsAccessibleForFaction
        {
            get { return (m_accessFlag & TextPanelAccessFlag.READ_AND_WRITE_FACTION) != TextPanelAccessFlag.NONE; }
        }

        public bool IsAccessibleForAll
        {
            get { return (m_accessFlag & TextPanelAccessFlag.READ_AND_WRITE_ALL) == TextPanelAccessFlag.READ_AND_WRITE_ALL; }
        }

        public bool IsReadableForFaction
        {
            get { return (m_accessFlag & TextPanelAccessFlag.READ_FACTION) == TextPanelAccessFlag.READ_FACTION; }
        }

        public bool IsReadableForAll
        {
            get { return (m_accessFlag & TextPanelAccessFlag.READ_ALL) == TextPanelAccessFlag.READ_ALL; }
        }

        public bool IsWritableForFaction
        {
            get { return (m_accessFlag & TextPanelAccessFlag.WRITE_FACTION) == TextPanelAccessFlag.WRITE_FACTION; }
        }

        public bool IsWritableForAll
        {
            get { return (m_accessFlag & TextPanelAccessFlag.WRITE_ALL) == TextPanelAccessFlag.WRITE_ALL; }
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

        public new MySyncTextPanel SyncObject
        {
            get { return (MySyncTextPanel)base.SyncObject; }
        }

        float m_changeInterval = 0.0f;
        public float ChangeInterval
        {
            get { return m_changeInterval; }
            set
            {
                if (m_changeInterval != value)
                {
                    m_changeInterval = (float)Math.Round(value, NUM_DECIMALS);
                    RaisePropertiesChanged();
                }
            }
        }

        bool m_fontSizeChanged = true;
        float m_fontSize = 1.0f;
        public float FontSize
        {
            get { return m_fontSize; }
            set
            {
                if (m_fontSize != value)
                {
                    m_fontSizeChanged = true;
                    m_fontSize = (float)Math.Round(value, NUM_DECIMALS);
                    RaisePropertiesChanged();
                }
            }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        internal new MyRenderComponentTextPanel Render
        {
            get { return (MyRenderComponentTextPanel)base.Render; }
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
            m_selectedTexturesToRemove = imageId;
        }

        public void SelectImageToDraw(List<MyGuiControlListbox.Item> imageIds)
        {
            m_selectedTextures = imageIds;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (IsBeingHacked)
            {
                PrivateDescription.Clear();
                SyncObject.SendChangeDescriptionMessage(PrivateDescription, false);
            }
            PowerReceiver.Update();
            if (IsFunctional && IsWorking)
            {
                if (ShowTextOnScreen && IsInRange() == false)
                {
                    m_isOutofRange = true;
                    ReleaseRenderTexture();
                    return;
                }

                if (ShowTextOnScreen&&(NeedsToDrawText() || m_isOutofRange || m_forceUpdateText))
                {
                    m_descriptionChanged = false;
                    m_forceUpdateText = false;
                    m_fontColorChanged = false;
                    m_fontSizeChanged = false;
                    m_backgroundColorChanged = false;
                    Render.RenderTextToTexture(EntityId, ShowTextFlag == ShowTextOnScreenFlag.PUBLIC ? m_publicDescription.ToString() : m_privateDescription.ToString(), FontSize * BlockDefinition.TextureResolution / DEFAULT_RESOLUTION, FontColor, BackgroundColor, BlockDefinition.TextureResolution, BlockDefinition.TextureAspectRadio);
                    FailedToRenderTexture = false;
                }

                m_isOutofRange = false;

                if (ShowTextOnScreen == false)
                {
                    UpdateTexture();
                }
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
            return ShowTextOnScreen && (m_descriptionChanged || m_fontSizeChanged || m_fontColorChanged || m_backgroundColorChanged);
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
            SyncObject.SendAddImagesToSelectionRequest(selection);
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
            SyncObject.SendRemoveSelectedImageRequest(selection);
        }

        public void SelectItems(int[] selection)
        {
            for (int j = 0; j < selection.Length; ++j)
            {
                m_selectedTexturesToDraw.Add(m_definitions[selection[j]]);
            }
            m_currentPos = 0;
            RaisePropertiesChanged();
        }

        public void RemoveItems(int[] selection)
        {
            for (int j = 0; j < selection.Length; ++j)
            {
                m_selectedTexturesToDraw.Remove(m_definitions[selection[j]]);
            }
            m_currentPos = 0;
            RaisePropertiesChanged();
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            UpdateText();
            UpdateIsWorking();
            if (CheckIsWorking() == false)
            {
                if (ShowTextOnScreen)
                {
                    Render.ReleaseRenderTexture();
                }
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
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
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
                if (ShowTextOnScreen)
                {
                    Render.ReleaseRenderTexture();
                }
                Render.ChangeTexture(GetPathForID(DEFAULT_OFFLINE_TEXTURE));
            }
        }

        static MyTextPanel()
        {
            var publicTitleField = new MyTerminalControlTextbox<MyTextPanel>("PublicTitle", MySpaceTexts.BlockPropertyTitle_TextPanelPublicTitle, MySpaceTexts.Blank);
            publicTitleField.Getter = (x) => x.PublicTitle;
            publicTitleField.Setter = (x, v) => x.SyncObject.SendChangeTitleMessage(v, true);
            publicTitleField.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(publicTitleField);

            var showPublicButton = new MyTerminalControlButton<MyTextPanel>("ShowPublicTextPanel", MySpaceTexts.BlockPropertyTitle_TextPanelShowPublicTextPanel, MySpaceTexts.Blank, (x) => x.OpenWindow(true, true, true));
            showPublicButton.Enabled = (x) => !x.IsOpen;
            showPublicButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(showPublicButton);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTextPanel>());

            var titleField = new MyTerminalControlTextbox<MyTextPanel>("Title", MySpaceTexts.BlockPropertyTitle_TextPanelTitle, MySpaceTexts.Blank);
            titleField.Getter = (x) => x.PrivateTitle;
            titleField.Setter = (x, v) => x.SyncObject.SendChangeTitleMessage(v, false);
            titleField.SupportsMultipleBlocks = false;

            MyTerminalControlFactory.AddControl(titleField);

            var showButton = new MyTerminalControlButton<MyTextPanel>("ShowTextPanel", MySpaceTexts.BlockPropertyTitle_TextPanelShowTextPanel, MySpaceTexts.Blank, (x) => x.OpenWindow(true, true, false));
            showButton.Enabled = (x) => !x.IsOpen;
            showButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(showButton);

            var comboAccess = new MyTerminalControlCombobox<MyTextPanel>("Access", MySpaceTexts.BlockPropertyTitle_TextPanelAccessType, MySpaceTexts.Blank);
            comboAccess.ComboBoxContent = (x) => FillComboBoxContent(x);
            comboAccess.Getter = (x) => (long)x.AccessFlag;
            comboAccess.Setter = (x, y) => x.SyncObject.SendChangeAccessFlagMessage((byte)y);
            comboAccess.Enabled = (x) => x.OwnerId != 0;
            MyTerminalControlFactory.AddControl(comboAccess);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTextPanel>());

            var showTextOnScreen = new MyTerminalControlCombobox<MyTextPanel>("ShowTextOnScreen", MySpaceTexts.BlockPropertyTitle_ShowTextOnScreen, MySpaceTexts.Blank);
            showTextOnScreen.ComboBoxContent = (x) => FillShowOnScreenComboBoxContent(x);
            showTextOnScreen.Getter = (x) => (long)x.ShowTextFlag;
            showTextOnScreen.Setter = (x, y) => x.SyncObject.SendShowOnScreenChangeRequest((byte)y);
            showTextOnScreen.Enabled = (x) => x.OwnerId != 0;

            MyTerminalControlFactory.AddControl(showTextOnScreen);

            var changeFontSlider = new MyTerminalControlSlider<MyTextPanel>("FontSize", MySpaceTexts.BlockPropertyTitle_LCDScreenTextSize, MySpaceTexts.Blank);
            changeFontSlider.SetLimits(0.1f, 10.0f);
            changeFontSlider.DefaultValue = 1.0f;
            changeFontSlider.Getter = (x) => x.FontSize;
            changeFontSlider.Setter = (x, v) => x.SyncObject.SendFontSizeChangeRequest(v);
            changeFontSlider.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.FontSize, 1));
            changeFontSlider.EnableActions();
            MyTerminalControlFactory.AddControl(changeFontSlider);

            var fontColor = new MyTerminalControlColor<MyTextPanel>("FontColor", MySpaceTexts.BlockPropertyTitle_FontColor);
            fontColor.Getter = (x) => x.FontColor;
            fontColor.Setter = (x, v) => x.SyncObject.SendChangeFontColorRequest(v);
            MyTerminalControlFactory.AddControl(fontColor);

            var backgroundColor = new MyTerminalControlColor<MyTextPanel>("BackgroundColor", MySpaceTexts.BlockPropertyTitle_BackgroundColor);
            backgroundColor.Getter = (x) => x.BackgroundColor;
            backgroundColor.Setter = (x, v) => x.SyncObject.SendChangeBackgroundColorRequest(v);
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
            changeIntervalSlider.Setter = (x, v) => x.SyncObject.SendIntervalChangeRequest(v);
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

        public MyTextPanel()
        {
            m_publicDescription = new StringBuilder();
            m_textBox = null;
            m_publicTitle = new StringBuilder();
            m_isOpen = false;

            m_privateDescription = new StringBuilder();
            m_privateTitle = new StringBuilder();

            Render = new MyRenderComponentTextPanel();
            m_definitions.Clear();
            foreach (var textureDefinition in MyDefinitionManager.Static.GetLCDTexturesDefinitions())
            {
                m_definitions.Add(textureDefinition);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_TextPanel ob = (MyObjectBuilder_TextPanel)objectBuilder;

            PrivateTitle.Append(ob.Title);
            PrivateDescription.Append(ob.Description);
            PublicDescription.Append(ob.PublicDescription);
            PublicTitle.Append(ob.PublicTitle);

            m_currentPos = ob.CurrentShownTexture;
            m_accessFlag = ob.AccessFlag;

            if (Sync.IsServer && Sync.Clients != null)
            {
                Sync.Clients.ClientRemoved += TextPanel_ClientRemoved;
            }

            FontColor = ob.FontColor;
            BackgroundColor = ob.BackgroundColor;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Render.NeedsDrawFromParent = true;
            this.ChangeInterval = ob.ChangeInterval;
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

            PowerReceiver = new MyPowerReceiver(
             MyConsumerGroupEnum.Utility,
             false,
             BlockDefinition.RequiredPowerInput,
             () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0f);

            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_TextPanel)base.GetObjectBuilderCubeBlock(copy);
            ob.Description = m_privateDescription.ToString();
            ob.Title = m_privateTitle.ToString();

            ob.PublicDescription = m_publicDescription.ToString();
            ob.PublicTitle = m_publicTitle.ToString();

            ob.AccessFlag = m_accessFlag;
            ob.ChangeInterval = ChangeInterval;
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

        protected override void OnOwnershipChanged()
        {
            m_accessFlag = TextPanelAccessFlag.READ_AND_WRITE_ALL;
            base.OnOwnershipChanged();

            //RaisePropertiesChanged();
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
                SyncObject.SendChangeOpenMessage(true, isEditable, Sync.MyId, isPublic);
                return;
            }
            m_isEditingPublic = isPublic;
            CreateTextBox(isEditable, isPublic ? PublicDescription : PrivateDescription, isPublic);
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = MyGuiScreenGamePlay.ActiveGameplayScreen;
            MyScreenManager.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = m_textBox);
        }

        public void OnClosedTextBox(ModAPI.ResultEnum result)
        {
            if (m_textBox.Description.Text.Length > MAX_NUMBER_CHARACTERS)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                     styleEnum: MyMessageBoxStyleEnum.Info,
                                     callback: OnClosedMessageBox,
                                     buttonType: MyMessageBoxButtonsType.YES_NO,
                                     messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextTooLongText)));
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
            SyncObject.SendChangeDescriptionMessage(m_textBox.Description.Text, isPublic);
            SyncObject.SendChangeOpenMessage(false);
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncTextPanel(this);
        }

        public void Use(UseActionEnum actionEnum, MyCharacter user)
        {
            if (m_isOpen)
                return;

            var relation = GetUserRelationToOwner(user.ControllerInfo.Controller.Player.Identity.IdentityId);

            if (OwnerId == 0)
            {
                OnOwnerUse(actionEnum, user);
            }
            else
            {
                switch (relation)
                {
                    case Common.MyRelationsBetweenPlayerAndBlock.Enemies:
                    case Common.MyRelationsBetweenPlayerAndBlock.Neutral:	// HACK: relation is neutral if sharing is set to none and we would like to access a faction text panel text field
						if (MySession.Static.Factions.TryGetPlayerFaction(user.ControllerInfo.Controller.Player.Identity.IdentityId) == MySession.Static.Factions.TryGetPlayerFaction(IDModule.Owner) && 
							actionEnum == UseActionEnum.Manipulate && IsAccessibleForFaction)
							OnFactionUse(actionEnum, user);
						else
							OnEnemyUse(actionEnum, user);
                        break;
                    case Common.MyRelationsBetweenPlayerAndBlock.FactionShare:
                        if (OwnerId == 0 && IsAccessibleForOnlyOwner)
                            OnOwnerUse(actionEnum, user);
                        else
                            OnFactionUse(actionEnum, user);
                        break;
                    case Common.MyRelationsBetweenPlayerAndBlock.Owner:
                        OnOwnerUse(actionEnum, user);
                        break;
                }
            }
        }

        private void OnEnemyUse(UseActionEnum actionEnum, MyCharacter user)
        {
            if (IsAccessibleForAll)
            {
                if (actionEnum == UseActionEnum.Manipulate)
                {
                    if (IsWritableForAll)
                        OpenWindow(true, true, false);
                    else if (IsReadableForAll)
                        OpenWindow(false, true, false);
                    else
                        Debug.Fail("Unknown state of text panel");
                }
            }
            else
            {
                if (user.ControllerInfo.Controller.Player == MySession.LocalHumanPlayer)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
            }
        }

        private void OnFactionUse(UseActionEnum actionEnum, MyCharacter user)
        {
            bool isAccessible = IsAccessibleForFaction;
            bool readOnlyNotification = false;

            if (IsAccessibleForFaction)
            {
                if (actionEnum == UseActionEnum.Manipulate)
                {
                    if (IsWritableForFaction)
                        OpenWindow(true, true, false);
                    else if (IsReadableForFaction)
                        OpenWindow(false, true, false);
                    else
                        Debug.Fail("Unknown state of text panel");
                }
                else if (actionEnum == UseActionEnum.OpenTerminal)
                {
                    if (IsWritableForFaction)
                    {
                        MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, this);
                    }
                    else
                    {
                        readOnlyNotification = true;
                    }
                }
            }

            if (user.ControllerInfo.Controller.Player == MySession.LocalHumanPlayer)
            {
                if (!isAccessible)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
                else if (readOnlyNotification)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.TextPanelReadOnly);
                }
            }
        }

        private void OnOwnerUse(UseActionEnum actionEnum, MyCharacter user)
        {
            if (actionEnum == UseActionEnum.Manipulate)
            {
                OpenWindow(true, true, false);
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

        public static void FillComboBoxContent(List<TerminalComboBoxItem> items)
        {
            items.Add(new TerminalComboBoxItem() { Key = (long)TextPanelAccessFlag.NONE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessOnlyOwner });
            items.Add(new TerminalComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_FACTION, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadFaction });
            items.Add(new TerminalComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_AND_WRITE_FACTION, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadWriteFaction });
            items.Add(new TerminalComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_ALL, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadAll });
            items.Add(new TerminalComboBoxItem() { Key = (long)TextPanelAccessFlag.READ_AND_WRITE_ALL, Value = MySpaceTexts.BlockComboBoxValue_TextPanelAccessReadWriteAll });
        }

        public static void FillShowOnScreenComboBoxContent(List<TerminalComboBoxItem> items)
        {
            items.Add(new TerminalComboBoxItem() { Key = (long)ShowTextOnScreenFlag.NONE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextNone });
            items.Add(new TerminalComboBoxItem() { Key = (long)ShowTextOnScreenFlag.PUBLIC, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextPublic });
            items.Add(new TerminalComboBoxItem() { Key = (long)ShowTextOnScreenFlag.PRIVATE, Value = MySpaceTexts.BlockComboBoxValue_TextPanelShowTextPrivate });

        }

        private void TextPanel_ClientRemoved(ulong playerId)
        {
            if (playerId == m_userId)
            {
                SyncObject.SendChangeOpenMessage(false);
            }
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.IsPowered ? PowerReceiver.RequiredInput : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        public bool IsInRange()
        {
            MyCharacter player = MySession.LocalCharacter;
            if (player == null)
            {
                return false;
            }

            var camera = MySector.MainCamera;
            if (camera == null)
            {
                return false;
            }

            MatrixD localOffset = MatrixD.CreateTranslation( this.PositionComp.LocalVolume.Center);
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
                Render.RenderTextToTexture(EntityId, currentDescription, FontSize * BlockDefinition.TextureResolution / DEFAULT_RESOLUTION, FontColor, BackgroundColor, BlockDefinition.TextureResolution, BlockDefinition.TextureAspectRadio);
                FailedToRenderTexture = false;
            }
        }

        public void ReleaseRenderTexture()
        {
            m_descriptionChanged = true;
            Render.ReleaseRenderTexture();
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
    }
}
