#if !XB1 // XB1_NOWORKSHOP
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;

using VRage;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;
using System.IO;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenLoadSubscribedWorld : MyGuiScreenBase
    {
        private MyGuiControlTable m_worldsTable;
        private MyGuiControlButton m_loadButton;
        private MyGuiControlButton m_openInWorkshopButton;
        private MyGuiControlButton m_refreshButton;
        private MyGuiControlButton m_browseWorkshopButton;
        private MyGuiControlButton m_copyButton;

        private MyGuiControlButton m_currentButton;

        private int m_selectedRow;
        private bool m_listNeedsReload;
        private List<MySteamWorkshop.SubscribedItem> m_subscribedWorlds;

        public MyGuiScreenLoadSubscribedWorld()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.9f, 0.97f))
        {
            EnabledBackgroundFade = true;

            m_listNeedsReload = true;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            new MyGuiControlScreenSwitchPanel(this, MyTexts.Get(MyCommonTexts.WorkshopScreen_Description));

            //AddCaption(MyCommonTexts.ScreenCaptionWorkshop);

            float MARGIN_TOP = 0.18f;
            float MARGIN_BOTTOM = 0.05f;
            float MARGIN_LEFT = 0.23f;
            float MARGIN_RIGHT = 0.035f;
            float MARGIN_LEFT_BUTTONS = 0.015f;

            var originR = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);
            var sizeR = m_size.Value / 2 - originR;
            sizeR.X -= MARGIN_RIGHT;
            sizeR.Y -= MARGIN_BOTTOM;
            var origin = -m_size.Value / 2 + new Vector2(MARGIN_LEFT_BUTTONS, MARGIN_TOP);
            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

            m_worldsTable = new MyGuiControlTable();
            m_worldsTable.Position = originR + new Vector2(0f, 0.004f);
            m_worldsTable.Size = sizeR;
            m_worldsTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_worldsTable.ColumnsCount = 1;
            m_worldsTable.VisibleRowsCount = 20;
            m_worldsTable.ItemSelected += OnTableItemSelected;
            m_worldsTable.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_worldsTable.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_worldsTable.SetCustomColumnWidths(new float[] { 1.0f });
            m_worldsTable.SetColumnComparison(0, (a, b) => ((StringBuilder)a.UserData).CompareToIgnoreCase((StringBuilder)b.UserData));
            Controls.Add(m_worldsTable);

            Vector2 buttonOrigin = origin + buttonSize * 0.5f;
            Vector2 buttonDelta = MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;

            // Load
            Controls.Add(m_loadButton = MakeButton(buttonOrigin + buttonDelta * 0, MyCommonTexts.ScreenLoadSubscribedWorldCopyAndLoad, MyCommonTexts.ToolTipWorkshopCopyAndLoad, OnLoadClick));
            Controls.Add(m_copyButton = MakeButton(buttonOrigin + buttonDelta * 1, MyCommonTexts.ScreenLoadSubscribedWorldCopyWorld, MyCommonTexts.ToolTipWorkshopCopyWorld, OnCopyClick));
            Controls.Add(m_openInWorkshopButton = MakeButton(buttonOrigin + buttonDelta * 2, MyCommonTexts.ScreenLoadSubscribedWorldOpenInWorkshop, MyCommonTexts.ToolTipWorkshopOpenInWorkshop, OnOpenInWorkshopClick));
            Controls.Add(m_refreshButton = MakeButton(buttonOrigin + buttonDelta * 3, MyCommonTexts.ScreenLoadSubscribedWorldRefresh, MyCommonTexts.ToolTipWorkshopRefresh, OnRefreshClick));
            Controls.Add(m_browseWorkshopButton = MakeButton(buttonOrigin + buttonDelta * 4, MyCommonTexts.ScreenLoadSubscribedWorldBrowseWorkshop, MyCommonTexts.ToolTipWorkshopBrowseWorkshop, OnBrowseWorkshopClick));

            m_loadButton.DrawCrossTextureWhenDisabled = false;
            m_openInWorkshopButton.DrawCrossTextureWhenDisabled = false;

            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenLoadSubscribedWorld";
        }

        #region Event handling
        private void OnOpenInWorkshopClick(MyGuiControlButton obj)
        {
            var selectedRow = m_worldsTable.SelectedRow;
            if (selectedRow == null)
                return;

            var world = (MySteamWorkshop.SubscribedItem)selectedRow.UserData;
            if (world == null)
                return;

            string url = string.Format(MySteamConstants.URL_WORKSHOP_VIEW_ITEM_FORMAT, world.PublishedFileId);
            MyGuiSandbox.OpenUrlWithFallback(url, "Steam Workshop");
        }

        private void OnBrowseWorkshopClick(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_WORLDS, "Steam Workshop");

            //if (!MySteam.IsOverlayEnabled)
            //{
            //    // message box
            //    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK,
            //        MyTexts.Get(MySpaceTexts.SteamOverlayDisabledText), MyTexts.Get(MySpaceTexts.SteamOverlayDisabledCaption)));
            //}
            //else
            //{
            //    MySteam.OpenOverlayUrl(MySteamConstants.URL_BROWSE_WORKSHOP_WORLDS);
            //}
        }

        private void OnRefreshClick(MyGuiControlButton obj)
        {
            if (!m_listNeedsReload)
            {
                m_listNeedsReload = true;
                FillList();
            }
        }

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = eventArgs.RowIndex;
        }

        private void OnLoadClick(MyGuiControlButton sender)
        {
            m_currentButton = m_loadButton;
            CreateAndLoadFromSubscribedWorld();
        }

        private void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_currentButton = m_loadButton;
            CreateAndLoadFromSubscribedWorld();
        }

        private void OnCopyClick(MyGuiControlButton sender)
        {
            m_currentButton = m_copyButton;
            CopyWorldAndGoToLoadScreen();
        }
        #endregion

        private void CreateAndLoadFromSubscribedWorld()
        {
            var selectedRow = m_worldsTable.SelectedRow;
            if (selectedRow == null)
                return;

            var world = (MySteamWorkshop.SubscribedItem)selectedRow.UserData;
            if (world == null)
                return;

            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginActionLoadSaves, endActionLoadSaves));
        }

        private void CopyWorldAndGoToLoadScreen()
        {
            var selectedRow = m_worldsTable.SelectedRow;
            if (selectedRow == null)
                return;

            var world = (MySteamWorkshop.SubscribedItem)selectedRow.UserData;
            if (world == null)
                return;

            //by Gregory: Changed functionality in order to add ovewrite dialog box. only one steam account is permitted right now
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginActionLoadSaves, endActionLoadSaves));
        }

        private void OnSuccess(string sessionPath)
        {
            if (m_currentButton == m_copyButton)
            {
                var loadScreen = new MyGuiScreenLoadSandbox();
                MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox());
                // TODO: FIx whatever I broke here
                //loadScreen.SelectSteamWorld(sessionPath);
            }
            else if (m_currentButton == m_loadButton)
            {
                MySessionLoader.LoadSingleplayerSession(sessionPath);
            }
            m_currentButton = null;
        }

        private void OverwriteWorldDialog()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,

                messageText: MyTexts.Get(m_currentButton == m_loadButton ? MyCommonTexts.MessageBoxTextWorldExistsDownloadOverwrite : MyCommonTexts.MessageBoxTextWorldExistsOverwrite),
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                callback: OnOverwriteWorld));
        }

        private void OnOverwriteWorld(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                var selectedRow = m_worldsTable.SelectedRow;
                var world = (MySteamWorkshop.SubscribedItem)selectedRow.UserData;
                MySteamWorkshop.CreateWorldInstanceAsync(world, MySteamWorkshop.MyWorkshopPathInfo.CreateWorldInfo(), true, delegate(bool success, string sessionPath)
                {
                    if (success)
                    {
                        OnSuccess(sessionPath);
                    }
                });
            }
        }

        public override bool Update(bool hasFocus)
        {
            if (m_worldsTable.SelectedRow != null)
            {
                m_loadButton.Enabled = true;
                m_copyButton.Enabled = true;
                m_openInWorkshopButton.Enabled = true;
            }
            else
            {
                m_loadButton.Enabled = false;
                m_copyButton.Enabled = false;
                m_openInWorkshopButton.Enabled = false;
            }

            return base.Update(hasFocus);
        }

        protected override void OnClosed()
        {
            base.OnClosed();

            MyAnalyticsHelper.ReportActivityEnd(null, "show_workshop");
        }

        protected override void OnShow()
        {
            base.OnShow();

            MyAnalyticsHelper.ReportActivityStart(null, "show_workshop", string.Empty, "gui", string.Empty);

            if (m_listNeedsReload)
                FillList();
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, MyStringId toolTip, Action<MyGuiControlButton> onClick)
        {
            return new MyGuiControlButton(
                            position: position,
                            text: MyTexts.Get(text),
                            toolTip: MyTexts.GetString(toolTip),
                            onButtonClick: onClick);
        }

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
            /// List of worlds user is subscribed to, or null if there was an error
            /// during operation.
            /// </summary>
            public List<MySteamWorkshop.SubscribedItem> SubscribedWorlds;

            public LoadListResult()
            {
                Task = Parallel.Start(() => LoadListAsync(out SubscribedWorlds));
            }

            void LoadListAsync(out List<MySteamWorkshop.SubscribedItem> list)
            {
                var worlds = new List<MySteamWorkshop.SubscribedItem>();
                if (MySteamWorkshop.GetSubscribedWorldsBlocking(worlds))
                    list = worlds;
                else
                    list = null;
            }
        }

        private void AddHeaders()
        {
            m_worldsTable.SetColumnName(0, MyTexts.Get(MyCommonTexts.Name));
        }

        private void RefreshGameList()
        {
            m_worldsTable.Clear();
            AddHeaders();

            if (m_subscribedWorlds != null)
            {
                for (int i = 0; i < m_subscribedWorlds.Count; ++i)
                {
                    var world = m_subscribedWorlds[i];
                    var row = new MyGuiControlTable.Row(world);
                    var titleSB = new StringBuilder(world.Title);
                    row.AddCell(new MyGuiControlTable.Cell(text: titleSB.ToString(),
                                                             userData: titleSB));
                    row.AddCell(new MyGuiControlTable.Cell());
                    m_worldsTable.Add(row);
                }
            }

            m_worldsTable.SelectedRowIndex = null;
        }

        private IMyAsyncResult beginAction()
        {
            return new LoadListResult();
        }

        private void endAction(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            m_listNeedsReload = false;

            var loadResult = (LoadListResult)result;
            m_subscribedWorlds = loadResult.SubscribedWorlds;
            RefreshGameList();
            screen.CloseScreen();
        }

        private IMyAsyncResult beginActionLoadSaves()
        {
            return new MyLoadWorldInfoListResult();
        }

        private void endActionLoadSaves(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreen();

            var selectedRow = m_worldsTable.SelectedRow;
            var world = (MySteamWorkshop.SubscribedItem)selectedRow.UserData;



            string safeName = MyUtils.StripInvalidChars(world.Title);
            var tempSessionPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);
            if (Directory.Exists(tempSessionPath))
            {
                OverwriteWorldDialog();
            }
            else
            {
                MySteamWorkshop.CreateWorldInstanceAsync(world, MySteamWorkshop.MyWorkshopPathInfo.CreateWorldInfo(), false, delegate(bool success, string sessionPath)
                {
                    if (success)
                    {
                        OnSuccess(sessionPath);
                    }
                });
            }
        }

        #endregion

    }
}
#endif // !XB1
