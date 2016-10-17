using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilders.Campaign;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenNewGame : MyGuiScreenBase
    {
        #region Campaign List

        private MyGuiControlList                m_campaignList;
        private MyGuiControlRadioButtonGroup    m_campaignTypesGroup;
        private MyObjectBuilder_Campaign        m_selectedCampaign;

        #endregion


        #region Right side of the screen

        private MyLayoutTable               m_tableLayout;

        private MyGuiControlLabel           m_nameLabel;
        private MyGuiControlTextbox         m_nameTextbox;

        private MyGuiControlLabel           m_difficultyLabel;
        private MyGuiControlCombobox        m_difficultyCombo;

        private MyGuiControlLabel           m_onlineModeLabel;
        private MyGuiControlCheckbox        m_onlineMode;

        private MyGuiControlMultilineText   m_descriptionMultilineText;

        #endregion

        float MARGIN_TOP = 0.12f;
        float MARGIN_BOTTOM = 0.12f;
        float MARGIN_LEFT = 0.3f;
        float MARGIN_RIGHT =0.075f;

        public MyGuiScreenNewGame()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.9f, 0.97f))
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "New Game";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenMenuButtonCampaign);

            InitCampaignList();
            InitRightSide();
            RefreshCampaignList();

            m_campaignTypesGroup.SelectByKey(0);
        }

        private void InitCampaignList()
        {
            Vector2 originL = -m_size.Value / 2 + new Vector2(0.07f, MARGIN_TOP);

            m_campaignTypesGroup = new MyGuiControlRadioButtonGroup();
            m_campaignTypesGroup.SelectedChanged += CampaignSelectionChanged;

            m_campaignList = new MyGuiControlList
            {
              OriginAlign  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
              Position = originL,
              Size = new Vector2(0.19f, m_size.Value.Y - 0.03f - MARGIN_TOP),
            };

            var separator = new MyGuiControlSeparatorList()
            {
                Size = new Vector2(0.03f, m_campaignList.Size.Y),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = m_campaignList.Position + new Vector2(m_campaignList.Size.X + 0.015f, 0f)
            };
            separator.AddVertical(new Vector2(0f, -m_campaignList.Size.Y/2), separator.Size.Y);

            Controls.Add(separator);
            Controls.Add(m_campaignList);
        }

        private void CampaignSelectionChanged(MyGuiControlRadioButtonGroup args)
        {
            var button = args.SelectedButton as MyGuiControlCampaignButton;
            if (button != null)
            {
                var campaign = button.UserData as MyObjectBuilder_Campaign;
                if(campaign == null) return;

                m_nameTextbox.Text = campaign.Name;
                m_onlineMode.IsChecked = campaign.IsMultiplayer;
                m_descriptionMultilineText.Text = new StringBuilder(campaign.Description);

                if(!string.IsNullOrEmpty(campaign.Difficulty))
                {
                    if(campaign.Difficulty.StartsWith("Easy"))
                        m_difficultyCombo.SelectItemByIndex(0);
                    else if(campaign.Difficulty.StartsWith("Normal"))
                        m_difficultyCombo.SelectItemByIndex(1);
                    else 
                        m_difficultyCombo.SelectItemByIndex(2);
                }

                m_selectedCampaign = campaign;
            }
        }

        private void InitRightSide()
        {
            Vector2 originL;
            originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);

            var screenSize = m_size.Value;
            var layoutSize = new Vector2(screenSize.X / 2 - originL.X, screenSize.Y - MARGIN_TOP - MARGIN_BOTTOM);
            var columnWidthLabel = layoutSize.X * 0.25f;
            var columnWidthControl = layoutSize.X - columnWidthLabel;
            var rowHeight = 0.052f;
            var leftHeight = layoutSize.Y - 4 * rowHeight;
            var descriptionMargin = 0.005f;

            m_tableLayout = new MyLayoutTable(this, originL, layoutSize);
            m_tableLayout.SetColumnWidthsNormalized(columnWidthLabel, columnWidthControl);
            m_tableLayout.SetRowHeightsNormalized(rowHeight, rowHeight, rowHeight, rowHeight, leftHeight);

            m_nameLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.Name), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, position: new Vector2(0, 0.2f));
            m_nameTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_NAME_LENGTH);
            m_nameTextbox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            m_nameTextbox.Enabled = false;

            float width = m_nameTextbox.Size.X;

            m_difficultyLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.Difficulty), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            m_difficultyCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            m_difficultyCombo.Enabled = false;
            m_difficultyCombo.AddItem((int)0, MySpaceTexts.DifficultyEasy);
            m_difficultyCombo.AddItem((int)1, MySpaceTexts.DifficultyNormal);
            m_difficultyCombo.AddItem((int)2, MySpaceTexts.DifficultyHard);

            m_onlineModeLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.WorldSettings_OnlineMode), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            m_onlineMode = new MyGuiControlCheckbox(originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            m_onlineMode.Enabled = false;

            m_descriptionMultilineText = new MyGuiControlMultilineText(
                    selectable: false,
                    font: MyFontEnum.Blue)
            {
                Name = "BriefingMultilineText",
                Position = new Vector2(-0.009f, -0.115f),
                Size = new Vector2(0.419f, 0.412f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };


            m_tableLayout.Add(m_nameLabel, MyAlignH.Left, MyAlignV.Center, 0, 0);
            m_tableLayout.Add(m_difficultyLabel, MyAlignH.Left, MyAlignV.Center, 2, 0);
            m_tableLayout.Add(m_onlineModeLabel, MyAlignH.Left, MyAlignV.Center, 3, 0);

            m_tableLayout.Add(m_nameTextbox, MyAlignH.Left, MyAlignV.Center, 0, 1);
            m_tableLayout.Add(m_difficultyCombo, MyAlignH.Left, MyAlignV.Center, 2, 1);
            m_tableLayout.Add(m_onlineMode, MyAlignH.Left, MyAlignV.Center, 3, 1);

            m_tableLayout.Add(panel, MyAlignH.Left, MyAlignV.Top, 4, 0, 1, 2);
            m_tableLayout.Add(m_descriptionMultilineText, MyAlignH.Left, MyAlignV.Top, 4, 0, 1, 2);
            m_descriptionMultilineText.Position = new Vector2(panel.Position.X + descriptionMargin, panel.Position.Y);
            panel.Size = new Vector2(m_nameTextbox.Size.X + columnWidthLabel, m_tableLayout.GetCellSize(4, 0).Y + 0.01f);
            m_descriptionMultilineText.Size = new Vector2(panel.Size.X - descriptionMargin, m_tableLayout.GetCellSize(4, 0).Y - descriptionMargin + 0.01f);

            var buttonsOrigin = m_size.Value / 2 - new Vector2(0.365f, 0.03f);
            var buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            // Ok/Cancel
            var okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.055f, 0f), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            var cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.055f, 0f), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            Controls.Add(okButton);
            Controls.Add(cancelButton);

            CloseButtonEnabled = true;
        }

        private void OnOkButtonClicked(MyGuiControlButton myGuiControlButton)
        {
            if(m_selectedCampaign == null) return;

            MyCampaignManager.Static.SwitchCampaign(m_selectedCampaign.Name, m_selectedCampaign.IsVanilla, m_selectedCampaign.IsLocalMod);
            MyCampaignManager.Static.RunNewCampaign();
        }

        private void OnCancelButtonClick(MyGuiControlButton myGuiControlButton)
        {
            CloseScreen();
        }

        private void RefreshCampaignList()
        {
            var campaigns = MyCampaignManager.Static.Campaigns;
            List<MyObjectBuilder_Campaign> vanilla = new List<MyObjectBuilder_Campaign>();
            List<MyObjectBuilder_Campaign> localMods = new List<MyObjectBuilder_Campaign>();
            List<MyObjectBuilder_Campaign> mods = new List<MyObjectBuilder_Campaign>();

            foreach (var campaign in campaigns)
            {
                if(campaign.IsVanilla)
                    vanilla.Add(campaign);
                else if(campaign.IsLocalMod)
                    localMods.Add(campaign);
                else
                    mods.Add(campaign);
            }

            m_campaignList.Controls.Clear();

            AddSeparator("Official");

            foreach (var campaign in vanilla)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, campaign.ImagePath);
                button.UserData = campaign;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }

            if(mods.Count > 0)
                AddSeparator("Workshop");

            foreach (var campaign in mods)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, GetImagePath(campaign));
                button.UserData = campaign;
                button.IsWorkshopMod = true;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }

            if(localMods.Count > 0)
                AddSeparator("Local");

            foreach (var campaign in localMods)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, GetImagePath(campaign));
                button.UserData = campaign;
                button.IsLocalMod = true;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }
        }

        private string GetImagePath(MyObjectBuilder_Campaign campaign)
        {
            string imagePath = campaign.ImagePath;

            if (!campaign.IsVanilla)
            {
                imagePath = Path.Combine(campaign.ModFolderPath, campaign.ImagePath);
                if (!MyFileSystem.FileExists(imagePath))
                {
                    imagePath = Path.Combine(MyFileSystem.ContentPath, campaign.ImagePath);
                }
            }

            return imagePath;
        }

        private void AddSeparator(string sectionName)
        {
            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = Vector2.Zero
            };

            var label = new MyGuiControlLabel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Text = sectionName,
                Font = MyFontEnum.Blue,
                PositionX = 0.005f
            };

            var imageOffset = 0.003f;
            var colorMask = MyGuiConstants.THEMED_GUI_LINE_COLOR;
            var img = new MyGuiControlImage(textures: new[] { @"Textures\GUI\FogSmall3.dds" })
            {
                Size = new Vector2(label.Size.X + imageOffset * 10, 0.007f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                //ColorMask = new Vector4(0,0,0,1),
                ColorMask = colorMask.ToVector4(),
                Position = new Vector2(-imageOffset, label.Size.Y)
            };

            var parent = new MyGuiControlParent
            {
                Size = new Vector2(m_campaignList.Size.X, label.Size.Y),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = Vector2.Zero
            };

            panel.Size = parent.Size + new Vector2(-0.035f, 0.01f);

            panel.Position -= parent.Size / 2 - new Vector2(-0.01f, 0f);
            label.Position -= parent.Size / 2;
            img.Position -= parent.Size / 2;

            parent.Controls.Add(panel);
            parent.Controls.Add(img);
            parent.Controls.Add(label);

            m_campaignList.Controls.Add(parent);
        }
    }
}
