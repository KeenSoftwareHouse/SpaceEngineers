using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentArmorHand : MySessionComponentBase
    {
        private MyCubeGrid m_lastCubeGrid;
        private Vector3I? m_lastBone;
        private Vector3I? m_lastCube;

        public override void Draw()
        {
            base.Draw();

            if (!MyFakes.ENABLE_ARMOR_HAND)
            {
                return;
            }

            Vector3 forward = MySector.MainCamera.ForwardVector;
            Vector3D origin = MySector.MainCamera.Position;
            Vector3D end = origin + forward * 100f;

            m_lastCubeGrid = null;
            m_lastBone = null;

            var hitInfo = MyPhysics.CastRay(origin, end, MyPhysics.CollisionLayers.ExplosionRaycastLayer);
            var hitEntity = hitInfo.HasValue ? ((MyPhysicsBody)hitInfo.Value.HkHitInfo.Body.UserObject).Entity : null;

            var grid = (hitEntity as MyCubeGrid);
            if (grid != null)
            {
                m_lastCubeGrid = grid;
                double shortestDistance = double.MaxValue;

                LineD line = new LineD(origin, end);
                Vector3I hitCube = new Vector3I();
                double distanceSquared = double.MaxValue;

                if (m_lastCubeGrid.GetLineIntersectionExactGrid(ref line, ref hitCube, ref distanceSquared))
                {
                    m_lastCube = hitCube;
                }
                else
                {
                    m_lastCube = null;
                }

                foreach (var bone in grid.Skeleton.Bones)
                {
                    var bonePos = (Vector3D)(bone.Key / (float)MyGridSkeleton.BoneDensity) * grid.GridSize + bone.Value;
                    bonePos -= new Vector3D(grid.GridSize / MyGridSkeleton.BoneDensity);
                    Vector3D pos = Vector3D.Transform(bonePos, grid.PositionComp.WorldMatrix);

                    Color color = Color.Red;

                    double distance = MyUtils.GetPointLineDistance(ref origin, ref end, ref pos);
                    if (distance < 0.1f)
                    {
                        double distanceToCamera = (origin - pos).LengthSquared();
                        if (distanceToCamera < shortestDistance)
                        {
                            shortestDistance = distanceToCamera;

                            color = Color.Blue;
                            m_lastBone = bone.Key;
                        }
                    }

                    MyRenderProxy.DebugDrawSphere(pos, 0.05f, color.ToVector3(), 0.5f, false, true);
                }
            }
        }

        private Vector3D m_localBonePosition;
        private MyCubeGrid m_movingCubeGrid;
        private Vector3I? m_movingBone;
        
        public override void HandleInput()
        {
            base.HandleInput();

            if (!MyFakes.ENABLE_ARMOR_HAND)
            {
                return;
            }

            if (MyInput.Static.IsNewLeftMousePressed())
            {
                if (m_lastCubeGrid != null && m_lastBone != null)
                {
                    var bonePos = (Vector3D)(m_lastBone / (float)MyGridSkeleton.BoneDensity) * m_lastCubeGrid.GridSize + m_lastCubeGrid.Skeleton.Bones[m_lastBone.Value];
                    bonePos -= new Vector3D(m_lastCubeGrid.GridSize / MyGridSkeleton.BoneDensity);
                    Vector3D pos = Vector3D.Transform(bonePos, m_lastCubeGrid.PositionComp.WorldMatrix);

                    m_localBonePosition = Vector3.Transform(pos, MySession.Static.LocalCharacter.PositionComp.WorldMatrixNormalizedInv);

                    m_movingCubeGrid = m_lastCubeGrid;
                    m_movingBone = m_lastBone;

                    m_lastCubeGrid.Skeleton.GetDefinitionOffsetWithNeighbours(m_lastCube.Value, m_movingBone.Value, m_lastCubeGrid);
                }
            }

            if (MyInput.Static.IsLeftMousePressed())
            {
                if (m_movingCubeGrid != null && m_movingBone != null)
                {
                    if (MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        var offset = GetBoneOnSphere(new Vector3I(2, 0, 0), m_movingBone.Value, m_movingCubeGrid);
                        m_movingCubeGrid.Skeleton.Bones[m_movingBone.Value] = offset;
                    }
                    else
                    {
                        Vector3D m_worldBonePosition = Vector3D.Transform(m_localBonePosition, MySession.Static.LocalCharacter.PositionComp.WorldMatrix);

                        var bonePos = Vector3D.Transform(m_worldBonePosition, m_movingCubeGrid.PositionComp.WorldMatrixInvScaled);
                        bonePos += new Vector3D(m_movingCubeGrid.GridSize / MyGridSkeleton.BoneDensity);
                        m_movingCubeGrid.Skeleton.Bones[m_movingBone.Value] = bonePos - (Vector3D)(m_movingBone / (float)MyGridSkeleton.BoneDensity) * m_movingCubeGrid.GridSize;

                        Vector3I gridPos = m_movingCubeGrid.WorldToGridInteger(m_worldBonePosition);
                        for (int i = -1; i <= 1; i++)
                            for (int j = -1; j <= 1; j++)
                                for (int k = -1; k <= 1; k++)
                                    m_movingCubeGrid.SetCubeDirty(new Vector3I(i, j, k) + gridPos);    
                    }
                }
            }

            if (MyInput.Static.IsNewLeftMouseReleased())
            {
                m_movingCubeGrid = null;
                m_movingBone = null;
            }
        }

        Vector3D BoneToWorld(Vector3I bone, Vector3 offset, MyCubeGrid grid)
        {
            var bonePos = (Vector3D)(bone / (float)MyGridSkeleton.BoneDensity) * grid.GridSize + offset;
            bonePos -= new Vector3D(grid.GridSize / MyGridSkeleton.BoneDensity);
            Vector3D pos = Vector3D.Transform(bonePos, grid.PositionComp.WorldMatrix);

            return pos;
        }

        private Vector3 GetBoneOnSphere(Vector3I center, Vector3I bonePos, MyCubeGrid grid)
        {
            Vector3D worldCenter = BoneToWorld(center, Vector3.Zero, grid);
            Vector3D worldBone = BoneToWorld(bonePos, Vector3.Zero, grid);

            BoundingSphereD sphere = new BoundingSphereD(worldCenter, grid.GridSize);
            Vector3D direction = worldCenter - worldBone;
            direction.Normalize();
            RayD ray = new RayD(worldBone, direction);

            double tmin, tmax;
            if (sphere.IntersectRaySphere(ray, out tmin, out tmax))
            {
                Vector3D onSphere = worldBone + direction * tmin;

                var worldOnSphere = Vector3D.Transform(onSphere, grid.PositionComp.WorldMatrixInvScaled);
                worldOnSphere += new Vector3D(grid.GridSize / MyGridSkeleton.BoneDensity);
                return (worldOnSphere - (Vector3D)(bonePos / (float)MyGridSkeleton.BoneDensity) * grid.GridSize);
            }
            else
            {
                return Vector3.Zero;
            }
        }
    }
}
