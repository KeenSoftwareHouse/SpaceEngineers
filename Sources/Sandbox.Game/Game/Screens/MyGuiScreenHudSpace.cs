using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
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
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using Color = VRageMath.Color;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using OreDepositMarker = VRage.MyTuple<VRageMath.Vector3D, Sandbox.Game.Entities.Cube.MyEntityOreDeposit>;
using Vector2 = VRageMath.Vector2;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenHudSpace : MyGuiScreenHudBase
    {
        private MyGuiControlToolbar m_toolbarControl;
        private MyGuiControlBlockInfo m_blockInfo;
        private MyGuiControlRotatingWheel m_rotatingWheelControl;
        private MyGuiControlMultilineText m_cameraInfoMultilineControl;

        private MyGuiControlLabel m_buildModeLabel;

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
        private MyGuiControlLabel m_relayNotification;

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
				RequiredLabelText = MySpaceTexts.HudBlockInfo_Required,
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

            m_chatControl = new MyHudControlChat(MyHud.Chat, Vector2.Zero, new Vector2(0.4f, 0.25f));
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
                text: MyTexts.GetString(MySpaceTexts.Hud_BuildMode));
            Controls.Add(m_buildModeLabel);

            m_relayNotification = new MyGuiControlLabel(new Vector2(1, 0), font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_relayNotification.TextEnum = MySpaceTexts.Multiplayer_IndirectConnection;
            m_relayNotification.Visible = false;
            Controls.Add(m_relayNotification);
            var offset = new Vector2(0, m_relayNotification.Size.Y);
            m_noMsgSentNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset,font: MyFontEnum.Debug, text: MyTexts.GetString(MySpaceTexts.Multiplayer_LastMsg), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_noMsgSentNotification.Visible = false;
            Controls.Add(m_noMsgSentNotification);
            offset += new Vector2(0, m_noMsgSentNotification.Size.Y);
            m_noConnectionNotification = new MyGuiControlLabel(new Vector2(1, 0) + offset, font: MyFontEnum.Red , originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_noConnectionNotification.TextEnum = MySpaceTexts.Multiplayer_NoConnection;
            m_noConnectionNotification.Visible = false;
            Controls.Add(m_noConnectionNotification);
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

            m_toolbarControl.Visible = !MyHud.MinimalHud;
            
            Vector2 position = new Vector2(0.99f, 0.8f);
            position = ConvertHudToNormalizedGuiPosition(ref position);
            if (MyVideoSettingsManager.IsTripleHead())
                position.X += 1.0f;

            // TODO: refactor this
            m_blockInfo.Visible = MyHud.BlockInfo.Visible && !MyHud.MinimalHud;
            m_blockInfo.BlockInfo = m_blockInfo.Visible ? MyHud.BlockInfo : null;
            m_blockInfo.Position = position;
            m_blockInfo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;

            m_rotatingWheelControl.Visible = MyHud.RotatingWheelVisible && !MyHud.MinimalHud;

            if (!base.Draw())
                return false;

            var bgPos = new Vector2(0.01f, 0.85f);
            bgPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref bgPos);
            m_chatControl.Position = bgPos + new Vector2(0.15f, 0);
            m_chatControl.TextScale = 0.9f;

            bgPos = new Vector2(0.03f, 0.1f);
            bgPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref bgPos);
            m_cameraInfoMultilineControl.Position = bgPos;
            m_cameraInfoMultilineControl.TextScale = 0.9f;

            if (MyHud.Crosshair.Visible && MySandboxGame.Config.ShowCrosshair)
            {
                DrawCrosshair(m_atlas, GetTextureCoord(MyHud.Crosshair.TextureEnum), MyHud.Crosshair);
            }

            MyHud.Notifications.Draw();

            m_buildModeLabel.Visible = !MyHud.MinimalHud && MyHud.IsBuildMode;

            if (MyHud.ShipInfo.Visible && !MyHud.MinimalHud)
                DrawShipInfo(MyHud.ShipInfo);

            if (MyHud.CharacterInfo.Visible && !MyHud.MinimalHud)
                DrawSuitInfo(MyHud.CharacterInfo);

            if (MyHud.ObjectiveLine.Visible && !MyHud.MinimalHud && MyFakes.ENABLE_OBJECTIVE_LINE)
                DrawObjectiveLine(MyHud.ObjectiveLine);

            MyHud.BlockInfo.Visible = false;
            m_blockInfo.BlockInfo = null;

            if (MyHud.GravityIndicator.Visible && !MyHud.MinimalHud)
                DrawGravityIndicator(MyHud.GravityIndicator, MyHud.CharacterInfo);

            if (MyHud.ConsumerGroupInfo.Visible && !MyHud.MinimalHud)
                DrawPowerGroupInfo(MyHud.ConsumerGroupInfo);

            if (MyHud.SelectedObjectHighlight.Visible && MyFakes.ENABLE_USE_OBJECT_HIGHLIGHT)
                DrawSelectedObjectHighlight(m_atlas, GetTextureCoord(MyHudTexturesEnum.corner), MyHud.SelectedObjectHighlight);

            if (MyHud.LocationMarkers.Visible && !MyHud.MinimalHud)
                m_markerRender.DrawLocationMarkers(MyHud.LocationMarkers);

            if (MyHud.GpsMarkers.Visible && !MyHud.MinimalHud && MyFakes.ENABLE_GPS)
                DrawGpsMarkers(MyHud.GpsMarkers);

            if (MyHud.ButtonPanelMarkers.Visible && !MyHud.MinimalHud)
                DrawButtonPanelMarkers(MyHud.ButtonPanelMarkers);

            if (MyHud.OreMarkers.Visible && !MyHud.MinimalHud)
                DrawOreMarkers(MyHud.OreMarkers);

            if (MyHud.LargeTurretTargets.Visible && !MyHud.MinimalHud)
                DrawLargeTurretTargets(MyHud.LargeTurretTargets);

            if (!MyHud.MinimalHud)
                DrawWorldBorderIndicator(MyHud.WorldBorderChecker);

            if (MyHud.HackingMarkers.Visible && !MyHud.MinimalHud)
                DrawHackingMarkers(MyHud.HackingMarkers);

            //m_chatControl.Visible = !MyHud.MinimalHud;

            if (!MyHud.MinimalHud)
                DrawCameraInfo(MyHud.CameraInfo);

            ProfilerShort.Begin("Draw netgraph");
            if (MyFakes.ENABLE_NETGRAPH && MyHud.IsNetgraphVisible)
                DrawNetgraph(MyHud.Netgraph);
            ProfilerShort.End();
            //if (Sync.MultiplayerActive)
            DrawMultiplayerNotifications();

            if (!MyHud.MinimalHud && MyHud.VoiceChat.Visible)
                DrawVoiceChat(MyHud.VoiceChat);

            if (MyHud.ScenarioInfo.Visible && !MyHud.MinimalHud)
                DrawScenarioInfo(MyHud.ScenarioInfo);

            return true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenHudSpace";
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
                m_noConnectionNotification.Visible = true;
            }
            else
                m_noConnectionNotification.Visible = false;
            if (!MySession.Static.MultiplayerDirect)
            {
                m_relayNotification.Visible = true;
            }
            else
                m_relayNotification.Visible = false;
        }

        private void DrawGravityIndicator(MyHudGravityIndicator indicator, MyHudCharacterInfo characterInfo)
        {
            if (indicator.Entity == null)
                return;

            Vector3 gravity = MyGravityProviderSystem.CalculateGravityInPoint(MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator ? indicator.Entity.PositionComp.WorldAABB.Center : MySpectatorCameraController.Static.Position);
            //gravity += MyPhysics.HavokWorld.Gravity;
            bool anyGravity = !MyUtils.IsZero(gravity.Length());

            // Background and text drawing
            MyFontEnum font;
            StringBuilder text;
            m_hudIndicatorText.Clear();
            m_hudIndicatorText.AppendFormatedDecimal("", gravity.Length() / 9.81f, 1, " g");
            MyGuiPaddedTexture bg;
            if (anyGravity)
            {
                font = MyFontEnum.Blue;
                text = MyTexts.Get(MySpaceTexts.HudInfoGravity);
                bg = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT;
            }
            else
            {
                font = MyFontEnum.Red;
                text = MyTexts.Get(MySpaceTexts.HudInfoNoGravity);
                bg = MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_RED;
            }

            bool drawOxygen = MySession.Static.Settings.EnableOxygen;
            Vector2 bgSizeDelta = new Vector2(0.015f, 0f);
            float oxygenLevel = 0f;

            Vector2 bgSize = bg.SizeGui + bgSizeDelta;

            Vector2 bgPos, textPos, gTextPos, position;
            MyGuiDrawAlignEnum align;
            if (indicator.Entity is MyCharacter)
            {
                bgPos = new Vector2(0.99f, 0.99f);
                bgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
                textPos = bgPos - bgSize * new Vector2(0.94f, 0.98f) + bg.PaddingSizeGui * Vector2.UnitY * 0.2f;
                gTextPos = bgPos - bgSize * new Vector2(0.56f, 0.98f) + bg.PaddingSizeGui * Vector2.UnitY * 0.2f;
                align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
                position = bgPos - bgSize * new Vector2(0.5f, 0.5f) + bg.PaddingSizeGui * Vector2.UnitY * 0.5f;

                oxygenLevel = (indicator.Entity as MyCharacter).EnvironmentOxygenLevel;
            }
            else
            {
                bgPos = new Vector2(0.01f, 1f - (characterInfo.Data.GetGuiHeight() + 0.02f));
                bgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
                textPos = bgPos + bgSize * new Vector2(1 - 0.94f, -0.98f) + bg.PaddingSizeGui * Vector2.UnitY * 0.2f;
                gTextPos = bgPos + bgSize * new Vector2(1 - 0.56f, -0.98f) + bg.PaddingSizeGui * Vector2.UnitY * 0.2f;
                align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                position = bgPos - bgSize * new Vector2(-0.5f, 0.5f) + bg.PaddingSizeGui * Vector2.UnitY * 0.5f;

                var cockpit = indicator.Entity as MyCockpit;
                if (cockpit != null && cockpit.Pilot != null)
                {
                    oxygenLevel = cockpit.Pilot.EnvironmentOxygenLevel;
                }
                else
                {
                    drawOxygen = false;
                }
            }

            if (drawOxygen)
            {
                bgSizeDelta += new Vector2(0f, 0.025f);
            }

            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, bg.SizeGui + bgSizeDelta, Color.White, align);

            MyGuiManager.DrawString(font, text, textPos, m_textScale);

            if (drawOxygen)
            {
                var oxygenFont = MyFontEnum.Blue;
                var oxygenText = new StringBuilder("Oxygen: ");
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
                
                MyGuiManager.DrawString(oxygenFont, oxygenText, textPos - new Vector2(0f, 0.025f), m_textScale);
            }

            if (anyGravity)
                MyGuiManager.DrawString(MyFontEnum.White, m_hudIndicatorText, gTextPos, m_textScale);

            position = MyGuiManager.GetHudSize() * ConvertNormalizedGuiToHud(ref position);
            if (MyVideoSettingsManager.IsTripleHead())
                position.X += 1.0f;

            // Draw each of gravity indicators.
            foreach (var generatorGravity in MyGravityProviderSystem.GravityVectors)
                DrawGravityVectorIndicator(position, generatorGravity, MyHudTexturesEnum.gravity_arrow, Color.Gray);

            //if (MyPhysics.HavokWorld.Gravity != Vector3.Zero)
            //    DrawGravityVectorIndicator(position, MyPhysics.HavokWorld.Gravity, MyHudTexturesEnum.gravity_arrow, Color.Gray);

            if (anyGravity)
                DrawGravityVectorIndicator(position, gravity, MyHudTexturesEnum.gravity_arrow, Color.White);

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
                position,
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

        private void DrawPowerGroupInfo(MyHudConsumerGroupInfo info)
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
            MyGuiManager.DrawSpriteBatch(bg.Texture, bgPos, new Vector2(bg.SizeGui.X * bgScale.X, suitInfo.Data.GetGuiHeight()), Color.White, align);

            namePos = bgPos + new Vector2(1f, -1f) * bg.PaddingSizeGui * bgScale;
            valuePos = bgPos + bgScale * (new Vector2(bg.SizeGui.X, 0f) - bg.PaddingSizeGui);

            suitInfo.Data.DrawBottomUp(namePos, valuePos, m_textScale);
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

            namePos.X = bgPos.X - (bg.SizeGui.X-bg.PaddingSizeGui.X) * bgScale.X;
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
            gpsMarkers.Sort();//re-sort by distance from new camera coordinates
            foreach (var gps in gpsMarkers.MarkerEntities)
            {
                m_tmpHudEntityParams.Text.Clear().Append(gps.Name);//reuse single instance to reduce overhead
                m_markerRender.DrawLocationMarker(
                    m_gpsHudMarkerStyle,
                    gps.Coords,
                    m_tmpHudEntityParams,
                    0, 0);
            }
            DrawTexts();
            ProfilerShort.End();
        }

        private void DrawButtonPanelMarkers(MyHudGpsMarkers buttonPanelMarkers)
        {
            ProfilerShort.Begin("MyGuiScreenHud.DrawGpsMarkers");
            foreach (var buttonPanel in buttonPanelMarkers.MarkerEntities)
            {

                m_tmpHudEntityParams.FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT;
                m_tmpHudEntityParams.IconColor = MyHudConstants.GPS_COLOR;
                m_tmpHudEntityParams.OffsetText = true;

                m_tmpHudEntityParams.Text.Clear().Append(buttonPanel.Name);//reuse single instance to reduce overhead
                m_markerRender.DrawLocationMarker(
                    m_buttonPanelHudMarkerStyle,
                    buttonPanel.Coords,
                    m_tmpHudEntityParams,
                    0, 0);
            }
            DrawTexts();
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

            foreach (var oreMarker in oreMarkers)
            {
                bool debugBoxDrawn = false;
                foreach (var depositData in oreMarker.Materials)
                {
                    var oreMaterial = depositData.Material;
                    Vector3D oreWorldPosition;
                    depositData.ComputeWorldPosition(oreMarker.VoxelMap, out oreWorldPosition);

                    var distanceSquared = Vector3.DistanceSquared((Vector3)oreWorldPosition, (Vector3)((MyEntity)MySession.ControlledEntity).WorldMatrix.Translation);
                    float nearestDistanceSquared = m_nearestDistanceSquared[oreMaterial.Index];
                    if (distanceSquared < nearestDistanceSquared)
                    {
                        m_nearestOreDeposits[oreMaterial.Index] = MyTuple.Create(oreWorldPosition, oreMarker);
                        m_nearestDistanceSquared[oreMaterial.Index] = distanceSquared;
                    }

                    if (false && !debugBoxDrawn)
                    {
                        const int shift = MyOreDetectorComponent.CELL_SIZE_IN_VOXELS_BITS + MyOreDetectorComponent.QUERY_LOD;
                        var worldPosition = oreWorldPosition;
                        Vector3I cellCoord;
                        MyVoxelCoordSystems.WorldPositionToVoxelCoord(oreMarker.VoxelMap.PositionLeftBottomCorner, ref worldPosition, out cellCoord);
                        cellCoord >>= shift;
                        worldPosition = cellCoord * MyOreDetectorComponent.CELL_SIZE_IN_METERS + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
                        worldPosition += oreMarker.VoxelMap.PositionLeftBottomCorner;
                        var bbox = new BoundingBoxD(worldPosition, worldPosition + MyOreDetectorComponent.CELL_SIZE_IN_METERS);

                        VRageRender.MyRenderProxy.DebugDrawAABB(bbox, Vector3.One, 1f, 1f, false);
                        debugBoxDrawn = true;
                    }
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

                var hudParams = new MyHudEntityParams()
                {
                    FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL,
                    Text = new StringBuilder(oreSubtype),
                    OffsetText = true,
                    Icon = MyHudTexturesEnum.HudOre,
                    IconSize = new Vector2(0.02f, 0.02f)
                };

                m_markerRender.DrawLocationMarker(
                    m_oreHudMarkerStyle,
                    nearestOreDeposit.Item1,
                    hudParams,
                    0, 0);
            }

            DrawTexts();

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

                m_markerRender.DrawLocationMarker(
                    m_markerRender.GetStyleForRelation(MyRelationsBetweenPlayerAndBlock.Enemies),
                    target.Key.PositionComp.WorldAABB.Center,
                    hudParams,
                    0,
                    0
                );
            }
            
            ProfilerShort.End();
        }

        private void DrawWorldBorderIndicator(MyHudWorldBorderChecker checker)
        {
            if (checker.WorldCenterHintVisible)
            {
                m_markerRender.DrawLocationMarker(
                    m_markerRender.GetStyleForRelation(MyRelationsBetweenPlayerAndBlock.Enemies),
                    Vector3.Zero,
                    MyHudWorldBorderChecker.HudEntityParams,
                    0.0f,
                    1.0f
                );
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

                    var hudParams2 = hudParams;
                    //hudParams2.Text = new StringBuilder("sdsdff");
                    Vector3 position = Vector3.Transform(hudParams2.RelativePosition, (Matrix)hudParams2.Parent.WorldMatrix);

                    m_markerRender.DrawLocationMarker(
                        m_markerRender.GetStyleForRelation(hudParams.TargetMode),
                        (Vector3)entity.LocationForHudMarker,
                        hudParams2,
                        0, 0);
                }

                DrawTexts();
            }
            finally
            {
                ProfilerShort.End();
            }
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
            Vector2 bgPos = MyGuiConstants.NETGRAPH_INITIAL_POSITION;
            var texture = MyGuiConstants.NETGRAPH_BG_TEXTURE;
            Vector2 normBgPos = ConvertHudToNormalizedGuiPosition(ref bgPos);
            Vector2 normSizeBg = MyGuiManager.GetNormalizedSizeFromScreenSize(texture.SizePx);
            normSizeBg.Y = MyGuiConstants.NETGRAPH_BG_NORM_SIZE_Y;


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
            for (int i = netgraph.CurrentFirstIndex; i < MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS; i++)
            {
                DrawNetgraphLine(netgraph.GetNetgraphLineDataAtIndex(i), ref linePosition, ref screenOptimalBarSizeMulitplier);
                linePosition.X++;
            }
            for (int i = 0; i < netgraph.CurrentFirstIndex; i++)
            {
                DrawNetgraphLine(netgraph.GetNetgraphLineDataAtIndex(i), ref linePosition, ref screenOptimalBarSizeMulitplier);
                linePosition.X++;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Draw average line");
            // draw average line
            linePosition = cachedLinePosition;
            float averageOnFirstLine = netgraph.GetNetgraphLineDataAtIndex(netgraph.CurrentFirstIndex).AverageOnThisLine;
            linePosition.X++;
            for (int i = netgraph.CurrentFirstIndex + 1; i < MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS; i++)
            {
                float averageOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageOnThisLine;
                DrawNetgraphAverageLine(ref linePosition, averageOnFirstLine, averageOnSecondLine, averageGraphSizeMultiplier, Color.Red);
                linePosition.X++;
                averageOnFirstLine = averageOnSecondLine;
            }
            for (int i = 0; i < netgraph.CurrentFirstIndex; i++)
            {
                float averageOnSecondLine = netgraph.GetNetgraphLineDataAtIndex(i).AverageOnThisLine;
                DrawNetgraphAverageLine(ref linePosition, averageOnFirstLine, averageOnSecondLine, averageGraphSizeMultiplier, Color.Red);
                linePosition.X++;
                averageOnFirstLine = averageOnSecondLine;
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
            m_helperSB.Clear().Append("Reliable");
            Color tmp = MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR;
            tmp.A = 255;
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            textPosition.Y += 0.02f;
            m_helperSB.Clear().Append("Unreliable");
            tmp = MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR;
            tmp.A = 255;
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            textPosition.Y += 0.02f;
            m_helperSB.Clear().Append("Sent");
            tmp = MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR;
            tmp.A = 255;
            MyGuiManager.DrawString(MyFontEnum.White, m_helperSB, textPosition, 0.7f, tmp, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
        }

        private void DrawNetgraphLine(MyHudNetgraph.NetgraphLineData lineData, ref Vector2 position, ref Vector2 size)
        {
            Vector2I offset = new Vector2I(0, 0);
            Vector2I positionI = new Vector2I((int)position.X, (int)position.Y);

            DrawNetgraphLineBar(positionI.X, ref positionI.Y, lineData.ByteCountReliableReceived, size.Y,
                MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR, MyGuiConstants.NETGRAPH_RELIABLE_PACKET_COLOR_TOP);

            DrawNetgraphLineBar(positionI.X, ref positionI.Y, lineData.ByteCountUnreliableReceived, size.Y,
                 MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR, MyGuiConstants.NETGRAPH_UNRELIABLE_PACKET_COLOR_TOP);

            DrawNetgraphLineBar(positionI.X, ref positionI.Y, lineData.ByteCountSent, size.Y,
                 MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR, MyGuiConstants.NETGRAPH_SENT_PACKET_COLOR_TOP);
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

        private void DrawNetgraphScaleForPacketScale(Vector2 position, int optimalLengthOfBarInPx, float optimalDataSizeOfBarInBytes, int stepCount, StringBuilder unitForScale, bool showIntervals, bool alignToRight = true, float textScale = 0.7f, MyFontEnum fontType = MyFontEnum.White)
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

        private void DrawNetgraphScaleForAverageScale(Vector2 position, int optimalLengthOfBarInPx, float maximumValueOfScale, int stepCount, bool alignToRight = true, float textScale = 0.7f, MyFontEnum fontType = MyFontEnum.White)
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