using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Network;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Gui
{
    [PreloadRequiredAttribute]
    [StaticEventOwner]
    public class MyGuiScreenAdminMenu : MyGuiScreenDebugBase
    {
        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;

        protected static MyEntityCyclingOrder m_order;
        private static float m_metricValue = 0;
        private static long m_entityId;
        private static bool m_showMedbayNotification = true;

        private long m_attachCamera; // Not static, when screen closed, no longer tries
        private MyGuiControlLabel m_errorLabel;
        private MyGuiControlLabel m_labelNumVisible;
        private MyGuiControlLabel m_labelCurrentIndex;
        protected MyGuiControlButton m_removeItemButton;
        private MyGuiControlButton m_depowerItemButton;
        protected MyGuiControlCheckbox m_onlySmallGridsCheckbox;
        MyGuiControlCheckbox m_onlyLargeGridsCheckbox;
        static CyclingOptions m_cyclingOtions = new CyclingOptions();
        protected Vector4 m_labelColor = Color.White.ToVector4();
        protected MyGuiControlCheckbox m_creativeCheckbox;

        public MyGuiScreenAdminMenu()
            : base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_canShareInput = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 cbOffset = new Vector2(-0.05f, 0.0f);
            Vector2 controlPadding = new Vector2(0.02f, 0.02f); // X: Left & Right, Y: Bottom & Top

            float textScale = 0.8f;
            float separatorSize = 0.01f;
            float usableWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - controlPadding.X * 2;
            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f - 0.02f;

            m_currentPosition = -m_size.Value / 2.0f;
            m_currentPosition += controlPadding;
            m_currentPosition.Y += hiddenPartTop;
            m_scale = textScale;

            float y;
            MyGuiControlButton btn;

            ///////////////////// SPACE MASTER /////////////////////
            var caption = AddCaption(MySpaceTexts.ScreenDebugAdminMenu_Caption, m_labelColor, controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y + separatorSize + controlPadding.Y;

            m_creativeCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode, false, OnEnableAdminModeChanged, true, null, m_labelColor, cbOffset);
            m_creativeCheckbox.SetToolTip(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode_Tooltip);
            m_creativeCheckbox.IsChecked = MySession.Static.IsAdminModeEnabled;
            m_creativeCheckbox.Enabled = MySession.Static.IsAdmin;

         
            ///////////////////// CYCLING /////////////////////
            AddSubcaption(MyCommonTexts.ScreenDebugAdminMenu_CycleObjects, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.02f));
            m_currentPosition.Y -= 0.04f;

            CreateSelectionCombo();
            m_labelCurrentIndex = AddLabel(String.Empty, m_labelColor, 1);
            m_labelCurrentIndex.TextToDraw = new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_CurrentValue), m_entityId == 0 ? "-" : m_metricValue.ToString());

            y = m_currentPosition.Y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_First, c => OnCycleClicked(true, true));
            btn.PositionX = -usableWidth / 3 - controlPadding.X;

            m_currentPosition.Y = y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Next, c => OnCycleClicked(false, false));
            btn.PositionX = -controlPadding.X + separatorSize / 2;

            m_currentPosition.Y = y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Previous, c => OnCycleClicked(false, true));
            btn.PositionX = usableWidth / 3 - controlPadding.X + separatorSize;


            y = m_currentPosition.Y;
            m_removeItemButton = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Remove, c => OnEntityRemoveClicked(MyTrashRemovalOperation.Remove));
            m_removeItemButton.PositionX = -usableWidth / 3 - controlPadding.X;

            m_currentPosition.Y = y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Stop, c => OnEntityRemoveClicked(MyTrashRemovalOperation.Stop));
            btn.PositionX = -controlPadding.X + separatorSize / 2;

            m_currentPosition.Y = y;
            CreateDepowerButton(usableWidth, controlPadding.X, separatorSize);

            CreateDebugButton(usableWidth, MyCommonTexts.SpectatorControls_None, OnPlayerControl, true, MySpaceTexts.SpectatorControls_None_Desc);

            m_onlySmallGridsCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_SmallGrids, false, OnSmallGridChanged, true, null, m_labelColor, cbOffset);
            m_onlySmallGridsCheckbox.SetToolTip(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode_Tooltip);


            m_onlyLargeGridsCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_LargeGrids, false, OnLargeGridChanged, true, null, m_labelColor, cbOffset);
            m_onlyLargeGridsCheckbox.IsChecked = m_cyclingOtions.OnlyLargeGrids;
            m_onlySmallGridsCheckbox.IsChecked = m_cyclingOtions.OnlySmallGrids;

            ///////////////////// TRASH /////////////////////
            AddSubcaption(MyCommonTexts.ScreenDebugAdminMenu_TrashRemoval, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.02f));
            m_currentPosition.Y -= 0.04f;

            //AddLabel("Select which objects WON'T be removed", white, 1);
            CreateTrashCheckBoxes(ref cbOffset, ref m_labelColor);

            var blockCountLabelText = string.Format(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_WithBlockCount), String.Empty);
            AddLabel(blockCountLabelText, m_labelColor, 1);
            var blockCount = AddTextbox(MyTrashRemoval.PreviewSettings.BlockCountThreshold.ToString(), OnBlockCountChanged, m_labelColor, 0.9f, MyGuiControlTextboxType.DigitsOnly);

            var distancePlayerText = string.Format(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_DistanceFromPlayer), String.Empty);
            AddLabel(distancePlayerText, m_labelColor, 1);
            AddTextbox(MyTrashRemoval.PreviewSettings.PlayerDistanceThreshold.ToString(), OnDistanceChanged, m_labelColor, 0.9f, MyGuiControlTextboxType.DigitsOnly);

            m_labelNumVisible = AddLabel(MyCommonTexts.ScreenDebugAdminMenu_DistanceFromPlayer.ToString(), m_labelColor, 1);
            m_labelNumVisible.TextToDraw = new StringBuilder();

            AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_ShowTrashAdminView, () => MyTrashRemoval.PreviewEnabled, v => MyTrashRemoval.PreviewEnabled = v, true, null, m_labelColor, cbOffset);

            y = m_currentPosition.Y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_RemoveTrash, c => OnTrashRemoveClicked(MyTrashRemovalOperation.Remove));
            btn.PositionX = -usableWidth / 3 - controlPadding.X;

            m_currentPosition.Y = y;
            btn = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_StopTrash, c => OnTrashRemoveClicked(MyTrashRemovalOperation.Stop));
            btn.PositionX = -controlPadding.X + separatorSize / 2;

            m_currentPosition.Y = y;
            CreateDepowerButtonTrash(usableWidth,controlPadding.X,separatorSize);

            // PREPARED FOR FLOATING REMOVAL
            //y = m_currentPosition.Y;
            //btn = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_RemoveTrash, null);
            //btn.PositionX = -usableWidth / 3 - controlPadding.X;

            //m_currentPosition.Y = y;
            //btn = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_StopTrash, null);
            //btn.PositionX = -controlPadding.X + separatorSize / 2;

            //m_currentPosition.Y = y;
            //btn = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_DepowerTrash, null);
            //btn.PositionX = usableWidth / 3 - controlPadding.X + separatorSize;
            CreateCustomButtons(usableWidth, controlPadding.X, separatorSize);

            UpdateSmallLargeGridSelection();
            UpdateCyclingAndDepower();
            RecalcTrash();

            bool isClient = Sync.IsServer == false;
            CreateDebugButton(usableWidth / 2, MyCommonTexts.ScreenDebugAdminMenu_ReplicateEverything, OnReplicateEverything, isClient, isClient ? MyCommonTexts.ScreenDebugAdminMenu_ReplicateEverything_Tooltip : MySpaceTexts.ScreenDebugAdminMenu_ReplicateEverythingServer_Tooltip);
        }

        private void UpdateCyclingAndDepower()
        {
            m_cyclingOtions.Enabled = m_order != MyEntityCyclingOrder.Characters && m_order != MyEntityCyclingOrder.FloatingObjects;
            if (m_depowerItemButton != null)
            {
                m_depowerItemButton.Enabled = m_order != MyEntityCyclingOrder.Characters && m_order != MyEntityCyclingOrder.FloatingObjects;
            }
        }

        private void UpdateSmallLargeGridSelection()
        {
            m_removeItemButton.Enabled = m_order != MyEntityCyclingOrder.Characters;
            m_onlySmallGridsCheckbox.Enabled = m_order != MyEntityCyclingOrder.Characters && m_order != MyEntityCyclingOrder.FloatingObjects;
            m_onlyLargeGridsCheckbox.Enabled = m_order != MyEntityCyclingOrder.Characters && m_order != MyEntityCyclingOrder.FloatingObjects;
        }

        protected virtual void CreateTrashCheckBoxes(ref Vector2 cbOffset, ref Vector4 white)
        {
            AddTrashCheckbox(MyTrashRemovalFlags.Fixed, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Stationary, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Linear, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Accelerating, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Powered, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Controlled, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.WithProduction, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.WithMedBay, true, null, white, cbOffset);
        }

        virtual protected void CreateSelectionCombo()
        {
            AddCombo<MyEntityCyclingOrder>(m_order, OnOrderChanged, color: m_labelColor);
        }

        virtual protected void CreateDepowerButton(float usableWidth, float controlPadding, float separatorSize)
        {
            m_depowerItemButton = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_Depower, c => OnEntityRemoveClicked(MyTrashRemovalOperation.Depower));
            m_depowerItemButton.PositionX = usableWidth / 3 - controlPadding + separatorSize;
        }

        virtual protected void CreateDepowerButtonTrash(float usableWidth, float controlPadding, float separatorSize)
        {
            var btn = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_DepowerTrash, c => OnTrashRemoveClicked(MyTrashRemovalOperation.Depower));
            btn.PositionX = usableWidth / 3 - controlPadding + separatorSize;
        }

        private void OnPlayerControl(MyGuiControlButton obj)
        {
            m_attachCamera = 0;
            MyGuiScreenGamePlay.SetCameraController();
        }

        protected void OnOrderChanged(MyEntityCyclingOrder obj)
        {
            m_order = obj;
            UpdateSmallLargeGridSelection();
            UpdateCyclingAndDepower();

            OnCycleClicked(true, true);
        }

        protected void AddTrashCheckbox(MyTrashRemovalFlags flag, bool enabled, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            string name = string.Format(MyTrashRemoval.GetName(flag), String.Empty);
            AddCheckBox(name, (MyTrashRemoval.PreviewSettings.Flags & flag) == flag, c => OnFlagChanged(flag, c.IsChecked), enabled, controlGroup, color, checkBoxOffset);
        }

        void RecalcTrash()
        {
            int num = MyTrashRemoval.Calculate(MyTrashRemoval.PreviewSettings);
            m_labelNumVisible.TextToDraw.Clear().ConcatFormat(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_NumberOfLocalTrash), num);
        }

        void OnCycleClicked(bool reset, bool forward)
        {
            MyMultiplayer.RaiseStaticEvent(x => CycleRequest_Implementation, m_order, reset, forward, m_metricValue, m_entityId, m_cyclingOtions);
        }

        [Event, Reliable, Server]
        static void CycleRequest_Implementation(MyEntityCyclingOrder order, bool reset, bool findLarger, float metricValue, long currentEntityId,CyclingOptions options)
        {
            if (reset)
            {
                metricValue = float.MinValue;
                currentEntityId = 0;
                findLarger = false;
            }

            MyEntityCycling.FindNext(order, ref metricValue, ref currentEntityId, findLarger, options);

            var e = MyEntities.GetEntityByIdOrDefault(currentEntityId);
            Vector3D pos = e != null ? e.WorldMatrix.Translation : Vector3D.Zero;

            if (MyEventContext.Current.IsLocallyInvoked)
            {
                Cycle_Implementation(metricValue, currentEntityId, pos);
            }
            else
            {
                var proxy = e as IMyEventProxy;
                if (proxy != null)
                {
                    if (proxy is MyCubeGrid && (proxy as MyCubeGrid).Physics == null)
                    {
                        //don't send grids without physics 
                    }
                    else
                    {
                        MyMultiplayer.ReplicateImmediatelly(proxy, MyEventContext.Current.Sender);
                    }
                }
                MyMultiplayer.RaiseStaticEvent(x => Cycle_Implementation, metricValue, currentEntityId, pos, MyEventContext.Current.Sender);
            }
        }

        private static void UpdateRemoveAndDepowerButton(MyGuiScreenAdminMenu menu,long entityId)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);
            menu.m_removeItemButton.Enabled = entity == null ? false : !(entity is MyCharacter);
            if (menu.m_depowerItemButton != null)
            {
                menu.m_depowerItemButton.Enabled = entity == null ? false : !(entity is MyCharacter);
            }
        }

        [Event, Reliable, Client]
        static void Cycle_Implementation(float newMetricValue, long newEntityId, Vector3D position)
        {
            m_metricValue = newMetricValue;
            m_entityId = newEntityId;
            bool cameraAttached = false;
            if (m_entityId != 0)
            {
                cameraAttached = TryAttachCamera(m_entityId);
                if (!cameraAttached)
                {
                    // When camera not attached, move it to expected object position and wait for replication
                    MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, position + Vector3.One * 50);
                }
            }

            var menu = MyScreenManager.GetFirstScreenOfType<MyGuiScreenAdminMenu>();
            if (menu != null)
            {
                UpdateRemoveAndDepowerButton(menu, m_entityId);

                menu.m_attachCamera = m_entityId;
                menu.m_labelCurrentIndex.TextToDraw.Clear().AppendFormat(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_CurrentValue), m_entityId == 0 ? "-" : m_metricValue.ToString());
            }
        }

        static bool TryAttachCamera(long entityId)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                // TODO: Would be nice to have 3rd person spectator with collision avoidance, zoom and orbit. Currently possible only on controlled entities.
                var volume = entity.PositionComp.WorldVolume;
                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                MySpectatorCameraController.Static.Position = volume.Center + Math.Max((float)volume.Radius,1.0f) * Vector3.One;
                MySpectatorCameraController.Static.Target = volume.Center;
                return true;
            }

            return false;
        }

        public override bool Update(bool hasFocus)
        {
            if (m_attachCamera != 0)
            {
                TryAttachCamera(m_attachCamera);
                UpdateRemoveAndDepowerButton(this, m_attachCamera);
            }

            return base.Update(hasFocus);
        }

        void OnEntityRemoveClicked(MyTrashRemovalOperation operation)
        {
            if (m_attachCamera != null)
            {
                MyMultiplayer.RaiseStaticEvent(x => RemoveEntity_Implementation, m_attachCamera, operation);
            }
        }

        [Event, Reliable, Server]
        static void RemoveEntity_Implementation(long entityId, MyTrashRemovalOperation operation)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerAdminRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
                MyTrashRemoval.ApplyOperation(entity, operation);
        }

        void OnTrashRemoveClicked(MyTrashRemovalOperation operation)
        {
            MyMultiplayer.RaiseStaticEvent(x => RemoveTrash_Implementation, MyTrashRemoval.PreviewSettings, operation);
        }

        [Event, Reliable, Server]
        static void RemoveTrash_Implementation(MyTrashRemovalSettings settings, MyTrashRemovalOperation operation)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerAdminRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            MyTrashRemoval.Apply(settings, operation);
        }

        void OnFlagChanged(MyTrashRemovalFlags flag, bool value)
        {
            if (flag == MyTrashRemovalFlags.WithMedBay && value && m_showMedbayNotification)
            {
                var msgBox = MyGuiSandbox.CreateMessageBox(messageText: MyTexts.Get(MySpaceTexts.ScreenDebugAdminMenu_MedbayNotification));
                MyScreenManager.AddScreen(msgBox);
                m_showMedbayNotification = false;
            }

            if (value) MyTrashRemoval.PreviewSettings.Flags |= flag;
            else MyTrashRemoval.PreviewSettings.Flags &= ~flag;
            RecalcTrash();
        }

        void OnReplicateEverything(MyGuiControlButton button)
        {
            MyMultiplayer.RaiseStaticEvent(x => ReplicateEverything_Implementation);
        }

        [Event, Reliable, Server]
        static void ReplicateEverything_Implementation()
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return;

            if (!MySession.Static.HasPlayerAdminRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            var server = (MyReplicationServer)MyMultiplayer.Static.ReplicationLayer;
            server.ForceEverything(MyEventContext.Current.Sender);
        }

        void OnBlockCountChanged(MyGuiControlTextbox textbox)
        {
            int numBlocks;
            if (int.TryParse(textbox.Text, out numBlocks))
            {
                MyTrashRemoval.PreviewSettings.BlockCountThreshold = numBlocks;
                RecalcTrash();
            }
        }

        void OnDistanceChanged(MyGuiControlTextbox textbox)
        {
            float distance;
            if (float.TryParse(textbox.Text, out distance))
            {
                MyTrashRemoval.PreviewSettings.PlayerDistanceThreshold = distance;
                RecalcTrash();
            }
        }

        void OnEnableAdminModeChanged(MyGuiControlCheckbox checkbox)
        {
            MySession.Static.IsAdminModeEnabled = checkbox.IsChecked;
        }

        void OnSmallGridChanged(MyGuiControlCheckbox checkbox)
        {
            m_cyclingOtions.OnlySmallGrids = checkbox.IsChecked;
            if (m_cyclingOtions.OnlySmallGrids)
            {
                m_onlyLargeGridsCheckbox.IsChecked = false;
            }
        }

        void OnLargeGridChanged(MyGuiControlCheckbox checkbox)
        {
            m_cyclingOtions.OnlyLargeGrids = checkbox.IsChecked;
            if (m_cyclingOtions.OnlyLargeGrids)
            {
                m_onlySmallGridsCheckbox.IsChecked = false;
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenAdminMenu";
        }

        public override bool Draw()
        {
            if (base.Draw())
            {
                return true;
            }
            return false;
        }

        private MyGuiControlButton CreateDebugButton(float usableWidth, MyStringId text, Action<MyGuiControlButton> onClick, bool enabled = true, MyStringId? tooltip = null)
        {
            var button = AddButton(MyTexts.Get(text), onClick);
            button.VisualStyle = MyGuiControlButtonStyleEnum.Rectangular;
            button.TextScale = m_scale;
            button.Size = new Vector2(usableWidth, button.Size.Y);
            button.Position = button.Position + new Vector2(-HIDDEN_PART_RIGHT / 2.0f, 0.0f);
            button.Enabled = enabled;
            if (tooltip != null)
            {
                button.SetToolTip(tooltip.Value);
            }
            return button;
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12) || MyInput.Static.IsNewKeyPressed(MyKeys.F11) || MyInput.Static.IsNewKeyPressed(MyKeys.F10))
            {
                this.CloseScreen();
            }
        }

        private void AddSeparator()
        {
            var separator = new MyGuiControlSeparatorList();
            separator.Size = new Vector2(1, 0.01f);
            separator.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            separator.AddHorizontal(Vector2.Zero, 1);

            Controls.Add(separator);
        }

        private MyGuiControlLabel CreateSliderWithDescription(MyGuiControlList list, float usableWidth, float min, float max, string description, ref MyGuiControlSlider slider)
        {
            var label = AddLabel(description, Vector4.One, m_scale);
            Controls.Remove(label);
            list.Controls.Add(label);

            CreateSlider(list, usableWidth, min, max, ref slider);

            var labelNoise = AddLabel("", Vector4.One, m_scale);
            Controls.Remove(labelNoise);
            list.Controls.Add(labelNoise);
            return labelNoise;
        }

        private void CreateSlider(MyGuiControlList list, float usableWidth, float min, float max, ref MyGuiControlSlider slider)
        {
            slider = new MyGuiControlSlider(
               position: m_currentPosition,
               width: 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
               minValue: min,
               maxValue: max,
               labelText: String.Empty,
               labelDecimalPlaces: 4,
               labelScale: 0.75f * m_scale,
               originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
               labelFont: MyFontEnum.Debug);

            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = Color.White.ToVector4();
            list.Controls.Add(slider);
        }

        protected virtual void CreateCustomButtons(float usableWidth, float controlPadding, float separatorSize)
        {

        }
    }
}
