using Sandbox.Definitions;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Gui;
using VRage.Input;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Color = VRageMath.Color;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using OreDepositMarker = VRage.MyTuple<VRageMath.Vector3D, Sandbox.Game.Entities.Cube.MyEntityOreDeposit>;
using Vector2 = VRageMath.Vector2;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenHudSpace : MyGuiScreenHudBase
    {
        public static MyGuiScreenHudSpace Static;

        //GR: Trigger recalculation of oxygen after when altitude differs by this amount
        private const float ALTITUDE_CHANGE_THRESHOLD = 2000;
        private const int PING_THRESHOLD_MILLISECONDS = 250;
        private const float SERVER_SIMSPEED_THRESHOLD = 0.5f;

        private MyGuiControlToolbar m_toolbarControl;
        private MyGuiControlBlockInfo m_blockInfo;
        private MyGuiControlRotatingWheel m_rotatingWheelControl;
        private MyGuiControlMultilineText m_cameraInfoMultilineControl;
        private MyGuiControlQuestlog m_questlogControl;

        private MyGuiControlLabel m_buildModeLabel;
        private MyGuiControlLabel m_blocksLeft;

        private MyHudControlChat m_chatControl;
        private MyHudMarkerRender m_markerRender;

        private int m_oreHudMarkerStyle;
        private int m_gpsHudMarkerStyle;
        private int m_buttonPanelHudMarkerStyle;

        private MyHudEntityParams m_tmpHudEntityParams;

        private OreDepositMarker[] m_nearestOreDeposits;
        private float[] m_nearestDistanceSquared;

        private MyGuiControlLabel m_noMsgSentNotification;
        private MyGuiControlLabel m_noConnectionNotification;
        private MyGuiControlLabel m_serverSavingNotification;
        private MyGuiControlLabel m_relayNotification;
        private MyGuiControlLabel m_highPingNotification;
        private MyGuiControlLabel m_lowSimSpeedNotification;

        private bool m_hiddenToolbar;

        public float m_gravityHudWidth;

        private float m_altitude;

        public MyGuiScreenHudSpace()
            : base()
        {
            RecreateControls(true);

            m_markerRender = new MyHudMarkerRender(this);
            m_oreHudMarkerStyle = m_markerRender.AllocateMarkerStyle(MyFontEnum.White, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_neutral, Color.White);
            m_gpsHudMarkerStyle = m_markerRender.AllocateMarkerStyle(MyFontEnum.DarkBlue, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_me, MyHudConstants.GPS_COLOR);
            m_buttonPanelHudMarkerStyle = m_markerRender.AllocateMarkerStyle(MyFontEnum.DarkBlue, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_me, MyHudConstants.GPS_COLOR);

            m_tmpHudEntityParams = new MyHudEntityParams()
            {
                Text = new StringBuilder(),
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL,
                IconColor = MyHudConstants.GPS_COLOR,
                OffsetText = true
            };

            Static = this;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_toolbarControl = new MyGuiControlToolbar();
            m_toolbarControl.Position = new Vector2(0.5f, 0.99f);
            m_toolbarControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;
            m_toolbarControl.IsActiveControl = false;
            Elements.Add(m_toolbarControl);
            m_textScale = MyGuiConstants.HUD_TEXT_SCALE * MyGuiManager.LanguageTextScale;

            var style = new MyGuiControlBlockInfo.MyControlBlockInfoStyle()
            {
                BlockNameLabelFont = MyFontEnum.White,
                EnableBlockTypeLabel = true,
                ComponentsLabelText = MySpaceTexts.HudBlockInfo_Components,
                ComponentsLabelFont = MyFontEnum.Blue,
                InstalledRequiredLabelText = MySpaceTexts.HudBlockInfo_Installed_Required,
                InstalledRequiredLabelFont = MyFontEnum.Blue,
                RequiredLabelText = MyCommonTexts.HudBlockInfo_Required,
                IntegrityLabelFont = MyFontEnum.White,
                IntegrityBackgroundColor = new Vector4(78 / 255.0f, 116 / 255.0f, 137 / 255.0f, 1.0f),
                IntegrityForegroundColor = new Vector4(0.5f, 0.1f, 0.1f, 1),
                IntegrityForegroundColorOverCritical = new Vector4(118 / 255.0f, 166 / 255.0f, 192 / 255.0f, 1.0f),
                LeftColumnBackgroundColor = new Vector4(46 / 255.0f, 76 / 255.0f, 94 / 255.0f, 1.0f),
                TitleBackgroundColor = new Vector4(72 / 255.0f, 109 / 255.0f, 130 / 255.0f, 1.0f),
                ComponentLineMissingFont = MyFontEnum.Red,
                ComponentLineAllMountedFont = MyFontEnum.White,
                ComponentLineAllInstalledFont = MyFontEnum.Blue,
                ComponentLineDefaultFont = MyFontEnum.White,
                ComponentLineDefaultColor = new Vector4(0.6f, 0.6f, 0.6f, 1f),
                ShowAvailableComponents = false,
                EnableBlockTypePanel = true,
            };
            m_blockInfo = new MyGuiControlBlockInfo(style);
            m_blockInfo.IsActiveControl = false;
            Controls.Add(m_blockInfo);

            m_questlogControl = new MyGuiControlQuestlog(new Vector2(20f, 20f));
            m_questlogControl.IsActiveControl = false;
            m_questlogControl.RecreateControls();
            Controls.Add(m_questlogControl);

            m_chatControl = new MyHudControlChat(MyHud.Chat, Vector2.Zero, new Vector2(0.4f, 0.28f), visibleLinesCount: 12);
            Elements.Add(m_chatControl);

            m_cameraInfoMultilineControl = new MyGuiControlMultilineText(
                position: Vector2.Zero,
                size: new Vector2(0.4f, 0.25f),
                backgroundColor: null,
                font: MyFontEnum.White,
                textScale: 0.7f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                contents: null,
                drawScrollbar: false,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            m_cameraInfoMultilineControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            Elements.Add(m_cameraInfoMultilineControl);

            m_rotatingWheelControl = new MyGuiControlRotatingWheel(position: new Vector2(0.5f, 0.85f));

            Controls.Add(m_rotatingWheelControl);

            Vector2 buildModePosition = new Vector2(0.5f, 0.02f);
            buildModePosition = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref buildModePosition);
            m_buildModeLabel = new MyGuiControlLabel(
                position: buildModePosition,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                font: MyFontEnum.White,
                text: MyTexts.GetString(MyCommonTexts.Hud_BuildMode));
            Controls.Add(m_buildModeLabel);

            m_blocksLeft = new MyGuiControlLabel(
                position: new Vector2(0.238f, 0.89f),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                font: MyFontEnum.White,
                text: MyHud.BlocksLeft.GetStringBuilder().ToString()
                );
            Controls.Add(m_blocksLeft);

            m_relayNotification = new MyGuiControlLabel(new Vector2(1, 0), font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_relayNotification.TextEnum = MyCommonTexts.Multiplayer_IndirectConnection;
            m_relayNotification.Visible = false;
            Controls.Add(m_relayNotification);
            var offset = new Vector2(0, m_relayNotification.Size.Y);
            m_noMsgSentNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.Debug, text: MyTexts.GetString(MyCommonTexts.Multiplayer_LastMsg), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_noMsgSentNotification.Visible = false;
            Controls.Add(m_noMsgSentNotification);
            offset += new Vector2(0, m_noMsgSentNotification.Size.Y);
            m_noConnectionNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.Red, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_noConnectionNotification.TextEnum = MyCommonTexts.Multiplayer_NoConnection;
            m_noConnectionNotification.Visible = false;
            Controls.Add(m_noConnectionNotification);

            m_serverSavingNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_serverSavingNotification.TextEnum = MyCommonTexts.SavingPleaseWait;
            m_serverSavingNotification.Visible = false;
            Controls.Add(m_serverSavingNotification);

            m_highPingNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_highPingNotification.TextEnum = MyCommonTexts.Multiplayer_HighPing;
            m_highPingNotification.Visible = false;
            Controls.Add(m_highPingNotification);

            offset += new Vector2(0, m_highPingNotification.Size.Y);
            m_lowSimSpeedNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_lowSimSpeedNotification.TextEnum = MyCommonTexts.Multiplayer_LowSimSpeed;
            m_lowSimSpeedNotification.Visible = false;
            Controls.Add(m_lowSimSpeedNotification);

            MyHud.ReloadTexts();
        }

        public override bool Draw()
        {
            if (m_transitionAlpha < 1.0f)
                return false;

            if (MyInput.Static.IsNewKeyPressed(MyKeys.J) && MyFakes.ENABLE_OBJECTIVE_LINE)
            {
                MyHud.ObjectiveLine.AdvanceObjective();
            }

            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                ProfilerShort.Begin("Marker rendering");
                ProfilerShort.Begin("m_markerRender.Draw");
                m_markerRender.Draw();
                ProfilerShort.BeginNextBlock("DrawTexts");
                DrawTexts();
                ProfilerShort.End();
                ProfilerShort.End();
            }

            m_toolbarControl.Visible = !(m_hiddenToolbar || MyHud.MinimalHud || MyHud.CutsceneHud);

            Vector2 position = new Vector2(0.99f, 0.75f);
            if (MySession.Static.ControlledEntity is MyShipController) position.Y = 0.65f;
            position = ConvertHudToNormalizedGuiPosition(ref position);
            if (MyVideoSettingsManager.IsTripleHead())
                position.X += 1.0f;

            // TODO: refactor this
            m_blockInfo.Visible = MyHud.BlockInfo.Visible && !MyHud.MinimalHud && !MyHud.CutsceneHud;
            m_blockInfo.BlockInfo = m_blockInfo.Visible ? MyHud.BlockInfo : null;
            m_blockInfo.Position = position;
            m_blockInfo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;

            m_questlogControl.Visible = MyHud.Questlog.Visible && !MyHud.MinimalHud && !MyHud.CutsceneHud;
            //m_questlogControl.QuestInfo = MyHud.Questlog;
            //m_questlogControl.RecreateControls();
            //m_questlogControl.Position = GetRealPositionOnCenterScreen(Vector2.Zero);
            //m_questlogControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            m_rotatingWheelControl.Visible = MyHud.RotatingWheelVisible && !MyHud.MinimalHud && !MyHud.CutsceneHud;

            m_chatControl.Visible = !(MyScreenManager.GetScreenWithFocus() is MyGuiScreenScenarioMpBase) && (!MyHud.MinimalHud || m_chatControl.HasFocus || MyHud.CutsceneHud);

            if (!base.Draw())
                return false;

            var bgPos = new Vector2(0.01f, 0.85f);
            bgPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref bgPos);
            m_chatControl.Position = bgPos + new Vector2(0.17f, 0);
            m_chatControl.TextScale = 0.9f;

            bgPos = new Vector2(0.03f, 0.1f);
            bgPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref bgPos);
            m_cameraInfoMultilineControl.Position = bgPos;
            m_cameraInfoMultilineControl.TextScale = 0.9f;

            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                bool inNaturalGravity = false;
                var cockpit = MySession.Static.ControlledEntity as MyCockpit;
                if (cockpit != null)
                {
                    var characterPosition = cockpit.PositionComp.GetPosition();
                    inNaturalGravity = MyGravityProviderSystem.CalculateHighestNaturalGravityMultiplierInPoint(characterPosition) != 0;
                }

                if (inNaturalGravity)
                    DrawArtificialHorizonAndAltitude();
            }

            MyHud.Notifications.Draw();

            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                m_buildModeLabel.Visible = MyHud.IsBuildMode;

                m_blocksLeft.Text = MyHud.BlocksLeft.GetStringBuilder().ToString();
                m_blocksLeft.Visible = MyHud.BlocksLeft.Visible;

                if (MyHud.ShipInfo.Visible)
                    DrawShipInfo(MyHud.ShipInfo);

                if (MyHud.CharacterInfo.Visible)
                    DrawSuitInfo(MyHud.CharacterInfo);

                if (MyHud.ObjectiveLine.Visible && MyFakes.ENABLE_OBJECTIVE_LINE)
                    DrawObjectiveLine(MyHud.ObjectiveLine);

                if (MySandboxGame.Config.EnablePerformanceWarnings)
                {
                    foreach (var warning in MySimpleProfiler.CurrentWarnings)
                    {
                        if (warning.Value.Time < 120)
                        {
                            DrawPerformanceWarning();
                            break;
                        }
                    }
                }
            }
            else
            {
                m_buildModeLabel.Visible = false;
                m_blocksLeft.Visible = false;
            }

            MyHud.BlockInfo.Visible = false;
            m_blockInfo.BlockInfo = null;

            if (MyFakes.ENABLE_USE_OBJECT_HIGHLIGHT)
            {
                MyHudObjectHighlightStyleData data = new MyHudObjectHighlightStyleData();
                data.AtlasTexture = m_atlas;
                data.TextureCoord = GetTextureCoord(MyHudTexturesEnum.corner);
                MyGuiScreenHudBase.HandleSelectedObjectHighlight(MyHud.SelectedObjectHighlight, data);
            }

            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                if (MyHud.GravityIndicator.Visible)
                    DrawGravityIndicator(MyHud.GravityIndicator, MyHud.CharacterInfo);

                if (MyHud.SinkGroupInfo.Visible)
                    DrawPowerGroupInfo(MyHud.SinkGroupInfo);

                if (MyHud.LocationMarkers.Visible)
                    m_markerRender.DrawLocationMarkers(MyHud.LocationMarkers);

                if (MyHud.GpsMarkers.Visible && MyFakes.ENABLE_GPS)
                    DrawGpsMarkers(MyHud.GpsMarkers);

                if (MyHud.ButtonPanelMarkers.Visible)
                    DrawButtonPanelMarkers(MyHud.ButtonPanelMarkers);

                if (MyHud.OreMarkers.Visible)
                    DrawOreMarkers(MyHud.OreMarkers);

                if (MyHud.LargeTurretTargets.Visible)
                    DrawLargeTurretTargets(MyHud.LargeTurretTargets);

                DrawWorldBorderIndicator(MyHud.WorldBorderChecker);

                if (MyHud.HackingMarkers.Visible)
                    DrawHackingMarkers(MyHud.HackingMarkers);

                //m_chatControl.Visible = !MyHud.MinimalHud;

            }
                DrawCameraInfo(MyHud.CameraInfo);

            ProfilerShort.Begin("Draw netgraph");
            if (MyFakes.ENABLE_NETGRAPH && MyHud.IsNetgraphVisible)
                DrawNetgraph(MyHud.Netgraph);
            ProfilerShort.End();
            //if (Sync.MultiplayerActive)
            DrawMultiplayerNotifications();

            if (MyHud.VoiceChat.Visible)
                DrawVoiceChat(MyHud.VoiceChat);

            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                if (MyHud.ScenarioInfo.Visible)
                    DrawScenarioInfo(MyHud.ScenarioInfo);
            }
            return true;
        }

        public override bool Update(bool hasFocus)
        {
            m_markerRender.Update();
            return base.Update(hasFocus);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenHudSpace";
        }

        /// <summary>
        /// Return position on middle screen based on real desired position on gamescreen.
        /// "Middle" make sense only for tripple monitors. For every else is middle screen all screen.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Vector2 GetRealPositionOnCenterScreen(Vector2 value)
        {
            Vector2 position;
            if (MyGuiManager.FullscreenHudEnabled)
                position = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(value);
            else
                position = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(value);


            if (MyVideoSettingsManager.IsTripleHead())
                position.X += 1.0f;
            return position;
        }

        private void DrawMultiplayerNotifications()
        {
            if (MySession.Static.MultiplayerLastMsg > 3)
            {
                m_noMsgSentNotification.Visible = true;
                m_noMsgSentNotification.UpdateFormatParams((int)MySession.Static.MultiplayerLastMsg);
            }
            else
                m_noMsgSentNotification.Visible = false;

            if (!MySession.Static.MultiplayerAlive)
            {
                m_noConnectionNotification.Visible = true && (MySession.Static.ServerSaving == false);
            }
            else
            {
                m_noConnectionNotification.Visible = false;
            }

            if (MySession.Static.ServerSaving)
            {
                m_serverSavingNotification.Visible = true;
            }
            else
            {
                m_serverSavingNotification.Visible = false;
            }



            if (!MySession.Static.MultiplayerDirect)
            {
                m_relayNotification.Visible = true;
            }
            else
                m_relayNotification.Visible = false;

            if (!Sync.IsServer && MySession.Static.MultiplayerPing.Milliseconds > PING_THRESHOLD_MILLISECONDS)
            {
                m_highPingNotification.Visible = true;
            }
            else
            {
                m_highPingNotification.Visible = false;
            }

            if (!Sync.IsServer && Sync.ServerSimulationRatio < SERVER_SIMSPEED_THRESHOLD)
            {
                m_lowSimSpeedNotification.Visible = true;
            }
            else
            {
                m_lowSimSpeedNotification.Visible = false;
            }
        }

        public void SetToolbarVisible(bool visible)
        {
            if (m_toolbarControl != null)
            {
                m_toolbarControl.Visible = visible;
                m_hiddenToolbar = !visible;
            }
        }

        private void DrawGravityIndicator(MyHudGravityIndicator indicator, MyHudCharacterInfo characterInfo)
        {
            if (indicator.Entity == null)
                return;

            Vector3D worldPosition = MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator ? indicator.Entity.PositionComp.WorldAABB.Center : MySpectatorCameraController.Static.Position;
            Vector3 totalGravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(worldPosition);
            Vector3 naturalGravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(worldPosition);
            Vector3 artificialGravity = totalGravity - naturalGravity;
            //gravity += MyPhysics.HavokWorld.Gravity;
            bool anyGravity = !Vector3.IsZero(totalGravity);
            bool anyNaturalGravity = anyGravity && !Vector3.IsZero(naturalGravity);

            // Background and text drawing
            string totalGravityFont, artificialGravityFont = MyFontEnum.Blue, naturalGravityFont = MyFontEnum.Blue;
            StringBuilder totalGravityText, artificialGravityText = null, naturalGravityText = null;
            MyGuiPaddedTexture backgroundTexture;
            if (anyGravity)
            {
                totalGravityFont = MyFontEnum.Blue;
                totalGravityText = MyTexts.Get(MySpaceTexts.HudInfoGravity);
                if (!anyNaturalGravity)
                    artificialGravityFont = MyFontEnum.White;
                artificialGravityText = MyTexts.Get(MySpaceTexts.HudInfoGravityArtificial);
                backgroundTexture = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT;
            }
            else
            {
                totalGravityFont = MyFontEnum.Red;
                totalGravityText = MyTexts.Get(MySpaceTexts.HudInfoNoGravity);
                backgroundTexture = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_RED;
            }

            if (anyNaturalGravity)
            {
                naturalGravityText = MyTexts.Get(MySpaceTexts.HudInfoGravityNatural);
                if (naturalGravity.Length() > artificialGravity.Length())
                {
                    artificialGravityFont = MyFontEnum.Blue;
                    naturalGravityFont = MyFontEnum.White;
                }
                else
                {
                    artificialGravityFont = MyFontEnum.White;
                    naturalGravityFont = MyFontEnum.Blue;
                }
            }

            bool drawOxygen = MySession.Static.Settings.EnableOxygen;
            Vector2 bgSizeDelta = new Vector2(0.015f + 0.02f, 0.05f);
            float oxygenLevel = 0f;

            Vector2 oxygenCompensation = Vector2.Zero;
            if (drawOxygen && anyNaturalGravity) oxygenCompensation = new Vector2(0f, 0.025f);
            Vector2 backgroundSize = backgroundTexture.SizeGui + bgSizeDelta + oxygenCompensation;

            Vector2 backgroundPosition, vectorPosition;
            Vector2 totalGravityTextPos, artificialGravityTextPos, naturalGravityTextPos;
            Vector2 totalGravityNumberPos, artificialGravityNumberPos, naturalGravityNumberPos;
            Vector2 dividerLinePosition, dividerLineSize;
            MyGuiDrawAlignEnum backgroundAlignment, gravityTextAlignment = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, gravityNumberAlignment = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            dividerLineSize = new Vector2(backgroundSize.X - backgroundTexture.PaddingSizeGui.X * 1f, backgroundSize.Y / 60f);

            if (indicator.Entity is MyCharacter)
            {
                backgroundAlignment = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
                backgroundPosition = new Vector2(0.99f, 0.99f);
                backgroundPosition = ConvertHudToNormalizedGuiPosition(ref backgroundPosition);

                totalGravityTextPos = backgroundPosition - backgroundSize * new Vector2(0.35f, 1.075f);
                totalGravityNumberPos = totalGravityTextPos + new Vector2(0.0075f, 0.002f);
                dividerLinePosition = new Vector2(backgroundPosition.X - backgroundSize.X + backgroundTexture.PaddingSizeGui.X / 2.0f, totalGravityTextPos.Y) + new Vector2(0.0f, 0.026f);


                {
                    artificialGravityTextPos = new Vector2(totalGravityTextPos.X, dividerLinePosition.Y) + new Vector2(0.0f, 0.005f);
                    artificialGravityNumberPos = new Vector2(totalGravityNumberPos.X, artificialGravityTextPos.Y);
                    naturalGravityTextPos = artificialGravityTextPos + new Vector2(0.0f, 0.025f);
                    naturalGravityNumberPos = artificialGravityNumberPos + new Vector2(0.0f, 0.025f);
                }

                vectorPosition = backgroundPosition - backgroundSize * new Vector2(0.5f, 0.5f - 0.05f) + backgroundTexture.PaddingSizeGui * Vector2.UnitY * 0.5f;

                oxygenLevel = (indicator.Entity as MyCharacter).OxygenComponent != null ? (indicator.Entity as MyCharacter).OxygenComponent.EnvironmentOxygenLevel : 0f;
            }
            else
            {
                backgroundAlignment = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                backgroundPosition = new Vector2(0.01f, 1f - (characterInfo.Data.GetGuiHeight() + 0.02f));
                backgroundPosition = ConvertHudToNormalizedGuiPosition(ref backgroundPosition);

                gravityTextAlignment = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
                gravityNumberAlignment = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                totalGravityTextPos = backgroundPosition + backgroundSize * new Vector2(1 - 0.35f, -0.99f) + backgroundTexture.PaddingSizeGui * Vector2.UnitY * 0.2f;
                totalGravityNumberPos = totalGravityNumberPos = totalGravityTextPos + new Vector2(0.0075f, 0.0025f);
                dividerLinePosition = new Vector2(backgroundPosition.X + backgroundTexture.PaddingSizeGui.X / 2.0f, totalGravityTextPos.Y - 0.022f) + new Vector2(0.0f, 0.026f);

                {
                    artificialGravityTextPos = new Vector2(totalGravityTextPos.X, dividerLinePosition.Y + 0.023f) + new Vector2(0.0f, 0.005f);
                    artificialGravityNumberPos = new Vector2(totalGravityNumberPos.X, artificialGravityTextPos.Y);
                    naturalGravityTextPos = artificialGravityTextPos + new Vector2(0.0f, 0.025f);
                    naturalGravityNumberPos = artificialGravityNumberPos + new Vector2(0.0f, 0.025f);
                }

                vectorPosition = backgroundPosition - backgroundSize * new Vector2(-0.5f, 0.5f - 0.05f) + backgroundTexture.PaddingSizeGui * Vector2.UnitY * 0.5f;

                var cockpit = indicator.Entity as MyCockpit;
                if (cockpit != null && cockpit.Pilot != null && cockpit.Pilot.OxygenComponent != null)
                {
                    oxygenLevel = cockpit.Pilot.OxygenComponent.EnvironmentOxygenLevel;
                }
                else
                {
                    drawOxygen = false;
                }
            }

            m_gravityHudWidth = backgroundSize.X;
            MyGuiManager.DrawSpriteBatch(backgroundTexture.Texture, backgroundPosition, backgroundSize + (true ? new Vector2(0f, 0.025f) : Vector2.Zero), Color.White, backgroundAlignment);
            var controlledEntity = MySession.Static.ControlledEntity as MyEntity;
            var scale = 0.85f;
            if (anyNaturalGravity && controlledEntity != null)
            {
                double cosRotation = Vector3D.Normalize(Vector3D.Reject(naturalGravity, controlledEntity.WorldMatrix.Forward)).Dot(Vector3D.Normalize(-controlledEntity.WorldMatrix.Up));
                float rotation = 0.0f;
                rotation = (float)Math.Acos(cosRotation);

                if (naturalGravity.Dot(controlledEntity.WorldMatrix.Right) >= 0)
                    rotation = 2.0f * (float)Math.PI - rotation;

                MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_HUD_GRAVITY_GLOBE.Texture, vectorPosition + new Vector2(0.045f, 0.065f) * scale + oxygenCompensation / 2, scale, Color.White, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, rotation, new Vector2(0.5f));
            }
            MyGuiManager.DrawString(totalGravityFont, totalGravityText, totalGravityTextPos, m_textScale, drawAlign: gravityTextAlignment);

            if (anyGravity)
                MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_HUD_GRAVITY_LINE.Texture, dividerLinePosition, dividerLineSize, Color.White, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            if (drawOxygen)
            {
                Vector2 oxygenTextPos = artificialGravityTextPos + new Vector2(0.0f, 0.025f) + oxygenCompensation.Y;
                var oxygenFont = MyFontEnum.Blue;
                var oxygenText = new StringBuilder(MyTexts.Get(MySpaceTexts.HudInfoOxygen).ToString());
                if (oxygenLevel == 0f)
                {
                    oxygenText.Append("None");
                    oxygenFont = MyFontEnum.Red;
                }
                else if (oxygenLevel < 0.5f)
                {
                    oxygenText.Append("Low");
                    oxygenFont = MyFontEnum.Red;
                }
                else
                {
                    oxygenText.Append("High");
                }

                MyGuiManager.DrawString(oxygenFont, oxygenText, oxygenTextPos, m_textScale, drawAlign: gravityTextAlignment);
            }

            if (anyGravity)
            {
                m_hudIndicatorText.Clear();
                m_hudIndicatorText.AppendFormatedDecimal("", totalGravity.Length() / MyGravityProviderSystem.G, 2, " g");
                MyGuiManager.DrawString(totalGravityFont, m_hudIndicatorText, totalGravityNumberPos, m_textScale, drawAlign: gravityNumberAlignment);
                m_hudIndicatorText.Clear();
                m_hudIndicatorText.AppendFormatedDecimal("", artificialGravity.Length() / MyGravityProviderSystem.G, 2, " g");
                MyGuiManager.DrawString(artificialGravityFont, artificialGravityText, artificialGravityTextPos, m_textScale, drawAlign: gravityTextAlignment);
                MyGuiManager.DrawString(artificialGravityFont, m_hudIndicatorText, artificialGravityNumberPos, m_textScale, drawAlign: gravityNumberAlignment);
                if (anyNaturalGravity)
                {
                    m_hudIndicatorText.Clear();
                    m_hudIndicatorText.AppendFormatedDecimal("", naturalGravity.Length() / MyGravityProviderSystem.G, 2, " g");
                    MyGuiManager.DrawString(naturalGravityFont, naturalGravityText, naturalGravityTextPos, m_textScale, drawAlign: gravityTextAlignment);
                    MyGuiManager.DrawString(naturalGravityFont, m_hudIndicatorText, naturalGravityNumberPos, m_textScale, drawAlign: gravityNumberAlignment);
                }
            }

            vectorPosition = MyGuiManager.GetHudSize() * ConvertNormalizedGuiToHud(ref vectorPosition) + (oxygenCompensation / 2) * scale;
            if (MyVideoSettingsManager.IsTripleHead())
                vectorPosition.X += 1.0f;

            // Draw each of gravity indicators.
            foreach (var generatorGravity in MyGravityProviderSystem.GravityVectors)
                DrawGravityVectorIndicator(vectorPosition, generatorGravity, MyHudTexturesEnum.gravity_arrow, Color.Gray);

            if (anyGravity)
                DrawGravityVectorIndicator(vectorPosition, totalGravity, MyHudTexturesEnum.gravity_arrow, Color.White);

            // Draw center
            MyAtlasTextureCoordinate centerTextCoord;
            if (anyGravity)
                centerTextCoord = GetTextureCoord(MyHudTexturesEnum.gravity_point_white);
            else
                centerTextCoord = GetTextureCoord(MyHudTexturesEnum.gravity_point_red);

            float hudSizeX = MyGuiManager.GetSafeFullscreenRectangle().Width / MyGuiManager.GetHudSize().X;
            float hudSizeY = MyGuiManager.GetSafeFullscreenRectangle().Height / MyGuiManager.GetHudSize().Y;
            Vector2 rightVector = Vector2.UnitX;

            MyRenderProxy.DrawSpriteAtlas(
                m_atlas,
                vectorPosition,
                centerTextCoord.Offset,
                centerTextCoord.Size,
                rightVector,
                new Vector2(hudSizeX, hudSizeY),
                MyHudConstants.HUD_COLOR_LIGHT,
                Vector2.One * 0.005f);

        }

        private void DrawShipInfo(MyHudShipInfo info)
        {
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            Color color = Color.White;
            Vector2 bgScale = new Vector2(1.2f, 1.05f);

            var bgPos = new Vector2(0.99f, 0.99f);
            bgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
            var bg = MyGuiConstants.TEXTURE_HUD_BG_LARGE_DEFAULT;
            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, bg.SizeGui * bgScale, color, align);

            //float scale = MyGuiConstants.HUD_TEXT_SCALE;
            var valuePos = bgPos - bg.PaddingSizeGui * bgScale;
            var namePos = bgPos - bgScale * new Vector2(bg.SizeGui.X - bg.PaddingSizeGui.X, bg.PaddingSizeGui.Y);

            info.Data.DrawBottomUp(namePos, valuePos, m_textScale);
        }

        private void DrawVoiceChat(MyHudVoiceChat voiceChat)
        {
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            var bg = MyGuiConstants.TEXTURE_VOICE_CHAT;
            var basePos = new Vector2(0.01f, 0.99f);
            var bgPos = ConvertHudToNormalizedGuiPosition(ref basePos);
            MyGuiManager.DrawSpriteBatch(
                bg.Texture,
                bgPos,
                bg.SizeGui,
                Color.White,
                align);
        }

        private void DrawPowerGroupInfo(MyHudSinkGroupInfo info)
        {
            var fsRect = MyGuiManager.GetSafeFullscreenRectangle();
            float namesOffset = -0.25f / (fsRect.Width / (float)fsRect.Height);
            var posValuesBase = new Vector2(0.985f, 0.65f); // coordinates in SafeFullscreenRectangle, but DrawString works with SafeGuiRectangle.
            var posNamesBase = new Vector2(posValuesBase.X + namesOffset, posValuesBase.Y);
            var posValues = ConvertHudToNormalizedGuiPosition(ref posValuesBase);
            var posNames = ConvertHudToNormalizedGuiPosition(ref posNamesBase);
            info.Data.DrawBottomUp(posNames, posValues, m_textScale);
        }

        private void DrawSuitInfo(MyHudCharacterInfo suitInfo)
        {
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            Color color = Color.White;
            var basePos = new Vector2(0.01f, 0.99f);
            var bgPos = ConvertHudToNormalizedGuiPosition(ref basePos);
            Vector2 namePos, valuePos, bgScale;
            MyGuiPaddedTexture bg;

            bg = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT;
            bgScale = new Vector2(1.1f, 1f);
            var bgWidth = (!MyHud.ShipInfo.Visible || MyUtils.IsZero(m_gravityHudWidth)) ? bg.SizeGui.X * bgScale.X : m_gravityHudWidth;
            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, new Vector2(bgWidth, suitInfo.Data.GetGuiHeight()), Color.White, align);

            namePos = bgPos + new Vector2(1f, -1f) * bg.PaddingSizeGui * bgScale;
            valuePos = bgPos + (new Vector2(bgWidth, 0f) - bg.PaddingSizeGui);

            suitInfo.Data.DrawBottomUp(namePos, valuePos, m_textScale);
        }

        private float FindDistanceToNearestPlanetSeaLevel(BoundingBoxD worldBB, out MyPlanet closestPlanet)
        {
            ProfilerShort.Begin("FindNearestPointOnPlanet");
            closestPlanet = MyGamePruningStructure.GetClosestPlanet(ref worldBB);
            double closestDistance = double.MaxValue;
            if (closestPlanet != null)
            {
                closestDistance = ((worldBB.Center - closestPlanet.PositionComp.GetPosition()).Length() - closestPlanet.AverageRadius);
            }
            ProfilerShort.End();

            return (float)closestDistance;
        }

        private void DrawArtificialHorizonAndAltitude()
        {
            var controlledEntity = MySession.Static.ControlledEntity as MyCubeBlock;
            var controlledEntityPosition = controlledEntity.CubeGrid.Physics.CenterOfMassWorld;
            var controlledEntityCenterOfMass = controlledEntity.GetTopMostParent().Physics.CenterOfMassWorld;
            if (controlledEntity == null)
                return;

            var shipController = controlledEntity as MyShipController;
            if (shipController != null && !shipController.HorizonIndicatorEnabled)
                return;

            MyPlanet nearestPlanet;
            FindDistanceToNearestPlanetSeaLevel(controlledEntity.PositionComp.WorldAABB, out nearestPlanet);
            if (nearestPlanet == null)
                return;

            Vector3D closestPoint = nearestPlanet.GetClosestSurfacePointGlobal(ref controlledEntityPosition);
            float distanceToSeaLevel = (float)Vector3D.Distance(closestPoint, controlledEntityPosition);

            string altitudeFont = MyFontEnum.Blue;
            var altitudeAlignment = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            var altitude = distanceToSeaLevel;

            //GR: Not the best place to start pressurization but it just sets a boolean and the Pressurization itself happens on a seperate thread
            //Do this because the environment oxygen level will not change in the Cubegrid
            if (Math.Abs(altitude - m_altitude) > ALTITUDE_CHANGE_THRESHOLD)
            {
                if (controlledEntity.CubeGrid.GridSystems.GasSystem != null)
                {
                    controlledEntity.CubeGrid.GridSystems.GasSystem.Pressurize();
                    m_altitude = altitude;
                }
            }

            var altitudeText = new StringBuilder().AppendDecimal(altitude, 0).Append(" m");

            var altitudeVerticalOffset = 0.03f;
            var widthRatio = MyGuiManager.GetFullscreenRectangle().Width / MyGuiManager.GetSafeFullscreenRectangle().Width;
            var heightRatio = MyGuiManager.GetFullscreenRectangle().Height / MyGuiManager.GetSafeFullscreenRectangle().Height;
            var altitudePosition = new Vector2(MyHud.Crosshair.Position.X * widthRatio / MyGuiManager.GetHudSize().X/* - MyHud.Crosshair.HalfSize.X*m_textScale*/, MyHud.Crosshair.Position.Y * heightRatio / MyGuiManager.GetHudSize().Y + altitudeVerticalOffset);

            if (MyVideoSettingsManager.IsTripleHead())
                altitudePosition.X -= 1.0f;

            MyGuiManager.DrawString(altitudeFont, altitudeText, altitudePosition, m_textScale, drawAlign: altitudeAlignment, useFullClientArea : true);

            var planetSurfaceNormal = (controlledEntityCenterOfMass - nearestPlanet.WorldMatrix.Translation);
            planetSurfaceNormal.Normalize();

            var rotationMatrix = controlledEntity.WorldMatrix;
            rotationMatrix.Translation = Vector3D.Zero;
            rotationMatrix.Up = planetSurfaceNormal;
            rotationMatrix.Forward = Vector3D.Normalize(Vector3D.Reject(planetSurfaceNormal, controlledEntity.WorldMatrix.Forward));
            rotationMatrix.Left = rotationMatrix.Up.Cross(rotationMatrix.Forward);
            var planetSurfaceTangent = Vector3D.Normalize(Vector3D.Transform(Vector3D.Forward, rotationMatrix));

            double cosVerticalAngle = planetSurfaceNormal.Dot(controlledEntity.WorldMatrix.Forward);
            var scale = 0.75f;

            var horizonDefaultCenterPosition = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(MyHud.Crosshair.Position / MyGuiManager.GetHudSize() * new Vector2(MyGuiManager.GetSafeFullscreenRectangle().Width, MyGuiManager.GetSafeFullscreenRectangle().Height));
            var horizonAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            var horizonTexture = MyGuiConstants.TEXTURE_HUD_GRAVITY_HORIZON;
            var horizonSize = horizonTexture.SizeGui;

            float offsetLimit;
            if (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator)
                offsetLimit = 0.45f;
            else
                offsetLimit = 0.35f;
            float distanceFromCenter = (float)cosVerticalAngle * offsetLimit;

            float cosRotation = Vector3.Reject(planetSurfaceTangent, controlledEntity.WorldMatrix.Forward).Dot(controlledEntity.WorldMatrix.Up);
            float rotation = (float)Math.Acos(cosRotation);
            if (nearestPlanet.Components.Get<MyGravityProviderComponent>().GetWorldGravity(controlledEntityCenterOfMass).Dot(controlledEntity.WorldMatrix.Right) >= 0)
                rotation = 2.0f * (float)Math.PI - rotation;//roll, direction to the left,
            float sinRotation = (float)Math.Sin(rotation);
            Vector2 magicOffset = new Vector2(0.0145f, 0.0175f);	// DrawSpriteBatch with rotation needs this
            var horizonPosition = new Vector2(-sinRotation * distanceFromCenter,
                                              cosRotation * distanceFromCenter);
            horizonPosition += horizonDefaultCenterPosition + scale * horizonSize / 2.0f + magicOffset;

            MyGuiManager.DrawSpriteBatch(horizonTexture.Texture, horizonPosition, scale, Color.White, horizonAlignment, rotation, new Vector2(0.5f));
        }

        private void DrawObjectiveLine(MyHudObjectiveLine objective)
        {
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            var color = Color.AliceBlue;
            var basePos = new Vector2(0.45f, 0.01f);
            var offset = new Vector2(0f, 0.02f);

            var bgPos = ConvertHudToNormalizedGuiPosition(ref basePos);
            MyGuiManager.DrawString(MyFontEnum.Debug, new StringBuilder(objective.Title), bgPos, 1f, drawAlign: align, colorMask: color);

            basePos += offset;
            bgPos = ConvertHudToNormalizedGuiPosition(ref basePos);
            MyGuiManager.DrawString(MyFontEnum.Debug, new StringBuilder("- " + objective.CurrentObjective), bgPos, 1f, drawAlign: align);

        }

        private void DrawGravityVectorIndicator(Vector2 centerPos, Vector3 worldGravity, MyHudTexturesEnum texture, Color color)
        {
            float hudSizeX = MyGuiManager.GetSafeFullscreenRectangle().Width / MyGuiManager.GetHudSize().X;
            float hudSizeY = MyGuiManager.GetSafeFullscreenRectangle().Height / MyGuiManager.GetHudSize().Y;

            var textureCoord = GetTextureCoord(texture);
            var viewGravity = Vector3.TransformNormal(worldGravity, MySector.MainCamera.ViewMatrix);
            var viewGravityLen = viewGravity.Length();
            if (!MyUtils.IsZero(viewGravityLen))
                viewGravity /= viewGravityLen;

            var right = new Vector2(viewGravity.Y, viewGravity.X);
            var rightLen = right.Length();

            if (!MyUtils.IsZero(rightLen))
                right /= rightLen;

            var scale = Vector2.One * new Vector2(0.003f, 0.013f);
            scale.Y *= rightLen;

            VRageRender.MyRenderProxy.DrawSpriteAtlas(
                m_atlas,
                centerPos + new Vector2(viewGravity.X, -viewGravity.Y) * 0.02f,
                textureCoord.Offset,
                textureCoord.Size,
                right,
                new Vector2(hudSizeX, hudSizeY),
                color,
                scale);
        }

        private void DrawScenarioInfo(MyHudScenarioInfo scenarioInfo)
        {
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            Color color = Color.White;
            var basePos = new Vector2(0.99f, 0.01f);
            var bgPos = ConvertHudToNormalizedGuiPosition(ref basePos);
            Vector2 namePos, valuePos, bgScale;
            MyGuiPaddedTexture bg;

            bg = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT;
            bgScale = new Vector2(1.1f, 1f);
            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, new Vector2(bg.SizeGui.X * bgScale.X, scenarioInfo.Data.GetGuiHeight()), Color.White, align);

            namePos.X = bgPos.X - (bg.SizeGui.X - bg.PaddingSizeGui.X) * bgScale.X;
            namePos.Y = bgPos.Y + bg.PaddingSizeGui.Y * bgScale.Y;

            valuePos.X = bgPos.X - bgScale.X * bg.PaddingSizeGui.X;
            valuePos.Y = bgPos.Y + bgScale.Y * bg.PaddingSizeGui.Y;

            scenarioInfo.Data.DrawTopDown(namePos, valuePos, m_textScale);
        }

        private void DrawGpsMarkers(MyHudGpsMarkers gpsMarkers)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawGpsMarkers");

            m_tmpHudEntityParams.FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL;
            m_tmpHudEntityParams.IconColor = MyHudConstants.GPS_COLOR;
            m_tmpHudEntityParams.OffsetText = true;

            MySession.Static.Gpss.updateForHud();
            //gpsMarkers.Sort();//re-sort by distance from new camera coordinates
            foreach (var gps in gpsMarkers.MarkerEntities)
            {
                m_markerRender.AddGPS(gps.Coords, gps.Name, gps.AlwaysVisible, gps.GPSColor);
            }
            ProfilerShort.End();
        }

        private void DrawButtonPanelMarkers(MyHudGpsMarkers buttonPanelMarkers)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawGpsMarkers");
            foreach (var buttonPanel in buttonPanelMarkers.MarkerEntities)
            {
                m_markerRender.AddButtonMarker(buttonPanel.Coords, buttonPanel.Name);
            }
            ProfilerShort.End();
        }

        private void DrawOreMarkers(MyHudOreMarkers oreMarkers)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawOreMarkers");

            if (m_nearestOreDeposits == null || m_nearestOreDeposits.Length < MyDefinitionManager.Static.VoxelMaterialCount)
            {
                m_nearestOreDeposits = new OreDepositMarker[MyDefinitionManager.Static.VoxelMaterialCount];
                m_nearestDistanceSquared = new float[m_nearestOreDeposits.Length];
            }

            for (int i = 0; i < m_nearestOreDeposits.Length; i++)
            {
                m_nearestOreDeposits[i] = default(OreDepositMarker);
                m_nearestDistanceSquared[i] = float.MaxValue;
            }

            Vector3D controlledEntityPosition = Vector3D.Zero;
            if (MySession.Static != null && MySession.Static.ControlledEntity != null)
                controlledEntityPosition = (MySession.Static.ControlledEntity as MyEntity).WorldMatrix.Translation;

            foreach (MyEntityOreDeposit oreMarker in oreMarkers)
            {
                for (int i = 0; i < oreMarker.Materials.Count; i++)
                {
                    MyEntityOreDeposit.Data depositData = oreMarker.Materials[i];

                    var oreMaterial = depositData.Material;
                    Vector3D oreWorldPosition;
                    //ProfilerShort.Begin("ComputeWorldPosition");
                    depositData.ComputeWorldPosition(oreMarker.VoxelMap, out oreWorldPosition);

                    //ProfilerShort.BeginNextBlock("Distance");
                    Vector3D diff = (controlledEntityPosition - oreWorldPosition);
                    float distanceSquared = (float)diff.LengthSquared();

                    //ProfilerShort.BeginNextBlock("Use");
                    float nearestDistanceSquared = m_nearestDistanceSquared[oreMaterial.Index];
                    if (distanceSquared < nearestDistanceSquared)
                    {
                        m_nearestOreDeposits[oreMaterial.Index] = MyTuple.Create(oreWorldPosition, oreMarker);
                        m_nearestDistanceSquared[oreMaterial.Index] = distanceSquared;
                    }
                    //ProfilerShort.End();
                }
            }

            for (int i = 0; i < m_nearestOreDeposits.Length; i++)
            {
                var nearestOreDeposit = m_nearestOreDeposits[i];
                if (nearestOreDeposit.Item2 == null ||
                    nearestOreDeposit.Item2.VoxelMap == null ||
                    nearestOreDeposit.Item2.VoxelMap.Closed)
                    continue;

                MyVoxelMaterialDefinition voxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)i);
                string oreSubtype = voxelMaterial.MinedOre;

                m_markerRender.AddOre(nearestOreDeposit.Item1, oreSubtype);
            }

            ProfilerShort.End();
        }

        private void DrawCameraInfo(MyHudCameraInfo cameraInfo)
        {
            cameraInfo.Draw(m_cameraInfoMultilineControl);
        }

        private void DrawLargeTurretTargets(MyHudLargeTurretTargets largeTurretTargets)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawLargeTurretTargets");

            foreach (var target in largeTurretTargets.Targets)
            {
                MyHudEntityParams hudParams = target.Value;
                if (hudParams.ShouldDraw != null && !hudParams.ShouldDraw())
                    continue;

                m_markerRender.AddTarget(target.Key.PositionComp.WorldAABB.Center);
            }

            ProfilerShort.End();
        }

        private void DrawWorldBorderIndicator(MyHudWorldBorderChecker checker)
        {
            if (checker.WorldCenterHintVisible)
            {
                m_markerRender.AddPOI(Vector3D.Zero, MyHudWorldBorderChecker.HudEntityParams.Text, MyRelationsBetweenPlayerAndBlock.Enemies);
            }
        }

        private void DrawHackingMarkers(MyHudHackingMarkers hackingMarkers)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawHackingMarkers");

            try
            {
                hackingMarkers.UpdateMarkers();

                if (MySandboxGame.TotalTimeInMilliseconds % 200 > 100)
                    return;

                foreach (var entityMarker in hackingMarkers.MarkerEntities)
                {
                    MyEntity entity = entityMarker.Key;
                    MyHudEntityParams hudParams = entityMarker.Value;
                    if (hudParams.ShouldDraw != null && !hudParams.ShouldDraw())
                        continue;

                    m_markerRender.AddHacking(entity.LocationForHudMarker, hudParams.Text);
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void DrawPerformanceWarning()
        {
            var bgPos = new Vector2(0.01f, 0.3f);
            bgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
            var bg = MyGuiConstants.TEXTURE_HUD_BG_PERFORMANCE;
            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, bg.SizeGui, Color.White, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            StringBuilder sb = new StringBuilder();
            MyGuiManager.DrawString(MyFontEnum.White, sb.AppendFormat(MyCommonTexts.PerformanceWarningHeading, MyGuiSandbox.GetKeyName(MyControlsSpace.HELP_SCREEN)), bgPos + new Vector2(0.02f, 0f), 1f, drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (MyHud.VoiceChat.Visible)
                MyHud.VoiceChat.Hide();
        }

        #region NetGraph

        private void DrawNetgraph(MyHudNetgraph netgraph)
        {
            //Vector2 bgPos = MyGuiConstants.NETGRAPH_INITIAL_POSITION;
            Vector2 bgPos = new Vector2(1.0f);


            var texture = MyGuiConstants.NETGRAPH_BG_TEXTURE;
            Vector2 normBgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
            Vector2 normSizeBg = new Vector2(1.3f);// MyGuiManager.GetNormalizedSizeFromScreenSize(texture.SizePx);
            
            //normSizeBg.Y = MyGuiConstants.NETGRAPH_BG_NORM_SIZE_Y;
            normSizeBg.Y = 1;


            Vector2 screenOptimalBarSize = MyGuiManager.GetScreenSizeFromNormalizedSize(MyHudNetgraph.OPTIMAL_LENGTH_BAR_NORMALIZED);
            Vector2 screenOptimalBarSizeMulitplier = screenOptimalBarSize * netgraph.CurrentPacketScaleInvertedMaximumValue;
            float averageGraphSizeMultiplier = screenOptimalBarSize.Y * netgraph.CurrentAverageScaleInvertedMaximumValue;
            Vector2 scalePosition = normBgPos;
            scalePosition.X -= normSizeBg.X - 0.035f;
            scalePosition.Y -= normSizeBg.Y * (0.09f / MyGuiConstants.NETGRAPH_BG_NORM_SIZE_Y);

            ProfilerShort.Begin("Draw scales");
            // draw left scale (packet size)
            float maximumValue = netgraph.CurrentPacketScaleMaximumValue;
            StringBuilder unitForScale = new StringBuilder();
            netgraph.GetProperFormatAndValueForBytes(netgraph.CurrentPacketScaleMaximumValue, out maximumValue, unitForScale);
            DrawNetgraphScaleForPacketScale(scalePosition, (int)screenOptimalBarSize.Y,
                maximumValue, 5, unitForScale, true, true, 0.7f, MyFontEnum.White);

            // draw right scale (average)
            Vector2 averageScalePosition = scalePosition;
            averageScalePosition.X += (normSizeBg.X - 0.06f);
            unitForScale.Clear();
            netgraph.GetProperFormatAndValueForBytes(netgraph.CurrentAverageScaleMaximumValue, out maximumValue, unitForScale, true);
            DrawNetgraphScaleForPacketScale(averageScalePosition, (int)screenOptimalBarSize.Y,
                 maximumValue, 5, unitForScale, false, false, 0.7f, MyFontEnum.Red);

            ProfilerShort.End();

            Vector2 lineNormalizedPosition = scalePosition;
            lineNormalizedPosition.X += 0.014f;
            Vector2 linePosition = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(lineNormalizedPosition);
            Vector2 cachedLinePosition = linePosition;

            ProfilerShort.Begin("Draw netgraph bars");
            // draw netgraph bars
            for (int i = netgraph.CurrentFirstIndex; i < MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS - 1; i++)
            {
                DrawNetgraphLine(netgraph.GetNetgraphLineDataAtIndex(i), netgraph.GetNetgraphLineDataAtIndex(i+1), ref linePosition, ref screenOptimalBarSizeMulitplier);
                linePosition.X++;
            }
            for (int i = 0; i < netgraph.CurrentFirstIndex - 1; i++)
            {
                DrawNetgraphLine(netgraph.GetNetgraphLineDataAtIndex(i), netgraph.GetNetgraphLineDataAtIndex(i+1), ref linePosition, ref screenOptimalBarSizeMulitplier);
                linePosition.X++;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Draw average line");
            // draw average line
            linePosition = cachedLinePosition;
            float averageReceivedOnFirstLine = netgraph.GetNetgraphLineDataAtIndex(netgraph.CurrentFirstIndex).AverageReceivedOnThisLine;
            float averageSentOnFirstLine = netgraph.GetNetgraphLineDataAtIndex(netgraph.CurrentFirstIndex).AverageSentOnThisLine;
            linePosition.X++;
            for (int i = netgraph.CurrentFirstIndex + 1; i < MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS; i++)
            {
                float averageReceivedOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageReceivedOnThisLine;
                float averageSentOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageSentOnThisLine;
                DrawNetgraphAverageLine(ref linePosition, averageReceivedOnFirstLine, averageReceivedOnSecondLine, averageGraphSizeMultiplier, Color.Red);
                DrawNetgraphAverageLine(ref linePosition, averageSentOnFirstLine, averageSentOnSecondLine, averageGraphSizeMultiplier, Color.Yellow);
                linePosition.X++;
                averageReceivedOnFirstLine = averageReceivedOnSecondLine;
                averageSentOnFirstLine = averageSentOnSecondLine;
            }
            for (int i = 0; i < netgraph.CurrentFirstIndex; i++)
            {
                float averageReceivedOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageReceivedOnThisLine;
                float averageSentOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageSentOnThisLine;
                DrawNetgraphAverageLine(ref linePosition, averageReceivedOnFirstLine, averageReceivedOnSecondLine, averageGraphSizeMultiplier, Color.Red);
                DrawNetgraphAverageLine(ref linePosition, averageSentOnFirstLine, averageSentOnSecondLine, averageGraphSizeMultiplier, Color.Yellow);
                linePosition.X++;
                averageReceivedOnFirstLine = averageReceivedOnSecondLine;
                averageSentOnFirstLine = averageSentOnSecondLine;
            }
            ProfilerShort.End();
            // draw netgraph status data
            Vector2 textPosition = lineNormalizedPosition;
            textPosition.X = scalePosition.X - 0.02f;
            textPosition.Y += normSizeBg.Y * (0.012f / MyGuiConstants.NETGRAPH_BG_NORM_SIZE_Y); // 0.012f;
            DrawNetgraphBasicStrings(netgraph, textPosition, new Vector2(0.06f, 0.02f));

            // draw bars legend
            textPosition = scalePosition;
            textPosition.X -= 0.022f;
            textPosition.Y -= 0.2f;
            m_helperSB.Clear().Append("Received");
            Color tmp = MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR;
            tmp.A = 255;
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            textPosition.Y += 0.02f;
            //m_helperSB.Clear().Append("Unreliable");
            //tmp = MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR;
            //tmp.A = 255;
            //MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            //textPosition.Y += 0.02f;
            m_helperSB.Clear().Append("Sent");
            //tmp = MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR;
            tmp = Color.CadetBlue;
            tmp.A = 255;
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            textPosition.Y += 0.05f;
            m_helperSB.Clear().Append("Avg in");
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, Color.Red, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            textPosition.Y += 0.02f;
            m_helperSB.Clear().Append("Avg out");
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
        }

        private void DrawNetgraphLine(MyHudNetgraph.NetgraphLineData lineData1, MyHudNetgraph.NetgraphLineData lineData2, ref Vector2 position, ref Vector2 size)
        {
            Vector2I offset = new Vector2I(0, 0);
            Vector2I positionI = new Vector2I((int)position.X, (int)position.Y);

            DrawNetgraphLineConnected(positionI.X, ref positionI.Y, lineData1.ByteCountReliableReceived, lineData2.ByteCountReliableReceived, size.Y,
                MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR, MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR_TOP);


            //DrawNetgraphLineBar(positionI.X, ref positionI.Y, lineData.ByteCountUnreliableReceived, size.Y,
            //     MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR, MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR_TOP);

            DrawNetgraphLineConnected(positionI.X, ref positionI.Y, lineData1.ByteCountSent, lineData2.ByteCountSent, size.Y,
                 MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR, MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR_TOP);
        }

        private void DrawNetgraphLineConnected(int positionX, ref int positionY, long value, long value2, float sizeMultiplier, Color colorLine, Color colorTop)
        {
            float offset1 = value * sizeMultiplier;
            float offset2 = value2 * sizeMultiplier;
            int offsetI1 = (int)Math.Ceiling(offset1);
            int offsetI2 = (int)Math.Ceiling(offset2);

            VRageRender.MyRenderProxy.DebugDrawLine2D(
                new Vector2(positionX, positionY - offsetI1),
                new Vector2(positionX + 1, positionY - offsetI2),
                colorLine,
                colorLine);

            // small dots above each bar
            //if (offsetI > 0)
            //{
            //MyGuiManager.DrawSpriteBatch(MyGuiConstants.NETGRAPH_BG_TEXTURE.Texture,
            //    positionX,
            //    (positionY - offsetI - 1),
            //    1,
            //    1,
            //    top);

            //VRageRender.MyRenderProxy.DebugDrawLine2D(
            //    new Vector2(positionX, positionY - offsetI),
            //    new Vector2(positionX, positionY - offsetI - 1),
            //    colorTop,
            //    colorTop);
            //}
            //positionY -= (offsetI);
        }
        private void DrawNetgraphLineBar(int positionX, ref int positionY, long value, float sizeMultiplier, Color colorLine, Color colorTop)
        {
            float offset = value * sizeMultiplier;
            int offsetI = (int)Math.Ceiling(offset);

            VRageRender.MyRenderProxy.DebugDrawLine2D(
                new Vector2(positionX, positionY),
                new Vector2(positionX, positionY - offsetI),
                colorLine,
                colorLine);

            // small dots above each bar
            //if (offsetI > 0)
            //{
            //MyGuiManager.DrawSpriteBatch(MyGuiConstants.NETGRAPH_BG_TEXTURE.Texture,
            //    positionX,
            //    (positionY - offsetI - 1),
            //    1,
            //    1,
            //    top);

            //VRageRender.MyRenderProxy.DebugDrawLine2D(
            //    new Vector2(positionX, positionY - offsetI),
            //    new Vector2(positionX, positionY - offsetI - 1),
            //    colorTop,
            //    colorTop);
            //}
            positionY -= (offsetI);
        }

        private void DrawNetgraphAverageLine(ref Vector2 position, float average1, float average2, float sizeMultiplier, Color color)
        {
            if (average1 != 0 || average2 != 0)
            {
                Vector2 pointFrom = new Vector2(position.X - 1, position.Y - average1 * sizeMultiplier);
                Vector2 pointTo = new Vector2(position.X, position.Y - average2 * sizeMultiplier);
                VRageRender.MyRenderProxy.DebugDrawLine2D(pointFrom, pointTo, color, color);
            }
        }

        private void DrawNetgraphScaleForPacketScale(Vector2 position, int optimalLengthOfBarInPx, float optimalDataSizeOfBarInBytes, int stepCount, StringBuilder unitForScale, bool showIntervals, bool alignToRight = true, float textScale = 0.7f, string fontType = MyFontEnum.White)
        {
            int step = optimalLengthOfBarInPx / stepCount;
            float stepValue = (float)(Math.Truncate((optimalDataSizeOfBarInBytes / stepCount) * 100.0) * 0.01f);
            float totalStepValue = 0;
            Vector2 vecStep = new Vector2(0, step);
            vecStep = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(vecStep);
            vecStep.X = 0;

            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            if (alignToRight)
                align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;

            m_helperSB.Clear();
            for (int i = 0; i < stepCount; i++)
            {
                Vector2 oldPosition = position;
                position -= vecStep;

                // small intervals next to the scale
                //if (showIntervals)
                //{
                //float intervalOffset = vecStep.Y / MyGuiConstants.NETGRAPH_SMALL_INTERVAL_COUNT;
                //for (int j = 1; j <= MyGuiConstants.NETGRAPH_SMALL_INTERVAL_COUNT; j++)
                //{
                //    Vector2 intervalMarkPosition = new Vector2(oldPosition.X + 0.005f, oldPosition.Y - j * intervalOffset);
                //    bool isLast = j == MyGuiConstants.NETGRAPH_SMALL_INTERVAL_COUNT;
                //    Vector2 intervalMarkEndPosition;
                //    Color intervalColor;
                //    if (isLast)
                //    {
                //        intervalMarkEndPosition = new Vector2(0.008f, 0.002f);
                //        intervalColor = MyGuiConstants.NETGRAPH_PACKET_SCALE_INTERVAL_POINT_COLOR;
                //    }
                //    else
                //    {
                //        intervalMarkEndPosition = new Vector2(0.005f, 0.002f);
                //        intervalColor = MyGuiConstants.NETGRAPH_PACKET_SCALE_SMALL_INTERVAL_COLOR;
                //    }
                //    MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE, intervalMarkPosition, intervalMarkEndPosition, MyGuiConstants.NETGRAPH_PACKET_SCALE_SMALL_INTERVAL_COLOR, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                //}
                //}

                totalStepValue += stepValue;
                m_helperSB.Clear();
                m_helperSB.Append(totalStepValue);
                MyGuiManager.DrawString(fontType, m_helperSB, position, textScale, null, align);
            }

            if (unitForScale.Length != 0)
            {
                position.Y -= 0.02f;
                MyGuiManager.DrawString(fontType, unitForScale, position, textScale, null, align);
            }
        }

        private void DrawNetgraphScaleForAverageScale(Vector2 position, int optimalLengthOfBarInPx, float maximumValueOfScale, int stepCount, bool alignToRight = true, float textScale = 0.7f, string fontType = MyFontEnum.White)
        {
            int step = optimalLengthOfBarInPx / stepCount;
            float stepValue = (maximumValueOfScale) / stepCount;
            float totalStepValue = 0;
            Vector2 vecStep = new Vector2(0, step);
            vecStep = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(vecStep);
            vecStep.X = 0;

            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            if (alignToRight)
                align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;

            m_helperSB.Clear();
            for (int i = 0; i < stepCount; i++)
            {
                position -= vecStep;
                totalStepValue += stepValue;
                m_helperSB.Clear();
                m_helperSB.Append(totalStepValue);
                MyGuiManager.DrawString(fontType, m_helperSB, position, textScale, null, align);
            }

            m_helperSB.Clear().Append("[kB]");
            position.Y -= 0.02f;
            MyGuiManager.DrawString(fontType, m_helperSB, position, textScale, null, align);
        }

        private void DrawNetgraphBasicStrings(MyHudNetgraph netgraph, Vector2 initialPosition, Vector2 normalizedOffset)
        {
            StringBuilder sb = new StringBuilder();

            // Draw last received packet         
            sb.Append("In: ").Append(netgraph.LastPacketBytesReceived);
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw average in
            sb.Clear();
            sb.Append("Avg In: ").Append(netgraph.AverageIncomingKBytes.ToString("0.##")).Append(" kB/s");
            initialPosition.X += normalizedOffset.X;
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw last sent packet
            sb.Clear();
            sb.Append("Out: ").Append(netgraph.LastPacketBytesSent);
            initialPosition.X -= normalizedOffset.X;
            initialPosition.Y += normalizedOffset.Y;
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw average out
            sb.Clear();
            sb.Append("Avg Out: ").Append(netgraph.AverageOutgoingKBytes.ToString("0.##")).Append(" kB/s");
            initialPosition.X += normalizedOffset.X;
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw updates per second
            sb.Clear();
            sb.Append("UPS: ").Append(netgraph.UpdatesPerSecond);
            initialPosition.X -= normalizedOffset.X;
            initialPosition.Y += normalizedOffset.Y;
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw frames per second
            sb.Clear();
            sb.Append("FPS: ").Append(netgraph.FramesPerSecond);
            initialPosition.X += normalizedOffset.X;
            DrawNetgraphStringValue(initialPosition, sb);

            // Draw latency
            sb.Clear();
            sb.Append("Ping: ").Append(netgraph.Ping);
            initialPosition.X -= normalizedOffset.X;
            initialPosition.Y += normalizedOffset.Y;
            DrawNetgraphStringValue(initialPosition, sb);
        }

        private void DrawNetgraphStringValue(Vector2 position, StringBuilder sb, float scale = 0.6f)
        {
            MyGuiManager.DrawString(MyFontEnum.White, sb, position, scale, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
        }

        #endregion
    }
}