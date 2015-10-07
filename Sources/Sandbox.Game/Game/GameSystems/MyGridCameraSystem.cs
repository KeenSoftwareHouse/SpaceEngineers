using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.EntityComponents;
using VRage.Input;
using VRage.Utils;

namespace Sandbox.Game.GameSystems
{
    class MyGridCameraSystem
    {
        private MyCubeGrid m_grid;
        private readonly List<MyCameraBlock> m_cameras;
        private readonly List<MyCameraBlock> m_relayedCameras;
        private MyCameraBlock m_currentCamera;

        private bool m_ignoreNextInput = false;

        public int CameraCount
        {
            get { return m_cameras.Count; }
        }

        public MyCameraBlock CurrentCamera
        {
            get { return m_currentCamera; }
        }

        private static MyHudCameraOverlay m_cameraOverlay;
        static MyGridCameraSystem()
        {
            //Useful for testing
            //MyHudCameraOverlay.TextureName = "Textures\\Models\\Cubes\\ArmorAlphaLod_de.dds";
        }

        public MyGridCameraSystem(MyCubeGrid grid)
        {
            m_grid = grid;
            m_cameras = new List<MyCameraBlock>();
            m_relayedCameras = new List<MyCameraBlock>();
        }

        public void Register(MyCameraBlock camera)
        {
            MyDebug.AssertDebug(camera != null);
            MyDebug.AssertDebug(!m_cameras.Contains(camera));

            m_cameras.Add(camera);
        }

        public void Unregister(MyCameraBlock camera)
        {
            MyDebug.AssertDebug(camera != null);
            MyDebug.AssertDebug(m_cameras.Contains(camera));

            if (camera == m_currentCamera)
            {
                ResetCamera();
            }

            m_cameras.Remove(camera);
        }

        public void CheckCurrentCameraStillValid()
        {
            if (m_currentCamera != null)
            {
                if (!m_currentCamera.IsWorking)
                {
                    ResetCamera();
                }
            }
        }

        public void SetAsCurrent(MyCameraBlock newCamera)
        {
            MyDebug.AssertDebug(newCamera != null);
            
            if (m_currentCamera == newCamera)
            {
                return;
            }

            if (newCamera.BlockDefinition.OverlayTexture != null)
            {
                MyHudCameraOverlay.TextureName = newCamera.BlockDefinition.OverlayTexture;
                MyHudCameraOverlay.Enabled = true;
            }
            else
            {
                MyHudCameraOverlay.Enabled = false;
            }

            string shipName = MyAntennaSystem.GetLogicalGroupRepresentative(m_grid).DisplayName ?? "";
            string cameraName = newCamera.DisplayNameText;
            
            MyHud.CameraInfo.Enable(shipName, cameraName);
            m_currentCamera = newCamera;
            m_ignoreNextInput = true;

            MySessionComponentVoxelHand.Static.Enabled = false;
            MyCubeBuilder.Static.Deactivate();
        }

        public void UpdateBeforeSimulation()
        {
            if (!MyFakes.ENABLE_CAMERA_BLOCK)
            {
                return;
            }

            if (m_currentCamera == null)
            {
                return;
            }

            if (MySession.Static.CameraController != m_currentCamera)
            {
                if (!(MySession.Static.CameraController is MyCameraBlock))
                {
                    DisableCameraEffects();
                }
                ResetCurrentCamera();
                return;
            }

            //Guard to make sure that when current camera moves from one system to another (through antennas), the input is not processed twice
            if (m_ignoreNextInput)
            {
                m_ignoreNextInput = false;
                return;
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_LEFT))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                SetPrev();
            }
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_RIGHT))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                SetNext();
            }

            if (MyInput.Static.DeltaMouseScrollWheelValue() != 0 && MyGuiScreenCubeBuilder.Static == null && !MyGuiScreenTerminal.IsOpen)
            {
                m_currentCamera.ChangeZoom(MyInput.Static.DeltaMouseScrollWheelValue());
            }
        }

        public void UpdateBeforeSimulation10()
        {
            if (!MyFakes.ENABLE_CAMERA_BLOCK)
            {
                return;
            }
            if (m_currentCamera != null)
            {
                if (!CameraIsInRangeAndPlayerHasAccess(m_currentCamera))
                {
                    ResetCamera();
                }
            }
        }

        public static bool CameraIsInRangeAndPlayerHasAccess(MyCameraBlock camera)
        {
            if (MySession.ControlledEntity != null)
            {
                MyIDModule module;
                if ((camera as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                {
                    if (!(camera.HasPlayerAccess(MySession.LocalPlayerId) || module.Owner == 0))
                    {
                        return false;
                    }
                }

                if (MySession.ControlledEntity is MyCharacter)
                {
                    return MyAntennaSystem.CheckConnection(MySession.LocalCharacter, camera.CubeGrid, MySession.LocalHumanPlayer);
                }
                else if (MySession.ControlledEntity is MyShipController)
                {
                    return MyAntennaSystem.CheckConnection((MySession.ControlledEntity as MyShipController).CubeGrid, camera.CubeGrid, MySession.LocalHumanPlayer);
                }
            }

            return false;
        }

        public void ResetCamera()
        {
            ResetCurrentCamera();
            //Can be null when closing the game
            if (MySession.LocalCharacter != null)
            {
                MySession.SetCameraController(MyCameraControllerEnum.Entity, MySession.LocalCharacter as MyEntity);
            }
            DisableCameraEffects();
        }

        private void DisableCameraEffects()
        {
            MyHudCameraOverlay.Enabled = false;
            MyHud.CameraInfo.Disable();

            MySector.MainCamera.FieldOfView = MySandboxGame.Config.FieldOfView;
        }

        private void ResetCurrentCamera()
        {
            m_currentCamera.OnExitView();
            
            m_currentCamera = null;
        }

        private void SetNext()
        {
            UpdateRelayedCameras();
            var nextCamera = GetNext(m_currentCamera);
            if (nextCamera != null)
            {
                SetCamera(nextCamera);
            }
        }

        private void SetPrev()
        {
            UpdateRelayedCameras();
            var prevCamera = GetPrev(m_currentCamera);
            if (prevCamera != null)
            {
                SetCamera(prevCamera);
            }
        }

        private void SetCamera(MyCameraBlock newCamera)
        {
            if (newCamera == m_currentCamera)
            {
                return;
            }

            if (m_cameras.Contains(newCamera))
            {
                SetAsCurrent(newCamera);
                newCamera.SetView();
            }
            else
            {
                MyHudCameraOverlay.Enabled = false;
                MyHud.CameraInfo.Disable();
                ResetCurrentCamera();
                newCamera.RequestSetView();
            }
        }

        private void UpdateRelayedCameras()
        {
            var mutualGridsInfo = MyAntennaSystem.GetMutuallyConnectedGrids(m_grid).ToList();

            
            //We need to sort to make sure that the list of relayed cameras is always the same, regardles of which grid system computes it.
            //This is so that cycling through the cameras will be the same.
            mutualGridsInfo.Sort(delegate(MyAntennaSystem.BroadcasterInfo b1, MyAntennaSystem.BroadcasterInfo b2)
            {
                return b1.EntityId.CompareTo(b2.EntityId);
            });

            m_relayedCameras.Clear();

            foreach (var gridInfo in mutualGridsInfo)
            {
                AddValidCamerasFromGridToRelayed(gridInfo.EntityId);
            }

            //We don't have an antenna connected
            if (m_relayedCameras.Count == 0)
            {
                AddValidCamerasFromGridToRelayed(m_grid);
            }
        }

        private void AddValidCamerasFromGridToRelayed(long gridId)
        {
            MyCubeGrid grid;
            MyEntities.TryGetEntityById(gridId, out grid);
            AddValidCamerasFromGridToRelayed(grid);
        }

        private void AddValidCamerasFromGridToRelayed(MyCubeGrid grid)
        {
            var blocks = grid.GridSystems.TerminalSystem.Blocks;
            foreach (var block in blocks)
            {
                var camera = block as MyCameraBlock;
                if (camera != null && camera.IsWorking && camera.HasLocalPlayerAccess())
                {
                    m_relayedCameras.Add(camera);
                }
            }
        }

        private MyCameraBlock GetNext(MyCameraBlock current)
        {
            MyDebug.AssertDebug(current != null);
            MyDebug.AssertDebug(m_cameras.Contains(current));

            if (m_relayedCameras.Count == 1)
            {
                return current;
            }

            int indexOf = m_relayedCameras.IndexOf(current);

            if (indexOf == -1)
            {
                ResetCamera();
                return null;
            }

            return m_relayedCameras[(indexOf + 1) % m_relayedCameras.Count];
        }

        private MyCameraBlock GetPrev(MyCameraBlock current)
        {
            MyDebug.AssertDebug(current != null);
            MyDebug.AssertDebug(m_cameras.Contains(current));

            if (m_relayedCameras.Count == 1)
            {
                return current;
            }

            int indexOf = m_relayedCameras.IndexOf(current);
            if (indexOf == -1)
            {
                ResetCamera();
                return null;
            }
            int prevIndex = indexOf - 1;
            if (prevIndex < 0)
            {
                prevIndex = m_relayedCameras.Count - 1;
            }
            return m_relayedCameras[prevIndex];
        }

        public void PrepareForDraw()
        {
            if (!MyFakes.ENABLE_CAMERA_BLOCK)
            {
                return;
            }

            if (m_currentCamera == null)
            {
                return;
            }
        }
    }
}
