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
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;
using VRage.Import;
using VRage.Game.Models;
using VRage.Render.Models;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public class MyCharacterRaycastDetectorComponent : MyCharacterDetectorComponent
    {
        List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

        protected override void DoDetection(bool useHead)
        {
            ProfilerShort.Begin("DoDetection");
            if (Character == MySession.Static.ControlledEntity)
                MyHud.SelectedObjectHighlight.RemoveHighlight();

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

            //EnableDetectorsInArea(from);
            GatherDetectorsInArea(from);
            float closestDetector = float.MaxValue;
            IMyEntity closestEntity = null;
            IMyUseObject closestInteractive = null;
            foreach(var entity in m_detectableEntities)
            {
                if (entity == Character)
                    continue;
                var use = entity.Components.Get<MyUseObjectsComponentBase>() as MyUseObjectsComponent;
                if(use != null)
                {
                    float detectorDistance;
                    var interactive = use.RaycastDetectors(from, to, out detectorDistance);
                    if(Math.Abs(detectorDistance) < Math.Abs(closestDetector))
                    {
                        closestDetector = detectorDistance;
                        closestEntity = entity;
                        closestInteractive = interactive;
                    }
                }

                //Floating object handling - give FO useobject component!
                var use2 = entity as IMyUseObject;
                if (use2 != null)
                {
                    var m = use2.ActivationMatrix;
                    var ray = new RayD(from, to - from);
                    var obb = new MyOrientedBoundingBoxD(m);
                    var dist = obb.Intersects(ref ray);
                    if(dist.HasValue && Math.Abs(dist.Value) < Math.Abs(closestDetector))
                    {
                        closestDetector = (float)dist.Value;
                        closestEntity = entity;
                        closestInteractive = use2;
                    }
                }
            }
            m_detectableEntities.Clear();
            //VRageRender.MyRenderProxy.DebugDrawLine3D(from, to, Color.Red, Color.Green, true);
            //VRageRender.MyRenderProxy.DebugDrawSphere(headPos, 0.05f, Color.Red.ToVector3(), 1.0f, false);

            StartPosition = from;

            MyPhysics.CastRay(from, to, m_hits, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);

            bool hasInteractive = false;

            int index = 0;
            while (index < m_hits.Count && (m_hits[index].HkHitInfo.Body == null || m_hits[index].HkHitInfo.GetHitEntity() == Character
                || m_hits[index].HkHitInfo.GetHitEntity() == null
                || m_hits[index].HkHitInfo.Body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT)
                || m_hits[index].HkHitInfo.Body.Layer == MyPhysics.CollisionLayers.VoxelLod1CollisionLayer)) // Skip invalid hits and self character
            {
                index++;
            }

            if (index < m_hits.Count && m_hits[index].HkHitInfo.HitFraction > closestDetector - 0.05f)//compensation
            {
                // TODO: Uncomment to enforce that character must face object by front to activate it
                //if (TestInteractionDirection(head.Forward, h.Position - GetPosition()))
                //return;
                HitPosition = from + dir * closestDetector;
                MyUseObjectsComponentBase useObject;
                if(closestEntity.Components.TryGet<MyUseObjectsComponentBase>(out useObject))
                {
                    var detectorPhysics = useObject.DetectorPhysics;
                    HitMaterial = detectorPhysics.GetMaterialAt(HitPosition);
                    HitBody = ((MyPhysicsBody)detectorPhysics).RigidBody;
                }
                else
                {
                    HitMaterial = closestEntity.Physics.GetMaterialAt(HitPosition);
                    HitBody = ((MyPhysicsBody)closestEntity.Physics).RigidBody;
                }

                DetectedEntity = closestEntity;
                var interactive = closestInteractive;

                if (UseObject != null && interactive != null && UseObject != interactive)
                {
                    UseObject.OnSelectionLost();
                }

                //if (interactive != null && interactive.SupportedActions != UseActionEnum.None && (Vector3D.Distance(from, (Vector3D)h.Position)) < interactive.InteractiveDistance && Character == MySession.Static.ControlledEntity)
                if (interactive != null && interactive.SupportedActions != UseActionEnum.None && closestDetector * MyConstants.DEFAULT_INTERACTIVE_DISTANCE < interactive.InteractiveDistance && Character == MySession.Static.ControlledEntity)
                {
                    HandleInteractiveObject(interactive);

                    UseObject = interactive;
                    hasInteractive = true;
                }
            }

            if (!hasInteractive)
            {
                if (UseObject != null)
                {
                    UseObject.OnSelectionLost();
                }

                UseObject = null;         
            }

            ProfilerShort.End();
        }
    }
}
