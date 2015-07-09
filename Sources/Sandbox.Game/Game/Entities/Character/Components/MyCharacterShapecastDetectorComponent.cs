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
    public class MyCharacterShapecastDetectorComponent : MyCharacterDetectorComponent
    {
        const float SHAPE_RADIUS = 0.1f;
        List<HkContactBodyData> m_hits = new List<HkContactBodyData>();
        protected override void DoDetection(bool useHead)
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

            MatrixD matrix = MatrixD.CreateTranslation(from);
            HkShape shape = new HkSphereShape(SHAPE_RADIUS);
            IMyEntity hitEntity = null;
            int shapeKey = -1;
            Vector3D hitPosition = Vector3D.Zero;
            m_hits.Clear();

            try
            {
                MyPhysics.CastShapeReturnContactBodyDatas(to, shape, ref matrix, 0, 0f, m_hits);

                int index = 0;
                while (index < m_hits.Count && (m_hits[index].Body == null || m_hits[index].Body.UserObject == Character.Physics
                    || (Character.VirtualPhysics != null && m_hits[index].Body.UserObject == Character.VirtualPhysics) || m_hits[index].Body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))) // Skip invalid hits and self character
                {
                    index++;
                }

                if (index < m_hits.Count)
                {
                    hitEntity = m_hits[index].Body.GetEntity();
                    shapeKey = m_hits[index].ShapeKey;
                    hitPosition = m_hits[index].HitPosition;
                }
            }
            finally
            {
                shape.RemoveReference();
            }

            bool hasInteractive = false;

            var interactive = hitEntity as IMyUseObject;
            DetectedEntity = hitEntity;

            if (hitEntity != null)
            {
                MyUseObjectsComponentBase useObject = null;
                hitEntity.Components.TryGet<MyUseObjectsComponentBase>(out useObject);
                if (useObject != null)
                {
                    interactive = useObject.GetInteractiveObject(shapeKey);
                }
            }

            if (UseObject != null && interactive != null && UseObject != interactive)
            {
                UseObject.OnSelectionLost();
            }

            if (interactive != null && interactive.SupportedActions != UseActionEnum.None && (Vector3D.Distance(from, hitPosition)) < interactive.InteractiveDistance && Character == MySession.ControlledEntity)
            {
                MyHud.SelectedObjectHighlight.Visible = true;
                MyHud.SelectedObjectHighlight.InteractiveObject = interactive;

                UseObject = interactive;
                hasInteractive = true;
            }

            if (!hasInteractive)
            {
                if (UseObject != null)
                    UseObject.OnSelectionLost();

                UseObject = null;
            }
        }
    }
}
