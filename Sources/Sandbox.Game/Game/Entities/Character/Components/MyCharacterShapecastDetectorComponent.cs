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
        public const float DEFAULT_SHAPE_RADIUS = 0.1f;
        List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

        public float ShapeRadius { get; set; }

        public MyCharacterShapecastDetectorComponent()
        {
            ShapeRadius = DEFAULT_SHAPE_RADIUS;
        }

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
                //var cameraMatrix = MySector.MainCamera.WorldMatrix;
                var cameraMatrix = Character.Get3rdBoneMatrix(true, true);
                dir = cameraMatrix.Forward;
                from = MyUtils.LinePlaneIntersection(headPos, (Vector3)dir, cameraMatrix.Translation, (Vector3)dir);
            }
            else
            {
                //Petr version
                dir = head.Forward;
                from = headPos;
            }

            Vector3D to = from + dir * 2.5f;

            StartPosition = from;

            MatrixD matrix = MatrixD.CreateTranslation(from);
            HkShape shape = new HkSphereShape(ShapeRadius);
            IMyEntity hitEntity = null;
            ShapeKey = uint.MaxValue;
            HitPosition = Vector3D.Zero;
            HitNormal = Vector3.Zero;
            m_hits.Clear();

            try
            {
                EnableDetectorsInArea(from);

                MyPhysics.CastShapeReturnContactBodyDatas(to, shape, ref matrix, 0, 0f, m_hits);

                int index = 0;
                while (index < m_hits.Count && (m_hits[index].HkHitInfo.Body == null || m_hits[index].HkHitInfo.GetHitEntity() == Character
                    || m_hits[index].HkHitInfo.Body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))) // Skip invalid hits and self character
                {
                    index++;
                }

                if (index < m_hits.Count)
                {
                    hitEntity = m_hits[index].HkHitInfo.GetHitEntity();
                    ShapeKey = m_hits[index].HkHitInfo.GetShapeKey(0);
                    HitPosition = m_hits[index].Position;
                    HitNormal = m_hits[index].HkHitInfo.Normal;
                    HitMaterial = m_hits[index].HkHitInfo.Body.GetBody().GetMaterialAt(HitPosition + HitNormal * 0.1f);
                    HitBody = m_hits[index].HkHitInfo.Body;
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
                    interactive = useObject.GetInteractiveObject(ShapeKey);
                }
            }

            if (UseObject != null && interactive != null && UseObject != interactive)
            {
                UseObject.OnSelectionLost();
            }

            if (interactive != null && interactive.SupportedActions != UseActionEnum.None && (Vector3D.Distance(from, HitPosition)) < interactive.InteractiveDistance && Character == MySession.ControlledEntity)
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

            DisableDetectors();
        }
    }
}
