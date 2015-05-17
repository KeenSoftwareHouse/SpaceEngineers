﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BulletXNA;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    class MyRadarComponent 
    {
        public List<MyGps> m_markers = new List<MyGps>(); 

        public delegate bool CheckControlDelegate();

        public float DetectionRadius { get; set; }
        public CheckControlDelegate OnCheckControl;
        private List<MyDataBroadcaster> m_broadcastersCache = new List<MyDataBroadcaster>();

        public bool BroadcastUsingAntennas { get; set; }

        public float MinimumSize { get; set; }
        public float MaximumSize { get; set; }
        public int TrackingLimit { get; set; }

        public bool SetRelayedRequest { get; set; }

        public MyRadarComponent() 
        {
            DetectionRadius = 5000;
            MinimumSize = 1;
            MaximumSize = 10000;
            TrackingLimit = 101;
            SetRelayedRequest = false;
            BroadcastUsingAntennas = false;
        }

        public void Clear()
        {
            foreach (var gps in m_markers)
                MyHud.RadarMarkers.UnregisterMarker(gps);
            m_markers.Clear();
        }

        class RadarSignature
        {
            public MyEntity m_entity;
            public BoundingSphereD m_boundingSphere;
            public double m_distance;
            public bool m_isAntenna;

            public Vector3D Center { get { return m_boundingSphere.Center; } }

            public double Radius { get { return m_boundingSphere.Radius; } }

            public RadarSignature(BoundingSphereD boundingSphere, MyEntity entity, bool isAntenna = false, double distance = 0.0)
            {
                m_entity = entity;
                m_boundingSphere = boundingSphere;
                m_distance = distance;
                m_isAntenna = isAntenna;
            }
        }

        public void Update(Vector3D position, bool checkControl = true)
        {
            Clear();

            if (!SetRelayedRequest && checkControl && !OnCheckControl())
            {
                return;
            }

            SetRelayedRequest = false;

            var sphere = new BoundingSphereD(position, DetectionRadius);
            List<RadarSignature> targets = new List<RadarSignature>();

            // Collect radio broadcasters. The radar can't display radio broadcasters itself, but broadcaster
            // signatures will hide nearby entities. Broadcasters use the broadcast radius as their radius, so 
            // they will almost always come first when checking for hidden entities.
            m_broadcastersCache.Clear();
            MyRadioBroadcasters.GetAllBroadcastersInSphere(sphere, m_broadcastersCache);
            for (int i = 0; i < m_broadcastersCache.Count; i++)
            {
                var myDataBroadcaster = m_broadcastersCache[i];
                targets.Add(
                    new RadarSignature(
                        new BoundingSphereD(myDataBroadcaster.BroadcastPosition,
                            (myDataBroadcaster as MyRadioBroadcaster).BroadcastRadius), myDataBroadcaster.Parent, true));
            }

            List<MyEntity> m_entitiesCache = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere<MyEntity>(ref sphere, m_entitiesCache);
            for (int i = 0; i < m_entitiesCache.Count; i++)
            {
                var myEntity = m_entitiesCache[i];
                if (myEntity is MyVoxelMap)
                    targets.Add(new RadarSignature(myEntity.PositionComp.WorldVolume, myEntity));
                else if (myEntity is MyCubeGrid)
                    targets.Add(new RadarSignature(myEntity.PositionComp.WorldVolume, myEntity));
            }

            targets.Sort((signature1, signature2) => Math.Sign(signature2.Radius - signature1.Radius));
            
            int validTargets = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var distance = Vector3D.Distance(position, targets[i].Center);
                targets[i].m_distance = distance;

                double modifiedDetectionRadius = DetectionRadius;
                if (targets[i].Radius < 50)
                    modifiedDetectionRadius *= targets[i].Radius / 50;
                if (distance > modifiedDetectionRadius)
                    continue;
                if (distance < 100)
                    continue;

                // Filter out targets which are too close to other, larger targets
                for (int j = 0; j < validTargets; j++)
                {
                    var separation = Vector3D.Distance(targets[i].Center, targets[j].Center);
                    if (separation < distance * 0.04f)
                        goto next_target;
                }
                targets[validTargets++] = targets[i];
            next_target:
                ;
            }

            targets.RemoveRange(validTargets, targets.Count - validTargets);
            targets.Sort((signature1, signature2) => Math.Sign(signature1.m_distance - signature2.m_distance));

            int targetCount = 0;
            for (int i = 0; i < validTargets; i++)
            {
                if (targets[i].m_isAntenna)
                    continue;
                if (MaximumSize < MyRadar.InfiniteSize && targets[i].Radius * 2 > MaximumSize)
                    continue;
                if (targets[i].Radius * 2 < MinimumSize)
                    continue;

                if (TrackingLimit < MyRadar.InfiniteTracking && ++targetCount > TrackingLimit)
                    break;

                var desc = new MyGps();
                desc.Description = "";
                desc.DiscardAt = null;
                desc.Coords = targets[i].Center;
                desc.ShowOnHud = true;

                m_markers.Add(desc);
                MyHud.RadarMarkers.RegisterMarker(desc);
            }
        }
    }
}
