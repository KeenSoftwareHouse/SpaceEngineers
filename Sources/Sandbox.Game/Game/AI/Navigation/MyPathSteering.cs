﻿using Sandbox.Engine.Utils;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace Sandbox.Game.AI.Navigation
{
    public class MyPathSteering : MyTargetSteering
    {
        private MySmartPath m_path;
        private float m_weight;

        private const float END_RADIUS = 0.5f;
        private const float DISTANCE_FOR_FINAL_APPROACH = 2.0f;

        public bool PathFinished { get; private set; }

        public MyPathSteering(MyBotNavigation navigation)
            : base(navigation)
        { }

        public override string GetName()
        {
            return "Path steering";
        }

        // CH: TODO: Make a path pool and transfer ownership by calling this method (or any similar)
        public void SetPath(MySmartPath path, float weight = 1.0f)
        {
            if (path == null || !path.IsValid)
            {
                UnsetPath();
                return;
            }

            if (m_path != null)
            {
                m_path.Dispose();
            }

            m_path = path;
            m_weight = weight;

            PathFinished = false;

            SetNextTarget();
        }

        public void UnsetPath()
        {
            ProfilerShort.Begin("UnsetPath");
            if (m_path != null)
            {
                m_path.Dispose();
            }
            m_path = null;
            UnsetTarget();
            PathFinished = true;
            ProfilerShort.End();
        }

        private void SetNextTarget()
        {
            Vector3D? prevTarget = TargetWorld;

            if (m_path == null || !m_path.IsValid)
            {
                UnsetTarget();
                return;
            }

            IMyDestinationShape dest = m_path.Destination;
            Vector3D closestPoint = dest.GetClosestPoint(CapsuleCenter());
            double distSq = TargetDistanceSq(ref closestPoint);

            // TODO: Get rid of the ad-hoc numbers and use goal shapes

            if (distSq > END_RADIUS * END_RADIUS)
            {
                float radius;
                Vector3D targetWorld;
                MyEntity relativeEntity;

                Vector3D currentParentPosition = Parent.PositionAndOrientation.Translation;
                if (m_path.PathCompleted)
                {
                    if (distSq < DISTANCE_FOR_FINAL_APPROACH * DISTANCE_FOR_FINAL_APPROACH)
                    {
                        var endEntity = m_path.EndEntity;
                        UnsetPath();
                        SetTarget(closestPoint, END_RADIUS, endEntity, m_weight);
                        return;
                    }
                    else
                    {
                        if (prevTarget.HasValue)
                        {
                            m_path.Reinit(prevTarget.Value);
                        }
                        else
                        {
                            m_path.Reinit(currentParentPosition);
                        }
                    }
                }

                ProfilerShort.Begin("MySmartPath.GetNextTarget");
                bool result = m_path.GetNextTarget(Parent.PositionAndOrientation.Translation, out targetWorld, out radius, out relativeEntity);
                ProfilerShort.End();

                if (result)
                {
                    SetTarget(targetWorld, radius, relativeEntity, m_weight);
                    return;
                }
            }

            UnsetPath();
        }

        public override void Update()
        {
            if (m_path == null)
            {
                base.Update();
                return;
            }

            if (!m_path.IsValid)
            {
                UnsetPath();
            }
            else if (TargetReached())
            {
                ProfilerShort.Begin("SetNextTarget");
                SetNextTarget();
                ProfilerShort.End();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if (m_path != null && m_path.IsValid)
            {
                m_path.Dispose();
            }
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            if (m_path == null || !m_path.IsValid)
            {
                return;
            }

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyFakes.DEBUG_DRAW_FOUND_PATH)
            {
                m_path.DebugDraw();
            }
        }
    }
}
