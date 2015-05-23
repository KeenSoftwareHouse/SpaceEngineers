#region Using

using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Common.ObjectBuilders.Serializer;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using VRage;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Compression;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Engine.Utils;
#endregion


namespace Sandbox.Game.Gui
{
    public enum MyBlueprintTypeEnum
    {
        STEAM,
        LOCAL,
        SHARED,
        DEFAULT
    }

    public class MyBlueprintItemInfo
    { 
        public MyBlueprintTypeEnum Type;
        public ulong? PublishedItemId = null;

        public MyBlueprintItemInfo(MyBlueprintTypeEnum type, ulong? id = null)
        {
            Type = type;
            PublishedItemId = id;
        }
    }

    [PreloadRequired]
    public class MyGuiBlueprintScreen : MyGuiBlueprintScreenBase
    {
        public static Task Task;
        private static bool m_downloadFromSteam = true;
        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;
        private static List<MyGuiControlListbox.Item> m_recievedBlueprints = new List<MyGuiControlListbox.Item>();
        private static bool m_needsExtract = false;
        public static List<MySteamWorkshop.SubscribedItem> m_subscribedItemsList = new List<MySteamWorkshop.SubscribedItem>();

        private Vector2 m_controlPadding = new Vector2(0.02f, 0.02f);
        private float m_textScale = 0.8f;

        private MyGuiControlButton m_detailsButton;
        private MyGuiControlButton m_screenshotButton;
        private MyGuiControlButton m_replaceButton;
        private MyGuiControlButton m_deleteButton;

        private MyGuiControlTextbox m_searchBox;
        private MyGuiControlButton m_searchClear;
        private static MyGuiControlListbox m_blueprintList = new MyGuiControlListbox(visualStyle: MyGuiControlListboxStyleEnum.Blueprints);
        private MyGuiDetailScreenBase m_detailScreen = null;
        private MyGuiControlImageButton m_thumbnailImage;
        private MyGuiControlImageButton m_selectedImage;
        private bool m_activeDetail = false;
        private MyGuiControlListbox.Item m_selectedItem = null;
        private MyGuiControlRotatingWheel m_wheel;
        private MyGridClipboard m_clipboard;

        private string[] filenames;
       
        

        public static bool FirstTime
        {
            get { return m_downloadFromSteam; }
            set { m_downloadFromSteam = value; }
        }

        static MyGuiBlueprintScreen()
        {
            MySyncLayer.RegisterMessage<ShareBlueprintMsg>(ShareBlueprintRequest, MyMessagePermissions.Any);
        }

        static void ShareBlueprintRequest(ref ShareBlueprintMsg msg, MyNetworkClient sender)
        {
            
            var itemId = msg.WorkshopId;
            var name = msg.Name;
            var info = new MyBlueprintItemInfo(MyBlueprintTypeEnum.SHARED, id: itemId);
            var item = new MyGuiControlListbox.Item(new StringBuilder(name.ToString()), userData: info, icon: MyGuiConstants.TEXTURE_BLUEPRINTS_ARROW.Normal);
            item.ColorMask = new Vector4(0.7f);
            if (!m_recievedBlueprints.Any(item2 => (item2.UserData as MyBlueprintItemInfo).PublishedItemId == (item.UserData as MyBlueprintItemInfo).PublishedItemId))
            {
                m_recievedBlueprints.Add(item);
                m_blueprintList.Add(item);
                if (sender != null)
                {
                    var notification = new MyHudNotificationDebug(sender.DisplayName + " just shared a blueprint with you.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }
        }

        public override string GetFriendlyName()
        {
            return "MyBlueprintScreen";
        }

        public MyGuiBlueprintScreen(MyGridClipboard clipboard) :
            base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            if (!Directory.Exists(m_localBlueprintFolder))
            {
                Directory.CreateDirectory(m_localBlueprintFolder);
            }

            if (!Directory.Exists(m_workshopBlueprintFolder))
            {
                Directory.CreateDirectory(m_workshopBlueprintFolder);
            }
            m_blueprintList.Items.Clear();
            GetLocalBlueprintNames(m_downloadFromSteam);

            if (m_downloadFromSteam)
            {
                m_downloadFromSteam = false;
            }

            RecreateControls(true);
            
            m_blueprintList.ItemsSelected += OnSelectItem;
            m_blueprintList.ItemDoubleClicked += OnItemDoubleClick;
            m_blueprintList.ItemMouseOver += OnMouseOverItem;
            OnEnterCallback += Ok;
            m_searchBox.TextChanged += OnSearchTextChange;

            if (clipboard != null)
            {
                m_clipboard = clipboard;
            }
            else
            {
                System.Diagnostics.Debug.Fail("Clipboard shouldn't be null!");
                m_clipboard = Sandbox.Game.Entities.MyCubeBuilder.Static.Clipboard;
            }
        }

        void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(-0.075f, 0.28f);
            Vector2 buttonOffset = new Vector2(0.15f, 0.035f);
            float width = 0.15f;

            var okButton = CreateButton(width, new StringBuilder("Ok"), OnOk, textScale: m_textScale );
            okButton.Position = buttonPosition;

            var cancelButton = CreateButton(width, new StringBuilder("Cancel"), OnCancel, textScale: m_textScale);
            cancelButton.Position = buttonPosition + new Vector2(1f, 0f) * buttonOffset;

            m_detailsButton = CreateButton(width, new StringBuilder("Details"), OnDetails, textScale: m_textScale, enabled: false);
            m_detailsButton.Position = buttonPosition + new Vector2(0f, 1f) * buttonOffset;

            m_screenshotButton = CreateButton(width, new StringBuilder("Take Screenshot"), OnScreenshot, textScale: m_textScale, enabled: false);
            m_screenshotButton.Position = buttonPosition + new Vector2(1f, 1f) * buttonOffset;

            width = 0.3f;

            m_deleteButton = CreateButton(width, new StringBuilder("Delete"), OnDelete, false, textScale: m_textScale);
            m_deleteButton.Position = (buttonPosition + new Vector2(0f, 2f) * buttonOffset) * new Vector2(0f, 1f);

            var createButton = CreateButton(width, new StringBuilder("Create from clipboard"), OnCreate, MyCubeBuilder.Static != null ? MyCubeBuilder.Static.Clipboard.HasCopiedGrids() : false, textScale: m_textScale);
            createButton.Position = (buttonPosition + new Vector2(0f, 3f) * buttonOffset) * new Vector2(0f, 1f);

            m_replaceButton = CreateButton(width, new StringBuilder("Replace with clipboard"), OnReplace, MyCubeBuilder.Static != null ? MyCubeBuilder.Static.Clipboard.HasCopiedGrids() && m_selectedItem != null : false, textScale: m_textScale);
            m_replaceButton.Position = (buttonPosition + new Vector2(0f, 4f) * buttonOffset) * new Vector2(0f, 1f);
            
            var reloadButton = CreateButton(width, new StringBuilder("Refresh Blueprints"), OnReload, textScale: m_textScale);
            reloadButton.Position = (buttonPosition + new Vector2(0f, 5f) * buttonOffset)* new Vector2(0f, 1f);
        }

        public void RefreshThumbnail()
        {
            m_selectedImage = new MyGuiControlImageButton();
            m_selectedImage.BorderTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK;

            m_thumbnailImage = new MyGuiControlImageButton();
            m_thumbnailImage.Position = new Vector2(-0.31f, -0.2f);
            m_thumbnailImage.Size = new Vector2(0.2f, 0.175f);
            m_thumbnailImage.BorderTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK;
            m_thumbnailImage.Visible = false;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 searchPosition = new Vector2(0f, SCREEN_SIZE.Y - 1.58f);

            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f;

            var searchBoxLabel = MakeLabel("Search:", searchPosition + new Vector2(-0.175f, -0.015f), m_textScale);
            m_searchBox = new MyGuiControlTextbox(searchPosition);
            m_searchBox.Size = new Vector2(0.2f, 0.2f);

            m_searchClear = new MyGuiControlButton()
            {
                Position = searchPosition + new Vector2(0.077f, 0f),
                Size = new Vector2(0.045f, 0.05666667f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close
            };
            m_searchClear.ButtonClicked += OnSearchClear;

            m_blueprintList.Position = new Vector2(0f, -0.03f);
            m_blueprintList.VisibleRowsCount = 17;
            m_blueprintList.MultiSelect = false;

            var caption = AddCaption("Blueprints screen", VRageMath.Color.White.ToVector4(), m_controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            Controls.Add(searchBoxLabel);
            Controls.Add(m_searchBox);
            Controls.Add(m_searchClear);
            Controls.Add(m_blueprintList);

            RefreshThumbnail();
            Controls.Add(m_thumbnailImage);

            CreateButtons();

            var texture = MyGuiConstants.LOADING_TEXTURE;

            m_wheel = new MyGuiControlRotatingWheel(
                searchPosition + new Vector2(0.127f, 0f),
                MyGuiConstants.ROTATING_WHEEL_COLOR,
                0.28f,
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                texture,
                true,
                multipleSpinningWheels: MyPerGameSettings.GUI.MultipleSpinningWheels);
            Controls.Add(m_wheel);
            m_wheel.Visible = false;
        }

        void GetLocalBlueprintNames(bool reload = false)
        {
            GetBlueprints(m_localBlueprintFolder, MyBlueprintTypeEnum.LOCAL);

            if (Task.IsComplete)
            {
                if (reload)
                {
                    GetWorkshopBlueprints();
                }
                else
                {
                    GetWorkshopItemsLocal();
                }
            }

            foreach (var i in m_recievedBlueprints)
            {
                m_blueprintList.Add(i);
            }

            if (MyFakes.ENABLE_DEFAULT_BLUEPRINTS)
                GetBlueprints(m_defaultBlueprintFolder, MyBlueprintTypeEnum.DEFAULT);
        }

        void GetBlueprints(string directory, MyBlueprintTypeEnum type)
        {
            if (!Directory.Exists(directory))
                return;
            string[] folders = Directory.GetDirectories(directory);
            List<string> fileNames = new List<string>();
            List<string> blueprintNames = new List<string>();

            foreach (var f in folders)
            {
                fileNames.Add(f + "\\bp.sbc");
                var tokens = f.Split('\\');
                blueprintNames.Add(tokens[tokens.Length - 1]);
            }

            for (int i = 0; i < blueprintNames.Count; i++)
            {
                String name = blueprintNames[i];
                var info = new MyBlueprintItemInfo(type);
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(name), toolTip: name, userData: info, icon: MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL.Normal);
                m_blueprintList.Add(item);
            }
        }

        bool ValidateModInfo(MyObjectBuilder_ModInfo info)
        {
            if (info == null)
                return false;
            if (info.SubtypeName == null)
                return false;
            if (info.WorkshopId == null)
                return false;
            if (info.SteamIDOwner == null)
                return false;
            return true;
        }

        void GetWorkshopItemsLocal()
        {
            var filePath = Path.Combine(m_workshopBlueprintFolder, "temp");
            if (Directory.Exists(filePath))
            {
                var folders = Directory.GetDirectories(filePath);
                var blueprintNames = new List<string>();
                var fileNames = new List<string>();

                foreach (var f in folders)
                {
                    var tokens = f.Split('\\');
                    blueprintNames.Add(tokens[tokens.Length - 1]);
                }
                for (int i = 0; i < blueprintNames.Count; i++)
                {
                    var modInfoPath = Path.Combine(filePath, blueprintNames[i], "info.temp");
                    MyObjectBuilder_ModInfo modInfo = null;
                    if (File.Exists(modInfoPath))
                    {
                        var success = MyObjectBuilderSerializer.DeserializeXML(modInfoPath, out modInfo);
                        
                        if (!ValidateModInfo(modInfo) || !success)
                            continue;

                        String name = modInfo.SubtypeName;
                        var info = new MyBlueprintItemInfo(MyBlueprintTypeEnum.STEAM, modInfo.WorkshopId);
                        var item = new MyGuiControlListbox.Item(text: new StringBuilder(name), toolTip: name, userData: info, icon: MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.Normal);
                        m_blueprintList.Add(item);
                    }
                }
            }
        }

        void ExtractWorkShopItems()
        {
            ProfilerShort.Begin("Blueprint screen - Extracting bluepritns");

            if (!Directory.Exists(m_workshopBlueprintFolder))
            {
                Directory.CreateDirectory(m_workshopBlueprintFolder);
            }
            var downloadedMods = Directory.GetFiles(m_workshopBlueprintFolder);

            foreach (var mod in downloadedMods)
            {
                var fileName = Path.GetFileNameWithoutExtension(mod);
                var id = ulong.Parse(fileName);
                if(!m_subscribedItemsList.Any(item => item.PublishedFileId == id))
                {
                    File.Delete(mod); 
                }
            }

            var tempPath = Path.Combine(m_workshopBlueprintFolder, "temp");
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            var tempDir = Directory.CreateDirectory(tempPath);

            foreach (var subItem in m_subscribedItemsList)
            {
                if (downloadedMods.Any(item => item.Contains(subItem.PublishedFileId.ToString())))
                {
                    string archive = Array.Find(downloadedMods, item => item.Contains(subItem.PublishedFileId.ToString()));

                    var extractPath = Path.Combine(tempDir.FullName, subItem.PublishedFileId.ToString());

                    if (!File.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                        var extracted = MyZipArchive.OpenOnFile(archive);

                        var modInfo = new MyObjectBuilder_ModInfo();
                        modInfo.SubtypeName = subItem.Title;
                        modInfo.WorkshopId = subItem.PublishedFileId;
                        modInfo.SteamIDOwner = subItem.SteamIDOwner;


                        var infoFile = Path.Combine(m_workshopBlueprintFolder, "temp", subItem.PublishedFileId.ToString(), "info.temp");
                        if (File.Exists(infoFile))
                        {
                            File.Delete(infoFile);
                        }
                        var infoSuccess = MyObjectBuilderSerializer.SerializeXML(infoFile, false, modInfo);
  
                        if (extracted.FileExists("thumb.png"))
                        {
                            var stream = extracted.GetFile("thumb.png").GetStream();
                            if (stream != null)
                            {
                                using (var file = File.Create(Path.Combine(extractPath, "thumb.png")))
                                {
                                    stream.CopyTo(file);
                                }
                            }
                            stream.Close();
                        }
                        
                        extracted.Dispose();

                        var info = new MyBlueprintItemInfo(MyBlueprintTypeEnum.STEAM, subItem.PublishedFileId);
                        var listItem = new MyGuiControlListbox.Item(text: new StringBuilder(subItem.Title), toolTip: subItem.Title, icon: MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.Normal, userData: info);

                        var itemIndex = m_blueprintList.Items.FindIndex(item => ((item.UserData as MyBlueprintItemInfo).PublishedItemId == (listItem.UserData as MyBlueprintItemInfo).PublishedItemId) && (item.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.STEAM);
                        if (itemIndex == -1)
                        {
                            m_blueprintList.Add(listItem);
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        void DownloadBlueprints()
        {
            ProfilerShort.Begin("Getting workshop blueprints.");
            m_downloadFromSteam = true;
            ProfilerShort.BeginNextBlock("downloading");
            m_subscribedItemsList.Clear();
            bool success = MySteamWorkshop.GetSubscribedBlueprintsBlocking(m_subscribedItemsList);
            bool success2 = false;
            if (success)
            {
                if (Directory.Exists(m_workshopBlueprintFolder))
                {
                    try
                    {
                        Directory.Delete(m_workshopBlueprintFolder, true);
                    }
                    catch (System.IO.IOException)
                    {
                    }
                }
                Directory.CreateDirectory(m_workshopBlueprintFolder);
                success2 = MySteamWorkshop.DownloadBlueprintsBlocking(m_subscribedItemsList);
            }
            if (success && success2)
            {
                m_needsExtract = true;
                m_downloadFromSteam = false;
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: new StringBuilder("Couldn't load blueprints from steam workshop"),
                    messageCaption: new StringBuilder("Error")));
            }
            ProfilerShort.End();
        }

        void GetWorkshopBlueprints()
        {
            Task = Parallel.Start(DownloadBlueprints);
        }

        override public void RefreshBlueprintList(bool fromTask = false)
        {
            m_blueprintList.Items.Clear();
            GetLocalBlueprintNames(fromTask);
        }

        void ReloadTextures()
        { 
            //locals
            var path = m_localBlueprintFolder;
            var files = Directory.GetDirectories(path);
            var texture = "";
            foreach (var file in files)
            {
                texture = Path.Combine(file, "thumb.png");
                if (File.Exists(texture))
                {
                    VRageRender.MyRenderProxy.UnloadTexture(texture);
                }
            }

            //default
            path = m_defaultBlueprintFolder;
            if (Directory.Exists(path))
            {
                files = Directory.GetDirectories(path);
                foreach (var file in files)
                {
                    texture = Path.Combine(file, "thumb.png");
                    if (File.Exists(texture))
                    {
                        VRageRender.MyRenderProxy.UnloadTexture(texture);
                    }
                }
            }

            //ws
            path = Path.Combine(m_workshopBlueprintFolder, "temp");
            if (Directory.Exists(path))
            {
                files = Directory.GetDirectories(path);
                foreach (var file in files)
                {
                    texture = Path.Combine(file, "thumb.png");
                    if (File.Exists(texture))
                    {
                        VRageRender.MyRenderProxy.UnloadTexture(texture);
                    }
                }
            }
        }

        public void RefreshAndReloadBlueprintList()
        {
            m_blueprintList.Items.Clear();
            GetLocalBlueprintNames(true);
            ReloadTextures();
        }

        void OnSearchClear(MyGuiControlButton button)
        {
            m_searchBox.Text = "";
        }

        void OnMouseOverItem(MyGuiControlListbox listBox)
        {
            var item = listBox.MouseOverItem;
            var path = "";
            if (item != null)
            {
                if ((item.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.LOCAL)
                {
                    path = Path.Combine(m_localBlueprintFolder, item.Text.ToString(), "thumb.png");
                    
                }
                else if ((item.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.STEAM)
                {
                    var id = (item.UserData as MyBlueprintItemInfo).PublishedItemId;
                    if (id != null)
                    {
                        path = Path.Combine(m_workshopBlueprintFolder, "temp", id.ToString(), "thumb.png");
                    }
                }
                else if ((item.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.DEFAULT)
                {
                    path = Path.Combine(m_defaultBlueprintFolder, item.Text.ToString(), "thumb.png");
                }

                if (File.Exists(path))
                {
                    m_thumbnailImage.SetTexture(path);
                    if (!m_activeDetail)
                    {
                        if (m_thumbnailImage.BackgroundTexture != null)
                        {
                            m_thumbnailImage.Visible = true;
                        }
                    }
                }
                else
                {
                    m_thumbnailImage.Visible = false;
                    m_thumbnailImage.BackgroundTexture = null;
                }
            }
            else
            {
                m_thumbnailImage.Visible = false;
            }
        }

        void OnSelectItem(MyGuiControlListbox list)
        {
            if (list.SelectedItems.Count == 0)
            {
                return;
            }
            
            m_selectedItem = list.SelectedItems[0];
            m_detailsButton.Enabled = true;
            m_screenshotButton.Enabled = true;
            m_replaceButton.Enabled = m_clipboard.HasCopiedGrids();

            var type = (m_selectedItem.UserData as MyBlueprintItemInfo).Type;
            var id = (m_selectedItem.UserData as MyBlueprintItemInfo).PublishedItemId;
            var path = "";

            if (type == MyBlueprintTypeEnum.LOCAL)
            {
                path = Path.Combine(m_localBlueprintFolder, m_selectedItem.Text.ToString(), "thumb.png");
                m_deleteButton.Enabled = true;
            }
            else if (type == MyBlueprintTypeEnum.STEAM)
            {
                path = Path.Combine(m_workshopBlueprintFolder, "temp", id.ToString(), "thumb.png");
                m_screenshotButton.Enabled = false;
                m_replaceButton.Enabled = false;
                m_deleteButton.Enabled = false;
            }
            else if (type == MyBlueprintTypeEnum.SHARED)
            {
                m_replaceButton.Enabled = false;
                m_screenshotButton.Enabled = false;
                m_detailsButton.Enabled = false;
                m_deleteButton.Enabled = false;
            }
            else if (type == MyBlueprintTypeEnum.DEFAULT)
            {
                path = Path.Combine(m_defaultBlueprintFolder, m_selectedItem.Text.ToString(), "thumb.png");
                m_replaceButton.Enabled = false;
                m_screenshotButton.Enabled = false;
                m_deleteButton.Enabled = false;
            }

            if (File.Exists(path))
            {
                m_selectedImage.SetTexture(path);
            }

            else
            {
                m_selectedImage.BackgroundTexture = null;
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

        bool CopySelectedItemToClipboard()
        {
            if (!ValidateSelecteditem())
                return false;

            var path = "";
            MyObjectBuilder_Definitions prefab = null;
            
            if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.LOCAL)
            {
                path = Path.Combine(m_localBlueprintFolder, m_selectedItem.Text.ToString(), "bp.sbc");
                if (File.Exists(path))
                {
                    prefab = LoadPrefab(path);
                }
            }
            else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.STEAM)
            {
                var id = (m_selectedItem.UserData as MyBlueprintItemInfo).PublishedItemId;
                path = Path.Combine(m_workshopBlueprintFolder, id.ToString() + m_workshopBlueprintSuffix);
                if (File.Exists(path))
                {
                    prefab = LoadWorkshopPrefab(path, id);
                }

            }
            else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.SHARED)
            {
                return false;
            }
            else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.DEFAULT)
            {
                path = Path.Combine(m_defaultBlueprintFolder, m_selectedItem.Text.ToString(), "bp.sbc");
                if (File.Exists(path))
                {
                    prefab = LoadPrefab(path);
                }
            }

            if (prefab != null)
            {
                return CopyBlueprintPrefabToClipboard(prefab, m_clipboard);
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            styleEnum: MyMessageBoxStyleEnum.Error,
                            messageCaption: new StringBuilder("Error"),
                            messageText: new StringBuilder("Failed to load the blueprint file.")
                            ));
                return false;
            }
        }

        public static bool CopyBlueprintPrefabToClipboard(MyObjectBuilder_Definitions prefab, MyGridClipboard clipboard, bool setOwner = true)
        {
            if (prefab.ShipBlueprints == null)
                return false;

            var cubeGrids = prefab.ShipBlueprints[0].CubeGrids;

            if (cubeGrids == null || cubeGrids.Count() == 0)
                return false;

            var localBB = MyCubeGridExtensions.CalculateBoundingSphere(cubeGrids[0]);

            var posAndOrient = cubeGrids[0].PositionAndOrientation.Value;
            var worldMatrix = MatrixD.CreateWorld(posAndOrient.Position, posAndOrient.Forward, posAndOrient.Up);
            var invertedNormalizedWorld = Matrix.Normalize(Matrix.Invert(worldMatrix));

            var worldBB = localBB.Transform(worldMatrix);

            var dragVector = Vector3.TransformNormal((Vector3)(Vector3D)posAndOrient.Position - worldBB.Center, invertedNormalizedWorld);
            var dragDistance = localBB.Radius + 10f;

            //Reset ownership to local player
            if (setOwner)
            {
                foreach (var gridBuilder in cubeGrids)
                {
                    foreach (var block in gridBuilder.CubeBlocks)
                    {
                        if (block.Owner != 0)
                        {
                            block.Owner = MySession.LocalPlayerId;
                        }
                    }
                }
            }

            clipboard.SetGridFromBuilders(cubeGrids, dragVector, dragDistance);
            clipboard.Deactivate();
            return true;
        }

        void OnSearchTextChange(MyGuiControlTextbox box)
        {
            if (box.Text != "")
            {
                String[] tmpSearch = box.Text.Split(' ');
                foreach (var item in m_blueprintList.Items)
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
                foreach (var item in m_blueprintList.Items)
                    item.Visible = true;
            }
        }

        void OpenSharedBlueprint(MyBlueprintItemInfo itemInfo)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
            buttonType: MyMessageBoxButtonsType.YES_NO_CANCEL,
            styleEnum: MyMessageBoxStyleEnum.Info,
            messageCaption: new StringBuilder("Shared Blueprint"),
            messageText: new StringBuilder("Do you want to open this blueprint in steam workshop?"),
            callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
            {
                if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                {
                    MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", itemInfo.PublishedItemId));
                    m_recievedBlueprints.Remove(m_selectedItem);
                    m_selectedItem = null;
                    RefreshBlueprintList();
                }
                else if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.NO)
                {
                    m_recievedBlueprints.Remove(m_selectedItem);
                    m_selectedItem = null;
                    RefreshBlueprintList();
                }
            }));
            
        }

        void OnItemDoubleClick(MyGuiControlListbox list)
        {
            m_selectedItem = list.SelectedItems[0];
            var itemInfo = m_selectedItem.UserData as MyBlueprintItemInfo;

            if (itemInfo.Type == MyBlueprintTypeEnum.SHARED)
            {
                OpenSharedBlueprint(itemInfo);
            }
            else
            {
                if (MySession.Static.SurvivalMode && m_clipboard == Sandbox.Game.Entities.MyCubeBuilder.Static.Clipboard)
                {
                    CloseScreen();
                }
                else
                {
                    var close = CopySelectedItemToClipboard();
                    if (close)
                    {
                        CloseScreen();
                    }
                }
            }
        }

        void Ok()
        {
            if (m_selectedItem == null)
            {
                CloseScreen();
                return;
            }
            var itemInfo = m_selectedItem.UserData as MyBlueprintItemInfo;
            if (itemInfo.Type == MyBlueprintTypeEnum.SHARED)
            {
                OpenSharedBlueprint(itemInfo);
                return;
            }

            else
            {
                if (MySession.Static.SurvivalMode && m_clipboard == Sandbox.Game.Entities.MyCubeBuilder.Static.Clipboard)
                {
                    CloseScreen();
                }
                else
                {
                    var close = CopySelectedItemToClipboard();
                    if (close)
                    {
                        CloseScreen();
                    }
                }
            }
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
            m_detailsButton.Enabled = false;
            m_screenshotButton.Enabled = false;

            RefreshAndReloadBlueprintList();
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
            else if(m_activeDetail)
            {
                MyScreenManager.RemoveScreen(m_detailScreen);
            }
            else if (!m_activeDetail)
            {
                if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.LOCAL)
                {
                    var path = Path.Combine(m_localBlueprintFolder, m_selectedItem.Text.ToString(), "bp.sbc");
                    if (File.Exists(path))
                    {
                        m_thumbnailImage.Visible = false;
                        m_detailScreen = new MyGuiDetailScreenLocal(
                            callBack: delegate(MyGuiControlListbox.Item item)
                            {
                                if (item == null)
                                {
                                    m_screenshotButton.Enabled = false;
                                    m_detailsButton.Enabled = false;
                                    m_replaceButton.Enabled = false;
                                    m_deleteButton.Enabled = false;
                                }
                                m_selectedItem = item;
                                m_activeDetail = false;
                                m_detailScreen = null;
                                if (Task.IsComplete)
                                {
                                    RefreshBlueprintList();
                                }
                            },
                            selectedItem: m_selectedItem,
                            parent: this,
                            thumbnailTexture: m_selectedImage.BackgroundTexture,
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
                                    messageCaption: new StringBuilder("Error"),
                                    messageText: new StringBuilder("Cannot find the blueprint file.")
                                    ));
                    }
                }
                else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.DEFAULT)
                {
                    var path = Path.Combine(m_defaultBlueprintFolder, m_selectedItem.Text.ToString(), "bp.sbc");
                    if (File.Exists(path))
                    {
                        m_thumbnailImage.Visible = false;
                        m_detailScreen = new MyGuiDetailScreenDefault(
                            callBack: delegate(MyGuiControlListbox.Item item)
                            {
                                if (item == null)
                                {
                                    m_screenshotButton.Enabled = false;
                                    m_detailsButton.Enabled = false;
                                    m_replaceButton.Enabled = false;
                                    m_deleteButton.Enabled = false;
                                }
                                m_selectedItem = item;
                                m_activeDetail = false;
                                m_detailScreen = null;
                                if (Task.IsComplete)
                                {
                                    RefreshBlueprintList();
                                }
                            },
                            selectedItem: m_selectedItem,
                            parent: this,
                            thumbnailTexture: m_selectedImage.BackgroundTexture,
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
                                    messageCaption: new StringBuilder("Error"),
                                    messageText: new StringBuilder("Cannot find the blueprint file.")
                                    ));
                    }
                }
                else if ((m_selectedItem.UserData as MyBlueprintItemInfo).Type == MyBlueprintTypeEnum.STEAM)
                {
                    var path2 = Path.Combine(m_workshopBlueprintFolder, (m_selectedItem.UserData as MyBlueprintItemInfo).PublishedItemId.ToString() + m_workshopBlueprintSuffix);
                    if (File.Exists(path2))
                    {
                        m_thumbnailImage.Visible = false;
                        m_detailScreen = new MyGuiDetailScreenSteam(
                            callBack: delegate(MyGuiControlListbox.Item item)
                            {
                                m_selectedItem = item;
                                m_activeDetail = false;
                                m_detailScreen = null;
                                if (Task.IsComplete)
                                {
                                    RefreshBlueprintList();
                                }
                            },
                            selectedItem: m_selectedItem,
                            parent: this,
                            thumbnailTexture: m_selectedImage.BackgroundTexture,
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
                                    messageCaption: new StringBuilder("Error"),
                                    messageText: new StringBuilder("Cannot find the blueprint file.")
                                    ));
                    }
                }
            }
        }

        public void TakeScreenshot(string name)
        {
            string path = Path.Combine(m_localBlueprintFolder, name, "thumb.png");
            
            VRageRender.MyRenderProxy.TakeScreenshot(new Vector2(0.5f, 0.5f), path, false, true, false);
            VRageRender.MyRenderProxy.UnloadTexture(path);
            m_thumbnailImage.Visible = true;
        }

        void OnScreenshot(MyGuiControlButton button)
        {
            if (m_selectedItem == null)
            {
                return;
            }
            TakeScreenshot(m_selectedItem.Text.ToString());
        }

        public void CreateFromClipboard(bool withScreenshot = false, bool replace = false)
        {
            if (MyCubeBuilder.Static.Clipboard.CopiedGridsName == null)
            {
                return;
            }
            string name = MyUtils.StripInvalidChars(MyCubeBuilder.Static.Clipboard.CopiedGridsName);
            string newName = name;
            string path = Path.Combine(m_localBlueprintFolder, name);
            int index = 1;

            while (MyFileSystem.DirectoryExists(path))
            {
                newName = name + "_" + index;
                path = Path.Combine(m_localBlueprintFolder, newName);
                index++;
            }
            string imagePath = Path.Combine(path, "thumb.png");

            if (withScreenshot)
            {
                TakeScreenshot(newName);
            }

            var prefab = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
            prefab.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), MyUtils.StripInvalidChars(name));
            prefab.CubeGrids = MyCubeBuilder.Static.Clipboard.CopiedGrids.ToArray();
            prefab.RespawnShip = false;
            prefab.DisplayName = MySteam.UserName;
            prefab.OwnerSteamId = MySteam.UserId;
            if (MyFakes.ENABLE_BATTLE_SYSTEM)
                prefab.BattlePoints = MyBattleHelper.GetBattlePoints(prefab.CubeGrids);
            prefab.CubeGrids[0].DisplayName = name;

            var definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            definitions.ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1];
            definitions.ShipBlueprints[0] = prefab;

            SavePrefabToFile(definitions, replace: replace);
            RefreshBlueprintList();
        }

        void OnDelete(MyGuiControlButton button)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                styleEnum: MyMessageBoxStyleEnum.Info,
                messageCaption: new StringBuilder("Delete"),
                messageText: new StringBuilder("Are you sure you want to delete this blueprint?"),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                {
                    if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        Debug.Assert(m_selectedItem != null, "Selected item shouldnt be null");
                        if (m_selectedItem != null)
                        {
                            if (DeleteBlueprint(m_selectedItem.Text.ToString()))
                            {
                                m_deleteButton.Enabled = false;
                                m_detailsButton.Enabled = false;
                                m_screenshotButton.Enabled = false;
                                m_replaceButton.Enabled = false;
                                m_selectedItem = null;
                            }

                            RefreshBlueprintList();
                        }
                    }
                }));
        }


        void OnCreate(MyGuiControlButton button)
        {
            CreateFromClipboard();
        }

        void OnReplace(MyGuiControlButton button)
        {
            if(m_selectedItem == null)
            {
                return;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                styleEnum: MyMessageBoxStyleEnum.Info,
                messageCaption: MyTexts.Get(MySpaceTexts.BlueprintsMessageBoxTitle_Replace),
                messageText: MyTexts.Get(MySpaceTexts.BlueprintsMessageBoxDesc_Replace),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                {
                    if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        string name = m_selectedItem.Text.ToString();
                        var path = Path.Combine(m_localBlueprintFolder, name, "bp.sbc");
                        if (File.Exists(path))
                        {
                            var oldBlueprint = LoadPrefab(path);
                            MyCubeBuilder.Static.Clipboard.CopiedGrids[0].DisplayName = name;
                            oldBlueprint.ShipBlueprints[0].CubeGrids = MyCubeBuilder.Static.Clipboard.CopiedGrids.ToArray();

                            if (MyFakes.ENABLE_BATTLE_SYSTEM)
                                oldBlueprint.ShipBlueprints[0].BattlePoints = MyBattleHelper.GetBattlePoints(oldBlueprint.ShipBlueprints[0].CubeGrids);

                            SavePrefabToFile(oldBlueprint, replace: true);
                            RefreshBlueprintList();
                        }
                    }
                }));
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
            if (!m_blueprintList.IsMouseOver)
            {
                m_thumbnailImage.Visible = false;
            }

            if(!Task.IsComplete)
            {
                m_wheel.Visible = true;
            }
            if (Task.IsComplete)
            {
                m_wheel.Visible = false;
                if (m_needsExtract)
                {
                    ExtractWorkShopItems();
                    m_needsExtract = false;
                    RefreshBlueprintList();
                }
            }
            return base.Update(hasFocus);
        }
       
        public override bool CloseScreen()
        {
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

    }
}
