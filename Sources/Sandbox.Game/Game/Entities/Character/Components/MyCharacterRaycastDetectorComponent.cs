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
    public class MyCharacterRaycastDetectorComponent : MyCharacterDetectorComponent
    {
        List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();
        IMyUseObject m_interactiveObject;

        MyHudNotification m_useObjectNotification;
        MyHudNotification m_showTerminalNotification;
        MyHudNotification m_openInventoryNotification;

        bool m_usingContinuously = false;


        public override void OnRemovedFromContainer()
        {
 	         base.OnRemovedFromContainer();

        }
    
        public override void UpdateAfterSimulation10()
        {
            if (m_useObjectNotification != null && !m_usingContinuously)
                MyHud.Notifications.Add(m_useObjectNotification);

            m_usingContinuously = false;

            if (MySession.ControlledEntity == Character && !Character.IsSitting && !Character.IsDead)
            {
                RayCast(MySession.GetCameraControllerEnum() != MyCameraControllerEnum.ThirdPersonSpectator);
            }
            else
            {
                if (MySession.ControlledEntity == Character)
                {
                    MyHud.SelectedObjectHighlight.Visible = false;
                }
            }
        }

        void RayCast(bool useHead)
        {
            if (Character == MySession.ControlledEntity)
                MyHud.SelectedObjectHighlight.Visible = false;

            var head = Character.GetHeadMatrix(false);
            var headPos = head.Translation - (Vector3D)head.Forward * 0.3; // Move to center of head, we don't want eyes (in front of head)

            Vector3D from;
            Vector3D dir;

            if (!useHead)
            {
                //Ondrej version
                var cameraMatrix = MySector.MainCamera.WorldMatrix;
                dir = cameraMatrix.Forward;
                from = MyUtils.LinePlaneIntersection(headPos, (Vector3)dir, cameraMatrix.Translation, (Vector3)dir);
            }
            else
            {
                //Petr version
                dir = head.Forward;
                from = headPos;
            }

            Vector3D to = from + dir * MyConstants.DEFAULT_INTERACTIVE_DISTANCE;


            //VRageRender.MyRenderProxy.DebugDrawLine3D(from, to, Color.Red, Color.Green, true);
            //VRageRender.MyRenderProxy.DebugDrawSphere(headPos, 0.05f, Color.Red.ToVector3(), 1.0f, false);

            MyPhysics.CastRay(from, to, m_hits);

            bool hasInteractive = false;

            int index = 0;
            while (index < m_hits.Count && (m_hits[index].HkHitInfo.Body == null || m_hits[index].HkHitInfo.Body.UserObject == Character.Physics
                || (Character.VirtualPhysics != null && m_hits[index].HkHitInfo.Body.UserObject == Character.VirtualPhysics) || m_hits[index].HkHitInfo.Body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))) // Skip invalid hits and self character
            {
                index++;
            }

            if (index < m_hits.Count)
            {
                //We must take only closest hit (others are hidden behind)
                var h = m_hits[index];
                var entity = h.HkHitInfo.Body.GetEntity();
                var interactive = entity as IMyUseObject;

                // TODO: Uncomment to enforce that character must face object by front to activate it
                //if (TestInteractionDirection(head.Forward, h.Position - GetPosition()))
                //return;

                if (entity != null)
                {
                    MyUseObjectsComponentBase useObject = null;
                    entity.Components.TryGet<MyUseObjectsComponentBase>(out useObject);
                    if (useObject != null)
                    {
                        interactive = useObject.GetInteractiveObject(h.HkHitInfo.GetShapeKey(0));
                    }
                }

                if (UseObject != null && interactive != null && UseObject != interactive)
                {
                    UseObject.OnSelectionLost();
                }

                if (interactive != null && interactive.SupportedActions != UseActionEnum.None && (Vector3D.Distance(from, (Vector3D)h.Position)) < interactive.InteractiveDistance && Character == MySession.ControlledEntity)
                {
                    MyHud.SelectedObjectHighlight.Visible = true;
                    MyHud.SelectedObjectHighlight.InteractiveObject = interactive;

                    UseObject = interactive;
                    hasInteractive = true;
                }
            }

            if (!hasInteractive)
            {
                if (UseObject != null)
                    UseObject.OnSelectionLost();

                UseObject = null;
            }
        }

        public override IMyUseObject UseObject
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

        void UseClose()
        {
            if (UseObject != null && UseObject.IsActionSupported(UseActionEnum.Close))
            {
                UseObject.Use(UseActionEnum.Close, Character);
            }
        }

        void InteractiveObjectRemoved()
        {
            Character.RemoveNotification(ref m_useObjectNotification);
            Character.RemoveNotification(ref m_showTerminalNotification);
            Character.RemoveNotification(ref m_openInventoryNotification);
        }

        void InteractiveObjectChanged()
        {
            if (MySession.ControlledEntity == this)
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

        public override void UseContinues()
        {
            MyHud.Notifications.Remove(m_useObjectNotification);
            m_usingContinuously = true;
        }




    }
}
