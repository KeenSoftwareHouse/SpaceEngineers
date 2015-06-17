#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using VRage.Components;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities
{
    partial class MyControllableSphere : MyEntity, IMyCameraController, IMyControllableEntity
    {
        #region Fields

        private MyControllerInfo m_info = new MyControllerInfo();
        public MyControllerInfo ControllerInfo { get { return m_info; } }

        #endregion

        #region Init

        public MyControllableSphere()
        {
            ControllerInfo.ControlAcquired += OnControlAcquired;
            ControllerInfo.ControlReleased += OnControlReleased;
        }

        public void Init()
        {
            base.Init(null, "Models\\Debug\\Sphere", null, null);

            WorldMatrix = MatrixD.Identity;

            this.InitSpherePhysics(MyMaterialType.METAL, Vector3.Zero, 0.5f, 100,
                           MyPerGameSettings.DefaultLinearDamping,
                           MyPerGameSettings.DefaultAngularDamping, 
                           MyPhysics.DefaultCollisionLayer,
                           RigidBodyFlag.RBF_DEFAULT);

            Render.SkipIfTooSmall = false;
            Save = false;

        }
        #endregion

        #region Movement

        public bool IsInFirstPersonView { get; set; }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float roll)
        {
            float speed = 0.1f;

            Vector3D pos = WorldMatrix.Translation + speed * WorldMatrix.Right * moveIndicator.X + speed * WorldMatrix.Up * moveIndicator.Y - speed * WorldMatrix.Forward * moveIndicator.Z;

            var m = (MatrixD)GetRotation(rotationIndicator, roll);
            m = m * WorldMatrix;

            m.Translation = pos;

            WorldMatrix = m;
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        public void MoveAndRotateStopped()
        {
        }

        public void Rotate(Vector2 rotationIndicator, float roll)
        {
            var m = (MatrixD)GetRotation(rotationIndicator, roll);
            WorldMatrix = m * WorldMatrix;
        }

        public void RotateStopped()
        {
        }

        private Matrix GetRotation(Vector2 rotationIndicator, float roll)
        {
            float rotSpeed = 0.001f;

            Matrix m = Matrix.CreateRotationY(-rotSpeed * rotationIndicator.Y);
            m *= Matrix.CreateRotationX(-rotSpeed * rotationIndicator.X);
            m *= Matrix.CreateRotationZ(-rotSpeed * roll * 10);

            return m;
        }

        #endregion

        #region Input handling

        public void BeginShoot(MyShootActionEnum action)
        {
          
        }

        public void OnBeginShoot(MyShootActionEnum action)
        {

        }

        void ShootInternal()
        {

        }

        private void ShootFailedLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {

        }

        private void ShootBeginFailed(MyShootActionEnum action, MyGunStatusEnum status)
        {

        }

        private void ShootSuccessfulLocal(MyShootActionEnum action)
        {

        }

        public void SwitchOnEndShoot(MyDefinitionId? weaponDefinition)
        {

        }

        private void EndShootAll()
        {
 
        }

        public void EndShoot(MyShootActionEnum action)
        {

        }

        public void OnEndShoot(MyShootActionEnum action)
        {
 
        }

        public void Zoom(bool newKeyPress)
        {
          
        }

        void EnableIronsight(bool enable, bool newKeyPress, bool changeCamera, bool updateSync = true)
        {
           
        }

      

        public void Use()
        {
         
        }

        public void UseContinues()
        {
           
        }

        public void UseFinished()
        {

        }

        public void Crouch()
        {

        }

        public void Down()
        {
          
        }

        public void Up()
        {
          
        }

        public void Jump()
        {

        }

        public void SwitchWalk()
        {

        }

        public void Sprint()
        {
        }

        public void SwitchBroadcasting()
        {

        }

        public void ShowInventory()
        {

        }

        public void ShowTerminal()
        {

        }

        public void SwitchHelmet()
        {

        }

        public void EnableDampeners(bool enable, bool updateSync = true)
        {
     
        }

        public void EnableJetpack(bool enable, bool fromLoad = false, bool updateSync = true, bool fromInit = false)
        {
            
        }

        /// <summary>
        /// Switches jetpack modes for character.
        /// </summary>
        public void SwitchDamping()
        {
          
        }

        public void SwitchThrusts()
        {
          
        }

        public void SwitchLights()
        {
         
        }

        public void SwitchReactors()
        {
        }

        public bool EnabledThrusts
        {
            get { return false; }
        }

        public bool EnabledDamping
        {
            get { return false; }
        }

        public bool EnabledLights
        {
            get { return false; }
        }

        public bool EnabledLeadingGears
        {
            get { return false; }
        }

        public bool EnabledReactors
        {
            get { return false; }
        }

        public bool EnabledBroadcasting
        {
            get { return false; }
        }

        public bool EnabledHelmet
        {
            get { return false; }
        }

        public bool CanSwitchToWeapon(MyDefinitionId? weaponDefinition)
        {

            return false;
        }

        public void OnControlAcquired(MyEntityController controller)
        {
           
        }

        public void OnControlReleased(MyEntityController controller)
        {

        }

        #endregion

        public void Die()
        {

        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
 
        }

        public bool PrimaryLookaround
        {
            get { return false; }
        }

        public MyEntity Entity { get { return this; } }

        public bool ForceFirstPersonCamera
        {
            get;
            set;
        }

        public MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            MatrixD m = WorldMatrix;
            m.Translation -= 4 * WorldMatrix.Forward;
            return m;
        }

        public override MatrixD GetViewMatrix()
        {
            MatrixD m = GetHeadMatrix(true);
            m = MatrixD.Invert(m);
            return m;
        }

        public void SwitchToWeapon(MyDefinitionId weaponDefinition)
        {
         
        }
		public void SwitchToWeapon(MyToolbarItemWeapon weapon)
		{

		}


        public void SwitchAmmoMagazine()
        {
            
        }

        public bool CanSwitchAmmoMagazine()
        {
            return false;
        }

        public void SwitchLeadingGears()
        {

        }

        public float HeadLocalXAngle { get; set; }
        public float HeadLocalYAngle { get; set; }

        public MyToolbarType ToolbarType
        {
            get
            {
                return MyToolbarType.Spectator;
            }
        }

        MatrixD IMyCameraController.GetViewMatrix()
        {
            return GetViewMatrix();
        }

        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            Rotate(rotationIndicator, rollIndicator);
        }

        void IMyCameraController.RotateStopped()
        {
            RotateStopped();
        }

        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {
            OnAssumeControl(previousCameraController);
        }

        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
        }

        bool IMyCameraController.IsInFirstPersonView
        {
            get
            {
                return IsInFirstPersonView;
            }
            set
            {
                IsInFirstPersonView = value;
            }
        }

        bool IMyCameraController.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.HandleUse()
        {
            return false;
        }

        bool IMyCameraController.AllowCubeBuilding 
        { 
            get 
            {
                return false; 
            } 
        }

        public MyEntityCameraSettings GetCameraEntitySettings()
        {
            return null;
        }

        public MyStringId ControlContext
        {
            get { return MyStringId.NullOrEmpty; }
        }
    }
}
