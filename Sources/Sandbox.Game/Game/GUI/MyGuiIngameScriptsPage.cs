
using ParallelTasks;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using VRage;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyScriptItemInfo:MyBlueprintItemInfo
    {
        public string Description;
        public string ScriptName;
#if !XB1 // XB1_NOWORKSHOP
        public MySteamWorkshop.SubscribedItem SteamItem;

        public MyScriptItemInfo(MyBlueprintTypeEnum type,string scriptName,ulong? id = null,string description =null,MySteamWorkshop.SubscribedItem item=null):
            base(type,id)
        {
            ScriptName = scriptName;
            Description = description;
            SteamItem = item;
        }
#else // XB1
        public MyScriptItemInfo(MyBlueprintTypeEnum type, string scriptName, ulong? id = null, string description = null) :
            base(type, id)
        {
            ScriptName = scriptName;
            Description = description;
        }
#endif // XB1
    }
    [PreloadRequired]
    public class MyGuiIngameScriptsPage : MyGuiScreenDebugBase
    {
        public const string STEAM_THUMBNAIL_NAME = @"Textures\GUI\Icons\IngameProgrammingIcon.png";
        public const string THUMBNAIL_NAME = @"thumb.png";
        public const string DEFAULT_SCRIPT_NAME = "Script";
        public const string SCRIPTS_DIRECTORY = "IngameScripts";
        public const string SCRIPT_EXTENSION = ".cs";
        public const string WORKSHOP_SCRIPT_EXTENSION = ".sbs";

        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.33f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;

        private static Task m_task;

#if !XB1 // XB1_NOWORKSHOP
        private static List<MySteamWorkshop.SubscribedItem> m_subscribedItemsList = new List<MySteamWorkshop.SubscribedItem>();
#endif // !XB1

        private Vector2 m_controlPadding = new Vector2(0.02f, 0.02f);
        private float m_textScale = 0.8f;

        private MyGuiControlButton m_createFromEditorButton;
        private MyGuiControlButton m_detailsButton;
        private MyGuiControlButton m_deleteButton;
        private MyGuiControlButton m_renameButton;
        private MyGuiControlButton m_replaceButton;

        private MyGuiControlTextbox m_searchBox;
        private MyGuiControlButton m_searchClear;
        private static MyGuiControlListbox m_scriptList = new MyGuiControlListbox(visualStyle: MyGuiControlListboxStyleEnum.IngameScipts);
        private MyGuiDetailScreenScriptLocal m_detailScreen = null;
        private bool m_activeDetail = false;
        private MyGuiControlListbox.Item m_selectedItem = null;
        private MyGuiControlRotatingWheel m_wheel;
        string m_localScriptFolder;
        string m_workshopFolder;
        
        Action OnClose = null;
        Action<string> OnScriptOpened = null;
        Func<string> GetCodeFromEditor = null;

        static MyGuiIngameScriptsPage()
        {
        }

        public override string GetFriendlyName()
        {
            return "MyIngameScriptScreen";
        }

        public MyGuiIngameScriptsPage(Action<string> onScriptOpened,Func<string> getCodeFromEditor,Action close) :
            base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            EnabledBackgroundFade = true;
            OnClose = close;
            this.GetCodeFromEditor = getCodeFromEditor;
            this.OnScriptOpened = onScriptOpened;
            m_localScriptFolder = Path.Combine(MyFileSystem.UserDataPath, SCRIPTS_DIRECTORY, "local");
            m_workshopFolder = Path.Combine(MyFileSystem.UserDataPath, SCRIPTS_DIRECTORY, "workshop");

            if (!Directory.Exists(m_localScriptFolder))
            {
                Directory.CreateDirectory(m_localScriptFolder);
            }

            if (!Directory.Exists(m_workshopFolder))
            {
                Directory.CreateDirectory(m_workshopFolder);
            }

            m_scriptList.Items.Clear();

#if !XB1 // XB1_NOWORKSHOP
            GetLocalScriptNames(m_subscribedItemsList.Count == 0);
#else // XB1
            GetLocalScriptNames(true);
#endif // XB1
            RecreateControls(true);

            m_scriptList.ItemsSelected += OnSelectItem;
            m_scriptList.ItemDoubleClicked += OnItemDoubleClick;
            OnEnterCallback += Ok;

            m_canShareInput = false;
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;

            m_searchBox.TextChanged += OnSearchTextChange;
        }

        void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(-0.075f, 0.25f);
            Vector2 buttonOffset = new Vector2(0.11f, 0.035f);
            float width = 0.11f;

            var okButton = CreateButton(width, MyTexts.Get(MyCommonTexts.Ok), OnOk, textScale: m_textScale);
            okButton.Position = buttonPosition;

            var cancelButton = CreateButton(width, MyTexts.Get(MyCommonTexts.Cancel), OnCancel, textScale: m_textScale);
            cancelButton.Position = buttonPosition + new Vector2(1f, 0f) * buttonOffset;

            m_detailsButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonDetails), OnDetails, textScale: m_textScale, enabled: false);
            m_detailsButton.Position = buttonPosition + new Vector2(0f, 1f) * buttonOffset;

            m_renameButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonRename), OnRename, textScale: m_textScale, enabled: false);
            m_renameButton.Position = buttonPosition + new Vector2(1f, 1f) * buttonOffset;

            width = 0.22f;

            m_deleteButton = CreateButton(width, MyTexts.Get(MyCommonTexts.LoadScreenButtonDelete), OnDelete, false, textScale: m_textScale);
            m_deleteButton.Position = (buttonPosition + new Vector2(0f, 2f) * buttonOffset) * new Vector2(0.29f, 1f);

            m_createFromEditorButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonCreateFromEditor), OnCreateFromEditor, textScale: m_textScale, enabled: true);
            m_createFromEditorButton.Position = (buttonPosition + new Vector2(0f, 3f) * buttonOffset) * new Vector2(0.29f, 1f);

            m_replaceButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonReplaceFromEditor), OnReplaceFromEditor, textScale: m_textScale, enabled: true);
            m_replaceButton.Position = (buttonPosition + new Vector2(0f, 4f) * buttonOffset) * new Vector2(0.29f, 1f);

            if (!MyFakes.XB1_PREVIEW)
            {
#if !XB1 // XB1_NOWORKSHOP
                var workshopButton = CreateButton(width, MyTexts.Get(MyCommonTexts.ScreenLoadSubscribedWorldBrowseWorkshop), OnOpenWorkshop, textScale: m_textScale);
                workshopButton.Position = (buttonPosition + new Vector2(0f, 5f) * buttonOffset) * new Vector2(0.29f, 1f);

                var reloadButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonRefreshScripts), OnReload, textScale: m_textScale);
                reloadButton.Position = (buttonPosition + new Vector2(0f, 6f) * buttonOffset) * new Vector2(0.29f, 1f);
#endif // !XB1
            }
            else
            {
                var reloadButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonRefreshScripts), OnReload, textScale: m_textScale);
                reloadButton.Position = (buttonPosition + new Vector2(0f, 5f) * buttonOffset) * new Vector2(0.29f, 1f);
            }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 searchPosition = new Vector2(-0.025f, SCREEN_SIZE.Y - 1.59f);

            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f;

            var searchBoxLabel = MakeLabel(MyTexts.GetString(MyCommonTexts.ScreenCubeBuilderBlockSearch), searchPosition + new Vector2(-0.129f, -0.015f), m_textScale);
            m_searchBox = new MyGuiControlTextbox(searchPosition);
            m_searchBox.Size = new Vector2(0.15f, 0.2f);

            m_searchClear = new MyGuiControlButton()
            {
                Position = searchPosition + new Vector2(0.078f, 0f),
                Size = new Vector2(0.045f, 0.05666667f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close
            };
            m_searchClear.ButtonClicked += OnSearchClear;

            m_scriptList.Size -= new Vector2(0.1f,0f);
            m_scriptList.Position = new Vector2(-0.015f, -0.07f);
            m_scriptList.VisibleRowsCount = 17;
            m_scriptList.MultiSelect = false;

            var caption = AddCaption(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_ScriptsScreenTitle), VRageMath.Color.White.ToVector4(), m_controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            Controls.Add(searchBoxLabel);
            Controls.Add(m_searchBox);
            Controls.Add(m_searchClear);
            Controls.Add(m_scriptList);

            CreateButtons();

            var texture = MyGuiConstants.LOADING_TEXTURE;

            m_wheel = new MyGuiControlRotatingWheel(
                searchPosition + new Vector2(0.123f, 0f),
                MyGuiConstants.ROTATING_WHEEL_COLOR,
                0.28f,
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                texture,
                true,
                multipleSpinningWheels: MyPerGameSettings.GUI.MultipleSpinningWheels);
            Controls.Add(m_wheel);
            m_wheel.Visible = false;
        }

        void GetLocalScriptNames(bool reload = false)
        {
            if (!Directory.Exists(m_localScriptFolder))
                return;
            string[] scriptNames = Directory.GetDirectories(m_localScriptFolder);

            foreach(var scriptName in scriptNames)
            {
                string directoryName = Path.GetFileName(scriptName);
                var info = new MyScriptItemInfo(MyBlueprintTypeEnum.LOCAL,directoryName);  
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(directoryName), toolTip: directoryName, userData: info, icon: MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL.Normal);
                m_scriptList.Add(item);
            }

#if !XB1 // XB1_NOWORKSHOP
            if (m_task.IsComplete && reload)
            {
                GetWorkshopScripts();
            }
            else         
            {
                AddWorkshopItemsToList();
            }
#endif // !XB1
        }

#if !XB1 // XB1_NOWORKSHOP
        private static void AddWorkshopItemsToList()
        {
            if (MyFakes.XB1_PREVIEW)
                return;

            foreach (var steamItem in m_subscribedItemsList)
            {
                var info = new MyScriptItemInfo(MyBlueprintTypeEnum.STEAM, steamItem.Title, steamItem.SteamIDOwner, steamItem.Description, steamItem);
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(steamItem.Title), toolTip: steamItem.Title, userData: info, icon: MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.Normal);
                m_scriptList.Add(item);
            }
        }

        void GetScriptsInfo()
        {
            ProfilerShort.Begin("Getting workshop scripts.");
            ProfilerShort.BeginNextBlock("downloading");
            m_subscribedItemsList.Clear();
            bool success = MySteamWorkshop.GetSubscribedIngameScriptsBlocking(m_subscribedItemsList);
            if (success)
            {
                if (Directory.Exists(m_workshopFolder))
                {
                    try
                    {
                        Directory.Delete(m_workshopFolder, true);
                    }
                    catch (System.IO.IOException)
                    {
                    }
                }
                Directory.CreateDirectory(m_workshopFolder);
            }
            if (success )
            {
                AddWorkshopItemsToList();
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: new StringBuilder("Couldn't load scripts from steam workshop"),
                    messageCaption: new StringBuilder("Error")));
            }
            ProfilerShort.End();
        }

        void GetWorkshopScripts()
        {
            if (MyFakes.XB1_PREVIEW)
                return;

            m_task = Parallel.Start(GetScriptsInfo);
        }
#endif // !XB1

        public void RefreshBlueprintList(bool fromTask = false)
        {
            m_scriptList.Items.Clear();
            GetLocalScriptNames(fromTask);
        }

        public void RefreshAndReloadScriptsList(bool refreshWorkshopList = false)
        {
            m_scriptList.Items.Clear();
            GetLocalScriptNames(refreshWorkshopList);
        }

        void OnSearchClear(MyGuiControlButton button)
        {
            m_searchBox.Text = "";
        }

        void OnSelectItem(MyGuiControlListbox list)
        {
            if (list.SelectedItems.Count == 0)
            {
                return;
            }

            m_selectedItem = list.SelectedItems[0];
            m_detailsButton.Enabled = true;
            m_renameButton.Enabled = false;

            var type = (m_selectedItem.UserData as MyBlueprintItemInfo).Type;
            var id = (m_selectedItem.UserData as MyBlueprintItemInfo).PublishedItemId;

            if (type == MyBlueprintTypeEnum.LOCAL)
            {
                m_deleteButton.Enabled = true;
                m_replaceButton.Enabled = true;
                m_renameButton.Enabled = true;
            }
#if !XB1 // XB1_NOWORKSHOP
            else if (type == MyBlueprintTypeEnum.STEAM)
            {
                m_deleteButton.Enabled = false;
                m_replaceButton.Enabled = false;
            }
#endif // !XB1
            else if (type == MyBlueprintTypeEnum.SHARED)
            {
                m_renameButton.Enabled = false;
                m_detailsButton.Enabled = false;
                m_deleteButton.Enabled = false;
            }
        }

        bool ValidateSelecteditem()
        {
            if (m_selectedItem == null)
                return false;
            if (m_selectedItem.UserData == null)
                return false;
            if (m_selectedItem.Text == null)
                return false;
            return true;
        }

        void OnSearchTextChange(MyGuiControlTextbox box)
        {
            if (box.Text != "")
            {
                String[] tmpSearch = box.Text.Split(' ');
                foreach (var item in m_scriptList.Items)
                {
                    String tmpName = item.Text.ToString().ToLower();
                    bool add = true;
                    foreach (var search in tmpSearch)
                        if (!tmpName.Contains(search.ToLower()))
                        {
                            add = false;
                            break;
                        }
                    if (add)
                        item.Visible = true;
                    else
                        item.Visible = false;
                }
            }
            else
            {
                foreach (var item in m_scriptList.Items)
                {
                    item.Visible = true;
                }
            }
        }

#if !XB1 // XB1_NOWORKSHOP
        void OpenSharedScript(MyScriptItemInfo itemInfo)
        {
            m_scriptList.Enabled = false;
            m_task = Parallel.Start(DownloadScriptFromSteam, OnScriptDownloaded);
        }

        void DownloadScriptFromSteam()
        {
            if (m_selectedItem != null)
            {
                MyScriptItemInfo itemInfo = (m_selectedItem.UserData as MyScriptItemInfo);
                MySteamWorkshop.DownloadScriptBlocking(itemInfo.SteamItem);             
            }
        }

        void OnScriptDownloaded()
        {
            if (OnScriptOpened != null && m_selectedItem != null)
            {
                MyScriptItemInfo itemInfo = (m_selectedItem.UserData as MyScriptItemInfo);
                OnScriptOpened(Path.Combine(MyFileSystem.UserDataPath, SCRIPTS_DIRECTORY, "workshop", itemInfo.SteamItem.PublishedFileId.ToString() + WORKSHOP_SCRIPT_EXTENSION));
            }
            m_scriptList.Enabled = true;
        }
#endif // !XB1

        void OnItemDoubleClick(MyGuiControlListbox list)
        {
            m_selectedItem = list.SelectedItems[0];
            var itemInfo = m_selectedItem.UserData as MyBlueprintItemInfo;
            OpenSelectedSript();
        }

        void Ok()
        {
            if (m_selectedItem == null)
            {
                CloseScreen();
                return;
            }
            OpenSelectedSript();
        }

        private void OpenSelectedSript()
        {
            var itemInfo = m_selectedItem.UserData as MyScriptItemInfo;
#if !XB1 // XB1_NOWORKSHOP
            if (itemInfo.Type == MyBlueprintTypeEnum.STEAM)
            {
                OpenSharedScript(itemInfo);
            }
            else
#endif // !XB1
            {
                if (OnScriptOpened != null)
                {
                    OnScriptOpened(Path.Combine(MyFileSystem.UserDataPath, SCRIPTS_DIRECTORY, "local", itemInfo.ScriptName,DEFAULT_SCRIPT_NAME+ SCRIPT_EXTENSION));
                }
            }
            CloseScreen();
        }

        void OnOk(MyGuiControlButton button)
        {
            Ok();
        }

        void OnCancel(MyGuiControlButton button)
        {
            CloseScreen();
        }

        void OnReload(MyGuiControlButton button)
        {
            m_selectedItem = null;
            m_renameButton.Enabled = false;
            m_detailsButton.Enabled = false;

            RefreshAndReloadScriptsList(true);
        }

        void OnDetails(MyGuiControlButton button)
        {
            if (m_selectedItem == null)
            {
                if (m_activeDetail)
                {
                    MyScreenManager.RemoveScreen(m_detailScreen);
                }
                return;
            }
            else if (m_activeDetail)
            {
                MyScreenManager.RemoveScreen(m_detailScreen);
            }
            else if (!m_activeDetail)
            {
                if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.LOCAL)
                {
                    var path = Path.Combine(m_localScriptFolder, m_selectedItem.Text.ToString());
                    if (Directory.Exists(path))
                    {
                        m_detailScreen = new MyGuiDetailScreenScriptLocal(
                              callBack: delegate(MyScriptItemInfo item)
                              {
                                  if (item == null)
                                  {
                                      m_renameButton.Enabled = false;
                                      m_detailsButton.Enabled = false;
                                      m_deleteButton.Enabled = false;
                                  }
                                 m_activeDetail = false;
                                  if (m_task.IsComplete)
                                  {
                                      RefreshBlueprintList(m_detailScreen.WasPublished);
                                  }
                              },
                              selectedItem: (m_selectedItem.UserData  as MyScriptItemInfo),
                              parent: this,
                              textScale: m_textScale
                              );
                        m_activeDetail = true;
                        MyScreenManager.InputToNonFocusedScreens = true;
                        MyScreenManager.AddScreen(m_detailScreen);
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.OK,
                                    styleEnum: MyMessageBoxStyleEnum.Error,
                                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                    messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_ScriptNotFound)
                                    ));
                    }
                }
#if !XB1 // XB1_NOWORKSHOP
                else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.STEAM)
                {
                    m_detailScreen = new MyGuiDetailScreenScriptLocal(
                       callBack: delegate(MyScriptItemInfo item)
                        {
                            m_activeDetail = false;
                            if (m_task.IsComplete)
                            {
                                RefreshBlueprintList();
                            }
                        },
                        selectedItem: (m_selectedItem.UserData as MyScriptItemInfo),
                        parent: this,
                        textScale: m_textScale
                        );
                    m_activeDetail = true;
                    MyScreenManager.InputToNonFocusedScreens = true;
                    MyScreenManager.AddScreen(m_detailScreen);
                }
#endif // !XB1
            }
        }

        void OnRename(MyGuiControlButton button)
        {
            if (m_selectedItem == null)
            {
                return;
            }

           MyScreenManager.AddScreen( new MyGuiBlueprintTextDialog(
               position: m_position - new Vector2(SCREEN_SIZE.X,0.0f),
               caption: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_NewScriptName),
               defaultName: m_selectedItem.Text.ToString(),
               maxLenght: 50,
               callBack: delegate(string result)
               {
                   if (result != null)
                   {
                       ChangeName(result);
                   }
               },
               textBoxWidth: 0.3f
               ));
        }

        public void ChangeName(string newName)
        {
            newName = MyUtils.StripInvalidChars(newName);
            string oldName = m_selectedItem.Text.ToString();

            string file = Path.Combine(m_localScriptFolder, oldName);
            string newFile = Path.Combine(m_localScriptFolder, newName);

            if (file == newFile)
            {
                return;
            }

            if (Directory.Exists(file))
            {
                if (Directory.Exists(newFile))
                {
                    if (file.ToLower() == newFile.ToLower())
                    {
                        RenameScript(oldName, newName);
                        RefreshAndReloadScriptsList();
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat(MySpaceTexts.ProgrammableBlock_ReplaceScriptNameDialogText, newName);

                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.YES_NO,
                            styleEnum: MyMessageBoxStyleEnum.Info,
                            messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_ReplaceScriptNameDialogTitle),
                            messageText: sb,
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                            {
                                if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                                {
                                    RenameScript(oldName, newName);
                                    RefreshAndReloadScriptsList();
                                }
                                else
                                {
                                    return;
                                }
                            }));
                    }
                }
                else
                {                
                    try
                    {
                        RenameScript(oldName, newName);
                        RefreshAndReloadScriptsList();
                    }
                    catch (System.IO.IOException )
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            styleEnum: MyMessageBoxStyleEnum.Error,
                            messageCaption: MyTexts.Get(MyCommonTexts.LoadScreenButtonDelete),
                            messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_ReplaceScriptNameUsed))
                            );

                        return;
                    }
                }
            }
        }

        public void OnDelete(MyGuiControlButton button)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                styleEnum: MyMessageBoxStyleEnum.Info,
                messageCaption: MyTexts.Get(MyCommonTexts.LoadScreenButtonDelete),
                messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_DeleteScriptDialogText),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                {
                    if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        Debug.Assert(m_selectedItem != null, "Selected item shouldnt be null");
                        if (m_selectedItem != null)
                        {
                            if (DeleteScript(m_selectedItem.Text.ToString()))
                            {
                                m_renameButton.Enabled = false;
                                m_deleteButton.Enabled = false;
                                m_detailsButton.Enabled = false;
                                m_selectedItem = null;
                            }

                            RefreshBlueprintList();
                        }
                    }
                }, 
                canHideOthers: false
                ));
        }

        private void RenameScript(string oldName, string newName)
        {
            string oldFilePath = Path.Combine(m_localScriptFolder, oldName);
            if (Directory.Exists(oldFilePath))
            {
                string newFilePath = Path.Combine(m_localScriptFolder, newName);
                Directory.Move(oldFilePath, newFilePath);
            }
            DeleteScript(oldName);
        }

        private bool DeleteScript(string p)
        {
            string fileName = Path.Combine(m_localScriptFolder, p);
            if (Directory.Exists(fileName))
            {
                Directory.Delete(fileName,true);
                return true;
            }
            return false;
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            if (m_activeDetail)
            {
                m_detailScreen.CloseScreen();
            }
        }

        public override bool Update(bool hasFocus)
        {
            if (!m_task.IsComplete)
            {
                m_wheel.Visible = true;
            }
            if (m_task.IsComplete)
            {
                m_wheel.Visible = false;            
            }
            return base.Update(hasFocus);
        }

        public override bool CloseScreen()
        {
            if (OnClose != null)
            {
                OnClose();
            }
            return base.CloseScreen();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12) || MyInput.Static.IsNewKeyPressed(MyKeys.F11) || MyInput.Static.IsNewKeyPressed(MyKeys.F10))
            {
                this.CloseScreen();
            }
        }

        public void OnCreateFromEditor(MyGuiControlButton button)
        {
            if (GetCodeFromEditor != null)
            {
                if (!Directory.Exists(m_localScriptFolder))
                {
                    return;
                }
                int numTrys = 0;

                while (Directory.Exists(Path.Combine(m_localScriptFolder, DEFAULT_SCRIPT_NAME + "_" + numTrys.ToString())))
                {
                    numTrys++;
                }

                string newScriptPath = Path.Combine(m_localScriptFolder, DEFAULT_SCRIPT_NAME + "_" + numTrys);
                Directory.CreateDirectory(newScriptPath);
                var fsPath = Path.Combine(MyFileSystem.ContentPath, STEAM_THUMBNAIL_NAME);
                File.Copy(fsPath, Path.Combine(newScriptPath,THUMBNAIL_NAME),true);
                string code = GetCodeFromEditor();
                File.WriteAllText(Path.Combine(newScriptPath, DEFAULT_SCRIPT_NAME + SCRIPT_EXTENSION), code, Encoding.UTF8);
                RefreshAndReloadScriptsList(false);
            }
        }
        public void OnReplaceFromEditor(MyGuiControlButton button)
        {
            if (m_selectedItem == null)
            {
                return;
            }
            if (GetCodeFromEditor != null)
            {
                if (!Directory.Exists(m_localScriptFolder))
                {
                    return;
                }          
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                           buttonType: MyMessageBoxButtonsType.YES_NO,
                           styleEnum: MyMessageBoxStyleEnum.Info,
                           messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_ReplaceScriptNameDialogTitle),
                           messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_ReplaceScriptDialogText),
                           callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                           {
                               if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                               {
                                   MyScriptItemInfo info = m_selectedItem.UserData as MyScriptItemInfo;
                                   string filePath = Path.Combine(m_localScriptFolder, info.ScriptName, DEFAULT_SCRIPT_NAME + SCRIPT_EXTENSION);
                                   if (File.Exists(filePath))
                                   {
                                       string code = GetCodeFromEditor();
                                       File.WriteAllText(filePath, code, Encoding.UTF8);
                                   }
                               }
                               else
                               {
                                   return;
                               }
                           },
                           canHideOthers:false
                           ));         
            }
        }

#if !XB1 // XB1_NOWORKSHOP
        void OnOpenWorkshop(MyGuiControlButton button)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_INGAMESCRIPTS, "Steam Workshop");
        }
#endif // !XB1

        protected MyGuiControlButton CreateButton(float usableWidth, StringBuilder text, Action<MyGuiControlButton> onClick, bool enabled = true, MyStringId? tooltip = null, float textScale = 1f)
        {
            var button = AddButton(text, onClick);
            button.VisualStyle = MyGuiControlButtonStyleEnum.Rectangular;
            button.TextScale = textScale;
            button.Size = new Vector2(usableWidth, button.Size.Y);
            button.Position = button.Position + new Vector2(-0.04f / 2.0f, 0.0f);
            button.Enabled = enabled;
            if (tooltip != null)
            {
                button.SetToolTip(tooltip.Value);
            }
            return button;
        }

        protected MyGuiControlLabel MakeLabel(String text, Vector2 position, float textScale = 1.0f)
        {
            return new MyGuiControlLabel(text: text, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: position, textScale: textScale);
        }
    }
}
