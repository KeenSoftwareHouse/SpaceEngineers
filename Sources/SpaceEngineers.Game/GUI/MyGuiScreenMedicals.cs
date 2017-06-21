#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.World;
using VRage;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace SpaceEngineers.Game.GUI
{
    [StaticEventOwner]
    public class MyGuiScreenMedicals : MyGuiScreenBase
    {
        class MyMedicalRoomInfo
        {
            public long MedicalRoomId;
            public string MedicalRoomName;
            public float OxygenLevel;
            public long OwnerId;
            public Vector3D PrefferedCameraPosition;
            public Vector3D MedicalRoomPosition;
            public Vector3D MedicalRoomUp;
            public Vector3 MedicalRoomVelocity;
        }

        #region Fields

        MyGuiControlLabel m_labelNoRespawn;
        StringBuilder m_noRespawnHeader = new StringBuilder();
        MyGuiControlTable m_respawnsTable;
        MyGuiControlButton m_respawnButton;
        MyGuiControlButton m_refreshButton;
        MyGuiControlMultilineText m_noRespawnText;

        MyGuiControlMultilineText m_multilineRespawnWhenShipReady;
        MyRespawnShipDefinition m_selectedRespawnShip;

        public static StringBuilder NoRespawnText
        { //get { return Static.m_noRespawnText.Text; } 
            set
            {
                if (Static != null)
                    Static.m_noRespawnText.Text = value;
            }
        }
        public static int ItemsInTable
        {
            get
            {
                if (Static == null || Static.m_respawnsTable == null)
                    return 0;
                return Static.m_respawnsTable.RowsCount;
            }
        }

        public static MyGuiScreenMedicals Static { get; private set; }

        static List<MyMedicalRoomInfo> m_medicalRooms = new List<MyMedicalRoomInfo>();

        #endregion

        #region Constructor

        public MyGuiScreenMedicals()
            : base(new Vector2(0.85f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            Static = this;
            EnabledBackgroundFade = true;
            CloseButtonEnabled = false;
            m_closeOnEsc = false;
            m_selectedRespawnShip = null;

            RecreateControls(true);

            MySandboxGame.PausePush();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMyGuiScreenMedicals";
        }

        protected override void OnClosed()
        {
            MySandboxGame.PausePop();
            base.OnClosed();
        }

        #endregion

        #region Input

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                if (!MyInput.Static.IsAnyShiftKeyPressed())
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(new MyGuiScreenMainMenu());
                }
                else
                {

                    if (m_respawnsTable.SelectedRow.UserData == null || m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo == null)
                    {
                        MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, new Vector3D(1000000)); //just somewhere out of the game area to see our beautiful skybox
                        return;
                    }

                    var medicalRoom = m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo;

                    MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, medicalRoom.PrefferedCameraPosition);
                    MySpectatorCameraController.Static.SetTarget(medicalRoom.MedicalRoomPosition, medicalRoom.MedicalRoomUp);
                    MySpectatorCameraController.Static.Velocity = medicalRoom.MedicalRoomVelocity;

                    Close();
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
            {
                onRespawnClick(m_respawnButton);
            }
        }

        #endregion

        #region Recreate

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            string filepath = MakeScreenFilepath("MedicalsScreen");
            MyObjectBuilder_GuiScreen objectBuilder;

            var fsPath = Path.Combine(MyFileSystem.ContentPath, filepath);
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_GuiScreen>(fsPath, out objectBuilder);
            Init(objectBuilder);

            m_multilineRespawnWhenShipReady = new MyGuiControlMultilineText()
            {
                Position = new Vector2(0, -0.5f * Size.Value.Y + 80f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                Size = new Vector2(Size.Value.X * 0.85f, 75f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                Font = MyFontEnum.Red,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            };

            Controls.Add(m_multilineRespawnWhenShipReady);

            UpdateRespawnShipLabel();

            m_respawnsTable = new MyGuiControlTable();
            m_respawnsTable.Position = new Vector2(0, -0.01f);
            m_respawnsTable.Size = new Vector2(550f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1.3f);
            m_respawnsTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_respawnsTable.VisibleRowsCount = 17;
            Controls.Add(m_respawnsTable);

            m_respawnsTable.ColumnsCount = 2;
            m_respawnsTable.ItemSelected += OnTableItemSelected;
            m_respawnsTable.ItemDoubleClicked += OnTableItemDoubleClick;
            m_respawnsTable.SetCustomColumnWidths(new float[] { 0.50f, 0.50f });

            m_respawnsTable.SetColumnName(0, MyTexts.Get(MyCommonTexts.Name));
            m_respawnsTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.ScreenMedicals_OwnerTimeoutColumn));

            m_labelNoRespawn = new MyGuiControlLabel()
            {
                Position = new Vector2(0, -0.35f),
                ColorMask = Color.Red,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            Controls.Add(m_labelNoRespawn);

            m_respawnButton = new MyGuiControlButton(
                            position: new Vector2(-0.1f, 0.35f),
                            text: MyTexts.Get(MyCommonTexts.Respawn),
                            onButtonClick: onRespawnClick
                            );
            Controls.Add(m_respawnButton);

            m_refreshButton = new MyGuiControlButton(
                          position: new Vector2(0.1f, 0.35f),
                          text: MyTexts.Get(MyCommonTexts.Refresh),
                          onButtonClick: onRefreshClick
                          );
            Controls.Add(m_refreshButton);

            m_noRespawnText = new MyGuiControlMultilineText(
                            position: new Vector2(-0.02f, -0.19f),
                            size: new Vector2(0.32f, 0.5f),
                            contents: MyTexts.Get(MySpaceTexts.ScreenMedicals_NoRespawnPossible),
                            textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                            font: MyFontEnum.Red
                            );
            Controls.Add(m_noRespawnText);

            RefreshRespawnPoints();
        }

        private void RefreshRespawnPoints()
        {
            m_respawnsTable.Clear();

            RefreshMedicalRooms();
        }

        private void RefreshMedicalRooms()
        {
            ulong playerSteamId = MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.Id.SteamId : Sync.MyId;
            MyMultiplayer.RaiseStaticEvent(s => RefreshMedicalRooms_Implementation, MySession.Static.LocalPlayerId, playerSteamId);
        }

        private void RefreshSpawnShips()
        {
            //if (MySession.Static.CreativeMode)
            //{
            //    return;
            //}
            var respawnShips = MyDefinitionManager.Static.GetRespawnShipDefinitions();
            foreach (var pair in respawnShips)
            {
                var respawnShip = pair.Value;
                var row = new MyGuiControlTable.Row(respawnShip);

                //Add description or name?
                row.AddCell(new MyGuiControlTable.Cell(text: respawnShip.DisplayNameText));

                var respawnTimeCell = new MyGuiControlTable.Cell(String.Empty);
                AddShipRespawnInfo(respawnShip, respawnTimeCell.Text);
                row.AddCell(respawnTimeCell);

                m_respawnsTable.Add(row);
            }
        }

        private void AddRespawnInSuit()
        {
            var row = new MyGuiControlTable.Row();
            row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.GetString(MySpaceTexts.SpawnInSpaceSuit)));
            row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.GetString(MySpaceTexts.ScreenMedicals_RespawnShipReady)));

            m_respawnsTable.Add(row);
        }

        private void UpdateSpawnShipTimes()
        {
            for (int i = 0; i < m_respawnsTable.RowsCount; ++i)
            {
                var row = m_respawnsTable.GetRow(i);
                var respawnShip = row.UserData as MyRespawnShipDefinition;
                if (respawnShip == null) continue;

                row.GetCell(1).Text.Clear();
                AddShipRespawnInfo(respawnShip, row.GetCell(1).Text);
            }
        }

        private static void AddShipRespawnInfo(MyRespawnShipDefinition respawnShip, StringBuilder text)
        {
            var rc = MySpaceRespawnComponent.Static;
            int respawnSeconds = MySession.Static.LocalHumanPlayer == null ? 0 : rc.GetRespawnCooldownSeconds(MySession.Static.LocalHumanPlayer.Id, respawnShip.Id.SubtypeName);
            if (!rc.IsSynced)
                text.Append(MyTexts.Get(MySpaceTexts.ScreenMedicals_RespawnShipNotReady));
            else if (respawnSeconds != 0)
                MyValueFormatter.AppendTimeExact(respawnSeconds, text);
            else
                text.Append(MyTexts.Get(MySpaceTexts.ScreenMedicals_RespawnShipReady));
        }

        private void UpdateRespawnShipLabel()
        {
            if (m_selectedRespawnShip == null)
            {
                m_multilineRespawnWhenShipReady.Visible = false;
            }
            else
            {
                MySpaceRespawnComponent.Static.GetRespawnCooldownSeconds(MySession.Static.LocalHumanPlayer.Id, m_selectedRespawnShip.Id.SubtypeName);
                m_multilineRespawnWhenShipReady.Text.Clear().AppendFormat(MyTexts.GetString(MySpaceTexts.ScreenMedicals_RespawnWhenShipReady), m_selectedRespawnShip.DisplayNameText);
                m_multilineRespawnWhenShipReady.RefreshText(false);
                m_multilineRespawnWhenShipReady.Visible = true;
            }
        }

        private static StringBuilder GetOwnerDisplayName(long owner)
        {
            if (owner == 0) return MyTexts.Get(MySpaceTexts.BlockOwner_Nobody);

            var identity = Sync.Players.TryGetIdentity(owner);
            if (identity != null) return new StringBuilder(identity.DisplayName);
            else return MyTexts.Get(MySpaceTexts.BlockOwner_Unknown);
        }

        [Event, Reliable, Server]
        static void RefreshMedicalRooms_Implementation(long playerId, ulong steamId)
        {
            m_medicalRooms.Clear();
            GetAvailableMedicalRooms(playerId, m_medicalRooms);

            MyMultiplayer.RaiseStaticEvent(s => RefreshMedicalRoomsResponse_Implementation, m_medicalRooms, new EndpointId(steamId));

        }

        static void GetAvailableMedicalRooms(long playerId, List<MyMedicalRoomInfo> medicalRooms)
        {
            List<MyCubeGrid> cubeGrids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();
            foreach (var grid in cubeGrids)
            {
                grid.GridSystems.UpdatePower();

                foreach (var slimBlock in grid.GetBlocks())
                {
                    MyMedicalRoom medicalRoom = slimBlock.FatBlock as MyMedicalRoom;
                    if (medicalRoom != null)
                    {
                        medicalRoom.UpdateIsWorking();

                        if (medicalRoom.IsWorking)
                        {
                            if (medicalRoom.HasPlayerAccess(playerId) || medicalRoom.SetFactionToSpawnee)
                            {
                                MyMedicalRoomInfo info = new MyMedicalRoomInfo();
                                info.MedicalRoomId = medicalRoom.EntityId;
                                info.MedicalRoomName = medicalRoom.CustomName != null ? medicalRoom.CustomName.ToString() : (medicalRoom.Name != null ? medicalRoom.Name : medicalRoom.ToString());
                                info.OxygenLevel = medicalRoom.GetOxygenLevel();
                                info.OwnerId = medicalRoom.IDModule.Owner;

                                Vector3D medRoomPosition = medicalRoom.PositionComp.GetPosition();
                                Vector3D preferredCameraPosition = medRoomPosition + medicalRoom.WorldMatrix.Up * 20 + medicalRoom.WorldMatrix.Right * 20 + medicalRoom.WorldMatrix.Forward * 20;
                                Vector3D? cameraPosition = MyEntities.FindFreePlace(preferredCameraPosition, 1);

                                if (!cameraPosition.HasValue)
                                    cameraPosition = preferredCameraPosition;

                                info.PrefferedCameraPosition = cameraPosition.Value;
                                info.MedicalRoomPosition = medRoomPosition;
                                info.MedicalRoomUp = medicalRoom.PositionComp.WorldMatrix.Up;
                                if (medicalRoom.CubeGrid.Physics != null)
                                    info.MedicalRoomVelocity = medicalRoom.CubeGrid.Physics.LinearVelocity;

                                medicalRooms.Add(info);
                            }
                        }
                    }

                }
            }
        }

        [Event, Reliable, Client]
        static void RefreshMedicalRoomsResponse_Implementation(List<MyMedicalRoomInfo> medicalRooms)
        {
            MyGuiScreenMedicals.Static.RefreshMedicalRooms(medicalRooms);
        }

        void RefreshMedicalRooms(List<MyMedicalRoomInfo> medicalRooms)
        {
            m_respawnsTable.Clear();

            foreach (var medRoom in medicalRooms)
            {
                var row = new MyGuiControlTable.Row(medRoom);
                row.AddCell(new MyGuiControlTable.Cell(text: medRoom.MedicalRoomName));


                var ownerText = new StringBuilder();
                if (MySession.Static.Settings.EnableOxygen)
                {
                    ownerText.Append("O2 ");
                    ownerText.Append((medRoom.OxygenLevel * 100).ToString("F0"));
                    ownerText.Append("% ");
                }
                ownerText.AppendStringBuilder(GetOwnerDisplayName(medRoom.OwnerId));

                row.AddCell(new MyGuiControlTable.Cell(text: ownerText));
                m_respawnsTable.Add(row);
            }

            
            if (MySession.Static.CreativeMode)
            {
                AddRespawnInSuit();
            }
            else
            if ((MySession.Static.Settings.EnableRespawnShips && !MySession.Static.Settings.Scenario))
            {
                RefreshSpawnShips();
                AddRespawnInSuit();
            }               

            if (m_respawnsTable.RowsCount > 0)
            {
                m_respawnsTable.SelectedRowIndex = 0;
                OnTableItemSelected(null, new MyGuiControlTable.EventArgs());
                m_noRespawnText.Visible = false;
            }
            else
            {
                m_noRespawnText.Visible = true;
            }
        }

        public override bool Update(bool hasFocus)
        {
            /*if (m_respawnsTable.RowsCount == 0 && State != MyGuiScreenState.CLOSING && MySession.Static.LocalHumanPlayer != null)
            {
                MyPlayerCollection.RespawnRequest(joinGame: true, newPlayer: true, medicalId: 0, shipPrefabId: null);
                CloseScreen();
            }*/
            UpdateSpawnShipTimes();
            bool retval = base.Update(hasFocus);

            if (m_selectedRespawnShip != null)
            {
                var rc = MySpaceRespawnComponent.Static;
                int cooldown = rc.GetRespawnCooldownSeconds(MySession.Static.LocalHumanPlayer.Id, m_selectedRespawnShip.Id.SubtypeName);
                if (rc.IsSynced && cooldown == 0)
                    RespawnShipImmediately(m_selectedRespawnShip.Id.SubtypeName);
            }

            if (m_respawnsTable.RowsCount == 0)
            {
                RefreshRespawnPoints();//because medical rooms are not powered when the map starts
            }


            if (m_labelNoRespawn.Text == null)
                m_labelNoRespawn.Visible = false;
            else
                m_labelNoRespawn.Visible = true;

            return retval;
        }

        public static void Close()
        {
            if (Static != null)
                Static.CloseScreen();
        }


        public override bool HandleInputAfterSimulation()
        {
            if (m_respawnsTable.SelectedRow != null && MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.Entity)
            {
                MyMedicalRoomInfo userData = m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo;

                if (userData != null)
                {
                    m_respawnButton.Enabled = false;
                    MyMedicalRoom medicalRoom;
                    if (MyEntities.TryGetEntityById<MyMedicalRoom>(userData.MedicalRoomId, out medicalRoom))
                    {
                        m_respawnButton.Enabled = true;
                        Vector3D medRoomPosition = (Vector3D)medicalRoom.PositionComp.GetPosition();
                        Vector3D preferredCameraPosition = medRoomPosition + medicalRoom.WorldMatrix.Up * 20 + medicalRoom.WorldMatrix.Right * 20 + medicalRoom.WorldMatrix.Forward * 20;
                        Vector3D? cameraPosition = MyEntities.FindFreePlace(preferredCameraPosition, 1);

                        if (!cameraPosition.HasValue)
                            cameraPosition = preferredCameraPosition;

                        MySpectatorCameraController.Static.Position = cameraPosition.Value;
                        MySpectatorCameraController.Static.SetTarget(medRoomPosition, medicalRoom.WorldMatrix.Up);                        
                    }
                }
            }

            return true;
        }

        private int m_lastTimeSec = -1;
        public static void SetNoRespawnText(StringBuilder text, int timeSec)
        {
            if (Static != null)
                Static.SetNoRespawnTexts(text, timeSec);
        }
        public void SetNoRespawnTexts(StringBuilder text, int timeSec)
        {
            NoRespawnText = text;
            if (timeSec != m_lastTimeSec)
            {
                m_lastTimeSec = timeSec;
                int minutes = timeSec / 60;
                m_noRespawnHeader.Clear().AppendFormat(MyTexts.GetString(MySpaceTexts.ScreenMedicals_NoRespawnPlaceHeader), minutes, timeSec - minutes * 60);
                m_labelNoRespawn.Text = m_noRespawnHeader.ToString();
            }
        }
        #endregion

        #region Event handling

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (m_respawnsTable.SelectedRow != null)
            {
                m_respawnButton.Enabled = true;

                if (m_respawnsTable.SelectedRow.UserData == null || m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo == null)
                {
                    MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, new Vector3D(1000000)); //just somewhere out of the game area to see our beautiful skybox
                    return;
                }

                var medicalRoom = m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo;

                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, medicalRoom.PrefferedCameraPosition);
                MySpectatorCameraController.Static.SetTarget(medicalRoom.MedicalRoomPosition, medicalRoom.MedicalRoomUp);
            }
            else
            {
                m_respawnButton.Enabled = false;
            }
        }

        private void OnTableItemDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (m_respawnsTable.SelectedRow != null)
            {
                MyMedicalRoomInfo userData = m_respawnsTable.SelectedRow.UserData as MyMedicalRoomInfo;
                MyMedicalRoom medicalRoom;
                if (userData == null || (userData != null && MyEntities.TryGetEntityById<MyMedicalRoom>(userData.MedicalRoomId, out medicalRoom)))
                {
                    onRespawnClick(m_respawnButton);
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                       canHideOthers: false,
                                       buttonType: MyMessageBoxButtonsType.OK,
                                       messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionNotReady),
                                       messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextNotReady)));
                }
            }
        }

        void onRespawnClick(MyGuiControlButton sender)
        {
            if (m_respawnsTable.SelectedRow == null)
                return;

            var userData = m_respawnsTable.SelectedRow.UserData;

            if (userData == null)
            {
                CheckPermaDeathAndRespawn(null);
            }
            else if (userData is MyRespawnShipDefinition)
            {
                var respawnShip = userData as MyRespawnShipDefinition;
                if (MySpaceRespawnComponent.Static.GetRespawnCooldownSeconds(MySession.Static.LocalHumanPlayer.Id, respawnShip.Id.SubtypeName) != 0)
                    return;

                CheckPermaDeathAndRespawn(respawnShip.Id.SubtypeName);
            }
            else
            {
                RespawnAtMedicalRoom(((MyMedicalRoomInfo)m_respawnsTable.SelectedRow.UserData).MedicalRoomId);
            }
        }

        private void CheckPermaDeathAndRespawn(string shipPrefabId)
        {
            var identity = Sync.Players.TryGetIdentity(MySession.Static.LocalPlayerId);
            Debug.Assert(identity != null, "Could not get local player identity! This should not happen!");
            if (identity == null) return;

            if (MySession.Static.Settings.PermanentDeath.Value && identity.FirstSpawnDone)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxCaptionRespawn),
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                    {
                        if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            this.RespawnShip(shipPrefabId);
                        }
                    }));
            }
            else
            {
                this.RespawnShip(shipPrefabId);
            }
        }

        private void RespawnAtMedicalRoom(long medicalId)
        {
            MyPlayerCollection.RespawnRequest(joinGame: MySession.Static.LocalCharacter == null, newIdentity: false, respawnEntityId: medicalId, shipPrefabId: null);
            CloseScreen();
        }

        // CH: TODO: Put this into MySpaceRespawnComponent?
        private void RespawnShip(string shipPrefabId)
        {
            var rc = MySpaceRespawnComponent.Static;
            int cooldown = (shipPrefabId == null || MySession.Static.LocalHumanPlayer == null) ? 0 : rc.GetRespawnCooldownSeconds(MySession.Static.LocalHumanPlayer.Id, shipPrefabId);

            if (shipPrefabId == null || rc.IsSynced && cooldown == 0)
            {
                RespawnShipImmediately(shipPrefabId);
            }
            else
            {
                var respawnShip = MyDefinitionManager.Static.GetRespawnShipDefinition(shipPrefabId);
                m_selectedRespawnShip = respawnShip;
                UpdateRespawnShipLabel();
            }
        }

        private void RespawnShipImmediately(string shipPrefabId)
        {
            var identity = Sync.Players.TryGetIdentity(MySession.Static.LocalPlayerId);
            Debug.Assert(identity != null, "Could not get local player identity! This should not happen!");
            bool newIdentity = identity == null || identity.FirstSpawnDone;

            MyPlayerCollection.RespawnRequest(MySession.Static.LocalCharacter == null, newIdentity, 0, shipPrefabId);
            CloseScreen();
        }

        void onRefreshClick(MyGuiControlButton sender)
        {
            RefreshRespawnPoints();
        }

        #endregion

    }
}
