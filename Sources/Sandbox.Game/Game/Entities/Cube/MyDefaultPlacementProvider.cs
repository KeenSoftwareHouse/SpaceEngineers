using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public class MyDefaultPlacementProvider : IMyPlacementProvider
    {
        private int m_lastUpdate = 0;
        private MyPhysics.HitInfo? m_hitInfo;
        private MyCubeGrid m_closestGrid;
        private MySlimBlock m_closestBlock;
        private MyVoxelBase m_closestVoxelMap;
        private readonly List<MyPhysics.HitInfo> m_tmpHitList = new List<MyPhysics.HitInfo>();

        public MyDefaultPlacementProvider(float intersectionDistance)
        {
            IntersectionDistance = intersectionDistance;
        }

        public Vector3D RayStart
        {
            get
            {
                var cameraController = MySession.Static.GetCameraControllerEnum();
                if (cameraController == MyCameraControllerEnum.Entity || cameraController == MyCameraControllerEnum.ThirdPersonSpectator)
                {
                    if (MySession.Static.ControlledEntity != null)
                        return MySession.Static.ControlledEntity.GetHeadMatrix(false).Translation;
                    else if (MySector.MainCamera != null)
                        return MySector.MainCamera.Position;
                }
                else
                {
                    if (MySector.MainCamera != null)
                        return MySector.MainCamera.Position;
                }
                return Vector3.Zero;
            }
        }

        public Vector3D RayDirection
        {
            get
            {
                return MySector.MainCamera.ForwardVector;
            }
        }

        public MyPhysics.HitInfo? HitInfo
        {
            get
            {
                if (MySession.Static.GameplayFrameCounter != m_lastUpdate)
                    UpdatePlacement();
                return m_hitInfo;
            }
        }

        public MyCubeGrid ClosestGrid
        {
            get
            {
                if (MySession.Static.GameplayFrameCounter != m_lastUpdate)
                    UpdatePlacement();
                return m_closestGrid;
            }
        }

        public MyVoxelBase ClosestVoxelMap
        {
            get
            {
                if (MySession.Static.GameplayFrameCounter != m_lastUpdate)
                    UpdatePlacement();
                return m_closestVoxelMap;
            }
        }

        public bool CanChangePlacementObjectSize { get { return false; } }
        public float IntersectionDistance { get; set; }

        public void RayCastGridCells(MyCubeGrid grid, List<Vector3I> outHitPositions, Vector3I gridSizeInflate, float maxDist)
        {
            grid.RayCastCells(RayStart, RayStart + RayDirection * maxDist, outHitPositions, gridSizeInflate);
        }

        public void UpdatePlacement()
        {
            m_lastUpdate = MySession.Static.GameplayFrameCounter;
            m_hitInfo = null;
            m_closestGrid = null;
            m_closestVoxelMap = null;
            LineD line = new LineD(RayStart, RayStart + RayDirection * IntersectionDistance);

            MyPhysics.CastRay(line.From, line.To, m_tmpHitList,
                MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            // Remove character hits.
            if (MySession.Static.ControlledEntity != null)
            {
                m_tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hitInfo)
                {
                    return (hitInfo.HkHitInfo.GetHitEntity() == MySession.Static.ControlledEntity.Entity);
                });
            }

            if (m_tmpHitList.Count == 0)
                return;

            var hit = m_tmpHitList[0];
            m_closestGrid = hit.HkHitInfo.GetHitEntity() as MyCubeGrid;
            if (m_closestGrid != null)
            {
                //always assign otherwise the block will be completely inside/behind the grid
                m_hitInfo = hit; 
                if (!ClosestGrid.Editable)
                    m_closestGrid = null;
                return;

            }

            //if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL) // TODO: check this MyFake to remove or what?
            //{
            m_closestVoxelMap = hit.HkHitInfo.GetHitEntity() as MyVoxelBase;
            if (m_closestVoxelMap != null)
                m_hitInfo = hit;
            //}
        }
    }

}
