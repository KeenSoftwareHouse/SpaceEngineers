using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Utils;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.OpenVRWrapper;
using VRage.Utils;
using VRageMath;
using ObjectBuilders;
using Havok;
using VRage.Game.ModAPI.Interfaces;
#if !XB1 // XB1_NOOPENVRWRAPPER
using Valve.VR;
#endif // !XB1
using Sandbox.Game.Screens.Helpers;
//using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
//using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;

namespace Sandbox.Game.Entities
{
    [MyComponentType(typeof(MyWeaponSharedActionsComponentBase))]
    public abstract class MyWeaponSharedActionsComponentBase : MyEntityComponentBase 
    {
        public override string ComponentTypeDebugString
        {
            get { return "WeaponSharedActionsComponentBase"; }
        }

        public abstract void Shoot(MyShootActionEnum action);
        public abstract void EndShoot(MyShootActionEnum action);
        public abstract void Update();
    }

    [MyEntityType(typeof(MyObjectBuilder_GhostCharacter))]
    public class MyGhostCharacter : MyEntity, IMyControllableEntity, VRage.Game.ModAPI.Interfaces.IMyCameraController, IMyDestroyableObject
    {
        public interface ITouchPadListener
        {
            void TouchPadChanged(Vector2 position);
        }

        private struct MyVRWeaponInfo
        {
            public MyDefinitionId DefinitionId;
            public int Reference;
        }

        private List<MyVRWeaponInfo> m_leftWeapons = new List<MyVRWeaponInfo>();
        private List<MyVRWeaponInfo> m_rightWeapons = new List<MyVRWeaponInfo>();
        private bool m_weaponsInitialized;

        private MyControllerInfo m_info = new MyControllerInfo();
        IMyHandheldGunObject<MyDeviceBase> m_leftWeapon, m_rightWeapon;
        MyVRWeaponInfo m_leftWeaponInfo, m_rightWeaponInfo;

        private MatrixD m_worldMatrixOriginal;
        private float MyDamage=0;
        private static readonly Color COLOR_WHEN_HIT = new Color(1, 0, 0, 0);
        private static readonly float PLAYER_TARGET_SIZE = 1f;

        public new MyPhysicsBody Physics { get { return base.Physics as MyPhysicsBody; } set { base.Physics = value; } }

        public World.MyControllerInfo ControllerInfo
        {
            get { return m_info; }
        }

        public VRage.Game.Entity.MyEntity Entity
        {
            get { return this; }
        }

        public float HeadLocalXAngle
        {
            get {return 0;}
            set {}
        }

        public float HeadLocalYAngle
        {
            get {return 0;}
            set {}
        }

        public void BeginShoot(MyShootActionEnum action)
        {
            /*Vector3 direction;
            if ((int)action < 2)
            {
                if (m_leftWeapon != null)
                {
                    direction = MyOpenVR.Controller1Matrix.Forward;
                    m_leftWeapon.Shoot(action, direction);
                }
            }
            else
            {
                if (m_rightWeapon != null)
                {
                    direction = MyOpenVR.Controller2Matrix.Forward;
                    m_rightWeapon.Shoot((MyShootActionEnum)((int)action - 2), direction);
                }
            }*/
        }

        public void EndShoot(MyShootActionEnum action)
        {
            /*if ((int)action < 2)
            {
                if (m_leftWeapon != null)
                    m_leftWeapon.EndShoot(action);
            }
            else
                if (m_rightWeapon != null)
                    m_rightWeapon.EndShoot((MyShootActionEnum)((int)action - 2));
             */
        }

        public void OnBeginShoot(MyShootActionEnum action)
        {
            //throw new NotImplementedException();
        }

        public void OnEndShoot(MyShootActionEnum action)
        {
            //throw new NotImplementedException();
        }

        public void UseFinished()
        {
            //throw new NotImplementedException();
        }

        public void PickUpFinished()
        {
            throw new NotImplementedException();
        }

        public void Sprint(bool enabled)
        {
            //throw new NotImplementedException();
        }

        public void SwitchToWeapon(VRage.Game.MyDefinitionId weaponDefinition)
        {
            //throw new NotImplementedException();
        }

        public void SwitchToWeapon(Screens.Helpers.MyToolbarItemWeapon weapon)
        {
            //throw new NotImplementedException();
        }

        public bool CanSwitchToWeapon(VRage.Game.MyDefinitionId? weaponDefinition)
        {
            //throw new NotImplementedException();
            return true;
        }

        public void SwitchAmmoMagazine()
        {
            //throw new NotImplementedException();
        }

        public bool CanSwitchAmmoMagazine()
        {
            //throw new NotImplementedException();
            return true;
        }

        public void SwitchBroadcasting()
        {
            //throw new NotImplementedException();
        }

        public bool EnabledBroadcasting
        {
            get { return false; }
        }

        public VRage.Game.MyToolbarType ToolbarType
        {
            get { return MyToolbarType.Character; }
        }

        //public MyControllerInfo ControllerInfo { get { return m_info; } }
        MyEntityCameraSettings m_cameraSettings;
        public Multiplayer.MyEntityCameraSettings GetCameraEntitySettings()
        {
            ulong playerId = 0;
            if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                playerId = ControllerInfo.Controller.Player.Id.SteamId;
                if (!MySession.Static.Cameras.TryGetCameraSettings(ControllerInfo.Controller.Player.Id, EntityId, out m_cameraSettings))
                {
                    if (ControllerInfo.IsLocallyHumanControlled())
                    {
                        m_cameraSettings = new MyEntityCameraSettings()
                        {
                            Distance = 0,
                            IsFirstPerson = true,
                            HeadAngle = new Vector2(HeadLocalXAngle, HeadLocalYAngle)
                        };
                    }
                }
            }
            return m_cameraSettings;
        }

        public VRage.Utils.MyStringId ControlContext
        {
            get {return MySpaceBindingCreator.CX_CHARACTER;}
        }

        public MyToolbar Toolbar { get { return null; } }


        VRage.ModAPI.IMyEntity VRage.Game.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return this; }
        }

        public bool ForceFirstPersonCamera
        {
            get {return false;}
            set {}
        }

        public VRageMath.MatrixD GetHeadMatrix(bool includeY, bool includeX, bool forceHeadAnim, bool forceHeadBone)
        {
            return MyOpenVR.HeadsetMatrixD * m_worldMatrixOriginal;
        }

        public void MoveAndRotate(VRageMath.Vector3 moveIndicator, VRageMath.Vector2 rotationIndicator, float rollIndicator)
        {
            //throw new NotImplementedException();
        }

        public void MoveAndRotateStopped()
        {
            //throw new NotImplementedException();
        }

        public void Use()
        {
            //throw new NotImplementedException();
        }

        public void UseContinues()
        {
            //throw new NotImplementedException();
        }

        public void PickUp()
        { 
        }
        public void PickUpContinues()
        {
        }

        public void Jump()
        {
            //throw new NotImplementedException();
        }

        public void SwitchWalk()
        {
            //throw new NotImplementedException();
        }

        public void Up()
        {
            //throw new NotImplementedException();
        }

        public void Crouch()
        {
            //throw new NotImplementedException();
        }

        public void Down()
        {
            //throw new NotImplementedException();
        }

        public void ShowInventory()
        {
            //throw new NotImplementedException();
        }

        public void ShowTerminal()
        {
            //throw new NotImplementedException();
        }

        public void SwitchThrusts()
        {
            //throw new NotImplementedException();
        }

        public void SwitchDamping()
        {
            //throw new NotImplementedException();
        }

        public void SwitchLights()
        {
            //throw new NotImplementedException();
        }

        public void SwitchLeadingGears()
        {
            //throw new NotImplementedException();
        }

        public void SwitchReactors()
        {
            //throw new NotImplementedException();
        }

        public void SwitchHelmet()
        {
            //throw new NotImplementedException();
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

        public bool EnabledHelmet
        {
            get { return false; }
        }

        public void DrawHud(VRage.Game.ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            //throw new NotImplementedException();
        }

        public void Die()
        {
            //throw new NotImplementedException();
        }

        public bool PrimaryLookaround
        {
            get { return false; }
        }


        // VRage.Game.ModAPI.Interfaces.IMyCameraController:
        /*MatrixD VRage.Game.ModAPI.Interfaces.IMyCameraController.GetViewMatrix()
        {
            MatrixD matrix = WorldMatrix;
            return MatrixD.Invert(matrix);
        }*/

        void VRage.Game.ModAPI.Interfaces.IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            //throw new NotImplementedException();
        }

        void VRage.Game.ModAPI.Interfaces.IMyCameraController.RotateStopped()
        {
            //throw new NotImplementedException();
        }
        void VRage.Game.ModAPI.Interfaces.IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            currentCamera.SetViewMatrix(GetViewMatrix());
        }

        void VRage.Game.ModAPI.Interfaces.IMyCameraController.OnAssumeControl(VRage.Game.ModAPI.Interfaces.IMyCameraController previousCameraController)
        {
            //throw new NotImplementedException();
        }

        void VRage.Game.ModAPI.Interfaces.IMyCameraController.OnReleaseControl(VRage.Game.ModAPI.Interfaces.IMyCameraController newCameraController)
        {
            //throw new NotImplementedException();
        }

        bool VRage.Game.ModAPI.Interfaces.IMyCameraController.HandleUse()
        {
            //throw new NotImplementedException();
            return false;
        }

        bool VRage.Game.ModAPI.Interfaces.IMyCameraController.HandlePickUp()
        {
            //throw new NotImplementedException();
            return false;
        }

        bool VRage.Game.ModAPI.Interfaces.IMyCameraController.IsInFirstPersonView
        {
            get {return true;}
            set {}
        }

        bool VRage.Game.ModAPI.Interfaces.IMyCameraController.ForceFirstPersonCamera
        {
            get
            {
                return false;// throw new NotImplementedException();
            }
            set
            {
                //throw new NotImplementedException();
            }
        }

        bool VRage.Game.ModAPI.Interfaces.IMyCameraController.AllowCubeBuilding
        {
            get { return false;/* throw new NotImplementedException(); */}
        }



        public void OnDestroy()
        {
 	        throw new NotImplementedException();
        }
        
        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, VRage.Game.ModAPI.MyHitInfo? hitInfo = null, long attackerId = 0)
        {
            MyDamage += damage;
            MyOpenVR.FadeToColor(0.1f, COLOR_WHEN_HIT);
            return true;
        }
        
        public float Integrity
        {
	        get { return 100f; }
        }

        public bool UseDamageSystem
        {
	        get { throw new NotImplementedException(); }
        }

        //===============================================================================================================================================
        public MyGhostCharacter()
        {
            
        }
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            //headset position update
            WorldMatrix = MyOpenVR.HeadsetMatrixD * m_worldMatrixOriginal;
            Physics.RigidBody.SetWorldMatrix(WorldMatrix);
            //VRageRender.MyRenderProxy.DebugDrawSphere(WorldMatrix.Translation, 1, Color.Red, 1, false);
            //Weapons' positions update:
            if (m_leftWeapon!=null)
            {
                var gunBase = m_leftWeapon.GunBase as MyGunBase;
                if (gunBase != null)
                    ((MyEntity)m_leftWeapon).WorldMatrix = gunBase.m_holdingDummyMatrix * MyOpenVR.Controller1Matrix * m_worldMatrixOriginal;
                else
                    ((MyEntity)m_leftWeapon).WorldMatrix = MyOpenVR.Controller1Matrix * m_worldMatrixOriginal;

                HandleButtons(ref m_leftWeapon, ref m_leftWeaponInfo, false, m_leftWeapons);
            }

            if (m_rightWeapon!=null)
            {
                var gunBase = m_rightWeapon.GunBase as MyGunBase;
                if (gunBase != null)
                    ((MyEntity)m_rightWeapon).WorldMatrix = gunBase.m_holdingDummyMatrix * MyOpenVR.Controller2Matrix * m_worldMatrixOriginal;
                else
                    ((MyEntity)m_rightWeapon).WorldMatrix = MyOpenVR.Controller2Matrix * m_worldMatrixOriginal;

                HandleButtons(ref m_rightWeapon, ref m_rightWeaponInfo, true, m_rightWeapons);
            }

            if (MyDamage>0)
            {
                MyDamage -= 80;
                if (MyDamage<=0)
                    MyOpenVR.UnFade(0.5f);
            }

        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyObjectBuilder_GhostCharacter characterOb = (MyObjectBuilder_GhostCharacter)objectBuilder;
            base.Init(objectBuilder);

            SetupPhysics(true);

            m_worldMatrixOriginal = this.WorldMatrix;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            MyOpenVR.LMUAdd(null, WorldMatrix, ControllerRole.head, 1);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_Character objectBuilder = (MyObjectBuilder_Character)base.GetObjectBuilder(copy);
            return objectBuilder;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!m_weaponsInitialized)
            {
                int weaponRefCounter = 2;

                var allDefinitions = MyDefinitionManager.Static.GetDefinitions<MyGhostCharacterDefinition>();
                if (allDefinitions != null)
                {
                    foreach (var definition in allDefinitions) 
                    {
                        foreach (var leftWeaponId in definition.LeftHandWeapons)
                            m_leftWeapons.Add(new MyVRWeaponInfo() { DefinitionId = leftWeaponId, Reference = weaponRefCounter++ });
                        SwitchWeapon(ref m_leftWeapon, ref m_leftWeaponInfo, m_leftWeapons, ControllerRole.leftHand, null);

                        foreach (var rightWeaponId in definition.RightHandWeapons)
                            m_rightWeapons.Add(new MyVRWeaponInfo() { DefinitionId = rightWeaponId, Reference = weaponRefCounter++ });
                        SwitchWeapon(ref m_rightWeapon, ref m_rightWeaponInfo, m_rightWeapons, ControllerRole.rightHand, null);
                    }
                }

                m_weaponsInitialized = true;
            }
        }

        private IMyHandheldGunObject<MyDeviceBase> CreateWeapon(MyDefinitionId? weaponDefinition, int lastMomentUpdateIndex)
        {
            var builder=MyObjectBuilderSerializer.CreateNewObject(weaponDefinition.Value.TypeId) ;
            MyObjectBuilder_EntityBase weaponEntityBuilder = builder as MyObjectBuilder_EntityBase;
            if (weaponEntityBuilder != null)
            {
                weaponEntityBuilder.SubtypeName = weaponDefinition.Value.SubtypeId.String;
                var gun = MyCharacter.CreateGun(weaponEntityBuilder);
                //EquipWeapon(gun);
                MyEntity gunEntity = (MyEntity)gun;
                gunEntity.Render.CastShadows = true;
                gunEntity.Render.NeedsResolveCastShadow = false;
                gunEntity.Render.LastMomentUpdateIndex = lastMomentUpdateIndex;
                gunEntity.Save = false;
                gunEntity.OnClose += gunEntity_OnClose;
                MyEntities.Add(gunEntity);

                if (gunEntity.Model != null) 
                { 
                    gunEntity.InitBoxPhysics(MyMaterialType.METAL, gunEntity.Model, 10,
                       MyPerGameSettings.DefaultAngularDamping, MyPhysics.CollisionLayers.DefaultCollisionLayer,
                       RigidBodyFlag.RBF_KINEMATIC);
                    gunEntity.Physics.Enabled = true;
                }
                return gun;
            }
            else
                Debug.Fail("Couldn't create builder for weapon! typeID: " + weaponDefinition.Value.TypeId.ToString());
            return null;
        }

        void HandleButtons(ref IMyHandheldGunObject<MyDeviceBase> weapon, ref MyVRWeaponInfo weaponInfo, bool secondController, List<MyVRWeaponInfo> weapons)
        {
            if (weapon == null)
                return;

            MyWeaponSharedActionsComponentBase sharedWeaponActionsComponent = null;
            ((MyEntity)weapon).Components.TryGet<MyWeaponSharedActionsComponentBase>(out sharedWeaponActionsComponent);
            if (sharedWeaponActionsComponent != null)
                sharedWeaponActionsComponent.Update();

            if (MyOpenVR.GetControllerState(secondController).IsButtonPressed(EVRButtonId.k_EButton_SteamVR_Trigger))
            {//holding the trigger
                var gunBase = weapon.GunBase as MyGunBase;
                MyGunStatusEnum status;
                weapon.CanShoot(MyShootActionEnum.PrimaryAction, this.EntityId, out status);
                if (status != MyGunStatusEnum.Cooldown && status != MyGunStatusEnum.BurstLimit)
                {
                    weapon.Shoot(MyShootActionEnum.PrimaryAction, gunBase != null ? (Vector3)gunBase.GetMuzzleWorldMatrix().Forward : Vector3.Forward, null);
                    if (sharedWeaponActionsComponent != null)
                        sharedWeaponActionsComponent.Shoot(MyShootActionEnum.PrimaryAction);
                }
            }

            if (MyOpenVR.GetControllerState(secondController).WasButtonReleased(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                weapon.EndShoot(MyShootActionEnum.PrimaryAction);
                if (sharedWeaponActionsComponent != null)
                    sharedWeaponActionsComponent.EndShoot(MyShootActionEnum.PrimaryAction);
            }

            if (MyOpenVR.GetControllerState(secondController).WasButtonPressed(EVRButtonId.k_EButton_Grip))
            {
                var gunBase = weapon.GunBase as MyGunBase;
                weapon.Shoot(MyShootActionEnum.SecondaryAction, gunBase != null ? (Vector3)gunBase.GetMuzzleWorldMatrix().Forward : Vector3.Forward, null);
                if (sharedWeaponActionsComponent != null)
                    sharedWeaponActionsComponent.Shoot(MyShootActionEnum.SecondaryAction);
            }
            if (MyOpenVR.GetControllerState(secondController).WasButtonReleased(EVRButtonId.k_EButton_Grip))
            {
                weapon.EndShoot(MyShootActionEnum.SecondaryAction);
                if (sharedWeaponActionsComponent != null)
                    sharedWeaponActionsComponent.EndShoot(MyShootActionEnum.SecondaryAction);
            }

            if (MyOpenVR.GetControllerState(secondController).WasButtonPressed(EVRButtonId.k_EButton_ApplicationMenu))
            {
                var gunBase = weapon.GunBase as MyGunBase;
                weapon.Shoot(MyShootActionEnum.TertiaryAction, gunBase != null ? (Vector3)gunBase.GetMuzzleWorldMatrix().Forward : Vector3.Forward, null);
                if (sharedWeaponActionsComponent != null)
                    sharedWeaponActionsComponent.Shoot(MyShootActionEnum.TertiaryAction);
            }
            if (MyOpenVR.GetControllerState(secondController).WasButtonReleased(EVRButtonId.k_EButton_ApplicationMenu))
            {
                weapon.EndShoot(MyShootActionEnum.TertiaryAction);
                if (sharedWeaponActionsComponent != null)
                    sharedWeaponActionsComponent.EndShoot(MyShootActionEnum.TertiaryAction);
            }

            Vector2 touchpadPos = Vector2.Zero;
            bool validTouchpadPos = false;
            validTouchpadPos = MyOpenVR.GetControllerState(secondController).GetTouchpadXY(ref touchpadPos);

            if (MyOpenVR.GetControllerState(secondController).WasButtonPressed(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                SwitchWeapon(ref weapon, ref weaponInfo, weapons, secondController ? ControllerRole.rightHand : ControllerRole.leftHand, validTouchpadPos ? touchpadPos : (Vector2?)null);
            }
            else
            {
                if (validTouchpadPos)
                {
                    if (weapon is ITouchPadListener)
                        (weapon as ITouchPadListener).TouchPadChanged(touchpadPos);
                }
            }
        }

        private void SwitchWeapon(ref IMyHandheldGunObject<MyDeviceBase> weapon, ref MyVRWeaponInfo weaponInfo, List<MyVRWeaponInfo> weapons, ControllerRole role, Vector2? touchpadPos)
        {
            int currentIndex = -1;

            if (weapon != null)
            {
                for (int i = 0; i < weapons.Count; ++i)
                {
                    var info = weapons[i];
                    if (info.DefinitionId == weapon.DefinitionId)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            int nextIndex = 0;
            if (touchpadPos != null)
            {
                Vector2 pos = touchpadPos.Value;
                if (touchpadPos != Vector2.Zero)
                {
                    pos.Normalize();
                    float anglePerWeaponSector = 360f / weapons.Count;
                    float dot = Vector2.Dot(Vector2.UnitX, pos);
                    float angle = (float)Math.Acos(Math.Abs(dot));
                    if (pos.Y >= 0)
                    {
                        if (dot < 0)
                            angle = 180 - angle;
                    }
                    else
                    {
                        if (dot < 0)
                            angle = 180 + angle;
                        else
                            angle = 360 - angle;
                    }

                    nextIndex = (int)Math.Floor(angle / anglePerWeaponSector);
                    Debug.Assert(nextIndex < weapons.Count);
                }
            }

            if (nextIndex == currentIndex)
                return;

            if (weapon != null)
            {
                weapon.OnControlReleased();
                MyEntity weaponEntity = (MyEntity)weapon;
                weaponEntity.Close();
                weapon = null;
            }

            {
                weaponInfo = weapons[nextIndex];
                weapon = CreateWeapon(weaponInfo.DefinitionId, weaponInfo.Reference);
                weapon.OnControlAcquired(null);
                MyEntity weaponEntity = (MyEntity)weapon;
            }

            var gunBase = weapon.GunBase as MyGunBase;
            Matrix holdingMatrix = gunBase != null ? gunBase.m_holdingDummyMatrix : Matrix.Identity;
            MyOpenVR.LMUAdd(holdingMatrix, m_worldMatrixOriginal, role, weaponInfo.Reference);
        }

        void gunEntity_OnClose(MyEntity obj)
        {
            if (m_leftWeapon == obj)
                m_leftWeapon = null;
            if (m_rightWeapon == obj)
                m_rightWeapon = null;
        }


        internal void SetupPhysics(bool isLocalPlayer)
        {
            Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
            Physics.IsPhantom = false;
            Vector3 center = PositionComp.LocalVolume.Center;
            Physics.CreateFromCollisionObject(new HkSphereShape(PLAYER_TARGET_SIZE), center, WorldMatrix, null, 0);
            Physics.Enabled = true;
            Physics.RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
            Physics.RigidBody.ContactPointCallbackEnabled = true;

            PositionComp.LocalAABB = new BoundingBox(new Vector3(-PLAYER_TARGET_SIZE), new Vector3(PLAYER_TARGET_SIZE));
        }

        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            if (Physics.CharacterProxy == null)
                return;

            if (!MySession.Static.Ready)
                return;

            if (value.Base.BodyA == null || value.Base.BodyB == null)
                return;

            if (value.Base.BodyA.UserObject == null || value.Base.BodyB.UserObject == null)
                return;

            if (value.Base.BodyA.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT) || value.Base.BodyB.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                return;
        }
    
    }
}
