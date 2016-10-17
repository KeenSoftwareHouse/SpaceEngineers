using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game
{
    /// <summary>
    /// This class is responsible for calculating damage from explosions
    /// It works by recursively raycasting from the point it needs to 
    /// calculate to the explosion center. It does two types of raycast, 
    /// 3D DDA raycasts for traversing grids (ships, stations) and Havok 
    /// raycasts for traversing space between grids. For each block, it builds
    /// a stack of blocks that are between it and the explosion center and then
    /// calculates the damage for all blocks in this stack. 
    /// It early exits if it encounters a block that was already calculated.
    /// </summary>
    public class MyGridExplosion
    {
        //This is used to enable damage that decreases with distance
        public struct MyRaycastDamageInfo
        {
            public MyRaycastDamageInfo(float damageRemaining, float distanceToExplosion)
            {
                DamageRemaining = damageRemaining;
                DistanceToExplosion = distanceToExplosion;
            }

            public float DamageRemaining;
            public float DistanceToExplosion;
        }

        public bool GridWasHit;
        public readonly HashSet<MyCubeGrid> AffectedCubeGrids = new HashSet<MyCubeGrid>();
        public readonly HashSet<MySlimBlock> AffectedCubeBlocks = new HashSet<MySlimBlock>();

        Dictionary<MySlimBlock, float> m_damagedBlocks = new Dictionary<MySlimBlock, float>();
        public Dictionary<MySlimBlock, float> DamagedBlocks
        {
            get
            {
                return m_damagedBlocks;
            }
        }
        Dictionary<MySlimBlock, MyRaycastDamageInfo> m_damageRemaining = new Dictionary<MySlimBlock, MyRaycastDamageInfo>();
        public Dictionary<MySlimBlock, MyRaycastDamageInfo> DamageRemaining
        {
            get
            {
                return m_damageRemaining;
            }
        }
        Stack<MySlimBlock> m_castBlocks = new Stack<MySlimBlock>();

        BoundingSphereD m_explosion;
        float m_explosionDamage;

        public float Damage
        {
            get
            {
                return m_explosionDamage;
            }
        }

        public BoundingSphereD Sphere 
        {
            get { return m_explosion; }
        }

        int stackOverflowGuard;
        const int MAX_PHYSICS_RECURSION_COUNT = 10;

        public MyGridExplosion()
        {
            
        }

        public void Init(BoundingSphereD explosion, float explosionDamage)
        {
            m_explosion = explosion;
            m_explosionDamage = explosionDamage;

            AffectedCubeBlocks.Clear();
            AffectedCubeGrids.Clear();
            m_damageRemaining.Clear();
            m_damagedBlocks.Clear();
            m_castBlocks.Clear();
        }

        /// <summary>
        /// Computes damage for all blocks assigned in constructor
        /// </summary>
        public void ComputeDamagedBlocks()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Volumetric explosion raycasts");

            foreach (var cubeBlock in AffectedCubeBlocks)
            {
                m_castBlocks.Clear();
                MyRaycastDamageInfo startDamage = CastDDA(cubeBlock);

                while (m_castBlocks.Count > 0)
                {
                    var cube = m_castBlocks.Pop();

                    Vector3D cubeWorldPosition;
                    cube.ComputeWorldCenter(out cubeWorldPosition);
                    float distanceToExplosion = (float)(cubeWorldPosition - m_explosion.Center).Length();

                    if (startDamage.DamageRemaining > 0f)
                    {
                        float distanceFactor = 1f - (distanceToExplosion - startDamage.DistanceToExplosion) / ((float)m_explosion.Radius - startDamage.DistanceToExplosion);
                        if (distanceFactor > 0)
                        {
                            m_damagedBlocks.Add(cube, startDamage.DamageRemaining * distanceFactor * cube.DeformationRatio);
                            startDamage.DamageRemaining = Math.Max(0f, startDamage.DamageRemaining * distanceFactor - cube.Integrity / cube.DeformationRatio);
                            startDamage.DistanceToExplosion = distanceToExplosion;
                        }
                    }
                    else
                    {
                        startDamage.DamageRemaining = 0f;
                    }
                    m_damageRemaining.Add(cube, startDamage);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        /// <summary>
        /// Used to calculate damage for entities that are not grids
        /// Can be called even if ComputeDamagedBlocks was not called before, but it doesn't do any caching
        /// </summary>
        /// <param name="worldPosition">World position from where the cast starts (usually the entity position)</param>
        /// <returns></returns>
        public MyRaycastDamageInfo ComputeDamageForEntity(Vector3D worldPosition)
        {
            return new MyRaycastDamageInfo(m_explosionDamage, (float)(worldPosition - m_explosion.Center).Length());
            var hitInfo = MyPhysics.CastRay(m_explosion.Center, worldPosition, MyPhysics.CollisionLayers.ExplosionRaycastLayer);
            if (hitInfo.HasValue)
            {
                return new MyRaycastDamageInfo(m_explosionDamage, (float)(hitInfo.Value.Position - m_explosion.Center).Length());
            }
            return new MyRaycastDamageInfo(m_explosionDamage, (float)(worldPosition - m_explosion.Center).Length());

            m_castBlocks.Clear();
            stackOverflowGuard = 0;
            return CastPhysicsRay(worldPosition);
        }

        List<Vector3I> m_cells = new List<Vector3I>();
        /// <summary>
        /// Performs a grid raycast (is prone to aliasing effects).
        /// It can be recursive (it calls CastPhysicsRay when exiting the grid or when it hits an empty cell).
        /// </summary>
        /// <param name="cubeBlock">Starting block</param>
        /// <returns>Returns starting damage for current stack</returns>
        private MyRaycastDamageInfo CastDDA(MySlimBlock cubeBlock)
        {
            if (m_damageRemaining.ContainsKey(cubeBlock))
            {
                return m_damageRemaining[cubeBlock];
            }

            stackOverflowGuard = 0;

            m_castBlocks.Push(cubeBlock);

            Vector3D startPosition;
            cubeBlock.ComputeWorldCenter(out startPosition);
            m_cells.Clear();
            cubeBlock.CubeGrid.RayCastCells(startPosition, m_explosion.Center, m_cells);
            var dir = (m_explosion.Center - startPosition);
            dir.Normalize();
            Vector3D oldCellWorldPosition = startPosition;
            foreach (var cell in m_cells)
            {
                Vector3D cellWorldPosition = Vector3D.Transform(cell * cubeBlock.CubeGrid.GridSize, cubeBlock.CubeGrid.WorldMatrix);
                if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_DDA_RAYCASTS)
                {
                    DrawRay(oldCellWorldPosition, cellWorldPosition, Color.Red, false);
                    oldCellWorldPosition = cellWorldPosition;
                }
                var cube = cubeBlock.CubeGrid.GetCubeBlock(cell);
                if (cube == null)
                {
                    if (IsExplosionInsideCell(cell, cubeBlock.CubeGrid))
                    {
                        return new MyRaycastDamageInfo(m_explosionDamage, (float)(cellWorldPosition - m_explosion.Center).Length());
                    }
                    else
                    {
                        //move to the edge of cube so we dont hit ourselves
                         //+ dir * (0.5 / dir).AbsMin()
                        //cmon cell is empty
                        return CastPhysicsRay(cellWorldPosition);
                    }
                }
                if (cube != cubeBlock)
                {
                    if (m_damageRemaining.ContainsKey(cube))
                    {
                        return m_damageRemaining[cube];
                    }
                    else
                    {
                        if (!m_castBlocks.Contains(cube))
                        {
                            m_castBlocks.Push(cube);
                        }
                    }
                }
                else
                {
                    if (IsExplosionInsideCell(cell, cubeBlock.CubeGrid))
                    {
                        return new MyRaycastDamageInfo(m_explosionDamage, (float)(cellWorldPosition - m_explosion.Center).Length());
                    }
                }
            }

            return new MyRaycastDamageInfo(m_explosionDamage, (float)(startPosition - m_explosion.Center).Length());
        }

        private bool IsExplosionInsideCell(Vector3I cell, MyCubeGrid cellGrid)
        {
            return cellGrid.WorldToGridInteger(m_explosion.Center) == cell;
        }

        /// <summary>
        /// Performes a physics raycast
        /// It can be recursive (it calls CastDDA when it hits a grid).
        /// </summary>
        /// <param name="fromWorldPos"></param>
        /// <returns>Returns starting damage for current stack</returns>
        private MyRaycastDamageInfo CastPhysicsRay(Vector3D fromWorldPos)
        {
            Vector3D pos = Vector3D.Zero;
            IMyEntity hitEntity = null;

            var hitInfo = MyPhysics.CastRay(fromWorldPos, m_explosion.Center, MyPhysics.CollisionLayers.ExplosionRaycastLayer);
            if (hitInfo.HasValue)
            {
                hitEntity = (hitInfo.Value.HkHitInfo.Body.UserObject != null) ? ((MyPhysicsBody)hitInfo.Value.HkHitInfo.Body.UserObject).Entity : null;
                pos = hitInfo.Value.Position;
            }
            Vector3D direction = (m_explosion.Center - fromWorldPos);
            float lengthToCenter = (float)direction.Normalize();

            var grid = (hitEntity as MyCubeGrid);
            if (grid == null)
            {
                MyCubeBlock hitBlock = hitEntity as MyCubeBlock;
                if (hitBlock != null)
                {
                    grid = hitBlock.CubeGrid;
                }
            }
            if (grid != null)
            {
                var localPos =  Vector3D.Transform(pos, grid.PositionComp.WorldMatrixNormalizedInv) * grid.GridSizeR;
                var localDir = Vector3D.TransformNormal(direction, grid.PositionComp.WorldMatrixNormalizedInv) * 1 / 8f;
                //Try advancing the point to find the intersected block
                //If the block is a cube, this is necessary because the raycast will return a point somewhere outside the cube
                //If the block is not a full cube (slope, special block), the raycast can return inside the cube
                //It advances 4 times, each time one 8th the grid size
                for (int i = 0; i < 5; i++)
                {
                    Vector3I gridPos = Vector3I.Round(localPos);
                    var cubeBlock = grid.GetCubeBlock(gridPos);
                    if (cubeBlock != null)
                    {
                        if (m_castBlocks.Contains(cubeBlock))
                        {
                            //This shouldn't happen
                            //There is a corner case where the explosion position is inside the empty cell, but this should be handleded somewhere higher
                            System.Diagnostics.Debug.Fail("Raycast failed!");
                            DrawRay(fromWorldPos, pos, Color.Red);
                            return new MyRaycastDamageInfo(0f, lengthToCenter);
                        }
                        else
                        {
                            //if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS)
                            //    DrawRay(fromWorldPos, pos, Color.Blue);
                            return CastDDA(cubeBlock);
                        }
                    }
                    localPos += localDir;
                }
                pos = Vector3D.Transform(localPos * grid.GridSize, grid.WorldMatrix);

                //We hit a grid but were unable to find the hit cube. Send another raycast
                //Ideally, we would want to get the cube in all cases, but it also has to be fast
                //We need to check if the explosion center is between the initial start position (fromWorldPos) and the new one (pos)

                Vector3D min = Vector3D.Min(fromWorldPos, pos);
                Vector3D max = Vector3D.Max(fromWorldPos, pos);

                BoundingBoxD boundingBox = new BoundingBoxD(min, max);
                if (boundingBox.Contains(m_explosion.Center) == ContainmentType.Contains)
                {
                    return new MyRaycastDamageInfo(m_explosionDamage, lengthToCenter);
                }

                stackOverflowGuard++;
                if (stackOverflowGuard > MAX_PHYSICS_RECURSION_COUNT)
                {
                    System.Diagnostics.Debug.Fail("Potential stack overflow!");
                    if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS)
                        DrawRay(fromWorldPos, pos, Color.Red);
                    return new MyRaycastDamageInfo(0f, lengthToCenter);
                }
                else
                {
                    if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS)
                        DrawRay(fromWorldPos, pos, Color.White);
                    return CastPhysicsRay(pos);
                }
            }
            else if (hitInfo.HasValue)
            {
                //Something was hit, but it wasn't a grid. This needs to be handled somehow
                if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS)
                    DrawRay(fromWorldPos, pos, Color.Violet);
                return new MyRaycastDamageInfo(0, lengthToCenter);
            }

            //Nothing was hit, so we can assume the there was nothing blocking the explosion
            //if (MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS)
            //    DrawRay(fromWorldPos, pos, Color.Salmon);
            return new MyRaycastDamageInfo(m_explosionDamage, lengthToCenter);
        }

        [Conditional("DEBUG")]
        private void DrawRay(Vector3D from, Vector3D to, float damage, bool depthRead = true)
        {
            Color color = damage > 0f ? Color.Lerp(Color.Green, Color.Red, damage / m_explosionDamage) : Color.Blue;
            DrawRay(from, to, color, depthRead);
        }
        [Conditional("DEBUG")]
        private void DrawRay(Vector3D from, Vector3D to, Color color, bool depthRead = true)
        {
            if (MyAlexDebugInputComponent.Static != null)
            {
                MyAlexDebugInputComponent.Static.AddDebugLine(new MyAlexDebugInputComponent.LineInfo(from, to, color, false));
            }
        }
    }
}
