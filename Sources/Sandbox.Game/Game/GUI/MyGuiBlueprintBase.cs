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
using Sandbox.Game.World;
using SteamSDK;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Multiplayer;
using ProtoBuf;
using System.Diagnostics;
using VRage.Compression;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using Sandbox.Game.Localization;
using VRage.Game;

#endregion

namespace Sandbox.Game.Gui
{
    public abstract class MyGuiBlueprintScreenBase : MyGuiScreenDebugBase
    {
        public static string m_localBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "local");
        public static string m_workshopBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "workshop");
        public static string m_defaultBlueprintFolder = Path.Combine(MyFileSystem.ContentPath, "Data", "Blueprints");
        public static readonly string m_workshopBlueprintSuffix = ".sbb";

        public MyGuiBlueprintScreenBase(Vector2 position, Vector2 size, Vector4 backgroundColor, bool isTopMostScreen) :
            base(position, size, backgroundColor, isTopMostScreen)

        {
            m_localBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "local");
            m_workshopBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "workshop");
            m_canShareInput = false;
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;
        }

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

        protected MyGuiControlCompositePanel AddCompositePanel(MyGuiCompositeTexture texture, Vector2 position, Vector2 size, MyGuiDrawAlignEnum panelAlign)
        {
            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = texture
            };
            panel.Position = position;
            panel.Size = size;
            panel.OriginAlign = panelAlign;
            Controls.Add(panel);

            return panel;
        }

        protected MyGuiControlLabel MakeLabel(String text, Vector2 position, float textScale = 1.0f)
        {
            return new MyGuiControlLabel(text: text, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: position, textScale: textScale);
        }

        protected static void SavePrefabToFile(MyObjectBuilder_Definitions prefab, string name, bool replace = false, MyBlueprintTypeEnum type = MyBlueprintTypeEnum.LOCAL)
        { 
            //if (name == null)
            //{
            //    name = MyUtils.StripInvalidChars(MyCubeBuilder.Static.Clipboard.CopiedGridsName);
            //}

            Debug.Assert(name != null, "Name cannot be null");

            string file = "";
            if (type == MyBlueprintTypeEnum.LOCAL)
            {
                file = Path.Combine(m_localBlueprintFolder, name);
            }
            else 
            {
                file = Path.Combine(m_workshopBlueprintFolder, "temp", name);
            }
            string filePath = "";
            int index = 1;

            try
            {
                if (!replace)
                {
                    while (MyFileSystem.DirectoryExists(file))
                    {
                        file = Path.Combine(m_localBlueprintFolder, name + "_" + index);
                        index++;
                    }
                    if (index > 1)
                    {
                        name += new StringBuilder("_" + (index - 1));
                    }
                }
                filePath = file + "\\bp.sbc";
                var success = MyObjectBuilderSerializer.SerializeXML(filePath, false, prefab);

                Debug.Assert(success, "falied to write blueprint to file");
                if (!success)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.OK,
                        styleEnum: MyMessageBoxStyleEnum.Error,
                        messageCaption: new StringBuilder("Error"),
                        messageText: new StringBuilder("There was a problem with saving blueprint")
                        ));
                    if (Directory.Exists(file))
                        Directory.Delete(file, true);
                }

            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine(String.Format("Failed to write prefab at file {0}, message: {1}, stack:{2}", filePath, e.Message, e.StackTrace));
            }
        }

#if !XB1 // XB1_NOWORKSHOP
        public static void Publish(MyObjectBuilder_Definitions prefab, string blueprintName, Action<ulong> publishCallback = null)
        {
            string file = Path.Combine(m_localBlueprintFolder, blueprintName);
            string title = prefab.ShipBlueprints[0].CubeGrids[0].DisplayName;
            string description = prefab.ShipBlueprints[0].Description;
            ulong publishId = prefab.ShipBlueprints[0].WorkshopId;
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                styleEnum: MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: new StringBuilder("Publish"),
                messageText: new StringBuilder("Do you want to publish this blueprint?"),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                {
                    if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        Action<MyGuiScreenMessageBox.ResultEnum, string[]> onTagsChosen = delegate(MyGuiScreenMessageBox.ResultEnum tagsResult, string[] outTags)
                        {
                            if (tagsResult == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                MySteamWorkshop.PublishBlueprintAsync(file, title, description, publishId, outTags, SteamSDK.PublishedFileVisibility.Public,
                                    callbackOnFinished: delegate(bool success, Result result, ulong publishedFileId)
                                    {
                                        if (success)
                                        {
                                            if (publishCallback != null)
                                                publishCallback(publishedFileId);

                                            prefab.ShipBlueprints[0].WorkshopId = publishedFileId;
                                            SavePrefabToFile(prefab, blueprintName, true);
                                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                styleEnum: MyMessageBoxStyleEnum.Info,
                                                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextWorldPublished),
                                                messageCaption: new StringBuilder("BLUEPRINT PUBLISHED"),
                                                callback: (a) =>
                                                {
                                                    MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", publishedFileId));
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
                                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWorldPublishFailed)));
                                        }
                                    });
                            }
                        };

                        if (MySteamWorkshop.BlueprintCategories.Length > 0)
                            MyGuiSandbox.AddScreen(new MyGuiScreenWorkshopTags(MySteamWorkshop.WORKSHOP_BLUEPRINT_TAG, MySteamWorkshop.BlueprintCategories, null, onTagsChosen));
                        else
                            onTagsChosen(MyGuiScreenMessageBox.ResultEnum.YES, new string[] { MySteamWorkshop.WORKSHOP_BLUEPRINT_TAG });
                    }
                }));

        }

        public static MyObjectBuilder_Definitions LoadWorkshopPrefab(string archive, ulong? publishedItemId)
        {
            if (!File.Exists(archive) || publishedItemId == null)
                return null;
            var subItem = MyGuiBlueprintScreen.m_subscribedItemsList.Find(item => item.PublishedFileId == publishedItemId);
            
            if (subItem == null)
                return null;

            var extracted = MyZipArchive.OpenOnFile(archive);
            if (!extracted.FileExists("bp.sbc"))
                return null;

            var stream = extracted.GetFile("bp.sbc").GetStream();
            
            if (stream == null)
                return null;
            
            MyObjectBuilder_Definitions objectBuilder = null;
            var success = MyObjectBuilderSerializer.DeserializeXML(stream, out objectBuilder);
            stream.Close();
            extracted.Dispose();
            
            if (success)
            {
                objectBuilder.ShipBlueprints[0].Description = subItem.Description;
                objectBuilder.ShipBlueprints[0].CubeGrids[0].DisplayName = subItem.Title;
                return objectBuilder;
            }
			return null;
        }
#endif // !XB1

        public static MyObjectBuilder_Definitions LoadPrefab(string filePath)
        {
            MyObjectBuilder_Definitions loadedPrefab = null;
            if (MyFileSystem.FileExists(filePath))
            {
                var success = MyObjectBuilderSerializer.DeserializeXML(filePath, out loadedPrefab);
                if (!success)
                {
                    return null;
                }
            }
            return loadedPrefab;
        }

        protected bool DeleteBlueprint(string name)
        {
            string file = Path.Combine(m_localBlueprintFolder, name);
            if (Directory.Exists(file))
            {
                Directory.Delete(file, true);
                return true;
            }
            else 
            {
                return false;
            }
        }

        public override bool CloseScreen()
        {
            return base.CloseScreen();
        }
        virtual public void RefreshBlueprintList(bool fromTask = false)
        { }
    }
}
