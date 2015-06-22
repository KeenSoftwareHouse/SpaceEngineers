#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.FileSystem;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace SpaceEngineers.Game.GUI
{
    class MyGuiScreenMedicals : MyGuiScreenBase
    {

        #region Fields

        MyGuiControlTable m_respawnsTable;
        MyGuiControlButton m_respawnButton;
        MyGuiControlButton m_refreshButton;
        MyGuiControlMultilineText m_noRespawnText;

        MyGuiControlMultilineText m_multilineRespawnWhenShipReady;
        MyRespawnShipDefinition m_selectedRespawnShip;

        #endregion

        #region Constructor

        public MyGuiScreenMedicals()
            : base(new Vector2(0.85f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            EnabledBackgroundFade = true;
            CloseButtonEnabled = false;
            m_closeOnEsc = false;
            m_selectedRespawnShip = null;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMyGuiScreenMedicals";
        }

        protected override void OnClosed()
        {

            base.OnClosed();
        }

        #endregion

        #region Input

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                MyGuiScreenMainMenu.AddMainMenu();
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

            m_respawnsTable.SetColumnName(0, MyTexts.Get(MySpaceTexts.Name));
            m_respawnsTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.ScreenMedicals_OwnerTimeoutColumn));

            m_respawnButton = new MyGuiControlButton(
                            position: new Vector2(-0.1f, 0.35f),
                            text: MyTexts.Get(MySpaceTexts.Respawn),
                            onButtonClick: onRespawnClick
                            );
            Controls.Add(m_respawnButton);

            m_refreshButton = new MyGuiControlButton(
                          position: new Vector2(0.1f, 0.35f),
                          text: MyTexts.Get(MySpaceTexts.Refresh),
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
            if (!MySession.Static.Settings.DisableRespawnShips)
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

        private void RefreshMedicalRooms()
        {
            List<MyMedicalRoom> medicalRooms;
            GetAvailableMedicalRooms(MySession.LocalPlayerId, out medicalRooms);

            foreach (var medRoom in medicalRooms)
            {
                var row = new MyGuiControlTable.Row(medRoom);
                row.AddCell(new MyGuiControlTable.Cell(text: medRoom.CustomName));


                var ownerText = new StringBuilder();
                if (MySession.Static.Settings.EnableOxygen)
                {
                    ownerText.Append("O2 ");
                    ownerText.Append((medRoom.GetOxygenLevel() * 100).ToString("F0"));
                    ownerText.Append("% ");
                }
                ownerText.AppendStringBuilder(GetOwnerDisplayName(medRoom));

                row.AddCell(new MyGuiControlTable.Cell(text: ownerText));
                m_respawnsTable.Add(row);
            }
        }

        private void RefreshSpawnShips()
        {
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
            int respawnSeconds = MySession.LocalHumanPlayer == null ? 0 : rc.GetRespawnCooldownSeconds(MySession.LocalHumanPlayer.Id, respawnShip.Id.SubtypeName);
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
                MySpaceRespawnComponent.Static.GetRespawnCooldownSeconds(MySession.LocalHumanPlayer.Id, m_selectedRespawnShip.Id.SubtypeName);
                m_multilineRespawnWhenShipReady.Text.Clear().AppendFormat(MyTexts.GetString(MySpaceTexts.ScreenMedicals_RespawnWhenShipReady), m_selectedRespawnShip.DisplayNameText);
                m_multilineRespawnWhenShipReady.RefreshText(false);
                m_multilineRespawnWhenShipReady.Visible = true;
            }
        }

        private static StringBuilder GetOwnerDisplayName(MyMedicalRoom medRoom)
        {
            long owner = medRoom.IDModule.Owner;

            if (owner == 0) return MyTexts.Get(MySpaceTexts.BlockOwner_Nobody);

            var identity = Sync.Players.TryGetIdentity(owner);
            if (identity != null) return new StringBuilder(identity.DisplayName);
            else return MyTexts.Get(MySpaceTexts.BlockOwner_Unknown);
        }

        void GetAvailableMedicalRooms(long playerId, out List<MyMedicalRoom> medicalRooms)
        {
            List<MyCubeGrid> cubeGrids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();

            medicalRooms = new List<MyMedicalRoom>();

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
                            if (medicalRoom.HasPlayerAccess(playerId))
                            {
                                medicalRooms.Add(medicalRoom);
                            }
                        }
                    }
                    
                }
            }
        }

        public override bool Update(bool hasFocus)
        {
            /*if (m_respawnsTable.RowsCount == 0 && State != MyGuiScreenState.CLOSING && MySession.LocalHumanPlayer != null)
            {
                MyPlayerCollection.RespawnRequest(joinGame: true, newPlayer: true, medicalId: 0, shipPrefabId: null);
                CloseScreen();
            }*/

            UpdateSpawnShipTimes();
            bool retval = base.Update(hasFocus);

            if (m_selectedRespawnShip != null)
            {
                var rc = MySpaceRespawnComponent.Static;
                int cooldown = rc.GetRespawnCooldownSeconds(MySession.LocalHumanPlayer.Id, m_selectedRespawnShip.Id.SubtypeName);
                if (rc.IsSynced && cooldown == 0)
                    RespawnShipImmediately(m_selectedRespawnShip.Id.SubtypeName);
            }

            if (m_respawnsTable.RowsCount==0)
                RefreshRespawnPoints();//because medical rooms are not powered when the map starts
            return retval;
        }

        #endregion
     
        #region Event handling

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (m_respawnsTable.SelectedRow != null)
            {
                m_respawnButton.Enabled = true;

                if (m_respawnsTable.SelectedRow.UserData == null || m_respawnsTable.SelectedRow.UserData as MyMedicalRoom == null)
                {
                    MySession.SetCameraController(MyCameraControllerEnum.Spectator, null, new Vector3D(1000000)); //just somewhere out of the game area to see our beautiful skybox
                    return;
                }

                MyMedicalRoom medicalRoom = (MyMedicalRoom)m_respawnsTable.SelectedRow.UserData;
                Vector3D medRoomPosition = (Vector3D)medicalRoom.PositionComp.GetPosition();
                Vector3D preferredCameraPosition = medRoomPosition + medicalRoom.WorldMatrix.Up * 20 + medicalRoom.WorldMatrix.Right * 20 + medicalRoom.WorldMatrix.Forward * 20;
                Vector3D? cameraPosition = MyEntities.FindFreePlace(preferredCameraPosition, 1);
                if (!cameraPosition.HasValue)
                    cameraPosition = preferredCameraPosition;

                MySession.SetCameraController(MyCameraControllerEnum.Spectator, null, cameraPosition);
                MySpectatorCameraController.Static.Target = (Vector3D)medRoomPosition;
            }
            else
            {
                m_respawnButton.Enabled = false;
            }
        }

        private void OnTableItemDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            onRespawnClick(m_respawnButton);
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
                CheckPermaDeathAndRespawn(respawnShip.Id.SubtypeName);
            }
            else
            {
                RespawnAtMedicalRoom(((MyMedicalRoom)m_respawnsTable.SelectedRow.UserData).EntityId);
            }
        }

        private void CheckPermaDeathAndRespawn(string shipPrefabId)
        {
            bool playerInGame = false;
            foreach (var player in Sync.Clients.GetClients())
            {
                if (player.SteamUserId == MySteam.UserId)
                {
                    playerInGame = true;
                    break;
                }
            }

            if (MySession.Static.Settings.PermanentDeath.Value && playerInGame && HasAnyOwnership())
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
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

        private bool HasAnyOwnership()
        {
            long id = MySession.LocalPlayerId;
            
            var entities = MyEntities.GetEntities();
            foreach (var entity in entities)
            {
                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    var blocks = grid.CubeBlocks;
                    foreach (var block in blocks)
                    {
                        if (block.FatBlock != null)
                        {
                            if (block.FatBlock.OwnerId == id)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void RespawnAtMedicalRoom(long medicalId)
        {
            MyPlayerCollection.RespawnRequest(joinGame: MySession.LocalCharacter == null, newPlayer: false, medicalId: medicalId, shipPrefabId: null);
            CloseScreen();
        }

        // CH: TODO: Put this into MySpaceRespawnComponent?
        private void RespawnShip(string shipPrefabId)
        {
            var rc = MySpaceRespawnComponent.Static;
            int cooldown = (shipPrefabId == null || MySession.LocalHumanPlayer == null) ? 0 : rc.GetRespawnCooldownSeconds(MySession.LocalHumanPlayer.Id, shipPrefabId);

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
            MyPlayerCollection.RespawnRequest(MySession.LocalCharacter == null, true, 0, shipPrefabId);
            CloseScreen();
        }


        void onRefreshClick(MyGuiControlButton sender)
        {
            RefreshRespawnPoints();
        }

        #endregion        

    }
}
