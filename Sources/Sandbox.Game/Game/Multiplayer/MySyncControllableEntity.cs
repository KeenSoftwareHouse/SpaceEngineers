using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;
using SteamSDK;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;
using System.Runtime.InteropServices;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Common;
using VRage.Library.Utils;
//using Sandbox.Game.Gui;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncControllableEntity : MySyncEntity
    {
        public delegate void SwitchToWeaponDelegate(MyDefinitionId? weapon, MyObjectBuilder_EntityBase weaponObjectBuilder, long weaponEntityId);
        public delegate void SwitchAmmoMagazineDelegate();

        new public IMyControllableEntity Entity
        {
            get { return (this as MySyncEntity).Entity as IMyControllableEntity; }
        }

        public long SyncedEntityId
        {
            get { return (this as MySyncEntity).Entity.EntityId; }
        }

        [MessageId(6, P2PMessageEnum.Reliable)]
        protected struct UseObject_UseMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public long UsedByEntityId;

            public UseActionEnum UseAction;
            public UseActionResult UseResult;
        }

        [MessageId(7, P2PMessageEnum.Reliable)]
        protected struct ControlledEntity_UseMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        [ProtoContract]
        [MessageId(8, P2PMessageEnum.Reliable)]
        struct SwitchToWeaponMsg : IEntityMessage
        {
            [ProtoMember]
            public long ControlledEntityId;
            public long GetEntityId() { return ControlledEntityId; }

            [ProtoMember]
            public SerializableDefinitionId? Weapon;

            //TODO: Temporary until inventory synchronization is done
            [ProtoMember]
            public MyObjectBuilder_EntityBase WeaponObjectBuilder;

            [ProtoMember]
            public long WeaponEntityId;
        }

        [ProtoContract]
        [MessageId(89, P2PMessageEnum.Reliable)]
        struct SwitchAmmoMagazineMsg : IEntityMessage
        {
            [ProtoMember]
            public long ControlledEntityId;
            public long GetEntityId() { return ControlledEntityId; }
        }

        [MessageId(39, P2PMessageEnum.Reliable)]
        protected struct ShootBeginMsg : IEntityMessage
        {
            public long EntityId;
            public Vector3 Direction;
            public MyShootActionEnum Action;

            public long GetEntityId() { return EntityId; }
        }

        [MessageId(40, P2PMessageEnum.Reliable)]
        protected struct ShootEndMsg : IEntityMessage
        {
            public long EntityId;
            public MyShootActionEnum Action;

            public long GetEntityId() { return EntityId; }
        }

        [MessageId(42, P2PMessageEnum.Unreliable)]
        protected struct ShootDirectionChangeMsg : IEntityMessage
        {
            public long EntityId;
            public Vector3 Direction;

            public long GetEntityId() { return EntityId; }
        }

        private bool[] m_isShooting;
        public bool IsShooting(MyShootActionEnum action)
        {
            return m_isShooting[(int)action];
        }

        public bool IsShooting()
        {
            foreach (MyShootActionEnum value in MyEnum<MyShootActionEnum>.Values)
            {
                if (m_isShooting[(int)value])
                    return true;
            }
            return false;
        }

        public MyShootActionEnum? GetShootingAction()
        {
            foreach (MyShootActionEnum value in MyEnum<MyShootActionEnum>.Values)
            {
                if (m_isShooting[(int)value])
                    return value;
            }
            return null;
        }

        public Vector3 ShootDirection = Vector3.One;

        static MySyncControllableEntity()
        {
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, UseObject_UseMsg>(UseRequestCallback, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, UseObject_UseMsg>(UseSuccessCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, UseObject_UseMsg>(UseFailureCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);

            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, ControlledEntity_UseMsg>(ControlledEntity_UseRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, ControlledEntity_UseMsg>(ControlledEntity_UseCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchToWeaponMsg>(OnSwitchToWeaponRequest, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchToWeaponMsg>(OnSwitchToWeaponSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchToWeaponMsg>(OnSwitchToWeaponFailure, MyMessagePermissions.Any, MyTransportMessageEnum.Failure);

            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, ShootBeginMsg>(ShootBeginCallback, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, ShootEndMsg>(ShootEndCallback, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, ShootDirectionChangeMsg>(ShootDirectionChangeCallback, MyMessagePermissions.Any);

            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchAmmoMagazineMsg>(OnSwitchAmmoMagazineRequest, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchAmmoMagazineMsg>(OnSwitchAmmoMagazineSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncControllableEntity, SwitchAmmoMagazineMsg>(OnSwitchAmmoMagazineFailure, MyMessagePermissions.Any, MyTransportMessageEnum.Failure);
        }

        public event Action ControlledEntity_Used;
        public event Action<UseActionEnum, IMyControllableEntity> UseSuccess;
        public event Action<UseActionEnum, UseActionResult, IMyControllableEntity> UseFailed;
        public event SwitchToWeaponDelegate SwitchToWeaponSuccessHandler;
        public event SwitchToWeaponDelegate SwitchToWeaponFailureHandler;
        public event SwitchAmmoMagazineDelegate SwitchAmmoMagazineSuccessHandler;
        public event SwitchAmmoMagazineDelegate SwitchAmmoMagazineFailureHandler;

        private int m_switchWeaponCounter;
        public bool IsWaitingForWeaponSwitch
        {
            get
            {
                return m_switchWeaponCounter != 0;
            }
        }

        private int m_switchAmmoMagazineCounter;
        public bool IsWaitingForAmmoMagazineSwitch
        {
            get
            {
                return m_switchAmmoMagazineCounter != 0;
            }
        }

        private long m_lastShootDirectionUpdate;

        public MySyncControllableEntity(MyEntity entity)
            : base(entity)
        {
            m_switchWeaponCounter = 0;
            m_switchAmmoMagazineCounter = 0;
            m_isShooting = new bool[(int)MyEnum<MyShootActionEnum>.MaxValue.Value + 1];
        }

        public virtual void ControlledEntity_Use()
        {
            ControlledEntity_UseMsg msg = new ControlledEntity_UseMsg();
            msg.EntityId = SyncedEntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ControlledEntity_UseRequest(MySyncControllableEntity sync, ref ControlledEntity_UseMsg msg, MyNetworkClient sender)
        {
            // TODO: check responsibility for update
            ControlledEntity_UseCallback(sync, ref msg, sender);
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ControlledEntity_UseCallback(MySyncControllableEntity sync, ref ControlledEntity_UseMsg msg, MyNetworkClient sender)
        {
            var handler = sync.ControlledEntity_Used;
            if (handler != null) 
                handler();
        }
      
        public virtual void RequestUse(UseActionEnum actionEnum, IMyControllableEntity usedBy)
        {
            Debug.Assert(Entity is IMyUsableEntity, "Entity must implement IMyUsableEntity to use it");

            UseObject_UseMsg msg = new UseObject_UseMsg();
            msg.EntityId = SyncedEntityId;
            msg.UseAction = actionEnum;
            msg.UsedByEntityId = usedBy.Entity.EntityId;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        void RaiseUseSuccess(UseActionEnum actionEnum, IMyControllableEntity usedBy)
        {
            var handler = UseSuccess;
            if (handler != null) handler(actionEnum, usedBy);
        }

        void RaiseUseFailure(UseActionEnum actionEnum, UseActionResult actionResult, IMyControllableEntity usedBy)
        {
            var handler = UseFailed;
            if (handler != null) 
                handler(actionEnum, actionResult, usedBy);
        }

        static void UseRequestCallback(MySyncEntity sync, ref UseObject_UseMsg msg, MyNetworkClient sender)
        {
            var usableEntity = sync.Entity as IMyUsableEntity;
            MyEntity controlledEntity;
            bool entityExists = MyEntities.TryGetEntityById<MyEntity>(msg.UsedByEntityId, out controlledEntity);
            IMyControllableEntity controllableEntity = controlledEntity as IMyControllableEntity;
            Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");
            Debug.Assert(entityExists && usableEntity != null);

            if (entityExists && usableEntity != null && (msg.UseResult = usableEntity.CanUse(msg.UseAction, controllableEntity)) == UseActionResult.OK)
            {
                MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                UseSuccessCallback(sync as MySyncControllableEntity, ref msg, Sync.Clients.LocalClient);
            }
            else
            {
                MySession.Static.SyncLayer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
            }
        }

        static void UseFailureCallback(MySyncControllableEntity sync, ref UseObject_UseMsg msg, MyNetworkClient sender)
        {
            MyEntity controlledEntity;
            bool userFound = MyEntities.TryGetEntityById<MyEntity>(msg.UsedByEntityId, out controlledEntity);
            Debug.Assert(userFound);
            IMyControllableEntity controllableEntity = controlledEntity as IMyControllableEntity;
            Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");
            sync.RaiseUseFailure(msg.UseAction, msg.UseResult, controllableEntity);
        }

        static void UseSuccessCallback(MySyncControllableEntity sync, ref UseObject_UseMsg msg, MyNetworkClient sender)
        {
            MyEntity controlledEntity;
            if (MyEntities.TryGetEntityById<MyEntity>(msg.UsedByEntityId, out controlledEntity))
            {
                var controllableEntity = controlledEntity as IMyControllableEntity;
                Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");

                if (controllableEntity != null)
                {
                    MyRelationsBetweenPlayerAndBlock relation = MyRelationsBetweenPlayerAndBlock.FactionShare;
                    var cubeBlock = sync.Entity as MyCubeBlock;
                    if (cubeBlock != null && controllableEntity.ControllerInfo.Controller != null)
                    {
                        relation = cubeBlock.GetUserRelationToOwner(controllableEntity.ControllerInfo.Controller.Player.Identity.IdentityId);
                    }

                    if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner)
                    {
                        sync.RaiseUseSuccess(msg.UseAction, controllableEntity);
                    }
                    else
                    {
                        sync.RaiseUseFailure(msg.UseAction, msg.UseResult, controllableEntity);
                    }
                }
            }
        }

        public void RequestSwitchToWeapon(MyDefinitionId? weapon, MyObjectBuilder_EntityBase weaponObjectBuilder, long weaponEntityId)
        {
            if (!Sync.IsServer)
            {
                m_switchWeaponCounter++;
            }

            var msg = new SwitchToWeaponMsg();
            msg.ControlledEntityId = SyncedEntityId;
            msg.Weapon = weapon;
            msg.WeaponObjectBuilder = weaponObjectBuilder;
            msg.WeaponEntityId = weaponEntityId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnSwitchToWeaponRequest(MySyncControllableEntity sync, ref SwitchToWeaponMsg msg, MyNetworkClient sender)
        {
            if (!sync.Entity.CanSwitchToWeapon(msg.Weapon))
            {
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
                return;
            }

            // Allocate a new entity id for the new gun, if needed
            if (msg.WeaponObjectBuilder != null && msg.WeaponObjectBuilder.EntityId == 0)
            {
                msg.WeaponObjectBuilder = (MyObjectBuilder_EntityBase)msg.WeaponObjectBuilder.Clone();
                msg.WeaponObjectBuilder.EntityId = msg.WeaponEntityId == 0 ? MyEntityIdentifier.AllocateId() : msg.WeaponEntityId;
            }
            OnSwitchToWeaponSuccess(sync, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnSwitchToWeaponSuccess(MySyncControllableEntity sync, ref SwitchToWeaponMsg msg, MyNetworkClient sender)
        {
            if (!Sync.IsServer)
            {
                // Update the counter only if we are waiting for it
                if (sync.m_switchWeaponCounter > 0)
                {
                    sync.m_switchWeaponCounter--;
                }
            }

            var handler = sync.SwitchToWeaponSuccessHandler;
            if (handler != null)
                handler(msg.Weapon, msg.WeaponObjectBuilder, msg.WeaponEntityId);
        }

        private static void OnSwitchToWeaponFailure(MySyncControllableEntity sync, ref SwitchToWeaponMsg msg, MyNetworkClient sender)
        {
            if (!Sync.IsServer)
            {
                sync.m_switchWeaponCounter--;
            }

            var handler = sync.SwitchToWeaponFailureHandler;
            if (handler != null)
                handler(msg.Weapon, msg.WeaponObjectBuilder, msg.WeaponEntityId);
        }

        public void RequestSwitchAmmoMagazine()
        {
            if (!Sync.IsServer)
            {
                m_switchAmmoMagazineCounter++;
            }

            var msg = new SwitchAmmoMagazineMsg();
            msg.ControlledEntityId = SyncedEntityId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnSwitchAmmoMagazineRequest(MySyncControllableEntity sync, ref SwitchAmmoMagazineMsg msg, MyNetworkClient sender)
        {
            if (!sync.Entity.CanSwitchAmmoMagazine())
            {
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
                return;
            }

            OnSwitchAmmoMagazineSuccess(sync, ref msg, Sync.Clients.LocalClient);
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnSwitchAmmoMagazineSuccess(MySyncControllableEntity sync, ref SwitchAmmoMagazineMsg msg, MyNetworkClient sender)
        {
            if (!Sync.IsServer)
            {
                // Update the counter only if we are waiting for it
                if (sync.m_switchAmmoMagazineCounter > 0)
                {
                    sync.m_switchAmmoMagazineCounter--;
                }
            }

            var handler = sync.SwitchAmmoMagazineSuccessHandler;
            if (handler != null)
                handler();
        }

        private static void OnSwitchAmmoMagazineFailure(MySyncControllableEntity sync, ref SwitchAmmoMagazineMsg msg, MyNetworkClient sender)
        {
            if (!Sync.IsServer)
            {
                sync.m_switchAmmoMagazineCounter--;
            }

            var handler = sync.SwitchAmmoMagazineFailureHandler;
            if (handler != null)
                handler();
        }

        public void BeginShoot(Vector3 direction, MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            ShootBeginMsg msg = new ShootBeginMsg();
            msg.EntityId = SyncedEntityId;
            msg.Direction = direction;
            msg.Action = action;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);

            StartShooting(action, direction);
            m_lastShootDirectionUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (MyFakes.SIMULATE_QUICK_TRIGGER)
                EndShootInternal(action);
        }

        public void UpdateShootDirection(Vector3 direction, int multiplayerUpdateInterval)
        {
            if (multiplayerUpdateInterval != 0 && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastShootDirectionUpdate > multiplayerUpdateInterval)
            {
                ShootDirectionChangeMsg msg = new ShootDirectionChangeMsg();
                msg.EntityId = SyncedEntityId;
                msg.Direction = direction;

                MySession.Static.SyncLayer.SendMessageToAll(ref msg);
                m_lastShootDirectionUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
            ShootDirection = direction;
        }

        public void EndShoot(MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            if (MyFakes.SIMULATE_QUICK_TRIGGER) return;

            EndShootInternal(action);
        }

        private void EndShootInternal(MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            ShootEndMsg msg = new ShootEndMsg();
            msg.EntityId = SyncedEntityId;
            msg.Action = action;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);

            StopShooting(action);
        }

        static void ShootBeginCallback(MySyncControllableEntity sync, ref ShootBeginMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }

            bool wouldCallStartTwice = Sync.IsServer && sender.IsGameServer();
            if (!wouldCallStartTwice)
                sync.StartShooting(msg.Action, msg.Direction);
        }

        static void ShootEndCallback(MySyncControllableEntity sync, ref ShootEndMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }

            bool wouldCallStopTwice = Sync.IsServer && sender.IsGameServer();
            if (!wouldCallStopTwice)
                sync.StopShooting(msg.Action);
        }

        static void ShootDirectionChangeCallback(MySyncControllableEntity sync, ref ShootDirectionChangeMsg msg, MyNetworkClient sender)
        {
            sync.ShootDirection = msg.Direction;
        }

        private void StartShooting(MyShootActionEnum action, Vector3 direction)
        {
            ShootDirection = direction;
            m_isShooting[(int)action] = true;

            Entity.OnBeginShoot(action);
        }

        private void StopShooting(MyShootActionEnum action)
        {
            m_isShooting[(int)action] = false;

            Entity.OnEndShoot(action);
        }
    }
}
