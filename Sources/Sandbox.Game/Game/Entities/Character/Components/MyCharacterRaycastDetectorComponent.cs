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
using VRage.Game.Entity;
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
using VRage.Import;
using VRage.Game.Models;
using VRage.Profiler;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public class MyCharacterRaycastDetectorComponent : MyCharacterDetectorComponent
    {
        private readonly List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();
        private readonly List<MyUseObjectsComponentBase> m_hitUseComponents = new List<MyUseObjectsComponentBase>();

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
            StartPosition = from;

            // Processing of hit entities 
            MyPhysics.CastRay(from, to, m_hits, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);
            bool hasUseObject = false;
            int index = 0;
            while (index < m_hits.Count)
            {
                var hitEntity = m_hits[index].HkHitInfo.GetHitEntity();

                // Skip invalid hits
                if (m_hits[index].HkHitInfo.Body == null
                    || hitEntity == null 
                    || hitEntity == Character
                    || m_hits[index].HkHitInfo.Body.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT)
                    || m_hits[index].HkHitInfo.Body.Layer == MyPhysics.CollisionLayers.VoxelLod1CollisionLayer)
                {
                    index++;
                    continue;
                }

                // Cube Grids are special case
                var cubeGrid = hitEntity as MyCubeGrid;
                if (cubeGrid != null)
                {
                    // Correct the hit position (Physics offset)
                    var correctedPosition = m_hits[index].Position 
                        - m_hits[index].HkHitInfo.Normal * m_hits[index].HkHitInfo.GetConvexRadius();
                    // For hit cube grids get the fat hit fatblock instead.
                    var slimBlock = cubeGrid.GetTargetedBlock(correctedPosition);
                    if (slimBlock != null && slimBlock.FatBlock != null)
                    {
                        hitEntity = slimBlock.FatBlock;
                    }
                }

                m_hitUseComponents.Clear();
                var hitUseObject = hitEntity as IMyUseObject;
                // Retrive all above use components from parent structure. (Because of subParts)
                GetUseComponentsFromParentStructure(hitEntity, m_hitUseComponents);
                // Check for UseObjects and entities with UseObjectComponentBase.
                // Assuming that entity cannot be IMyUseObject and have UseObjectComponentBase in hierarchy
                // at the same time.
                if(hitUseObject != null || m_hitUseComponents.Count > 0)
                {
                    if (m_hitUseComponents.Count > 0)
                    {
                        // Process the valid hit entity
                        var closestDetectorDistance = float.MaxValue;
                        double physicalHitDistance = Vector3D.Distance(from, m_hits[index].Position);
                        MyUseObjectsComponentBase hitUseComp = null;
                        // Evaluate the set of found detectors and try to find the closest one
                        foreach (var hitUseComponent in m_hitUseComponents)
                        {
                            float detectorDistance;
                            var interactive = hitUseComponent.RaycastDetectors(from, to, out detectorDistance);
                            detectorDistance *= MyConstants.DEFAULT_INTERACTIVE_DISTANCE;
                            if (Math.Abs(detectorDistance) < Math.Abs(closestDetectorDistance) 
                                && (detectorDistance < physicalHitDistance 
                                || hitUseComponent.Entity == hitEntity)) // Remove to fix the problem with picking through physic bodies,
                            {                                           // but will introduce new problem with detectors inside physic bodies.
                                closestDetectorDistance = detectorDistance;
                                hitUseComp = hitUseComponent;
                                hitEntity = hitUseComponent.Entity;
                                hitUseObject = interactive;
                            }
                        }

                        // Detector found
                        if (hitUseComp != null)
                        {
                            // Process successful hit with results
                            var detectorPhysics = hitUseComp.DetectorPhysics;
                            HitMaterial = detectorPhysics.GetMaterialAt(HitPosition);
                            HitBody = m_hits[index].HkHitInfo.Body;
                            HitPosition = m_hits[index].Position;
                            DetectedEntity = hitEntity;
                        }
                    } 
                    else
                    {
                        // Case for hitting IMyUseObject already before even looking for UseComponent.
                        // Floating object case.
                        HitMaterial = hitEntity.Physics.GetMaterialAt(HitPosition);
                        HitBody = m_hits[index].HkHitInfo.Body;
                    }

                    // General logic for processing both cases.
                    if (hitUseObject != null)
                    {
                        HitPosition = m_hits[index].Position;
                        DetectedEntity = hitEntity;

                        if (UseObject != null && UseObject != hitEntity && UseObject != hitUseObject)
                        {
                            UseObject.OnSelectionLost();
                        }

                        if (Character == MySession.Static.ControlledEntity && hitUseObject.SupportedActions != UseActionEnum.None)
                        {
                            HandleInteractiveObject(hitUseObject);

                            UseObject = hitUseObject;
                            hasUseObject = true;
                        }
                    }
                    break;
                }

                index++;
            }

            if (!hasUseObject)
            {
                if (UseObject != null)
                {
                    UseObject.OnSelectionLost();
                }

                UseObject = null;         
            }

            ProfilerShort.End();
        }

        // Retrieves UseComponents from provided entity and above parent structure.
        private void GetUseComponentsFromParentStructure(IMyEntity currentEntity, List<MyUseObjectsComponentBase> useComponents)
        {
            var useComp = currentEntity.Components.Get<MyUseObjectsComponentBase>();
            if (useComp != null)
            {
                useComponents.Add(useComp);
            }

            if (currentEntity.Parent != null)
            {
                GetUseComponentsFromParentStructure(currentEntity.Parent, useComponents);
            }
        }
    }
}
