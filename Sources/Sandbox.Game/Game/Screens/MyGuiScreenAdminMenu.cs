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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Network;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Replication;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.SessionComponents;
using VRage.Game.ModAPI;
using VRage.Serialization;

namespace Sandbox.Game.Gui
{
    [PreloadRequiredAttribute]
    [StaticEventOwner]
    public class MyGuiScreenAdminMenu : MyGuiScreenDebugBase
    {
        private class MyEntityListInfoItem
        {
            public MyEntityListInfoItem()
            { }

            public MyEntityListInfoItem(string displayName, long entityId, int blockCount, float mass, Vector3D position, float speed, float distanceFromPlayers, string owner = "", float ownerLogin = 0)
            {
                //truncate displayname to limit the amount of data we send
                if (string.IsNullOrEmpty(displayName))
                    DisplayName = "----";
                else
                    DisplayName = displayName.Length < 50 ? displayName : displayName.Substring(0, 49);
                EntityId = entityId;
                BlockCount = blockCount;
                Mass = mass;
                Position = position;
                OwnerName = owner;
                Speed = speed;
                DistanceFromPlayers = distanceFromPlayers;
                OwnerLoginTime = ownerLogin;
            }
            public string DisplayName;
            public long EntityId;
            public int BlockCount;
            public float Mass;
            public Vector3D Position;
            public string OwnerName;
            public float Speed;
            public float DistanceFromPlayers;
            public float OwnerLoginTime;
        }

        #region Fields

        private static MyGuiScreenAdminMenu m_static;

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
        private MyGuiControlLabel m_labelEntityName;
        protected MyGuiControlButton m_removeItemButton;
        private MyGuiControlButton m_depowerItemButton;
        protected MyGuiControlButton m_stopItemButton;
        protected MyGuiControlCheckbox m_onlySmallGridsCheckbox;
        MyGuiControlCheckbox m_onlyLargeGridsCheckbox;
        static CyclingOptions m_cyclingOtions = new CyclingOptions();
        protected Vector4 m_labelColor = Color.White.ToVector4();
        protected MyGuiControlCheckbox m_creativeCheckbox;
        List<IMyGps> m_gpsList = new List<IMyGps>();
        protected MyGuiControlCombobox m_modeCombo;
        protected MyGuiControlCheckbox m_invulnerableCheckbox;
        protected MyGuiControlCheckbox m_showPlayersCheckbox;
        protected MyGuiControlCheckbox m_canUseTerminals;
        protected MyGuiControlListbox m_entityListbox;
        protected MyGuiControlCombobox m_entityTypeCombo;
        protected MyGuiControlCombobox m_entitySortCombo;
        private MyEntityTypeEnum m_selectedType;
        private MyEntitySortOrder m_selectedSort;
        private static bool m_invertOrder;
        private static bool m_damageHandler;
        private static HashSet<long> m_protectedCharacters = new HashSet<long>();
        private MyPageEnum m_currentPage;
        int m_currentGpsIndex = 0;

        #endregion

        private enum MyPageEnum
        {
            SpaceMaster = 0,
            Cleanup = 1,
            AdminTools = 2,
            EntityList = 3,
        }

        private enum MyEntityTypeEnum
        {
            Grids = 0,
            SmallGrids = 1,
            LargeGrids = 2,
            Characters = 3,
            FloatingObjects = 4,
            Planets = 5,
            Asteroids = 6,
        }

        private enum MyEntitySortOrder
        {
            DisplayName = 0,
            BlockCount = 1,
            Mass = 2,
            OwnerName = 3,
            DistanceFromCenter = 4,
            Speed = 5,
            DistanceFromPlayers = 6,
            OwnerLastLogin = 7,
        }

        public MyGuiScreenAdminMenu()
            : base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            if (!Sync.IsServer)
            {
                m_static = this;
                MyMultiplayer.RaiseStaticEvent(x => RequestSettingFromServer_Implementation);
            }
            else
                CreateScreen();
        }

        private void CreateScreen()
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
            
            ///////////////////// MODE SELECT /////////////////////
            AddCaption(MySpaceTexts.ScreenDebugAdminMenu_ModeSelect, m_labelColor, controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y  + controlPadding.Y;

            m_modeCombo = AddCombo();

            if (MySession.Static.IsSpaceMaster)
            {
                m_modeCombo.AddItem((long)MyPageEnum.SpaceMaster, MySpaceTexts.ScreenDebugAdminMenu_Caption);
                m_modeCombo.AddItem((long)MyPageEnum.Cleanup, MySpaceTexts.ScreenDebugAdminMenu_Cleanup);
                m_modeCombo.AddItem((long)MyPageEnum.AdminTools, MySpaceTexts.ScreenDebugAdminMenu_AdminTools);
                if (MySession.Static.IsAdministrator)
                {
                    //these options are for admins only, don't even bother showing them to other players
                    m_modeCombo.AddItem((long)MyPageEnum.EntityList, MySpaceTexts.ScreenDebugAdminMenu_EntityList);
                }

                m_modeCombo.SelectItemByKey((long)m_currentPage);
            }
            else //user is moderator, they only need the admin tools page
            {
                m_modeCombo.AddItem((long)MyPageEnum.AdminTools, MySpaceTexts.ScreenDebugAdminMenu_AdminTools);
                m_currentPage = MyPageEnum.AdminTools;
                m_modeCombo.SelectItemByKey((long)m_currentPage);
            }

            m_modeCombo.ItemSelected += OnModeComboSelect;
            
            switch (m_currentPage)
            {
                case MyPageEnum.SpaceMaster:
                    ///////////////////// SPACE MASTER /////////////////////
                    AddSubcaption(MySpaceTexts.ScreenDebugAdminMenu_Caption, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;

                    m_creativeCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode, false, OnEnableAdminModeChanged, true, null, m_labelColor, cbOffset);
                    m_creativeCheckbox.SetToolTip(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode_Tooltip);
                    m_creativeCheckbox.IsChecked = MySession.Static.CreativeToolsEnabled(Sync.MyId);
                    m_creativeCheckbox.Enabled = MySession.Static.HasCreativeRights;

                    ///////////////////// CYCLING /////////////////////
                    AddSubcaption(MyCommonTexts.ScreenDebugAdminMenu_CycleObjects, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;

                    CreateSelectionCombo();
                    m_labelCurrentIndex = AddLabel(String.Empty, m_labelColor, 1);
                    m_labelCurrentIndex.TextToDraw = new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_CurrentValue), m_entityId == 0 ? "-" : m_metricValue.ToString());

                    m_labelEntityName = AddLabel(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_EntityName) + " -", m_labelColor, 1);
             
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
                    m_stopItemButton = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Stop, c => OnEntityRemoveClicked(MyTrashRemovalOperation.Stop));
                    m_stopItemButton.PositionX = -controlPadding.X + separatorSize / 2;

                    m_currentPosition.Y = y;
                    CreateDepowerButton(usableWidth, controlPadding.X, separatorSize);

                    CreateDebugButton(usableWidth, MyCommonTexts.SpectatorControls_None, OnPlayerControl, true, MySpaceTexts.SpectatorControls_None_Desc);
                    CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugAdminMenu_TeleportHere, OnTeleportButton, MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.Parent == null, MySpaceTexts.ScreenDebugAdminMenu_TeleportHereToolTip);

                    m_onlySmallGridsCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_SmallGrids, false, OnSmallGridChanged, true, null, m_labelColor, cbOffset);
                    m_onlySmallGridsCheckbox.SetToolTip(MyCommonTexts.ScreenDebugAdminMenu_EnableAdminMode_Tooltip);


                    m_onlyLargeGridsCheckbox = AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_LargeGrids, false, OnLargeGridChanged, true, null, m_labelColor, cbOffset);
                    m_onlyLargeGridsCheckbox.IsChecked = m_cyclingOtions.OnlyLargeGrids;
                    m_onlySmallGridsCheckbox.IsChecked = m_cyclingOtions.OnlySmallGrids;

                    bool isClient = Sync.IsServer == false;
                    CreateDebugButton(usableWidth / 2, MyCommonTexts.ScreenDebugAdminMenu_ReplicateEverything, OnReplicateEverything, isClient, isClient ? MyCommonTexts.ScreenDebugAdminMenu_ReplicateEverything_Tooltip : MySpaceTexts.ScreenDebugAdminMenu_ReplicateEverythingServer_Tooltip);
                    break;
                case MyPageEnum.Cleanup:
                    ///////////////////// TRASH /////////////////////
                    AddSubcaption(MyCommonTexts.ScreenDebugAdminMenu_TrashRemoval, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;

                    CreateTrashCheckBoxes(ref cbOffset, ref m_labelColor);

                    Vector2? screenSize = this.GetSize();

                    //With less blocks than
                    var blockCountY = m_currentPosition.Y;
                    m_currentPosition.Y += 0.005f;
                    var blockCountLabelText = string.Format(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_WithBlockCount), String.Empty);
                    AddLabel(blockCountLabelText, m_labelColor, 1);
                    m_currentPosition.Y = blockCountY;
                    var blockCount = AddTextbox(MyTrashRemoval.PreviewSettings.BlockCountThreshold.ToString(), OnBlockCountChanged, m_labelColor, 0.9f, MyGuiControlTextboxType.DigitsOnly);
                    blockCount.Size = new Vector2(0.07f, blockCount.Size.Y);
                    blockCount.PositionX = m_currentPosition.X + screenSize.Value.X - blockCount.Size.X - 0.003f;

                    //Further from player than
                    blockCountY = m_currentPosition.Y;
                    var distancePlayerText = string.Format(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_DistanceFromPlayer), String.Empty);
                    AddLabel(distancePlayerText, m_labelColor, 1);
                    m_currentPosition.Y = blockCountY - 0.01f;
                    var distanceTrash = AddTextbox(MyTrashRemoval.PreviewSettings.PlayerDistanceThreshold.ToString(), OnDistanceChanged, m_labelColor, 0.9f, MyGuiControlTextboxType.DigitsOnly);
                    distanceTrash.Size = new Vector2(0.07f, distanceTrash.Size.Y);
                    distanceTrash.PositionX = m_currentPosition.X + screenSize.Value.X - distanceTrash.Size.X - 0.003f;
                    m_currentPosition.Y -= 0.01f;

                    //Continuous trash interval
                    AddLabel(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_ContinuousTrashInterval), m_labelColor, 1);
                    ////Time label
                    blockCountY = m_currentPosition.Y;
                    m_currentPosition.Y += 0.007f;
                    var continuousTrashLabel = AddLabel("text", m_labelColor, 1.5f);
                    m_currentPosition.Y = blockCountY;
                    continuousTrashLabel.TextScale = 1f;
                    continuousTrashLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                    ////Slider
                    var trashIntervalSlider = new MyGuiControlSlider(
                        position: m_currentPosition,
                        width: 0.25f,
                        labelSpaceWidth: 0.01f,
                        originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

                    trashIntervalSlider.MinValue = 0;
                    trashIntervalSlider.MaxValue = 1;
                    trashIntervalSlider.DefaultValue = 0f;
                    trashIntervalSlider.Value = MathHelper.InterpLogInv(MyTrashRemoval.CurrentRemovalInterval, MyTrashRemoval.REMOVAL_INTERVAL_MINIMUM_S, MyTrashRemoval.REMOVAL_INTERVAL_MAXIMUM_S);
                    StringBuilder tmpBuilder = new StringBuilder();
                    trashIntervalSlider.ValueChanged += (MyGuiControlSlider s) =>
                    {
                        tmpBuilder.Clear();
                        if (s.Value < 0) s.Value = 0;
                        if (s.Value > 1) s.Value = 1;
                        int time = (int)Math.Round(MathHelper.InterpLog(s.Value, MyTrashRemoval.REMOVAL_INTERVAL_MINIMUM_S, MyTrashRemoval.REMOVAL_INTERVAL_MAXIMUM_S));
                        MyValueFormatter.AppendTimeInBestUnit(time, tmpBuilder);
                        MyTrashRemoval.CurrentRemovalInterval = time;
                        continuousTrashLabel.Text = tmpBuilder.ToString();
                        RecalcTrash();
                    };
                    tmpBuilder.Clear();
                    MyValueFormatter.AppendTimeInBestUnit(MyTrashRemoval.CurrentRemovalInterval, tmpBuilder);
                    continuousTrashLabel.Text = tmpBuilder.ToString();
                    continuousTrashLabel.PositionX = m_currentPosition.X + trashIntervalSlider.Size.X + 0.003f;
                    Controls.Add(trashIntervalSlider);
                    m_currentPosition.Y += trashIntervalSlider.Size.Y;

                    //Trash action
                    AddLabel(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_TrashAction), m_labelColor, 1);
                    blockCountY = m_currentPosition.Y;
                    var trashActionCombobox = AddCombo<MyTrashRemovalOperation>(MyTrashRemoval.TrashOperation, OnTrashOptionChanged, color: m_labelColor, openAreaItemsCount: 3);
                    trashActionCombobox.Size = new Vector2(0.15f, trashActionCombobox.Size.Y);
            
                    //Trash pause button
                    m_currentPosition.Y = blockCountY;
                    btn = CreateDebugButton(
                    usableWidth / 2,
                    (MyTrashRemoval.RemovalPaused ?
                        MyCommonTexts.ScreenDebugAdminMenu_ResumeTrashButton :
                        MyCommonTexts.ScreenDebugAdminMenu_PauseTrashButton),
                    OnTrashButtonClicked,
                    true,
                    MyCommonTexts.ScreenDebugAdminMenu_PauseTrashButtonTooltip);
                    btn.PositionX = m_currentPosition.X + usableWidth - (btn.Size.X / 2) + 0.007f;

                    //Local trash count
                    m_labelNumVisible = AddLabel(MyCommonTexts.ScreenDebugAdminMenu_NumberOfLocalTrash.ToString(), m_labelColor, 1);
                    m_labelNumVisible.TextToDraw = new StringBuilder();

                    AddCheckBox(MyCommonTexts.ScreenDebugAdminMenu_ShowTrashAdminView, () => MyTrashRemoval.PreviewEnabled, v => MyTrashRemoval.PreviewEnabled = v, true, null, m_labelColor, cbOffset);

                    ///////////////////// REMOVE FLOATING OBJECTS /////////////////////
                    y = m_currentPosition.Y;
                    btn = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugAdminMenu_RemoveFloating, OnRemoveFloating, true);
                    btn.PositionX = m_currentPosition.X + btn.Size.X / 2;

                    CreateCustomButtons(usableWidth, controlPadding.X, separatorSize);

                    UpdateSmallLargeGridSelection();
                    UpdateCyclingAndDepower();
                    RecalcTrash();

                    CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugAdminMenu_StopAll, OnStopEntities);
                    break;
                case MyPageEnum.AdminTools:
                    ///////////////////// ADMIN TOOLS /////////////////////
                    AddSubcaption(MySpaceTexts.ScreenDebugAdminMenu_AdminTools, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;

                    m_invulnerableCheckbox = AddCheckBox(MySpaceTexts.ScreenDebugAdminMenu_Invulnerable, false, OnInvulnerableChanged, true, null, m_labelColor, cbOffset);
                    m_invulnerableCheckbox.SetToolTip(MySpaceTexts.ScreenDebugAdminMenu_InvulnerableToolTip);
                    m_invulnerableCheckbox.IsChecked = MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.Invulnerable);
                    m_invulnerableCheckbox.Enabled = MySession.Static.IsAdministrator;

                    m_showPlayersCheckbox = AddCheckBox(MySpaceTexts.ScreenDebugAdminMenu_ShowPlayers, false, OnShowPlayersChanged, true, null, m_labelColor, cbOffset);
                    m_showPlayersCheckbox.SetToolTip(MySpaceTexts.ScreenDebugAdminMenu_ShowPlayersToolTip);
                    m_showPlayersCheckbox.IsChecked = MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.ShowPlayers);
                    m_showPlayersCheckbox.Enabled = MySession.Static.IsModerator;

                    m_canUseTerminals = AddCheckBox(MySpaceTexts.ScreenDebugAdminMenu_UseTerminals, false, OnUseTerminalsChanged, true, null, m_labelColor, cbOffset);
                    m_canUseTerminals.SetToolTip(MySpaceTexts.ScreenDebugAdminMenu_UseTerminalsToolTip);
                    m_canUseTerminals.IsChecked = MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.UseTerminals);
                    m_canUseTerminals.Enabled = MySession.Static.IsAdministrator;
                    break;
                case MyPageEnum.EntityList:
                    ///////////////////// ENTITY LIST /////////////////////
                    AddSubcaption(MySpaceTexts.ScreenDebugAdminMenu_SortOptions, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;
                    m_entityTypeCombo = AddCombo<MyEntityTypeEnum>(m_selectedType, ValueChanged, color: m_labelColor);
                    m_entitySortCombo = AddCombo<MyEntitySortOrder>(m_selectedSort, ValueChanged, color: m_labelColor);
                    AddSubcaption(MySpaceTexts.ScreenDebugAdminMenu_EntityList, m_labelColor, new Vector2(-HIDDEN_PART_RIGHT, -0.03f));
                    m_currentPosition.Y -= 0.065f;
                    m_entityListbox = new MyGuiControlListbox(Vector2.Zero, MyGuiControlListboxStyleEnum.Blueprints);
                    m_entityListbox.Size = new Vector2(usableWidth, 0);
                    m_entityListbox.Enabled = true;
                    m_entityListbox.VisibleRowsCount = 15;
                    m_entityListbox.Position = m_entityListbox.Size/2 + m_currentPosition;
                    m_entityListbox.ItemClicked += EntityListItemClicked;
                    m_entityListbox.MultiSelect = true;
                    m_currentPosition = m_entityListbox.GetPositionAbsoluteBottomLeft();
                    m_currentPosition.Y += 0.01f;

                    y = m_currentPosition.Y;
                    m_removeItemButton = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Remove, c => OnEntityListRemoveClicked(MyTrashRemovalOperation.Remove));
                    m_removeItemButton.PositionX = -usableWidth / 3 - controlPadding.X;

                    m_currentPosition.Y = y;
                    m_stopItemButton = CreateDebugButton(usableWidth / 3, MyCommonTexts.ScreenDebugAdminMenu_Stop, c => OnEntityListRemoveClicked(MyTrashRemovalOperation.Stop));
                    m_stopItemButton.PositionX = -controlPadding.X + separatorSize / 2;

                    m_currentPosition.Y = y;
                   // CreateDepowerButton(usableWidth, controlPadding.X, separatorSize);
                    m_depowerItemButton = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_Depower, c => OnEntityListRemoveClicked(MyTrashRemovalOperation.Depower));
                    m_depowerItemButton.PositionX = usableWidth / 3 - controlPadding.X + separatorSize;

                    CreateDebugButton(usableWidth, MyCommonTexts.SpectatorControls_None, OnPlayerControl, true, MySpaceTexts.SpectatorControls_None_Desc);
                    CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugAdminMenu_TeleportHere, OnTeleportButton, MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.Parent == null, MySpaceTexts.ScreenDebugAdminMenu_TeleportHereToolTip);

                    ValueChanged((MyEntityTypeEnum)m_entityTypeCombo.GetSelectedKey());
                    Controls.Add(m_entityListbox);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region Methods

        void CircleGps(bool reset, bool forward)
        { 
            if(MySession.Static != null && MySession.Static.Gpss != null && MySession.Static.LocalHumanPlayer != null)
            {
                if (forward)
                {
                    m_currentGpsIndex--;
                }
                else
                {
                    m_currentGpsIndex++;
                }

                m_gpsList.Clear();
                MySession.Static.Gpss.GetGpsList(MySession.Static.LocalPlayerId, m_gpsList);

                if(m_gpsList.Count == 0)
                {
                    m_currentGpsIndex = 0;
                    return;
                }

                if(m_currentGpsIndex < 0)
                {
                    m_currentGpsIndex = m_gpsList.Count - 1;
                }
              
                if(m_gpsList.Count <= m_currentGpsIndex || reset)
                {
                    m_currentGpsIndex = 0;
                }

                Vector3D gpsPosition =  m_gpsList[m_currentGpsIndex].Coords;

                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                Vector3D? cameraPosition = MyEntities.FindFreePlace(gpsPosition + Vector3D.One , 2.0f,30);
                 
                MySpectatorCameraController.Static.Position = cameraPosition.HasValue ? cameraPosition.Value : (gpsPosition + Vector3D.One);
                MySpectatorCameraController.Static.Target = gpsPosition;
            }
        }
        
        private static StringBuilder GetFormattedDisplayName(MyEntitySortOrder selectedOrder, MyEntityListInfoItem item, bool isGrid)
        {
            StringBuilder sb = new StringBuilder(item.DisplayName);
            switch (selectedOrder)
            {
                case MyEntitySortOrder.DisplayName:
                    break;
                case MyEntitySortOrder.BlockCount:
                    if (isGrid)
                        sb.Append(" | " + item.BlockCount);
                    break;
                case MyEntitySortOrder.Mass:
                    sb.Append(" | ");
                    if (item.Mass == 0)
                        sb.Append("-");
                    else
                        MyValueFormatter.AppendWeightInBestUnit(item.Mass, sb);
                    break;
                case MyEntitySortOrder.OwnerName:
                    if (isGrid)
                        sb.Append(" | " + (string.IsNullOrEmpty(item.OwnerName) ? MyTexts.GetString(MySpaceTexts.BlockOwner_Nobody) : item.OwnerName));
                    break;
                case MyEntitySortOrder.DistanceFromCenter:
                    sb.Append(" | ");
                    MyValueFormatter.AppendDistanceInBestUnit((float)item.Position.Length(), sb);
                    break;
                case MyEntitySortOrder.Speed:
                    sb.Append(" | " + item.Speed.ToString("0.### m/s"));
                    break;
                case MyEntitySortOrder.DistanceFromPlayers:
                    sb.Append(" | ");
                    MyValueFormatter.AppendDistanceInBestUnit(item.DistanceFromPlayers, sb);
                    break;
                case MyEntitySortOrder.OwnerLastLogin:
                    if (isGrid)
                    {
                        sb.Append(" | " + (string.IsNullOrEmpty(item.OwnerName) ? MyTexts.GetString(MySpaceTexts.BlockOwner_Nobody) : item.OwnerName));
                        sb.Append(": ");
                        MyValueFormatter.AppendTimeInBestUnit(item.OwnerLoginTime, sb);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sb;
        }

        private static string GetTooltipText(MyEntityListInfoItem item, bool isGrid)
        {
            StringBuilder sb = new StringBuilder();
            
            if (!isGrid)
            {
                sb.Append(MyEntitySortOrder.Mass + ": ");
                if (item.Mass > 0)
                    MyValueFormatter.AppendWeightInBestUnit(item.Mass, sb);
                else
                    sb.Append("-");
                sb.AppendLine();
                sb.Append(MyEntitySortOrder.DistanceFromCenter + ": ");
                MyValueFormatter.AppendDistanceInBestUnit((float)item.Position.Length(), sb);
                sb.AppendLine();
                sb.Append(MyEntitySortOrder.Speed + ": " + item.Speed + " m/s");
            }
            else
            {
                sb.AppendLine(MyEntitySortOrder.BlockCount + ": " + item.BlockCount);
                sb.Append(MyEntitySortOrder.Mass + ": ");
                if (item.Mass > 0)
                    MyValueFormatter.AppendWeightInBestUnit(item.Mass, sb);
                else
                    sb.Append("-");
                sb.AppendLine();
                sb.AppendLine(MyEntitySortOrder.OwnerName + ": " + item.OwnerName);
                sb.AppendLine(MyEntitySortOrder.Speed + ": " + item.Speed + " m/s");
                sb.Append(MyEntitySortOrder.DistanceFromCenter + ": ");
                MyValueFormatter.AppendDistanceInBestUnit((float)item.Position.Length(), sb);
                sb.AppendLine();
                sb.Append(MyEntitySortOrder.DistanceFromPlayers + ": ");
                MyValueFormatter.AppendDistanceInBestUnit(item.DistanceFromPlayers, sb);
                sb.AppendLine();
                sb.Append(MyEntitySortOrder.OwnerLastLogin + ": ");
                MyValueFormatter.AppendTimeInBestUnit(item.OwnerLoginTime, sb);
            }

            return sb.ToString();
        }

        private static float GetPlayerDistance(MyEntity entity)
        {
            var pos = entity.WorldMatrix.Translation;
            float minDistSq = float.MaxValue;

            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                var controlledEntity = player.Controller.ControlledEntity;
                if (controlledEntity != null)
                {
                    var distSq = Vector3.DistanceSquared(controlledEntity.Entity.WorldMatrix.Translation, pos);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                    }
                }
            }
            return (float)Math.Sqrt(minDistSq);
        }

        private static float GetOwnerLoginTimeSeconds(MyCubeGrid grid)
        {
            if (grid == null)
                return 0;

            if (grid.BigOwners.Count == 0)
                return 0;

            var identity = MySession.Static.Players.TryGetIdentity(grid.BigOwners[0]);
            if (identity == null)
                return 0;

            return (int)(DateTime.Now - identity.LastLoginTime).TotalSeconds;
        }

        void RecalcTrash()
        {
            if (Sync.IsServer == false)
            {
                MyMultiplayer.RaiseStaticEvent(
                    x => UploadSettingsToServer,
                    MyTrashRemoval.CurrentRemovalInterval,
                    MyTrashRemoval.PreviewEnabled,
                    MyTrashRemoval.PreviewSettings,
                    MyTrashRemoval.RemovalPaused,
                    MyTrashRemoval.TrashOperation
                );
            }
            int num = MyTrashRemoval.Calculate(MyTrashRemoval.PreviewSettings);
            m_labelNumVisible.TextToDraw.Clear().ConcatFormat(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_NumberOfLocalTrash), num);
        }

        private static void SortEntityList(MyEntitySortOrder selectedOrder, ref List<MyEntityListInfoItem> items)
        {
            switch (selectedOrder)
            {
                case MyEntitySortOrder.DisplayName:
                    items.Sort((a, b) =>
                               {
                                   int res = string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.BlockCount:
                    items.Sort((a, b) =>
                               {
                                   int res = b.BlockCount.CompareTo(a.BlockCount);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.Mass:
                    items.Sort((a, b) =>
                               {
                                   int res;
                                   if (a.Mass == 0)
                                       res = -1;
                                   else if (b.Mass == 0)
                                       res = 1;
                                   else
                                       res = b.Mass.CompareTo(a.Mass);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.OwnerName:
                    items.Sort((a, b) =>
                               {
                                   int res = string.Compare(a.OwnerName, b.OwnerName, StringComparison.CurrentCultureIgnoreCase);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.DistanceFromCenter:
                    items.Sort((a, b) =>
                               {
                                   int res = a.Position.LengthSquared().CompareTo(b.Position.LengthSquared());
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.Speed:
                    items.Sort((a, b) =>
                               {
                                   int res = b.Speed.CompareTo(a.Speed);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.DistanceFromPlayers:
                    items.Sort((a, b) =>
                               {
                                   int res = b.DistanceFromPlayers.CompareTo(a.DistanceFromPlayers);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                case MyEntitySortOrder.OwnerLastLogin:
                    items.Sort((a, b) =>
                               {
                                   int res = b.OwnerLoginTime.CompareTo(a.OwnerLoginTime);
                                   if (m_invertOrder)
                                       return -res;
                                   return res;
                               });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

                // debug animation system
                MySessionComponentAnimationSystem.Static.EntitySelectedForDebug = entity;

                return true;
            }

            return false;
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

        private static void UpdateRemoveAndDepowerButton(MyGuiScreenAdminMenu menu,long entityId)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);
            menu.m_removeItemButton.Enabled = entity != null && !(entity is MyCharacter);
            if (menu.m_depowerItemButton != null)
            {
                menu.m_depowerItemButton.Enabled = entity is MyCubeGrid;
            }
            if (menu.m_stopItemButton != null)
            {
                menu.m_stopItemButton.Enabled = entity != null && !(entity is MyVoxelBase);
            }

            if (!(entity is MyVoxelBase))
                menu.m_labelEntityName.TextToDraw = new StringBuilder(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_EntityName) + (entity == null ? "-" : entity.DisplayName));
            else
                menu.m_labelEntityName.TextToDraw = new StringBuilder(MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_EntityName) + ((MyVoxelBase)entity).StorageName);
        }
        
        /// <summary>
        /// Send SM settings back to requester
        /// </summary>
        [Event, Reliable, Server]
        private static void RequestSettingFromServer_Implementation()
        {
            MyMultiplayer.RaiseStaticEvent(
                x => DownloadSettingFromServer,
                MyTrashRemoval.CurrentRemovalInterval,
                MyTrashRemoval.PreviewEnabled,
                MyTrashRemoval.PreviewSettings,
                MyTrashRemoval.RemovalPaused,
                MyTrashRemoval.TrashOperation,
                MyEventContext.Current.Sender);
        }
        
        #endregion

        #region UI Events

        private void OnStopEntities(MyGuiControlButton myGuiControlButton)
        {
            MyMultiplayer.RaiseStaticEvent(x => StopEntities_Implementation);
        }

        private void ValueChanged(MyEntitySortOrder selectedOrder)
        {
            if (m_selectedSort == selectedOrder)
                m_invertOrder = !m_invertOrder;
            else
                m_invertOrder = false;

            m_selectedSort = selectedOrder;
           var items = new List<MyEntityListInfoItem>(m_entityListbox.Items.Count);
            foreach (var item in m_entityListbox.Items)
            {
                items.Add((MyEntityListInfoItem)item.UserData);
            }
            SortEntityList(selectedOrder, ref items);
            m_entityListbox.Items.Clear();
            var selectedType = (MyEntityTypeEnum)m_entityTypeCombo.GetSelectedKey();
            bool isGrid = selectedType == MyEntityTypeEnum.Grids || selectedType == MyEntityTypeEnum.LargeGrids || selectedType == MyEntityTypeEnum.SmallGrids;
            foreach (var sortedItem in items)
            {
                StringBuilder displayName = GetFormattedDisplayName(selectedOrder, sortedItem, isGrid);

                m_entityListbox.Add(new MyGuiControlListbox.Item(displayName, GetTooltipText(sortedItem, isGrid), userData: sortedItem));
            }
        }

        private void EntityListItemClicked(MyGuiControlListbox myGuiControlListbox)
        {
            var item = (MyEntityListInfoItem)myGuiControlListbox.SelectedItems[myGuiControlListbox.SelectedItems.Count -1].UserData;
           
            m_attachCamera = item.EntityId;
            if (!TryAttachCamera(item.EntityId))
            {
                //Client doesn't have this entity right now. Move camera to where it *should* be and wait for replication
                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, item.Position + Vector3.One * 50);
                MyMultiplayer.RaiseStaticEvent(x => EntityRequest_Server, item.EntityId);
            }
        }

        private void ValueChanged(MyEntityTypeEnum myEntityTypeEnum)
        {
            m_selectedType = myEntityTypeEnum;
            MyMultiplayer.RaiseStaticEvent(x => EntityListRequest, myEntityTypeEnum);
        }

        private void OnModeComboSelect()
        {
            m_currentPage = (MyPageEnum)m_modeCombo.GetSelectedKey();
            RecreateControls(false);
        }

        private void OnRemoveFloating(MyGuiControlButton obj)
        {
            if (Sync.IsServer == false)
            {
                MyMultiplayer.RaiseStaticEvent(x => RemoveFloating_Implementation);
            }
            else
            {
                RemoveFloating_Implementation();
            }
        }

        void OnCycleClicked(bool reset, bool forward)
        {
            if (m_order != MyEntityCyclingOrder.Gps)
            {
                MyMultiplayer.RaiseStaticEvent(x => CycleRequest_Implementation, m_order, reset, forward, m_metricValue, m_entityId, m_cyclingOtions);
            }
            else
            {
                CircleGps(reset, forward);
            }

        }

        private void OnPlayerControl(MyGuiControlButton obj)
        {
            m_attachCamera = 0;
            MySessionComponentAnimationSystem.Static.EntitySelectedForDebug = null; // reset debugging
            MyGuiScreenGamePlay.SetCameraController();
        }

        private void OnTeleportButton(MyGuiControlButton obj)
        {
            if (MySession.Static.CameraController == MySession.Static.LocalCharacter)
                return;

            //TODO: Match character velocity with the ship we're teleporting to?
            MyMultiplayer.TeleportControlledEntity(MySpectatorCameraController.Static.Position);
        }

        protected void OnOrderChanged(MyEntityCyclingOrder obj)
        {
            m_order = obj;
            UpdateSmallLargeGridSelection();
            UpdateCyclingAndDepower();

            OnCycleClicked(true, true);
        }

        void OnEntityListRemoveClicked(MyTrashRemovalOperation operation)
        {
            var entitiesToRemove = new List<long>();
            var itemsToRemove = new List<MyGuiControlListbox.Item>();

            foreach (var item in m_entityListbox.SelectedItems)
            {
                entitiesToRemove.Add(((MyEntityListInfoItem)item.UserData).EntityId);
                itemsToRemove.Add(item);
            }

            if (operation == MyTrashRemovalOperation.Remove)
            {
                m_entityListbox.SelectedItems.Clear();
                foreach (var item in itemsToRemove)
                    m_entityListbox.Items.Remove(item);
            }

            MyMultiplayer.RaiseStaticEvent(x => RemoveEntities_Implementation, entitiesToRemove, operation);
        }

        void OnEntityRemoveClicked(MyTrashRemovalOperation operation)
        {
            if (m_attachCamera != 0)
            {
                MyMultiplayer.RaiseStaticEvent(x => RemoveEntity_Implementation, m_attachCamera, operation);
            }
        }

        private void RaiseAdminSettingsChanged()
        {
            MyMultiplayer.RaiseStaticEvent(x => AdminSettingsChanged, MySession.Static.AdminSettings);
        }
        
        void OnTrashFlagChanged(MyTrashRemovalFlags flag, bool value)
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
            MySession.Static.EnableCreativeTools(Sync.MyId,checkbox.IsChecked);
        }

        private void OnInvulnerableChanged(MyGuiControlCheckbox checkbox)
        {
            if(checkbox.IsChecked)
                MySession.Static.AdminSettings |= AdminSettingsEnum.Invulnerable;
            else
                MySession.Static.AdminSettings &= ~AdminSettingsEnum.Invulnerable;

            RaiseAdminSettingsChanged();

            if (MySession.Static.LocalCharacter != null)
                MyMultiplayer.RaiseStaticEvent(x => SetCharacterInvulnerable, checkbox.IsChecked, MySession.Static.LocalCharacter.EntityId);
        }

        private void OnUseTerminalsChanged(MyGuiControlCheckbox checkbox)
        {
            if (checkbox.IsChecked)
                MySession.Static.AdminSettings |= AdminSettingsEnum.UseTerminals;
            else
                MySession.Static.AdminSettings &= ~AdminSettingsEnum.UseTerminals;

            RaiseAdminSettingsChanged();
        }

        private void OnShowPlayersChanged(MyGuiControlCheckbox checkbox)
        {
            if (checkbox.IsChecked)
                MySession.Static.AdminSettings |= AdminSettingsEnum.ShowPlayers;
            else
                MySession.Static.AdminSettings &= ~AdminSettingsEnum.ShowPlayers;

            RaiseAdminSettingsChanged();
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

        private void OnTrashButtonClicked(MyGuiControlButton obj)
        {
            MyTrashRemoval.RemovalPaused = !MyTrashRemoval.RemovalPaused;
            if (MyTrashRemoval.RemovalPaused)
                obj.Text = MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_ResumeTrashButton);
            else
                obj.Text = MyTexts.GetString(MyCommonTexts.ScreenDebugAdminMenu_PauseTrashButton);
            RecalcTrash();
        }

        #endregion

        #region Server Events

        [Event, Server, Reliable]
        private static void StopEntities_Implementation()
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            foreach (var entity in MyEntities.GetEntities())
            {
                if (entity.Physics == null || entity.Closed)
                    continue;

                //don't stop players
                if (entity is MyCharacter)
                    continue;

                //don't stop piloted ships
                if (MySession.Static.Players.GetEntityController(entity) != null)
                    continue;

                entity.Physics.ClearSpeed();
            }
        }

        [Event, Reliable, Server]
        private static void EntityRequest_Server(long entityId)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(entityId, out entity))
                return;

            MyMultiplayer.ReplicateImmediatelly(entity as IMyEventProxy);
        }

        //client wants a list of ALL entities in the world of a given type
        //we have to process this on the server because the client only has a fraction of the entities
        [Event, Reliable, Server]
        private static void EntityListRequest(MyEntityTypeEnum selectedType)
        {
            var entities = MyEntities.GetEntities();
            var result = new List<MyEntityListInfoItem>(entities.Count);
            switch (selectedType)
            {
                case MyEntityTypeEnum.Grids:
                    foreach (var entity in entities)
                    {
                        var grid = entity as MyCubeGrid;
                        if (grid == null)
                            continue;
                        if (grid.Closed || grid.Physics==null)
                            continue;

                        string ownerName = string.Empty;

                        if (grid.BigOwners.Count > 0)
                            ownerName = MySession.Static.Players.TryGetIdentity(grid.BigOwners[0]).DisplayName;

                        result.Add(new MyEntityListInfoItem(grid.DisplayName, grid.EntityId, grid.BlocksCount, grid.Physics.Mass, grid.PositionComp.GetPosition(), grid.Physics.LinearVelocity.Length(), GetPlayerDistance(grid), ownerName, GetOwnerLoginTimeSeconds(grid)));
                    }
                    break;
                case MyEntityTypeEnum.SmallGrids:
                    foreach (var entity in entities)
                    {
                        var grid = entity as MyCubeGrid;
                        if (grid == null)
                            continue;
                        if (grid.Closed || grid.Physics == null)
                            continue;

                        if (grid.GridSizeEnum != MyCubeSize.Small)
                            continue;

                        string ownerName = string.Empty;

                        if (grid.BigOwners.Count > 0)
                            ownerName = MySession.Static.Players.TryGetIdentity(grid.BigOwners[0]).DisplayName;

                        result.Add(new MyEntityListInfoItem(grid.DisplayName, grid.EntityId, grid.BlocksCount, grid.Physics.Mass, grid.PositionComp.GetPosition(), grid.Physics.LinearVelocity.Length(), GetPlayerDistance(grid), ownerName, GetOwnerLoginTimeSeconds(grid)));
                    }
                    break;
                case MyEntityTypeEnum.LargeGrids:
                    foreach (var entity in entities)
                    {
                        var grid = entity as MyCubeGrid;
                        if (grid == null)
                            continue;
                        if (grid.Closed || grid.Physics == null)
                            continue;

                        if (grid.GridSizeEnum != MyCubeSize.Large)
                            continue;

                        string ownerName = string.Empty;

                        if (grid.BigOwners.Count > 0)
                            ownerName = MySession.Static.Players.TryGetIdentity(grid.BigOwners[0]).DisplayName;

                        result.Add(new MyEntityListInfoItem(grid.DisplayName, grid.EntityId, grid.BlocksCount, grid.Physics.Mass, grid.PositionComp.GetPosition(), grid.Physics.LinearVelocity.Length(), GetPlayerDistance(grid), ownerName, GetOwnerLoginTimeSeconds(grid)));
                    }
                    break;
                case MyEntityTypeEnum.Characters:
                    //use GetOnlinePlayers because characters are removed from the entity list while in a cockpit
                    foreach(var player in MySession.Static.Players.GetOnlinePlayers())
                    {
                        var character = player.Character;
                        if (character == null)
                            continue;

                        if (character.Closed || character.Physics == null)
                            continue;

                        result.Add(new MyEntityListInfoItem(character.DisplayName, character.EntityId, 0, character.CurrentMass, character.PositionComp.GetPosition(), character.Physics.LinearVelocity.Length(), 0));
                    }
                    break;
                case MyEntityTypeEnum.FloatingObjects:
                    foreach (var entity in entities)
                    {
                        var floating = entity as MyFloatingObject;
                        if (floating == null)
                            continue;

                        if (floating.Closed || floating.Physics == null)
                            continue;

                        result.Add(new MyEntityListInfoItem(floating.DisplayName, floating.EntityId, 0, floating.Physics.Mass, floating.PositionComp.GetPosition(), floating.Physics.LinearVelocity.Length(), GetPlayerDistance(floating)));
                    }
                    break;
                case MyEntityTypeEnum.Planets:
                    foreach (var entity in entities)
                    {
                        var planet = entity as MyPlanet;
                        if (planet == null)
                            continue;

                        if (planet.Closed)
                            continue;

                        result.Add(new MyEntityListInfoItem(planet.StorageName, planet.EntityId, 0, 0, planet.PositionComp.GetPosition(), 0, GetPlayerDistance(planet)));
                    }
                    break;
                case MyEntityTypeEnum.Asteroids:
                    foreach (var entity in entities)
                    {
                        var asteroid = entity as MyVoxelBase;
                        if (asteroid == null || asteroid is MyPlanet)
                            continue;

                        if (asteroid.Closed)
                            continue;

                        result.Add(new MyEntityListInfoItem(asteroid.StorageName, asteroid.EntityId, 0, 0, asteroid.PositionComp.GetPosition(), 0, GetPlayerDistance(asteroid)));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (!MyEventContext.Current.IsLocallyInvoked)
                MyMultiplayer.RaiseStaticEvent(x => EntityListResponse, result, MyEventContext.Current.Sender);
            else
                EntityListResponse(result);
        }

        [Event, Reliable, Server]
        static void RemoveFloating_Implementation()
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            foreach (var entity in MyEntities.GetEntities())
            {
                MyFloatingObject floating = entity as MyFloatingObject;
                if (floating != null)
                {
                    floating.SendCloseRequest();
                }
            }
        }

        [Event, Reliable, Server]
        static void CycleRequest_Implementation(MyEntityCyclingOrder order, bool reset, bool findLarger, float metricValue, long currentEntityId,CyclingOptions options)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

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

        [Event, Server, Reliable]
        private static void RemoveEntities_Implementation(List<long> entityIds, MyTrashRemovalOperation operation)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            foreach (var entityId in entityIds)
            {
                MyEntity entity;
                if (MyEntities.TryGetEntityById(entityId, out entity))
                    MyTrashRemoval.ApplyOperation(entity, operation);
            }
        }

        [Event, Reliable, Server]
        static void UploadSettingsToServer(int remInt, bool isPrev, MyTrashRemovalSettings setting, bool isPaused, MyTrashRemovalOperation oper)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            MyTrashRemoval.PreviewSettings = setting;
            MyTrashRemoval.TrashOperation = oper;
            MyTrashRemoval.RemovalPaused = isPaused;
            MyTrashRemoval.PreviewEnabled = isPrev;
            MyTrashRemoval.CurrentRemovalInterval = remInt;
        }

        [Event, Reliable, Server]
        static void RemoveEntity_Implementation(long entityId, MyTrashRemovalOperation operation)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
                MyTrashRemoval.ApplyOperation(entity, operation);
        }

        [Event, Reliable, Server]
        static void TrashOptionChanged_Implementation(MyTrashRemovalOperation operation)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            MyTrashRemoval.TrashOperation = operation;
            //MyTrashRemoval.Apply(settings, operation);
        }

        [Event, Reliable, Server]
        static void ReplicateEverything_Implementation()
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return;

            if (!MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            var server = (MyReplicationServer)MyMultiplayer.Static.ReplicationLayer;
            server.ForceEverything(MyEventContext.Current.Sender);
        }

        [Event, Reliable, Server]
        private static void AdminSettingsChanged(AdminSettingsEnum settings)
        {
            ulong steamId = MyEventContext.Current.Sender.Value;
            if (MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE && !MySession.Static.IsUserAdmin(steamId))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            MySession.Static.RemoteAdminSettings[steamId] = settings;
        }

        [Event, Reliable, Server]
        private static void SetCharacterInvulnerable(bool enabled, long entityId)
        {
            if (MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE && !MySession.Static.IsUserAdmin(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            if (enabled)
                MyLargeTurretBase.IgnoredEntities.Add(entityId);
            else
                MyLargeTurretBase.IgnoredEntities.Remove(entityId);
        }
        
        #endregion

        #region Client Events

        [Event, Reliable, Client]
        private static void EntityListResponse(List<MyEntityListInfoItem> entities)
        {
            var menu = MyScreenManager.GetFirstScreenOfType<MyGuiScreenAdminMenu>();
            if (menu == null)
                return;
            
            var listbox = menu.m_entityListbox;

            listbox.Items.Clear();

            SortEntityList(menu.m_selectedSort, ref entities);
            bool isGrid = menu.m_selectedType == MyEntityTypeEnum.Grids || menu.m_selectedType == MyEntityTypeEnum.LargeGrids || menu.m_selectedType == MyEntityTypeEnum.SmallGrids;
            foreach (var info in entities)
            {
                var sb = GetFormattedDisplayName(menu.m_selectedSort, info, isGrid);
                listbox.Items.Add(new MyGuiControlListbox.Item(sb, GetTooltipText(info, isGrid), userData: info));
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

        /// <summary>
        /// Catch response on SM settings. Save them and show SM.
        /// </summary>
        /// <param name="remInt"></param>
        /// <param name="isPrev"></param>
        /// <param name="setting"></param>
        /// <param name="isPaused"></param>
        /// <param name="oper"></param>
        [Event, Reliable, Client]
        private static void DownloadSettingFromServer(int remInt, bool isPrev, MyTrashRemovalSettings setting, bool isPaused, MyTrashRemovalOperation oper)
        {
            MyTrashRemoval.CurrentRemovalInterval = remInt;
            MyTrashRemoval.PreviewEnabled = isPrev;
            MyTrashRemoval.PreviewSettings = setting;
            MyTrashRemoval.RemovalPaused = isPaused;
            MyTrashRemoval.TrashOperation = oper;
            MyGuiScreenAdminMenu.m_static.CreateScreen();
        }

        #endregion

        #region Overrides

        public override bool Update(bool hasFocus)
        {
            if (m_attachCamera != 0)
            {
                TryAttachCamera(m_attachCamera);
                UpdateRemoveAndDepowerButton(this, m_attachCamera);
            }

            return base.Update(hasFocus);
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
        
        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12) || MyInput.Static.IsNewKeyPressed(MyKeys.F11) || MyInput.Static.IsNewKeyPressed(MyKeys.F10))
            {
                this.CloseScreen();
            }
        }

        public override bool CloseScreen()
        {
            m_static = null;
            return base.CloseScreen();
        }

        #endregion

        #region GUI Helpers

        protected virtual void CreateTrashCheckBoxes(ref Vector2 cbOffset, ref Vector4 white)
        {
            AddTrashCheckbox(MyTrashRemovalFlags.Fixed, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Stationary, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Linear, true, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Accelerating, MySession.Static.IsAdministrator, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Powered, MySession.Static.IsAdministrator, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.Controlled, MySession.Static.IsAdministrator, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.WithProduction, MySession.Static.IsAdministrator, null, white, cbOffset);
            AddTrashCheckbox(MyTrashRemovalFlags.WithMedBay, MySession.Static.IsAdministrator, null, white, cbOffset);
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
            var btn = CreateDebugButton(usableWidth / 3, MySpaceTexts.ScreenDebugAdminMenu_DepowerTrash, c => OnTrashOptionChanged(MyTrashRemovalOperation.Depower));
            btn.PositionX = usableWidth / 3 - controlPadding + separatorSize;
        }

        private void OnTrashOptionChanged(MyTrashRemovalOperation obj)
        {
            MyTrashRemoval.TrashOperation = obj;
            MyMultiplayer.RaiseStaticEvent(x => TrashOptionChanged_Implementation, obj);
        }
        
        protected void AddTrashCheckbox(MyTrashRemovalFlags flag, bool enabled, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            string name = string.Format(MyTrashRemoval.GetName(flag), String.Empty);
            AddCheckBox(name, (MyTrashRemoval.PreviewSettings.Flags & flag) == flag, c => OnTrashFlagChanged(flag, c.IsChecked), enabled, controlGroup, color, checkBoxOffset);
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
       
        #endregion
    }
}
