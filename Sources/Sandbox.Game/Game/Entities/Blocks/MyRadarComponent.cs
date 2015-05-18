using System;
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
        public List<MyEntity> m_markers = new List<MyEntity>(); 

        public delegate bool CheckControlDelegate();

        public float DetectionRadius { get; set; }
        public CheckControlDelegate OnCheckControl;
        private List<MyDataBroadcaster> m_broadcastersCache = new List<MyDataBroadcaster>();
        private List<MyEntity> m_entitiesCache = new List<MyEntity>();

        public bool BroadcastUsingAntennas { get; set; }

        public float MinimumSize { get; set; }
        public float MaximumSize { get; set; }
        public int TrackingLimit { get; set; }

        public bool SetRelayedRequest { get; set; }

        private Dictionary<MyEntity, RadarSignature> m_signatureCache = new Dictionary<MyEntity, RadarSignature>();
        private List<RadarSignature> m_targetsCache = new List<RadarSignature>();

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

            public Vector3D Center { get { return m_boundingSphere.Center; } }

            public double Radius { get { return m_boundingSphere.Radius; } }

            public RadarSignature(BoundingSphereD boundingSphere, MyEntity entity)
            {
                m_entity = entity;
                m_boundingSphere = boundingSphere;
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

            m_entitiesCache.Clear();
            m_targetsCache.Clear();
            MyGamePruningStructure.GetAllEntitiesInSphere<MyEntity>(ref sphere, m_entitiesCache);
            for (int i = 0; i < m_entitiesCache.Count; i++)
            {
                var myEntity = m_entitiesCache[i];
                if (myEntity is MyVoxelMap || myEntity is MyCubeGrid)
                {
                    RadarSignature signature;
                    if (m_signatureCache.TryGetValue(myEntity, out signature))
                    {
                        signature.m_boundingSphere = myEntity.PositionComp.WorldVolume;                       
                    }
                    else
                    {
                        signature = new RadarSignature(myEntity.PositionComp.WorldVolume, myEntity);
                        m_signatureCache[myEntity] = signature;
                    }
                    m_targetsCache.Add(signature);
                }
            }

            if (m_signatureCache.Count > m_targetsCache.Count + 500)
                m_signatureCache.Clear();

            m_targetsCache.Sort(SizeComparison);
            
            int validTargets = 0;
            for (int i = 0; i < m_targetsCache.Count; i++)
            {
                var distance = Vector3D.Distance(position, m_targetsCache[i].Center);
                m_targetsCache[i].m_distance = distance;

                double modifiedDetectionRadius = DetectionRadius;
                if (m_targetsCache[i].Radius < 50)
                    modifiedDetectionRadius *= m_targetsCache[i].Radius / 50;
                if (distance > modifiedDetectionRadius)
                    continue;
                if (distance < 100)
                    continue;

                // Filter out targets which are too close to other, larger targets
                for (int j = 0; j < validTargets; j++)
                {
                    var separation = Vector3D.Distance(m_targetsCache[i].Center, m_targetsCache[j].Center) -
                                        (m_targetsCache[i].Radius + m_targetsCache[j].Radius) * distance / DetectionRadius;
                    if (separation < distance * 0.04)
                        goto next_target;
                }
                m_targetsCache[validTargets++] = m_targetsCache[i];
            next_target:
                ;
            }

            m_targetsCache.RemoveRange(validTargets, m_targetsCache.Count - validTargets);
            m_targetsCache.Sort((signature1, signature2) => Math.Sign(signature1.m_distance - signature2.m_distance));

            int targetCount = 0;
            for (int i = 0; i < validTargets; i++)
            {
                var radarSignature = m_targetsCache[i];
                if (MaximumSize < MyRadar.InfiniteSize && radarSignature.Radius * 2 > MaximumSize)
                    continue;
                if (radarSignature.Radius * 2 < MinimumSize)
                    continue;

                if (TrackingLimit < MyRadar.InfiniteTracking && ++targetCount > TrackingLimit)
                    break;

                m_markers.Add(radarSignature.m_entity);
                MyHud.RadarMarkers.RegisterMarker(radarSignature.m_entity);
            }
        }

        private static int SizeComparison(RadarSignature signature1, RadarSignature signature2)
        {
            return signature1.Radius != signature2.Radius ? signature2.Radius.CompareTo(signature1.Radius) : signature1.m_entity.EntityId.CompareTo(signature2.m_entity.EntityId);
        }
    }
}
