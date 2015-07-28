using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;

using VRage;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenLoadSubscribedWorld : MyGuiScreenBase
    {
        private MyGuiControlTable m_worldsTable;
        private MyGuiControlButton m_loadButton;
        private MyGuiControlButton m_openInWorkshopButton;
        private MyGuiControlButton m_refreshButton;
        private MyGuiControlButton m_browseWorkshopButton;
        private int m_selectedRow;
        private bool m_listNeedsReload;
        private List<MySteamWorkshop.SubscribedItem> m_subscribedWorlds;

        public MyGuiScreenLoadSubscribedWorld()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            EnabledBackgroundFade = true;

            m_listNeedsReload = true;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionWorkshop);

            var origin = new Vector2(-0.4375f, -0.3f);
            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

            m_worldsTable = new MyGuiControlTable();
            m_worldsTable.Position = origin + new Vector2(buttonSize.X * 1.1f, 0f);
            m_worldsTable.Size = new Vector2(1075f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1f);
            m_worldsTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_worldsTable.ColumnsCount = 1;
            m_worldsTable.VisibleRowsCount = 17;
            m_worldsTable.ItemSelected += OnTableItemSelected;
            m_worldsTable.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_worldsTable.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_worldsTable.SetCustomColumnWidths(new float[] { 1.0f });
            m_worldsTable.SetColumnComparison(0, (a, b) => ((StringBuilder)a.UserData).CompareToIgnoreCase((StringBuilder)b.UserData));
            Controls.Add(m_worldsTable);

            Vector2 buttonOrigin = origin + buttonSize * 0.5f;
            Vector2 buttonDelta = MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;

            // Load
            Controls.Add(m_loadButton = MakeButton(buttonOrigin + buttonDelta * 0, MySpaceTexts.ScreenLoadSubscribedWorldCopyAndLoad, MySpaceTexts.ToolTipWorkshopCopyAndLoad, OnLoadClick));
            Controls.Add(m_openInWorkshopButton = MakeButton(buttonOrigin + buttonDelta * 1, MySpaceTexts.ScreenLoadSubscribedWorldOpenInWorkshop, MySpaceTexts.ToolTipWorkshopOpenInWorkshop, OnOpenInWorkshopClick));
            Controls.Add(m_refreshButton = MakeButton(buttonOrigin + buttonDelta * 2, MySpaceTexts.ScreenLoadSubscribedWorldRefresh, MySpaceTexts.ToolTipWorkshopRefresh, OnRefreshClick));
            Controls.Add(m_browseWorkshopButton = MakeButton(buttonOrigin + buttonDelta * 3, MySpaceTexts.ScreenLoadSubscribedWorldBrowseWorkshop, MySpaceTexts.ToolTipWorkshopBrowseWorkshop, OnBrowseWorkshopClick));

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
            CreateAndLoadFromSubscribedWorld();
        }

        private void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            CreateAndLoadFromSubscribedWorld();
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
            MySteamWorkshop.CreateWorldInstanceAsync(world, MySteamWorkshop.MyWorkshopPathInfo.CreateWorldInfo(), false, delegate(bool success, string sessionPath)
            {
                if (success)
                    MyGuiScreenLoadSandbox.LoadSingleplayerSession(sessionPath);
            });

        }

        public override bool Update(bool hasFocus)
        {
            if (m_worldsTable.SelectedRow != null)
            {
                m_loadButton.Enabled = true;
                m_openInWorkshopButton.Enabled = true;
            }
            else
            {
                m_loadButton.Enabled = false;
                m_openInWorkshopButton.Enabled = false;
            }

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
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MySpaceTexts.LoadingPleaseWait, null, beginAction, endAction));
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
            m_worldsTable.SetColumnName(0, MyTexts.Get(MySpaceTexts.Name));
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

        #endregion

    }
}
