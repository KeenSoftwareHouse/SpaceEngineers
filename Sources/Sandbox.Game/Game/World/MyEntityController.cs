using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;
using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;

namespace Sandbox.Game.World
{
    public partial class MyEntityController
    {
        private Action<MyEntity> m_controlledEntityClosing;

        /// <summary>
        /// The entity that this controller controls
        /// </summary>
        public IMyControllableEntity ControlledEntity { get; protected set; }

        /// <summary>
        /// event params: oldEntity, newEntity
        /// </summary>
        public event Action<IMyControllableEntity, IMyControllableEntity> ControlledEntityChanged;

        public MyPlayer Player { get; private set; }

        public MyEntityController(MyPlayer parent)
        {
            Player = parent;

            m_controlledEntityClosing = ControlledEntity_OnClosing;
        }

        public void TakeControl(IMyControllableEntity entity)
        {
            if (ControlledEntity == entity)
                return;

            if (entity != null && entity.ControllerInfo.Controller != null)
            {
                Debug.Fail("Entity controlled by another controller, release it first");
                return;
            }

            var old = ControlledEntity;

            if (old != null)
            {
                var entityCameraSettings = old.GetCameraEntitySettings();

                float headLocalXAngle = old.HeadLocalXAngle;
                float headLocalYAngle = old.HeadLocalYAngle;

                old.Entity.OnClosing -= m_controlledEntityClosing;
                old.ControllerInfo.Controller = null; // This will call OnControlReleased
                ControlledEntity = null;

                bool firstPerson = entityCameraSettings != null? entityCameraSettings.IsFirstPerson : (MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.ThirdPersonSpectator);

                if (!MySandboxGame.IsDedicated)
                {
                    MySession.Static.Cameras.SaveEntityCameraSettings(
                        Player.Id,
                        old.Entity.EntityId,
                        firstPerson,
                        MyThirdPersonSpectator.Static.GetViewerDistance(),
                        headLocalXAngle,
                        headLocalYAngle);
                }

            }
            if (entity != null)
            {
                ControlledEntity = entity;
                ControlledEntity.Entity.OnClosing += m_controlledEntityClosing;
                ControlledEntity.ControllerInfo.Controller = this; // This will call OnControlAcquired

                if (!MySandboxGame.IsDedicated && ControlledEntity.Entity is IMyCameraController)
                {
                    MySession.Static.SetEntityCameraPosition(Player.Id, ControlledEntity.Entity);
                }
            }

            if (old != entity)
                RaiseControlledEntityChanged(old, entity);
        }

        private void RaiseControlledEntityChanged(IMyControllableEntity old, IMyControllableEntity entity)
        {
            var handler = ControlledEntityChanged;
            if (handler != null) handler(old, entity);
        }

        private void ControlledEntity_OnClosing(MyEntity entity)
        {
            if (ControlledEntity == null) // Already freed
                return;

            Debug.Assert(entity == ControlledEntity, "Inconsistency, event should not be registered on this entity");
            TakeControl(null);
        }
    }

    public class MyControllerInfo
    {
        private MyEntityController m_controller;
        public MyEntityController Controller
        {
            get
            {
                return m_controller;
            }
            set
            {
                if (m_controller != value)
                {
                    if (m_controller != null)
                    {
                        var handler = ControlReleased;
                        if (handler != null) handler(m_controller);

                        m_controller = null;
                    }

                    if (value != null)
                    {
                        m_controller = value;

                        var handler = ControlAcquired;
                        if (handler != null) handler(m_controller);
                    }
                }

                Debug.Assert(m_controller == null || m_controller.ControlledEntity.ControllerInfo == this, "Inconsistency! ControlledEntity in MyEntityController is not set correctly!");
            }
        }

        public long ControllingIdentityId
        {
            get
            {
                if (Controller == null) return 0;
                return Controller.Player.Identity.IdentityId;
            }
        }

        public event Action<MyEntityController> ControlAcquired;
        public event Action<MyEntityController> ControlReleased;

        public bool IsLocallyControlled()
        {
            return Controller != null && Sync.Clients != null && Controller.Player.Client == Sync.Clients.LocalClient;
        }

        public bool IsLocallyHumanControlled()
        {
            return Controller != null && Controller.Player == Sync.Clients.LocalClient.FirstPlayer;
        }

        public bool IsRemotelyControlled()
        {
            return Controller != null && Controller.Player.Client != Sync.Clients.LocalClient;
        }
    }
}
