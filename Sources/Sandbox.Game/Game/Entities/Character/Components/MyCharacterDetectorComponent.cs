#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Components;
using VRage.FileSystem;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public abstract class MyCharacterDetectorComponent : MyCharacterComponent
    {
        IMyEntity m_detectedEntity;
        IMyUseObject m_interactiveObject;
        private static List<MyEntity> m_detectableEntities = new List<MyEntity>();

        protected MyHudNotification m_useObjectNotification;
        protected MyHudNotification m_showTerminalNotification;
        protected MyHudNotification m_openInventoryNotification;

        protected bool m_usingContinuously = false;

        public override void UpdateAfterSimulation10()
        {
            if (m_useObjectNotification != null && !m_usingContinuously)
                MyHud.Notifications.Add(m_useObjectNotification);

            m_usingContinuously = false;

            if (!Character.IsSitting && !Character.IsDead)
            {
                DoDetection();
            }
            else
            {
                if (MySession.ControlledEntity == Character)
                {
                    MyHud.SelectedObjectHighlight.Visible = false;
                }
            }
        }

        public void DoDetection()
        {
            DoDetection(!Character.TargetFromCamera);
        }

        protected abstract void DoDetection(bool useHead);

        public IMyUseObject UseObject
        {
            get { return m_interactiveObject; }
            set
            {
                bool changed = value != m_interactiveObject;

                if (changed)
                {
                    if (m_interactiveObject != null)
                    {
                        UseClose();
                        InteractiveObjectRemoved();
                    }

                    m_interactiveObject = value;
                    InteractiveObjectChanged();
                }
            }
        }

        public IMyEntity DetectedEntity
        {
            protected set 
            { 
                if (m_detectedEntity != null)
                {
                    m_detectedEntity.OnMarkForClose -= OnDetectedEntityMarkForClose;
                }

                m_detectedEntity = value; 

                if (m_detectedEntity != null)
                {
                    m_detectedEntity.OnMarkForClose += OnDetectedEntityMarkForClose;
                }
            }
            get { return m_detectedEntity; }
        }

        public Vector3D HitPosition { protected set; get; }

        public Vector3 HitNormal { protected set; get; }

        public uint ShapeKey { protected set; get; }

        public Vector3D StartPosition { protected set; get; }

        public MyStringHash HitMaterial { protected set; get; }

        public HkRigidBody HitBody { protected set; get; }

        protected virtual void OnDetectedEntityMarkForClose(IMyEntity obj)
        {
            DetectedEntity = null;

            if (UseObject == null)
                return;

            UseObject = null;
            MyHud.SelectedObjectHighlight.Visible = false;
            MyHud.SelectedObjectHighlight.InteractiveObject = null;
        }

        void UseClose()
        {
            if (Character != null && UseObject != null && UseObject.IsActionSupported(UseActionEnum.Close))
            {
                UseObject.Use(UseActionEnum.Close, Character);
            }
        }

        void InteractiveObjectRemoved()
        {
            if (Character != null)
            {
                Character.RemoveNotification(ref m_useObjectNotification);
                Character.RemoveNotification(ref m_showTerminalNotification);
                Character.RemoveNotification(ref m_openInventoryNotification);
            }
        }

        void InteractiveObjectChanged()
        {
            if (MySession.ControlledEntity == this.Character && UseObject != null)
            {
                GetNotification(UseObject, UseActionEnum.Manipulate, ref m_useObjectNotification);
                GetNotification(UseObject, UseActionEnum.OpenTerminal, ref m_showTerminalNotification);
                GetNotification(UseObject, UseActionEnum.OpenInventory, ref m_openInventoryNotification);
                var useText = m_useObjectNotification != null ? m_useObjectNotification.Text : MySpaceTexts.Blank;
                var showText = m_showTerminalNotification != null ? m_showTerminalNotification.Text : MySpaceTexts.Blank;
                var openText = m_openInventoryNotification != null ? m_openInventoryNotification.Text : MySpaceTexts.Blank;
                if (useText != MySpaceTexts.Blank)
                    MyHud.Notifications.Add(m_useObjectNotification);
                if (showText != MySpaceTexts.Blank && showText != useText)
                    MyHud.Notifications.Add(m_showTerminalNotification);
                if (openText != MySpaceTexts.Blank && openText != showText && openText != useText)
                    MyHud.Notifications.Add(m_openInventoryNotification);
            }
        }

        void GetNotification(IMyUseObject useObject, UseActionEnum actionType, ref MyHudNotification notification)
        {
            if ((useObject.SupportedActions & actionType) != 0)
            {
                var actionInfo = useObject.GetActionInfo(actionType);
                Character.RemoveNotification(ref notification);
                notification = new MyHudNotification(actionInfo.Text, 0, level: actionInfo.IsTextControlHint ? MyNotificationLevel.Control : MyNotificationLevel.Normal);
                if (!MyInput.Static.IsJoystickConnected())
                {
                    notification.SetTextFormatArguments(actionInfo.FormatParams);
                }
                else
                {
                    if (actionInfo.JoystickText.HasValue)
                        notification.Text = actionInfo.JoystickText.Value;
                    if (actionInfo.JoystickFormatParams != null)
                        notification.SetTextFormatArguments(actionInfo.JoystickFormatParams);
                }
            }
        }

        public void UseContinues()
        {
            MyHud.Notifications.Remove(m_useObjectNotification);
            m_usingContinuously = true;
        }

        public override void OnCharacterDead()
        {
            base.OnCharacterDead();

            InteractiveObjectRemoved();
		}

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();            
            NeedsUpdateAfterSimulation10 = true;
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            InteractiveObjectRemoved();
        }

        protected void EnableDetectorsInArea(Vector3D from)
        {
            Debug.Assert(m_detectableEntities.Count == 0, "Detected entities weren't cleared");
            var boundingSphere = new BoundingSphereD(from, MyConstants.DEFAULT_INTERACTIVE_DISTANCE);
            MyGamePruningStructure.GetAllEntitiesInSphere(ref boundingSphere, m_detectableEntities);
            foreach (var ent in m_detectableEntities)
            {
                MyUseObjectsComponentBase use;
                if (ent.Components.TryGet<MyUseObjectsComponentBase>(out use))
                {
                    if (use.DetectorPhysics != null)
                    {
                        use.PositionChanged(use.Container.Get<MyPositionComponentBase>());
                        use.DetectorPhysics.Enabled = true;
                    }
                }
            }
        }

        protected void DisableDetectors()
        {
            foreach (var ent in m_detectableEntities)
            {
                MyUseObjectsComponentBase use;
                if (ent.Components.TryGet<MyUseObjectsComponentBase>(out use))
                {
                    if (use.DetectorPhysics != null)
                        use.DetectorPhysics.Enabled = false;
                }
            }
            m_detectableEntities.Clear();
        }
    }
}
