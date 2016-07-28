#if !XB1 // XB1_NOWORKSHOP
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using Sandbox.Game.Multiplayer;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenMods : MyGuiScreenBase
    {
        private MyGuiControlLabel m_labelEnabled;
        private MyGuiControlLabel m_labelDisabled;

        private MyGuiControlTable m_modsTableEnabled;
        private MyGuiControlTable m_modsTableDisabled;

        private MyGuiControlButton m_moveUpButton;
        private MyGuiControlButton m_moveDownButton;
        private MyGuiControlButton m_moveTopButton;
        private MyGuiControlButton m_moveBottomButton;

        private MyGuiControlButton m_moveLeftButton;
        private MyGuiControlButton m_moveLeftAllButton;
        private MyGuiControlButton m_moveRightButton;
        private MyGuiControlButton m_moveRightAllButton;

        private MyGuiControlButton m_openInWorkshopButton;
        private MyGuiControlButton m_refreshButton;
        private MyGuiControlButton m_browseWorkshopButton;
        private MyGuiControlButton m_publishModButton;

        private MyGuiControlButton m_okButton;
        private MyGuiControlButton m_cancelButton;

        private MyGuiControlTable.Row m_selectedRow;
        private MyGuiControlTable m_selectedTable;
        private bool m_listNeedsReload;
        private bool m_keepActiveMods;
        private List<MySteamWorkshop.SubscribedItem> m_subscribedMods;
        private List<MySteamWorkshop.SubscribedItem> m_worldMods;
        private List<MyObjectBuilder_Checkpoint.ModItem> m_modListToEdit;

        private HashSet<string> m_worldLocalMods = new HashSet<string>();
        private HashSet<ulong> m_worldWorkshopMods = new HashSet<ulong>();

        private MyGuiControlButton m_categoryCategorySelectButton;

        private List<MyGuiControlButton> m_categoryButtonList = new List<MyGuiControlButton>();

        private MyGuiControlTextbox m_searchBox;
        private MyGuiControlButton m_searchClear;

        private List<String> m_tmpSearch = new List<string>();

        private List<String> m_selectedCategories = new List<String>();

        public MyGuiScreenMods(List<MyObjectBuilder_Checkpoint.ModItem> modListToEdit)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.95f))
        {
            m_modListToEdit = modListToEdit;

            if(m_modListToEdit == null)
                m_modListToEdit = new List<MyObjectBuilder_Checkpoint.ModItem>();

            EnabledBackgroundFade = true;

            GetWorldMods(m_modListToEdit);

            m_listNeedsReload = true;

            RecreateControls(true);
        }

        void GetWorldMods(ListReader<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            m_worldLocalMods.Clear();
            m_worldWorkshopMods.Clear();

            foreach (var mod in mods)
            {
                if (mod.PublishedFileId == 0)
                    m_worldLocalMods.Add(mod.Name);
                else
                    m_worldWorkshopMods.Add(mod.PublishedFileId);
            }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionWorkshop);

            var origin = new Vector2(-0.4375f, -0.375f);
            Vector2 tinyButtonsOrigin = new Vector2(-0.0015f, -4.5f * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA.Y);


            m_modsTableDisabled = new MyGuiControlTable();
            if (MyFakes.ENABLE_MOD_CATEGORIES)
            {
                m_modsTableDisabled.Position = origin + new Vector2(0f, 0.1f);
                m_modsTableDisabled.VisibleRowsCount = 17;
            }
            else
            {
                m_modsTableDisabled.Position = origin;
                m_modsTableDisabled.VisibleRowsCount = 20;
            }
            m_modsTableDisabled.Size = new Vector2(m_size.Value.X * 0.4375f, 1.25f);
            m_modsTableDisabled.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_modsTableDisabled.ColumnsCount = 2;

            m_modsTableDisabled.ItemSelected += OnTableItemSelected;
            m_modsTableDisabled.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_modsTableDisabled.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_modsTableDisabled.SetCustomColumnWidths(new float[] { 0.085f, 0.905f });
            m_modsTableDisabled.SetColumnComparison(1, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            Controls.Add(m_modsTableDisabled);

            m_modsTableEnabled = new MyGuiControlTable();
            m_modsTableEnabled.Position = origin + new Vector2(m_modsTableDisabled.Size.X + 0.04f, 0.1f);
            m_modsTableEnabled.Size = new Vector2(m_size.Value.X * 0.4375f, 1.25f);
            m_modsTableEnabled.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_modsTableEnabled.ColumnsCount = 2;
            m_modsTableEnabled.VisibleRowsCount = MyFakes.ENABLE_MOD_CATEGORIES ? 17 : 20;
            
            m_modsTableEnabled.ItemSelected += OnTableItemSelected;
            m_modsTableEnabled.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_modsTableEnabled.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_modsTableEnabled.SetCustomColumnWidths(new float[] { 0.085f, 0.905f });
            m_modsTableEnabled.SetColumnComparison(1, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            Controls.Add(m_modsTableEnabled);

            Controls.Add(m_labelEnabled = MakeLabel(m_modsTableEnabled.Position + new Vector2(m_modsTableEnabled.Size.X / 2f, 0f), MyCommonTexts.ScreenMods_ActiveMods));
            Controls.Add(m_labelDisabled = MakeLabel(m_modsTableDisabled.Position + new Vector2(m_modsTableDisabled.Size.X / 2f, 0f), MyCommonTexts.ScreenMods_AvailableMods));

            Controls.Add(m_moveUpButton = MakeButtonTiny(tinyButtonsOrigin + 0 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, -MathHelper.PiOver2, MyCommonTexts.ToolTipScreenMods_MoveUp, MyGuiConstants.TEXTURE_BUTTON_ARROW_SINGLE, OnMoveUpClick));
            Controls.Add(m_moveTopButton = MakeButtonTiny(tinyButtonsOrigin + 1 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, -MathHelper.PiOver2, MyCommonTexts.ToolTipScreenMods_MoveTop, MyGuiConstants.TEXTURE_BUTTON_ARROW_DOUBLE, OnMoveTopClick));
            Controls.Add(m_moveBottomButton = MakeButtonTiny(tinyButtonsOrigin + 2 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MathHelper.PiOver2, MyCommonTexts.ToolTipScreenMods_MoveBottom, MyGuiConstants.TEXTURE_BUTTON_ARROW_DOUBLE, OnMoveBottomClick));
            Controls.Add(m_moveDownButton = MakeButtonTiny(tinyButtonsOrigin + 3 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MathHelper.PiOver2, MyCommonTexts.ToolTipScreenMods_MoveDown, MyGuiConstants.TEXTURE_BUTTON_ARROW_SINGLE, OnMoveDownClick));

            Controls.Add(m_moveLeftButton = MakeButtonTiny(tinyButtonsOrigin + 5 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MathHelper.Pi, MyCommonTexts.ToolTipScreenMods_MoveLeft, MyGuiConstants.TEXTURE_BUTTON_ARROW_SINGLE, OnMoveLeftClick));
            Controls.Add(m_moveLeftAllButton = MakeButtonTiny(tinyButtonsOrigin + 6 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MathHelper.Pi, MyCommonTexts.ToolTipScreenMods_MoveLeftAll, MyGuiConstants.TEXTURE_BUTTON_ARROW_DOUBLE, OnMoveLeftAllClick));
            Controls.Add(m_moveRightAllButton = MakeButtonTiny(tinyButtonsOrigin + 7 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, 0f, MyCommonTexts.ToolTipScreenMods_MoveRightAll, MyGuiConstants.TEXTURE_BUTTON_ARROW_DOUBLE, OnMoveRightAllClick));
            Controls.Add(m_moveRightButton = MakeButtonTiny(tinyButtonsOrigin + 8 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, 0f, MyCommonTexts.ToolTipScreenMods_MoveRight, MyGuiConstants.TEXTURE_BUTTON_ARROW_SINGLE, OnMoveRightClick));

            Controls.Add(m_publishModButton = MakeButton(m_modsTableDisabled.Position + new Vector2(0f, m_modsTableDisabled.Size.Y + 0.01f), MyCommonTexts.LoadScreenButtonPublish, MyCommonTexts.LoadScreenButtonPublish, OnPublishModClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));
            Controls.Add(m_openInWorkshopButton = MakeButton(m_publishModButton.Position + new Vector2(m_publishModButton.Size.X + 0.04f, 0f), MyCommonTexts.ScreenLoadSubscribedWorldOpenInWorkshop, MyCommonTexts.ToolTipWorkshopOpenModInWorkshop, OnOpenInWorkshopClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));
            Controls.Add(m_refreshButton = MakeButton(m_publishModButton.Position + new Vector2(0f, m_publishModButton.Size.Y + 0.01f), MyCommonTexts.ScreenLoadSubscribedWorldRefresh, MyCommonTexts.ToolTipWorkshopRefreshMod, OnRefreshClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));
            Controls.Add(m_browseWorkshopButton = MakeButton(m_openInWorkshopButton.Position + new Vector2(0f, m_publishModButton.Size.Y + 0.01f), MyCommonTexts.ScreenLoadSubscribedWorldBrowseWorkshop, MyCommonTexts.ToolTipWorkshopBrowseWorkshop, OnBrowseWorkshopClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));

            Controls.Add(m_cancelButton = MakeButton(m_modsTableEnabled.Position + m_modsTableEnabled.Size + new Vector2(0f, 0.01f), MyCommonTexts.Cancel, MyCommonTexts.Cancel, OnCancelClick, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP));
            Controls.Add(m_okButton = MakeButton(m_cancelButton.Position - new Vector2(m_cancelButton.Size.X + 0.04f, 0f), MyCommonTexts.Ok, MyCommonTexts.Ok, OnOkClick, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP));

            //category buttons
            if (MyFakes.ENABLE_MOD_CATEGORIES)
            {
                Vector2 buttonPosition = m_modsTableDisabled.Position + new Vector2(0.02f, -0.03f);
                Vector2 buttonOffset = new Vector2(0.0414f, 0f);

                var categories = MySteamWorkshop.ModCategories;
                int i = 0;
                for (; i < categories.Length; ++i)
                {
                    Controls.Add(MakeButtonCategory(buttonPosition + buttonOffset * i, categories[i]));
                }

                var m_categoryCategorySelectButton = new MyGuiControlButton()
                {
                    Position = (buttonPosition + buttonOffset * i) + new Vector2(-0.02f, -0.014f),
                    Size = new Vector2(0.05f, 0.05f),
                    Name = "SelectCategory",
                    Text = "...",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    VisualStyle = MyGuiControlButtonStyleEnum.Tiny,
                };
                m_categoryCategorySelectButton.SetToolTip(MyCommonTexts.TooltipScreenMods_SelectCategories);
                m_categoryCategorySelectButton.ButtonClicked += OnSelectCategoryClicked;
                Controls.Add(m_categoryCategorySelectButton);

                Vector2 searchPosition = m_modsTableDisabled.Position + new Vector2(0.1625f, -0.08f);

                var searchBoxLabel = MakeLabel(searchPosition + new Vector2(-0.135f, 0.01f), MyCommonTexts.ScreenMods_SearchLabel);
                m_searchBox = new MyGuiControlTextbox(searchPosition);
                m_searchBox.Size = new Vector2(0.2f, 0.2f);
                m_searchBox.TextChanged += OnSearchTextChanged;

                m_searchClear = new MyGuiControlButton()
                {
                    Position = searchPosition + new Vector2(0.077f, 0f),
                    Size = new Vector2(0.045f, 0.05666667f),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    VisualStyle = MyGuiControlButtonStyleEnum.Close,
                    ActivateOnMouseRelease = true
                };
                m_searchClear.ButtonClicked += OnSearchClear;

                Vector2 buttonsOffset = new Vector2(0, 0.05f);

                m_moveUpButton.Position += buttonsOffset;
                m_moveTopButton.Position += buttonsOffset;
                m_moveBottomButton.Position += buttonsOffset;
                m_moveDownButton.Position += buttonsOffset;

                m_moveLeftButton.Position += buttonsOffset;
                m_moveLeftAllButton.Position += buttonsOffset;
                m_moveRightAllButton.Position += buttonsOffset;
                m_moveRightButton.Position += buttonsOffset;


                Controls.Add(searchBoxLabel);
                Controls.Add(m_searchBox);
                Controls.Add(m_searchClear);

                m_labelDisabled.Position += new Vector2(0, -0.1f);
            }

            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMods";
        }

        private MyGuiControlLabel MakeLabel(Vector2 position, MyStringId textEnum)
        {
            return new MyGuiControlLabel(position: position, text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, MyStringId toolTip, Action<MyGuiControlButton> onClick, MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            return new MyGuiControlButton(
                            position: position,
                            text: MyTexts.Get(text),
                            toolTip: MyTexts.GetString(toolTip),
                            onButtonClick: onClick,
                            originAlign: originAlign);
        }

        private MyGuiControlButton MakeButtonTiny(Vector2 position, float rotation, MyStringId toolTip, MyGuiHighlightTexture icon, Action<MyGuiControlButton> onClick, Vector2? size = null)
        {
            var button = new MyGuiControlButton(
                            position: position,
                            toolTip: MyTexts.GetString(toolTip),
                            onButtonClick: onClick,
                            visualStyle: MyGuiControlButtonStyleEnum.Square,
                            size: size);
            button.Icon = icon;
            button.IconRotation = rotation;
            button.IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            return button;
        }

        private MyGuiControlButton MakeButtonCategory(Vector2 position, MySteamWorkshop.Category category)
        {
            var idWithoutSpaces = category.Id.Replace(" ", "");
            var button = MakeButtonTiny(position, 0f, category.LocalizableName,
                new MyGuiHighlightTexture
                {
                    Normal = string.Format(@"Textures\GUI\Icons\buttons\{0}.dds", idWithoutSpaces),
                    Highlight = string.Format(@"Textures\GUI\Icons\buttons\{0}Highlight.dds", idWithoutSpaces),
                    SizePx = new Vector2(64f, 64f),
                },
                OnCategoryButtonClick);
            button.UserData = category.Id;
            button.HighlightType = MyGuiControlHighlightType.FORCED;
            m_categoryButtonList.Add(button);
            return button;
        }

        private void MoveSelectedItem(MyGuiControlTable from, MyGuiControlTable to)
        {
            to.Add(from.SelectedRow);

            from.RemoveSelectedRow();
            m_selectedRow = from.SelectedRow;
        }

        private void GetActiveMods(List<MyObjectBuilder_Checkpoint.ModItem> outputList)
        {
            for (int i = m_modsTableEnabled.RowsCount - 1; i >= 0; --i)
            {
                outputList.Add((MyObjectBuilder_Checkpoint.ModItem)m_modsTableEnabled.GetRow(i).UserData);
            }
        }

        #region Event handling
        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = sender.SelectedRow;
            m_selectedTable = sender;

            if (sender == m_modsTableEnabled)
                m_modsTableDisabled.SelectedRowIndex = null;

            if (sender == m_modsTableDisabled)
                m_modsTableEnabled.SelectedRowIndex = null;

            if (MyInput.Static.IsAnyCtrlKeyPressed())
                OnTableItemConfirmedOrDoubleClick(sender, eventArgs);
        }

        private void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (sender.SelectedRow == null)
                return;

            var moveTo = sender == m_modsTableEnabled ? m_modsTableDisabled : m_modsTableEnabled;

            MoveSelectedItem(sender, moveTo);
        }

        private void OnMoveUpClick(MyGuiControlButton sender)
        {
            m_selectedTable.MoveSelectedRowUp();
        }

        private void OnMoveDownClick(MyGuiControlButton sender)
        {
            m_selectedTable.MoveSelectedRowDown();
        }

        private void OnMoveTopClick(MyGuiControlButton sender)
        {
            m_selectedTable.MoveSelectedRowTop();
        }

        private void OnMoveBottomClick(MyGuiControlButton sender)
        {
            m_selectedTable.MoveSelectedRowBottom();
        }

        private void OnMoveLeftClick(MyGuiControlButton sender)
        {
            MoveSelectedItem(m_modsTableEnabled, m_modsTableDisabled);
        }

        private void OnMoveRightClick(MyGuiControlButton sender)
        {
            MoveSelectedItem(m_modsTableDisabled, m_modsTableEnabled);
        }

        private void OnMoveLeftAllClick(MyGuiControlButton sender)
        {
            while (m_modsTableEnabled.RowsCount > 0)
            {
                m_modsTableEnabled.SelectedRowIndex = 0;
                MoveSelectedItem(m_modsTableEnabled, m_modsTableDisabled);
            }
        }

        private void OnMoveRightAllClick(MyGuiControlButton sender)
        {
            while (m_modsTableDisabled.RowsCount > 0)
            {
                m_modsTableDisabled.SelectedRowIndex = 0;
                MoveSelectedItem(m_modsTableDisabled, m_modsTableEnabled);
            }
        }

        //TODO fix highlight
        private void OnCategoryButtonClick(MyGuiControlButton sender)
        {
            if (sender.UserData == null || !(sender.UserData is string))
            {
                return;
            }

            string category = (string)sender.UserData;
            if (m_selectedCategories.Contains(category))
            {
                m_selectedCategories.Remove(category);
                sender.Selected = false;
            }
            else
            {
                m_selectedCategories.Add(category);
                sender.Selected = true;
            }
            RefreshGameList();
        }

        private void OnSelectCategoryClicked(MyGuiControlButton sender)
        {
            var inTags = m_selectedCategories.ToArray();
            MyGuiSandbox.AddScreen(new MyGuiScreenWorkshopTags(MySteamWorkshop.WORKSHOP_MOD_TAG, MySteamWorkshop.ModCategories, inTags, delegate(MyGuiScreenMessageBox.ResultEnum tagsResult, string[] outTags)
            {
                if (tagsResult == MyGuiScreenMessageBox.ResultEnum.YES)
                {
                    var outList = outTags.ToList();
                    outList.Remove(MySteamWorkshop.WORKSHOP_MOD_TAG);
                    m_selectedCategories = outList;
                    RefreshCategoryButtons();
                    RefreshGameList();
                }
            }));
        }

        private void OnSearchTextChanged(MyGuiControlTextbox sender)
        {
            if (sender.Text != "")
            {
                String[] tmpSearch = sender.Text.Split(' ');
                m_tmpSearch = tmpSearch.ToList();
            }
            else
                m_tmpSearch.Clear();
            RefreshGameList();
        }

        private void OnSearchClear(MyGuiControlButton sender)
        {
            m_searchBox.Text = "";
        }

        private void OnPublishModClick(MyGuiControlButton sender)
        {
            if (m_selectedRow == null)
                return;

            if (m_selectedRow.UserData == null)
                return;

            var mod = (MyObjectBuilder_Checkpoint.ModItem)m_selectedRow.UserData;
            var modFullPath = Path.Combine(MyFileSystem.ModsPath, mod.Name);
            var nameSB = m_selectedRow.GetCell(1).Text;
            var name = nameSB.ToString();

            mod.PublishedFileId = MySteamWorkshop.GetWorkshopIdFromLocalMod(modFullPath);

            MyStringId textQuestion, captionQuestion;
            if (mod.PublishedFileId != 0)
            {
                textQuestion = MyCommonTexts.MessageBoxTextDoYouWishToUpdateMod;
                captionQuestion = MyCommonTexts.MessageBoxCaptionDoYouWishToUpdateMod;
            }
            else
            {
                textQuestion = MyCommonTexts.MessageBoxTextDoYouWishToPublishMod;
                captionQuestion = MyCommonTexts.MessageBoxCaptionDoYouWishToPublishMod;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                styleEnum: MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: MyTexts.Get(textQuestion),
                messageCaption: MyTexts.Get(captionQuestion),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                {
                    if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        string[] inTags = null;
                        var subscribedItem = GetSubscribedItem(mod.PublishedFileId);
                        if (subscribedItem != null)
                        {
                            inTags = subscribedItem.Tags;

                            if (subscribedItem.SteamIDOwner != Sync.MyId)
                            {
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextPublishFailed_OwnerMismatchMod),
                                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModPublishFailed)));
                                return;
                            }
                        }

                        MyGuiSandbox.AddScreen(new MyGuiScreenWorkshopTags(MySteamWorkshop.WORKSHOP_MOD_TAG, MySteamWorkshop.ModCategories, inTags, delegate(MyGuiScreenMessageBox.ResultEnum tagsResult, string[] outTags)
                        {
                            if (tagsResult == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                MySteamWorkshop.PublishModAsync(modFullPath, name, null, mod.PublishedFileId, outTags, SteamSDK.PublishedFileVisibility.Public, callbackOnFinished: delegate(bool success, Result result, ulong publishedFileId)
                                {
                                    if (success)
                                    {
                                        MySteamWorkshop.GenerateModInfo(modFullPath, publishedFileId, Sync.MyId);
                                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            styleEnum: MyMessageBoxStyleEnum.Info,
                                            messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextModPublished),
                                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModPublished),
                                            callback: (a) =>
                                            {
                                                MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", publishedFileId));
                                                FillList();
                                            }));
                                    }
                                    else
                                    {
                                        MyStringId error;
                                        switch (result)
                                        {
                                            case Result.AccessDenied:
                                                error = MyCommonTexts.MessageBoxTextPublishFailed_AccessDenied;
                                                break;
                                            default:
                                                error = MyCommonTexts.MessageBoxTextWorldPublishFailed;
                                                break;
                                        }

                                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            messageText: MyTexts.Get(error),
                                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModPublishFailed)));
                                    }
                                });
                            }
                        }));
                    }
                }));
        }

        private void OnOpenInWorkshopClick(MyGuiControlButton obj)
        {
            if (m_selectedRow == null)
                return;

            var mod = m_selectedRow.UserData;
            if (mod == null)
                return;

            string url = string.Format(MySteamConstants.URL_WORKSHOP_VIEW_ITEM_FORMAT, ((MyObjectBuilder_Checkpoint.ModItem)mod).PublishedFileId);
            MyGuiSandbox.OpenUrlWithFallback(url, "Steam Workshop");
        }

        private void OnBrowseWorkshopClick(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_MODS, "Steam Workshop");
        }

        private void OnRefreshClick(MyGuiControlButton obj)
        {
            if (!m_listNeedsReload)
            {
                m_listNeedsReload = true;
                FillList();
            }
        }

        private void OnOkClick(MyGuiControlButton obj)
        {
            // Apply changes to currently edited list of mods.
            m_modListToEdit.Clear();
            GetActiveMods(m_modListToEdit);
            this.CloseScreen();
        }

        private void OnCancelClick(MyGuiControlButton obj)
        {
            this.CloseScreen();
        }

        public override bool Update(bool hasFocus)
        {
            bool isRowSelected = m_selectedRow != null;
            bool isUserDataValid = isRowSelected && m_selectedRow.UserData != null;
            bool isLocalMod = isUserDataValid && ((MyObjectBuilder_Checkpoint.ModItem)m_selectedRow.UserData).PublishedFileId == 0;
            bool isWorkshopMod = isUserDataValid && ((MyObjectBuilder_Checkpoint.ModItem)m_selectedRow.UserData).PublishedFileId != 0;

            m_openInWorkshopButton.Enabled = isRowSelected && isWorkshopMod;
            m_publishModButton.Enabled = isRowSelected && isLocalMod;

            m_moveUpButton.Enabled = m_moveTopButton.Enabled = isRowSelected && m_selectedTable.SelectedRowIndex.HasValue && m_selectedTable.SelectedRowIndex.Value > 0;
            m_moveDownButton.Enabled = m_moveBottomButton.Enabled = isRowSelected && m_selectedTable.SelectedRowIndex.HasValue && m_selectedTable.SelectedRowIndex.Value < m_selectedTable.RowsCount - 1;

            m_moveLeftButton.Enabled = isRowSelected && m_selectedTable == m_modsTableEnabled;
            m_moveRightButton.Enabled = isRowSelected && m_selectedTable == m_modsTableDisabled;

            m_moveLeftAllButton.Enabled = m_modsTableEnabled.RowsCount > 0;
            m_moveRightAllButton.Enabled = m_modsTableDisabled.RowsCount > 0;

            return base.Update(hasFocus);
        }

        public override bool Draw()
        {
            // Dont draw screen when the list is about to be reloaded,
            // otherwise it will flick just before opening the loading screen
            if (m_listNeedsReload)
                return false;
            return base.Draw();
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (m_listNeedsReload)
                FillList();
        }

        #endregion

        #region Async Loading

        void FillList()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginAction, endAction));
        }

        class LoadListResult : IMyAsyncResult
        {
            public bool IsCompleted { get { return this.Task.IsComplete; } }
            public Task Task
            {
                get;
                private set;
            }

            /// <summary>
            /// List of mods user is subscribed to, or null if there was an error
            /// during operation.
            /// </summary>
            public List<MySteamWorkshop.SubscribedItem> SubscribedMods;
            public List<MySteamWorkshop.SubscribedItem> WorldMods;

            public LoadListResult(HashSet<ulong> ids)
            {
                Task = Parallel.Start(() =>
                {
                    SubscribedMods = new List<MySteamWorkshop.SubscribedItem>(ids.Count);
                    WorldMods = new List<MySteamWorkshop.SubscribedItem>();

                    if (!MySteam.IsOnline)
                        return;

                    if (!MySteamWorkshop.GetSubscribedModsBlocking(SubscribedMods))
                        return;

                    var toGet = new HashSet<ulong>(ids);

                    foreach (var mod in SubscribedMods)
                        toGet.Remove(mod.PublishedFileId);
                    if (toGet.Count > 0)
                        MySteamWorkshop.GetItemsBlocking(WorldMods, toGet);
                });
            }
        }

        private void AddHeaders()
        {
            m_modsTableEnabled.SetColumnName(1, MyTexts.Get(MyCommonTexts.Name));
            m_modsTableDisabled.SetColumnName(1, MyTexts.Get(MyCommonTexts.Name));
        }

        private void AddMod(bool active, StringBuilder title, StringBuilder toolTip, StringBuilder modState, MyGuiHighlightTexture? icon, MyObjectBuilder_Checkpoint.ModItem mod, Color? textColor = null)
        {
            var row = new MyGuiControlTable.Row(mod);
            row.AddCell(new MyGuiControlTable.Cell(text: String.Empty, toolTip: modState.ToString(), icon: icon));
            row.AddCell(new MyGuiControlTable.Cell(text: title, toolTip: toolTip.ToString(), textColor: textColor));

            if (active)
                m_modsTableEnabled.Insert(0, row);
            else
                m_modsTableDisabled.Insert(0, row);
        }

        private MySteamWorkshop.SubscribedItem GetSubscribedItem(ulong publishedFileId)
        {
            foreach (var subcribedItem in m_subscribedMods)
                if (subcribedItem.PublishedFileId == publishedFileId)
                    return subcribedItem;
            foreach (var worldItem in m_worldMods)
                if (worldItem.PublishedFileId == publishedFileId)
                    return worldItem;

            return null;
        }

        private void RefreshCategoryButtons()
        {
            foreach (var button in m_categoryButtonList)
            {
                if (button.UserData != null)
                {
                    var name = (button.UserData as string).ToLower();
                    button.Selected = m_selectedCategories.Contains(name);
                }
            }
        }

        private void RefreshGameList()
        {
            m_selectedRow = null;
            m_selectedTable = null;

            ListReader<MyObjectBuilder_Checkpoint.ModItem> lastActiveMods;
            if (m_keepActiveMods)
            {
                var tmp = new List<MyObjectBuilder_Checkpoint.ModItem>(m_modsTableEnabled.RowsCount);
                GetActiveMods(tmp);
                lastActiveMods = tmp;
            }
            else
            {
                lastActiveMods = m_modListToEdit;
            }
            m_keepActiveMods = true;
            GetWorldMods(lastActiveMods);

            m_modsTableEnabled.Clear();
            m_modsTableDisabled.Clear();
            AddHeaders();

            foreach (var mod in lastActiveMods)
            {
                if (mod.PublishedFileId == 0)
                {
                    var title = new StringBuilder(mod.Name);
                    var modFullPath = Path.Combine(MyFileSystem.ModsPath, mod.Name);
                    var toolTip = new StringBuilder(modFullPath);
                    var modState = MyTexts.Get(MyCommonTexts.ScreenMods_LocalMod);
                    Color? textColor = null;
                    MyGuiHighlightTexture icon = MyGuiConstants.TEXTURE_ICON_MODS_LOCAL;

                    if (!Directory.Exists(modFullPath) && !File.Exists(modFullPath))
                    {
                        toolTip = MyTexts.Get(MyCommonTexts.ScreenMods_MissingLocalMod);
                        modState = toolTip;
                        textColor = MyHudConstants.MARKER_COLOR_RED;
                    }

                    AddMod(true, title, toolTip, modState, icon, mod, textColor);
                }
                else
                {
                    var title = new StringBuilder();
                    var toolTip = new StringBuilder();
                    var modState = MyTexts.Get(MyCommonTexts.ScreenMods_WorkshopMod);
                    Color? textColor = null;

                    var subscribedItem = GetSubscribedItem(mod.PublishedFileId);
                    if (subscribedItem != null)
                    {
                        title.Append(subscribedItem.Title);

                        var shortLen = Math.Min(subscribedItem.Description.Length, 128);
                        var newlineIndex = subscribedItem.Description.IndexOf("\n");
                        if (newlineIndex > 0)
                            shortLen = Math.Min(shortLen, newlineIndex - 1);
                        toolTip.Append(subscribedItem.Description.Substring(0, shortLen));
                    }
                    else
                    {
                        title.Append(mod.PublishedFileId.ToString());
                        toolTip = MyTexts.Get(MyCommonTexts.ScreenMods_MissingDetails);
                        textColor = MyHudConstants.MARKER_COLOR_RED;
                    }

                    MyGuiHighlightTexture icon = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP;

                    AddMod(true, title, toolTip, modState, icon, mod, textColor);
                }
            }

            if (!Directory.Exists(MyFileSystem.ModsPath))
                Directory.CreateDirectory(MyFileSystem.ModsPath);

            foreach (var modFullPath in Directory.GetDirectories(MyFileSystem.ModsPath, "*", SearchOption.TopDirectoryOnly))
            {
                var modName = Path.GetFileName(modFullPath);
                if (m_worldLocalMods.Contains(modName))
                    continue;

                if (Directory.GetFileSystemEntries(modFullPath).Length == 0)
                    continue;

                if (MyFakes.ENABLE_MOD_CATEGORIES)
                {
                    if (!CheckSearch(modName))
                        continue;
                }

                var titleSB = new StringBuilder(modName);
                var descriptionSB = modFullPath;
                var modStateSB = MyTexts.GetString(MyCommonTexts.ScreenMods_LocalMod);

                var publishedFileId = MySteamWorkshop.GetWorkshopIdFromLocalMod(modFullPath);

                MyGuiHighlightTexture icon = MyGuiConstants.TEXTURE_ICON_MODS_LOCAL;

                var row = new MyGuiControlTable.Row(new MyObjectBuilder_Checkpoint.ModItem(modName, 0));
                row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(), toolTip: modStateSB, icon: icon));
                row.AddCell(new MyGuiControlTable.Cell(text: titleSB, toolTip: descriptionSB));
                m_modsTableDisabled.Add(row);
            }

            if (m_subscribedMods != null)
            {
                foreach (var mod in m_subscribedMods)
                {
                    if (mod == null || m_worldWorkshopMods.Contains(mod.PublishedFileId))
                        continue;
                    if (MyFakes.ENABLE_MOD_CATEGORIES)
                    {
                        bool add = false;
                        foreach (var tag in mod.Tags)
                        {
                            if (m_selectedCategories.Contains(tag.ToLower()) || m_selectedCategories.Count == 0)
                            {
                                add = true;
                                break;
                            }
                        }
                        if (!CheckSearch(mod.Title))
                            continue;
                        if (!add)
                            continue;
                    }
                    var titleSB = new StringBuilder(mod.Title);
                    var shortLen = Math.Min(mod.Description.Length, 128);
                    var newlineIndex = mod.Description.IndexOf("\n");
                    if (newlineIndex > 0)
                        shortLen = Math.Min(shortLen, newlineIndex - 1);
                    var descriptionSB = new StringBuilder();
                    var modStateSB = MyTexts.GetString(MyCommonTexts.ScreenMods_WorkshopMod);

                    var path = Path.Combine(MyFileSystem.ModsPath, string.Format("{0}.sbm", mod.PublishedFileId));

                    if (mod.Description.Length != 0)
                        descriptionSB.AppendLine(path);
                    else
                        descriptionSB.Append(path);
                    descriptionSB.Append(mod.Description.Substring(0, shortLen));

                    MyGuiHighlightTexture icon = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP;

                    var row = new MyGuiControlTable.Row(new MyObjectBuilder_Checkpoint.ModItem(null, mod.PublishedFileId));
                    row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(), toolTip: modStateSB, icon: icon));
                    row.AddCell(new MyGuiControlTable.Cell(text: titleSB, toolTip: descriptionSB.ToString()));
                    m_modsTableDisabled.Add(row);
                }
            }
        }

        private bool CheckSearch(string name)
        {
            var add = true;
            var tmpName = name.ToLower();
            foreach (var search in m_tmpSearch)
            {
                if (!tmpName.Contains(search.ToLower()))
                {
                    add = false;
                    break;
                }
            }
            return add;
        }

        private IMyAsyncResult beginAction()
        {
            return new LoadListResult(m_worldWorkshopMods);
        }

        private void endAction(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            m_listNeedsReload = false;

            var loadResult = (LoadListResult)result;
            m_subscribedMods = loadResult.SubscribedMods;
            m_worldMods = loadResult.WorldMods;
            RefreshGameList();
            screen.CloseScreen();
        }

        #endregion

    }
}
#endif // !XB1
