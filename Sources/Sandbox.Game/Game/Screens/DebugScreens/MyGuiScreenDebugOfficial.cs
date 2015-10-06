using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage;
using VRage.FileSystem;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenDebugOfficial : MyGuiScreenDebugBase
    {        
        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugOfficial";
        }

        public MyGuiScreenDebugOfficial() :
            base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_canShareInput = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;

            RecreateControls(true);
        }

        public override bool CloseScreen()
        {
            return base.CloseScreen();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 cbOffset = new Vector2(-0.05f, 0.0f);
            Vector2 controlPadding = new Vector2(0.02f, 0.02f); // X: Left & Right, Y: Bottom & Top
            Vector2 multilinePadding = new Vector2(0.008f, 0.005f); // X: Left, Y: Bottom & Top

            float textScale = 0.8f;
            float separatorSize = 0.02f;
            float usableWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - controlPadding.X * 2;
            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f;

            m_currentPosition = -m_size.Value / 2.0f;
            m_currentPosition += controlPadding;
            m_currentPosition.Y += hiddenPartTop;
            m_scale = textScale;

            var caption = AddCaption(MySpaceTexts.ScreenDebugOfficial_Caption, Color.White.ToVector4(), controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y * 2.0f;

            AddCheckBox(MySpaceTexts.ScreenDebugOfficial_EnableDebugDraw, () => MyDebugDrawSettings.ENABLE_DEBUG_DRAW, (bool b) => MyDebugDrawSettings.ENABLE_DEBUG_DRAW = b, color: Color.White.ToVector4(), checkBoxOffset: cbOffset);

            m_currentPosition.Y += separatorSize;

            AddCheckBox(MySpaceTexts.ScreenDebugOfficial_ModelDummies, () => MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES, (bool b) => MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES = b, color: Color.White.ToVector4(), checkBoxOffset: cbOffset);
            AddCheckBox(MySpaceTexts.ScreenDebugOfficial_MountPoints, () => MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS, (bool b) => MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS = b, color: Color.White.ToVector4(), checkBoxOffset: cbOffset);
            AddCheckBox(MySpaceTexts.ScreenDebugOfficial_PhysicsPrimitives, () => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_SHAPES, (bool b) => { MyDebugDrawSettings.DEBUG_DRAW_PHYSICS |= b; MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_SHAPES = b; }, color: Color.White.ToVector4(), checkBoxOffset: cbOffset);

            m_currentPosition.Y += separatorSize;

            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_ReloadTextures, ReloadTextures);
            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_ReloadModels, ReloadModels);
            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_SavePrefab, SavePrefab, MyCubeBuilder.Static != null ? MyCubeBuilder.Static.Clipboard.HasCopiedGrids() : false, MySpaceTexts.ToolTipSaveShip);

            // Don't enable the SE bot debugging in official builds yet
            if (MyPerGameSettings.Game == GameEnum.ME_GAME || !MyFinalBuildConstants.IS_OFFICIAL)
                CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_BotSettings, OpenBotsScreen); 

            AddSubcaption(MyTexts.GetString(MySpaceTexts.ScreenDebugOfficial_ErrorLogCaption), Color.White.ToVector4(), new Vector2(-HIDDEN_PART_RIGHT, 0.0f));

            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_OpenErrorLog, CreateErrorLogScreen);
            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugOfficial_CopyErrorLogToClipboard, CopyErrorLogToClipboard);

            m_currentPosition.Y += separatorSize;

            Vector2 textboxSize = (MyGuiManager.GetMaxMouseCoord() / 2.0f) - m_currentPosition;
            textboxSize.X = usableWidth;
            textboxSize.Y -= controlPadding.Y;
            m_currentPosition.X += multilinePadding.X / 2.0f;

            // Because multiline text does not allow padding
            var textBackground = new MyGuiControlPanel(m_currentPosition - multilinePadding, textboxSize + new Vector2(multilinePadding.X, multilinePadding.Y * 2.0f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            textBackground.BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST;
            Controls.Add(textBackground);

            var text = AddMultilineText(size: textboxSize);

            if (MyDefinitionErrors.GetErrors().Count() == 0)
            {
                text.AppendText(MyTexts.Get(MySpaceTexts.ScreenDebugOfficial_NoErrorText));
            }
            else
            {
                var errors = MyDefinitionErrors.GetErrors();
                var errorDictionary = new Dictionary<string, Tuple<int, ErrorSeverity>>();

                // Count the number of most severe error per mod
                // Errors are already sorted by severity, so the first one per mod is the most severe
                foreach (var error in errors)
                {
                    var modName = error.ModName ?? "Local Content";

                    if (errorDictionary.ContainsKey(modName))
                    {
                        if (errorDictionary[modName].Item2 == error.Severity)
                        {
                            var info = errorDictionary[modName];
                            errorDictionary[modName] = new Tuple<int, ErrorSeverity>(info.Item1 + 1, info.Item2);
                        }
                    }
                    else
                    {
                        errorDictionary[modName] = new Tuple<int, ErrorSeverity>(1, error.Severity);
                    }
                }

                // Convert to list and sort to make sure most severe is displayed first
                var errorList = new List<Tuple<string, int, ErrorSeverity>>();
                foreach (var entry in errorDictionary)
                {
                    errorList.Add(new Tuple<string, int, ErrorSeverity>(entry.Key, entry.Value.Item1, entry.Value.Item2));
                }

                Comparison<Tuple<string, int, ErrorSeverity>> comp = (e1, e2) => e2.Item3 - e1.Item3;
                errorList.Sort(comp);

                foreach (var error in errorList)
                {
                    var errorText = new StringBuilder();
                    errorText.Append(error.Item1);
                    errorText.Append(" [");
                    if (error.Item3 == ErrorSeverity.Critical)
                    {
                        errorText.Append(MyDefinitionErrors.Error.GetSeverityName(error.Item3, false));
                        errorText.Append("]");
                    }
                    else
                    {
                        errorText.Append(error.Item2.ToString());
                        errorText.Append(" ");
                        errorText.Append(MyDefinitionErrors.Error.GetSeverityName(error.Item3, error.Item2 != 1));
                        errorText.Append("]");
                    }
                    text.AppendText(errorText, text.Font, text.TextScaleWithLanguage, MyDefinitionErrors.Error.GetSeverityColor(error.Item3).ToVector4());
                    text.AppendLine();
                }
            }
        }

        private void CreateDebugButton(float usableWidth, MyStringId text, Action<MyGuiControlButton> onClick, bool enabled = true, MyStringId? tooltip = null)
        {
            var button = AddButton(MyTexts.Get(text), onClick);
            button.VisualStyle = Common.ObjectBuilders.Gui.MyGuiControlButtonStyleEnum.Rectangular;
            button.TextScale = m_scale;
            button.Size = new Vector2(usableWidth, button.Size.Y);
            button.Position = button.Position + new Vector2(-HIDDEN_PART_RIGHT / 2.0f, 0.0f);
            button.Enabled = enabled;
            if (tooltip != null)
            {
                button.SetToolTip(tooltip.Value);
            }
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12) || MyInput.Static.IsNewKeyPressed(MyKeys.F11) || MyInput.Static.IsNewKeyPressed(MyKeys.F10))
            {
                this.CloseScreen();
            }
        }

        private void CreateErrorLogScreen(MyGuiControlButton obj)
        {
            var debugErrorsScreen = new MyGuiScreenDebugErrors();
            MyGuiSandbox.AddScreen(debugErrorsScreen);
        }

        private void ReloadTextures(MyGuiControlButton obj)
        {
            VRageRender.MyRenderProxy.ReloadTextures();
            MyHud.Notifications.Add(new MyHudNotificationDebug("Reloaded all textures in the game (modder only feature)", font: MyFontEnum.Red));
        }

        private void ReloadModels(MyGuiControlButton obj)
        {
            VRageRender.MyRenderProxy.ReloadModels();
            MyHud.Notifications.Add(new MyHudNotificationDebug("Reloaded all models in the game (modder only feature)", font: MyFontEnum.Red));
        }

        private void OpenBotsScreen(MyGuiControlButton obj)
        {
            var botSettingsScreen = new MyGuiScreenBotSettings();
            MyGuiSandbox.AddScreen(botSettingsScreen);
        }

        private void SavePrefab(MyGuiControlButton obj)
        {
            string name = MyUtils.StripInvalidChars(MyCubeBuilder.Static.Clipboard.CopiedGridsName);
            string filePath = Path.Combine(MyFileSystem.UserDataPath, "Export", name + ".sbc");
            int index = 1;
            try
            {
                while (MyFileSystem.FileExists(filePath))
                {
                    filePath = Path.Combine(MyFileSystem.UserDataPath, "Export", name + "_" + index + ".sbc");
                    index++;
                }
                MyCubeBuilder.Static.Clipboard.SaveClipboardAsPrefab(name, filePath);
            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine(String.Format("Failed to write prefab at file {0}, message: {1}, stack:{2}", filePath, e.Message, e.StackTrace));
            }
        }

        private void CopyErrorLogToClipboard(MyGuiControlButton obj)
        {
            StringBuilder text = new StringBuilder();

            if (MyDefinitionErrors.GetErrors().Count() == 0)
            {
                text.Append(MyTexts.Get(MySpaceTexts.ScreenDebugOfficial_NoErrorText));
            }
            foreach (var error in MyDefinitionErrors.GetErrors())
            {
                text.Append(error.ToString());
                text.AppendLine();
            }

            Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(text.ToString()));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
