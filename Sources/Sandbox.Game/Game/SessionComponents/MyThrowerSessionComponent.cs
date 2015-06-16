using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Components
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class MySessionComponentThrower : MySessionComponentBase
    {
        public static MySessionComponentThrower Static { get; set; }
        public static bool USE_SPECTATOR_FOR_THROW = false;

        public bool Enabled { get { return m_isActive; } set { m_isActive = value; } }
        public MyPrefabThrowerDefinition CurrentDefinition { get; set; }

        private bool m_isActive = false;
        private int m_startTime;

        public override bool IsRequiredByGame
        {
            get
            {
                return MyFakes.ENABLE_PREFAB_THROWER;
            }
        }

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyToolbarComponent) };
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        public override void HandleInput()
        {
            if (!m_isActive)
                return;
            if (!(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
                return;

            if (!
                (VRage.Input.MyInput.Static.ENABLE_DEVELOPER_KEYS || !MySession.Static.SurvivalMode || (MyMultiplayer.Static != null && MyMultiplayer.Static.IsAdmin(MySession.LocalHumanPlayer.Id.SteamId)))
                )
                return;


            base.HandleInput();

            if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
            {
                m_startTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }

            if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
            {
                var gridBuilders = MyPrefabManager.Static.GetGridPrefab(CurrentDefinition.PrefabToThrow);

                Vector3D cameraPos = Vector3D.Zero;
                Vector3D cameraDir = Vector3D.Zero;

                if (USE_SPECTATOR_FOR_THROW)
                {
                    cameraPos = MySpectator.Static.Position;
                    cameraDir = MySpectator.Static.Orientation.Forward;
                }
                else
                {
                    if (MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.ThirdPersonSpectator || MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.Entity)
                    {
                        if (MySession.ControlledEntity == null)
                            return;

                        cameraPos = MySession.ControlledEntity.GetHeadMatrix(true, true).Translation;
                        cameraDir = MySession.ControlledEntity.GetHeadMatrix(true, true).Forward;
                    }
                    else
                    {
                        cameraPos = MySector.MainCamera.Position;
                        cameraDir = MySector.MainCamera.WorldMatrix.Forward;
                    }
                }

                var position = cameraPos + cameraDir;
                
                float deltaSeconds = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_startTime) / 1000.0f;
                float velocity = deltaSeconds / CurrentDefinition.PushTime * CurrentDefinition.MaxSpeed;
                velocity = MathHelper.Clamp(velocity, CurrentDefinition.MinSpeed, CurrentDefinition.MaxSpeed);
                var linearVelocity = cameraDir * velocity;
                float mass = 0;
                if (CurrentDefinition.Mass.HasValue)
                {
                    mass = Sandbox.Engine.Physics.MyDestructionHelper.MassToHavok(CurrentDefinition.Mass.Value);
                }

                gridBuilders[0].EntityId = MyEntityIdentifier.AllocateId();
                MySyncThrower.RequestThrow(gridBuilders[0], position, linearVelocity, mass, CurrentDefinition.ThrowSound);

                m_startTime = 0;
            }
        }

        public void Throw(MyObjectBuilder_CubeGrid grid, Vector3D position, Vector3D linearVelocity, float mass, MyCueId throwSound)
        {
            var entity = MyEntities.CreateFromObjectBuilder(grid);
            if (entity == null)
            {
                return;
            }

            entity.PositionComp.SetPosition(position);
            entity.Physics.LinearVelocity = linearVelocity;

            if (mass > 0 && Sync.IsServer)
            {
                entity.Physics.RigidBody.Mass = mass;
            }

            MyEntities.Add(entity);

            if (!throwSound.IsNull)
            {
                var emitter = MyAudioComponent.TryGetSoundEmitter();
                if (emitter != null)
                {
                    emitter.SetPosition(position);
                    emitter.PlaySound(throwSound);
                }
            }
        }


        public void Activate()
        {
            m_isActive = true;
        }

        public void Deactivate()
        {
            m_isActive = false;
        }

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
            MyToolbarComponent.CurrentToolbar.SelectedSlotChanged += CurrentToolbar_SelectedSlotChanged;
            MyToolbarComponent.CurrentToolbar.SlotActivated += CurrentToolbar_SlotActivated;
            MyToolbarComponent.CurrentToolbar.Unselected += CurrentToolbar_Unselected;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            if (MyToolbarComponent.CurrentToolbar != null)
            {
                MyToolbarComponent.CurrentToolbar.SelectedSlotChanged -= CurrentToolbar_SelectedSlotChanged;
                MyToolbarComponent.CurrentToolbar.SlotActivated -= CurrentToolbar_SlotActivated;
                MyToolbarComponent.CurrentToolbar.Unselected -= CurrentToolbar_Unselected;
            }
        }

        private void CurrentToolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.SelectedItem is MyToolbarItemPrefabThrower))
                Enabled = false;
        }

        private void CurrentToolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemPrefabThrower))
                Enabled = false;
        }

        private void CurrentToolbar_Unselected(MyToolbar toolbar)
        {
            Enabled = false;
        }
    }
}
