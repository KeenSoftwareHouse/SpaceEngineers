#region Using

using Havok;
using Sandbox.Common;
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
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game.Components;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
using VRage.Game.Models;

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
            DoDetection(useHead, false);
        }

        // CHECK-ME Evaluate adding more query customization on the base class
        public void DoDetectionModel()
        {
            DoDetection(!Character.TargetFromCamera, true);
        }

        private void DoDetection(bool useHead, bool doModelIntersection)
        {
            if (Character == MySession.Static.ControlledEntity)
                MyHud.SelectedObjectHighlight.RemoveHighlight();

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

            Vector3D to = from + dir * 2.5;//MyConstants.DEFAULT_INTERACTIVE_DISTANCE;

            StartPosition = from;

            MatrixD matrix = MatrixD.CreateTranslation(from);
            HkShape shape = new HkSphereShape(ShapeRadius);
            IMyEntity hitEntity = null;
            ShapeKey = uint.MaxValue;
            HitPosition = Vector3D.Zero;
            HitNormal = Vector3.Zero;
            HitMaterial = MyStringHash.NullOrEmpty;
            HitTag = null;
            m_hits.Clear();

            Vector3 interactivePosition = Vector3D.Zero;

            try
            {
                EnableDetectorsInArea(from);

                MyPhysics.CastShapeReturnContactBodyDatas(to, shape, ref matrix, 0, 0f, m_hits);

                m_hits.Sort(delegate(MyPhysics.HitInfo info1, MyPhysics.HitInfo info2)
                {
                    float dot1 = Vector3.Dot(head.Forward, Vector3.Normalize(info1.Position - StartPosition));
                    float dot2 = Vector3.Dot(head.Forward, Vector3.Normalize(info2.Position - StartPosition));
                    return dot1.CompareTo(dot2);
                });
                
                if (m_hits.Count > 0)
                {

                    int index = 0;

                    bool isValidBlock = false;
                    bool isPhysicalBlock = false;

                    do
                    {
                        HkRigidBody body = m_hits[index].HkHitInfo.Body;
                        IMyEntity entity = m_hits[index].HkHitInfo.GetHitEntity();

                        if (entity is VRage.Game.Entity.MyEntitySubpart)
                        {
                            entity = entity.Parent;
                        }

                        isValidBlock = body != null && entity != null && entity != Character && !body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT);

                        isPhysicalBlock = entity != null && entity.Physics != null;

                        if (hitEntity == null && isValidBlock)
                        {
                            hitEntity = entity;
                            ShapeKey = m_hits[index].HkHitInfo.GetShapeKey(0);
                        }

                        // Set hit material etc. only for object's that have physical representation in the world, this exclude detectors
                        if (HitMaterial.Equals(MyStringHash.NullOrEmpty) && isValidBlock && isPhysicalBlock)
                        {
                            HitBody = body;
                            HitNormal = m_hits[index].HkHitInfo.Normal;
                            HitPosition = m_hits[index].GetFixedPosition();
                            HitMaterial = body.GetBody().GetMaterialAt(HitPosition);

                            interactivePosition = HitPosition;
                        }
                        else if (body != null)
                        {
                            interactivePosition = m_hits[index].GetFixedPosition();
                        }

                        index++;

                    } while (index < m_hits.Count && (!isValidBlock || !isPhysicalBlock));
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

                // Do accurate collision checking on model
                if (doModelIntersection)
                {
                    LineD line = new LineD(from, to);
                    var character = hitEntity as MyCharacter;
                    if (character == null)
                    {
                        MyIntersectionResultLineTriangleEx? result;
                        bool success = hitEntity.GetIntersectionWithLine(ref line, out result, IntersectionFlags.ALL_TRIANGLES);
                        if (success)
                        {
                            HitPosition = result.Value.IntersectionPointInWorldSpace;
                            HitNormal = result.Value.NormalInWorldSpace;
                        }
                    }
                    else
                    {
                        bool success = character.GetIntersectionWithLine(ref line, ref CharHitInfo);
                        if (success)
                        {
                            HitPosition = CharHitInfo.Triangle.IntersectionPointInWorldSpace;
                            HitNormal = CharHitInfo.Triangle.NormalInWorldSpace;
                            HitTag = CharHitInfo;
                        }
                    }
                }
            }

            if (UseObject != null && interactive != null && UseObject != interactive)
            {
                UseObject.OnSelectionLost();
            }

            if (interactive != null && interactive.SupportedActions != UseActionEnum.None && (Vector3D.Distance(from, interactivePosition)) < interactive.InteractiveDistance && Character == MySession.Static.ControlledEntity)
            {
                HandleInteractiveObject(interactive);

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
