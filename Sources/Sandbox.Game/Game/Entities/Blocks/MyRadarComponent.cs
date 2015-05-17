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
        public List<MyGps> m_markers = new List<MyGps>(); 

        public delegate bool CheckControlDelegate();

        public float DetectionRadius { get; set; }
        public CheckControlDelegate OnCheckControl;

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
            public BoundingSphereD m_boundingSphere;
            public double m_distance;
            public bool m_isAntenna;

            public RadarSignature(BoundingSphereD boundingSphere, bool isAntenna = false, double distance = 0.0)
            {
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

            // Collect radio broadcasters. The radar can't display radio broadcasters itself, but radar broadcaster
            // signatures will hide nearby entities.
            List<MyDataBroadcaster> broadcasters = new List<MyDataBroadcaster>();
            MyRadioBroadcasters.GetAllBroadcastersInSphere(sphere, broadcasters);
            for (int i = 0; i < broadcasters.Count; i++)
            {
                targets.Add(
                    new RadarSignature(
                        new BoundingSphereD(broadcasters[i].BroadcastPosition,
                            (broadcasters[i] as MyRadioBroadcaster).BroadcastRadius), true));
            }

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere<MyEntity>(ref sphere, entities);
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i] is MyVoxelMap)
                    targets.Add(new RadarSignature(entities[i].PositionComp.WorldVolume));
                else if (entities[i] is MyCubeGrid)
                    targets.Add(new RadarSignature(entities[i].PositionComp.WorldVolume));
            }
            targets.Sort((d, sphereD) => Math.Sign(sphereD.m_boundingSphere.Radius - d.m_boundingSphere.Radius));
            int validTargets = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var distance = Vector3D.Distance(position, targets[i].m_boundingSphere.Center);
                targets[i].m_distance = distance;

                double modifiedDetectionRadius = DetectionRadius;
                if (targets[i].m_boundingSphere.Radius < 50)
                    modifiedDetectionRadius *= targets[i].m_boundingSphere.Radius / 50;
                if (distance > modifiedDetectionRadius)
                    continue;
                if (distance < 100)
                    continue;

                // Filter out targets which are too close to other, larger targets
                for (int j = 0; j < validTargets; j++)
                {
                    var separation = Vector3D.Distance(targets[i].m_boundingSphere.Center, targets[j].m_boundingSphere.Center);
                    if (separation < distance * 0.04f)
                        goto next_target;
                }
                targets[validTargets++] = targets[i];
            next_target:
                ;
            }

            targets.RemoveRange(validTargets, targets.Count - validTargets);
            targets.Sort((signature, radarSignature) => Math.Sign(signature.m_distance - radarSignature.m_distance));

            int targetCount = 0;
            for (int i = 0; i < validTargets; i++)
            {
                if (targets[i].m_isAntenna)
                    continue;
                if (targets[i].m_boundingSphere.Radius * 2 > MaximumSize)
                    continue;
                if (targets[i].m_boundingSphere.Radius * 2 < MinimumSize)
                    continue;

                if (TrackingLimit <= 100 && ++targetCount > TrackingLimit)
                    break;

                var desc = new MyGps();
                desc.Description = "";
                desc.DiscardAt = null;
                desc.Coords = targets[i].m_boundingSphere.Center;
                desc.ShowOnHud = true;

                m_markers.Add(desc);
                MyHud.RadarMarkers.RegisterMarker(desc);
            }
        }
    }
}
