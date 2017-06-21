#region Using
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using VRage;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GUI;
using System.Drawing;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using SteamSDK;
using Sandbox.Game.Multiplayer;
using System.Diagnostics;
using VRage;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
#endregion

namespace Sandbox.Game.Gui
{
    abstract class MyGuiDetailScreenBase : MyGuiBlueprintScreenBase
    {
        protected static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        protected float m_textScale;
        protected string m_blueprintName;
        protected MyGuiControlListbox.Item m_selectedItem;
        protected MyObjectBuilder_Definitions m_loadedPrefab;
        protected MyGuiControlMultilineText m_textField;
        protected MyGuiControlMultilineText m_descriptionField;
        protected MyGuiControlImage m_thumbnailImage;
        protected Action<MyGuiControlListbox.Item> callBack;
        protected MyGuiBlueprintScreenBase m_parent;
        protected MyGuiBlueprintTextDialog m_dialog;
        protected bool m_killScreen = false;
        protected Vector2 m_offset = new Vector2(-0.01f, 0f);
        protected int maxNameLenght = 40;

        public MyGuiDetailScreenBase(bool isTopMostScreen, MyGuiBlueprintScreenBase parent, string thumbnailTexture, MyGuiControlListbox.Item selectedItem, float textScale)
            : base(new Vector2(0.37f, 0.325f), new Vector2(0.725f, 0.4f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, isTopMostScreen)
        {
            m_thumbnailImage = new MyGuiControlImage()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };
            m_thumbnailImage.SetPadding(new MyGuiBorderThickness(3f, 2f, 3f, 2f));
            m_thumbnailImage.SetTexture(thumbnailTexture);
            
            m_selectedItem = selectedItem;
            m_blueprintName = selectedItem.Text.ToString();
            m_textScale = textScale;
            m_parent = parent;
        }

        protected int GetNumberOfBlocks()
        {
            int result = 0;
            foreach (var g in m_loadedPrefab.ShipBlueprints[0].CubeGrids)
            {
                result += g.CubeBlocks.Count;
            }
            return result;
        }

        protected int GetNumberOfBattlePoints() 
        {
            return (int)m_loadedPrefab.ShipBlueprints[0].Points;
        }

        protected void RefreshTextField()
        {
            if (m_textField == null)
            {
                return;
            }
            var displayName = m_blueprintName;
            if (displayName.Length > 25)
            {
                displayName = displayName.Substring(0, 25) + "...";
            }
            m_textField.Clear();
            m_textField.AppendText("Name: " + displayName);//zxc translate
            m_textField.AppendLine();

            MyCubeSize type = m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].GridSizeEnum;

            m_textField.AppendText(MyTexts.GetString(MyCommonTexts.BlockPropertiesText_Type));
            if (m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].IsStatic && type == MyCubeSize.Large)
            {
                m_textField.AppendText(MyTexts.GetString(MyCommonTexts.DetailStaticGrid));
            }
            else
            {
                if ( type == MyCubeSize.Small )
                    m_textField.AppendText(MyTexts.GetString(MyCommonTexts.DetailSmallGrid));
                else
                    m_textField.AppendText(MyTexts.GetString(MyCommonTexts.DetailLargeGrid));
            }

            m_textField.AppendLine();
            m_textField.AppendText("Number of blocks: " + GetNumberOfBlocks());//zxc translate
            m_textField.AppendLine();
            m_textField.AppendText("Author: " + m_loadedPrefab.ShipBlueprints[0].DisplayName);//zxc translate
            m_textField.AppendLine();
        }


        protected void CreateTextField()
        {
            var textPosition = new Vector2(-0.325f, -0.175f) + m_offset;
            var textSize = new Vector2(0.175f, 0.175f);
            var padding = new Vector2(0.005f, 0f);

            var textBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, textPosition, textSize, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            m_textField = new MyGuiControlMultilineText();
            m_textField = AddMultilineText(offset: textPosition + padding, textScale: m_textScale, size: textSize - padding);

            RefreshTextField();
        }


        protected void CreateDescription()
        {
            var descPosition = new Vector2(-0.325f, 0.015f) + m_offset;
            var descSize = new Vector2(0.67f, 0.15f);
            var padding = new Vector2(0.005f, 0f);

            var descriptionBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, descPosition, descSize, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            m_descriptionField = AddMultilineText(offset: descPosition + padding, textScale: m_textScale, size: descSize - (padding + new Vector2(0f, 0.005f)), selectable: false);

            RefreshDescriptionField();
        }

        protected void RefreshDescriptionField()
        {
            if (m_descriptionField == null)
            {
                return;
            }
            m_descriptionField.Clear();
            var description = m_loadedPrefab.ShipBlueprints[0].Description;
            if (description != null)
            {
                m_descriptionField.AppendText(description);
            }
        }

        public override void RecreateControls(bool constructor)
        {
            if (m_loadedPrefab == null)
            {
                CloseScreen();
            }
            else
            {
                base.RecreateControls(constructor);
                CreateTextField();
                CreateDescription();
                CreateButtons();
                m_thumbnailImage.Position = new Vector2(-0.03f, -0.088f) + m_offset;
                m_thumbnailImage.Size = new Vector2(0.2f, 0.175f);
                Controls.Add(m_thumbnailImage);
            }
        }

        protected void CallResultCallback(MyGuiControlListbox.Item val)
        {
            callBack(val);
        }

        protected override void Canceling()
        {
            CallResultCallback(m_selectedItem);
            base.Canceling();
        }

        protected abstract void CreateButtons();

        protected void OnCloseButton(MyGuiControlButton button)
        {
            CloseScreen();
            CallResultCallback(m_selectedItem);
        }

        public override bool Update(bool hasFocus)
        {
            if (m_killScreen)
            {
                CallResultCallback(null);
                CloseScreen();
            }
            return base.Update(hasFocus);
        }
    }

#if !XB1 // XB1_NOWORKSHOP
    class MyGuiDetailScreenSteam : MyGuiDetailScreenBase
    {
        private ulong? m_publishedItemId;
        private MyGuiControlCombobox m_sendToCombo;

        public MyGuiDetailScreenSteam(Action<MyGuiControlListbox.Item> callBack, MyGuiControlListbox.Item selectedItem, MyGuiBlueprintScreen parent , string thumbnailTexture, float textScale) :
            base(false, parent, thumbnailTexture, selectedItem, textScale)
        {
            this.callBack = callBack;

            m_publishedItemId = (selectedItem.UserData as MyBlueprintItemInfo).PublishedItemId;

            var prefabPath = Path.Combine(m_workshopBlueprintFolder, m_publishedItemId.ToString() + m_workshopBlueprintSuffix);
            
            if (File.Exists(prefabPath))
            {
                m_loadedPrefab = LoadWorkshopPrefab(prefabPath, m_publishedItemId);

                Debug.Assert(m_loadedPrefab != null);
                if (m_loadedPrefab == null)
                {
                    m_killScreen = true;
                }
                else
                {
                    var name = m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].DisplayName;
                    if (name.Length > 40)
                    {
                        var newName = name.Substring(0, 40);
                        m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].DisplayName = newName;
                    }
                    RecreateControls(true);
                }
            }
            else
            {
                m_killScreen = true;
            }
        }
        
        public override string GetFriendlyName()
        {
            return "MyDetailScreen";
        }

        protected override void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(0.215f, -0.173f) + m_offset;
            Vector2 buttonOffset = new Vector2(0.13f, 0.045f);

            float width = 0.26f;

            var openInWorkshopButton = CreateButton(width, new StringBuilder("Open In Workshop"), OnOpenInWorkshop, textScale: m_textScale);
            openInWorkshopButton.Position = buttonPosition;

            width = 0.14f;

            var closeButton = CreateButton(width, new StringBuilder("Close"), OnCloseButton, textScale: m_textScale);
            closeButton.Position = buttonPosition + new Vector2(0.5f, 2f) * buttonOffset + new Vector2(-0.005f, 0.005f);

            var sendLabel = MakeLabel("Send to Player", buttonPosition + new Vector2(-1f, 1f) * buttonOffset, m_textScale);
            Controls.Add(sendLabel);

            m_sendToCombo = AddCombo(size: new Vector2(0.14f, 0.1f));
            m_sendToCombo.Position = buttonPosition + new Vector2(-0.075f, 1f) * buttonOffset;

            foreach (var player in Sync.Clients.GetClients())
            {
                m_sendToCombo.AddItem(Convert.ToInt64(player.SteamUserId), new StringBuilder(player.DisplayName));
                if (player.SteamUserId != Sync.MyId)
                {
                    m_sendToCombo.AddItem(Convert.ToInt64(player.SteamUserId), new StringBuilder(player.DisplayName));
                }
            }
            m_sendToCombo.ItemSelected += OnSendToPlayer;
        }

        void OnSendToPlayer()
        {
            var playerId = (ulong)m_sendToCombo.GetSelectedKey();
            MyMultiplayer.RaiseStaticEvent(x => MyGuiBlueprintScreen.ShareBlueprintRequest, (ulong)m_publishedItemId, m_blueprintName, playerId, MySession.Static.LocalHumanPlayer.DisplayName);
        }

        void OnOpenInWorkshop(MyGuiControlButton button) 
        {
            if (m_publishedItemId != null)
            {
                string url = string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", m_publishedItemId);
                MyGuiSandbox.OpenUrlWithFallback(url, "Steam Workshop");
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    styleEnum: MyMessageBoxStyleEnum.Error,
                    messageCaption: new StringBuilder("Invalid workshop id"),
                    messageText: new StringBuilder("")
                    ));
            }
        }
    }
#endif // !XB1

    class MyGuiDetailScreenDefault : MyGuiDetailScreenBase
    {
        public MyGuiDetailScreenDefault(Action<MyGuiControlListbox.Item> callBack, MyGuiControlListbox.Item selectedItem, MyGuiBlueprintScreen parent, string thumbnailTexture, float textScale) :
            base(false, parent, thumbnailTexture, selectedItem, textScale)
        {
            var prefabPath = Path.Combine(m_defaultBlueprintFolder, m_blueprintName, "bp.sbc");
            this.callBack = callBack;

            if (File.Exists(prefabPath))
            {
                m_loadedPrefab = LoadPrefab(prefabPath);

                if (m_loadedPrefab == null)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.OK,
                        styleEnum: MyMessageBoxStyleEnum.Error,
                        messageCaption: new StringBuilder("Error"),
                        messageText: new StringBuilder("Failed to load the blueprint file.")
                        ));
                    m_killScreen = true;
                }
                else
                {
                    RecreateControls(true);
                }
            }
            else
            {
                m_killScreen = true;
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiDetailScreenDefault";
        }

        protected override void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(0.215f, -0.173f) + m_offset;
            Vector2 buttonOffset = new Vector2(0.13f, 0.0f);

            var closeButton = CreateButton(0.14f, new StringBuilder("Close"), OnCloseButton, textScale: m_textScale);
            closeButton.Position = buttonPosition + new Vector2(0.5f, 0f) * buttonOffset + new Vector2(-0.005f, 0.005f);
        }
    }

    class MyGuiDetailScreenLocal : MyGuiDetailScreenBase
    {
        public MyGuiDetailScreenLocal(Action<MyGuiControlListbox.Item> callBack, MyGuiControlListbox.Item selectedItem, MyGuiBlueprintScreenBase parent , string thumbnailTexture, float textScale) :
            base(false, parent, thumbnailTexture, selectedItem, textScale)
        {
            var prefabPath = Path.Combine(m_localBlueprintFolder, m_blueprintName, "bp.sbc");
            this.callBack = callBack;

            if (File.Exists(prefabPath))
            {
                m_loadedPrefab = LoadPrefab(prefabPath);

                if (m_loadedPrefab == null)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.OK,
                        styleEnum: MyMessageBoxStyleEnum.Error,
                        messageCaption: new StringBuilder("Error"),
                        messageText: new StringBuilder("Failed to load the blueprint file.")
                        ));
                    m_killScreen = true;
                }
                else
                {
                    RecreateControls(true);
                }
            }
            else 
            {
                m_killScreen = true;
            }
        }

        protected override void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(0.15f, -0.173f) + m_offset;
            Vector2 buttonOffset = new Vector2(0.13f, 0.045f);

            float width = 0.13f;

            var renameButton = CreateButton(width, new StringBuilder("Rename"), OnRename, textScale: m_textScale);
            renameButton.Position = buttonPosition;

            if (!MyFakes.XB1_PREVIEW)
            {
                var publishButton = CreateButton(width, new StringBuilder("Publish"), OnPublish, textScale: m_textScale);
                publishButton.Position = buttonPosition + new Vector2(1f, 0f) * buttonOffset;
            }

            var deleteButton = CreateButton(width, new StringBuilder("Delete"), OnDelete, textScale: m_textScale);
            deleteButton.Position = buttonPosition + new Vector2(0f, 1f) * buttonOffset;

            if (!MyFakes.XB1_PREVIEW)
            {
                var openWorkshopButton = CreateButton(width, new StringBuilder("Open WorkShop"), OnOpenWorkshop, textScale: m_textScale);
                openWorkshopButton.Position = buttonPosition + new Vector2(1f, 1f) * buttonOffset;
            }

            var closeButton = CreateButton(width, new StringBuilder("Close"), OnCloseButton, textScale: m_textScale);
            closeButton.Position = buttonPosition + new Vector2(1f, 2f) * buttonOffset;
        }

        public override string GetFriendlyName()
        {
            return "MyDetailScreen";
        }

        void ChangeDescription(string newDescription) 
        {
            string file = Path.Combine(m_localBlueprintFolder, m_blueprintName);

            if (Directory.Exists(file))
            {
                m_loadedPrefab.ShipBlueprints[0].Description = newDescription;
                SavePrefabToFile(m_loadedPrefab, m_blueprintName, true);
                RefreshDescriptionField();
            }
        }

        void OnEditDescription(MyGuiControlButton button)
        {
            m_dialog = new MyGuiBlueprintTextDialog(
                position: m_position,
                caption: "Enter new description", 
                defaultName: m_loadedPrefab.ShipBlueprints[0].Description,
                maxLenght: 8000,
                callBack: delegate(string result)
            {
                if (result != null)
                {
                    ChangeDescription(result);
                }
            }
                );
            MyScreenManager.AddScreen(m_dialog);
        }

        void OnDeleteDescription(MyGuiControlButton button)
        {

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                styleEnum: MyMessageBoxStyleEnum.Info,
                messageCaption: new StringBuilder("Delete description"),
                messageText: new StringBuilder("Are you sure you want to delete the description of this blueprint?"),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                {
                    if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        ChangeDescription("");
                    }
                }));
        }

        void ChangeName(string name)
        {
            name = MyUtils.StripInvalidChars(name);
            var deleteName = m_blueprintName;
            string file = Path.Combine(m_localBlueprintFolder, deleteName);
            string newFile = Path.Combine(m_localBlueprintFolder, name);

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
                        m_loadedPrefab.ShipBlueprints[0].Id.SubtypeId = name;
                        m_loadedPrefab.ShipBlueprints[0].Id.SubtypeName = name;
                        m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].DisplayName = name;
                        var tempDir = Path.Combine(m_localBlueprintFolder, "temp");
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                        Directory.Move(file, tempDir);
                        Directory.Move(tempDir, newFile);
                        m_thumbnailImage.SetTexture(Path.Combine(newFile, "thumb.png"));
                        SavePrefabToFile(m_loadedPrefab, name, true);
                        m_blueprintName = name;
                        RefreshTextField();
                        m_parent.RefreshBlueprintList();

                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.YES_NO,
                            styleEnum: MyMessageBoxStyleEnum.Info,
                            messageCaption: new StringBuilder("Replace"),
                            messageText: new StringBuilder("Blueprint with the name \"" + name + "\" already exists. Do you want to replace it?"),
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                            {
                                if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                                {
                                    DeleteBlueprint(name);
                                    m_loadedPrefab.ShipBlueprints[0].Id.SubtypeId = name;
                                    m_loadedPrefab.ShipBlueprints[0].Id.SubtypeName = name;
                                    m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].DisplayName = name;
                                    Directory.Move(file, newFile);
                                    VRageRender.MyRenderProxy.UnloadTexture(Path.Combine(newFile, "thumb.png"));
                                    m_thumbnailImage.SetTexture(Path.Combine(newFile, "thumb.png"));
                                    SavePrefabToFile(m_loadedPrefab, name, true);
                                    m_blueprintName = name;
                                    RefreshTextField();
                                    m_parent.RefreshBlueprintList();
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
                    m_loadedPrefab.ShipBlueprints[0].Id.SubtypeId = name;
                    m_loadedPrefab.ShipBlueprints[0].Id.SubtypeName = name;
                    m_loadedPrefab.ShipBlueprints[0].CubeGrids[0].DisplayName = name;
                    try
                    {
                        Directory.Move(file, newFile);
                    }
                    catch (System.IO.IOException ex)
                    {

                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            styleEnum: MyMessageBoxStyleEnum.Error,
                            messageCaption: new StringBuilder("Delete"),
                            messageText: new StringBuilder("Cannot rename blueprint because it is used by another process."))
                            );
                    
                        return;
                    }

                    VRageRender.MyRenderProxy.UnloadTexture(Path.Combine(newFile, "thumb.png"));
                    m_thumbnailImage.SetTexture(Path.Combine(newFile, "thumb.png"));
                    SavePrefabToFile(m_loadedPrefab, name, true);
                    m_blueprintName = name;
                    RefreshTextField();
                    m_parent.RefreshBlueprintList();
                }
            }
        }

        void OnRename(MyGuiControlButton button)
        {
            m_dialog = new MyGuiBlueprintTextDialog(
                position: m_position,
                caption: "Enter new name",
                defaultName: m_blueprintName,
                maxLenght: maxNameLenght,
                callBack: delegate(string result)
                {
                    if (result != null)
                    {
                        ChangeName(result);
                    }
                },
                textBoxWidth: 0.3f
                );

            MyScreenManager.AddScreen(m_dialog);
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
                        DeleteBlueprint(m_blueprintName);
                        //m_parent.RefreshBlueprintList();
                        CallResultCallback(null);
                        CloseScreen();
                    }
                }));
        }

        void OnPublish(MyGuiControlButton button)
        {
#if !XB1 // XB1_NOWORKSHOP
            Publish(m_loadedPrefab, m_blueprintName);
#else // XB1
            System.Diagnostics.Debug.Assert(false); //TODO?
#endif // XB1
        }

        void OnOpenWorkshop(MyGuiControlButton button)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_BLUEPRINTS, "Steam Workshop");
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            CallResultCallback(m_selectedItem);
            if (m_dialog != null)
            {
                m_dialog.CloseScreen();
            }
        }
    }
}
