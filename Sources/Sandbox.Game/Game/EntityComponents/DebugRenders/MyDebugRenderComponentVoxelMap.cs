using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentVoxelMap : MyDebugRenderComponent
    {
        MyVoxelBase m_voxelMap = null;

        public MyDebugRenderComponentVoxelMap(MyVoxelBase voxelMap)
            : base(voxelMap)
        {
            m_voxelMap = voxelMap;
        }

        public override void DebugDraw()
        {
            var minCorner = m_voxelMap.PositionLeftBottomCorner;
            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_MAP_AABB)
            {
                MyRenderProxy.DebugDrawAABB(m_voxelMap.PositionComp.WorldAABB, Color.White, alpha: 0.2f);
                MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(1f, 0f, 0f), Color.Red, Color.Red, true);
                MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(0f, 1f, 0f), Color.Green, Color.Green, true);
                MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(0f, 0f, 1f), Color.Blue, Color.Blue, true);

                MyRenderProxy.DebugDrawAxis(m_voxelMap.PositionComp.WorldMatrix, 2f, false);

                MyRenderProxy.DebugDrawSphere(m_voxelMap.PositionComp.GetPosition(), 1, Color.OrangeRed, 1, false);
            }

            m_voxelMap.Storage.DebugDraw(m_voxelMap, MyDebugDrawSettings.DEBUG_DRAW_VOXELS_MODE);
            if (m_voxelMap.Physics != null)
            {
                m_voxelMap.Physics.DebugDraw();
            }
            //if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_GEOMETRY_CELL)
            //{
            //    LineD worldLine;
            //    if (false)
            //    {
            //        var entityMatrix = MySession.Static.ControlledEntity.Entity.WorldMatrix;
            //        worldLine = new LineD(entityMatrix.Translation, entityMatrix.Translation + 25f * entityMatrix.Forward);
            //    }
            //    else
            //    {
            //        var camera = MySector.MainCamera;
            //        worldLine = new LineD(camera.Position, camera.Position + 25f * camera.ForwardVector);
            //    }
            //    VRage.Game.Models.MyIntersectionResultLineTriangleEx? result;
            //    bool depthRead = true;
            //    if (m_voxelMap.GetIntersectionWithLine(ref worldLine, out result))
            //    {
            //        var t = result.Value.Triangle.InputTriangle;
            //        MyRenderProxy.DebugDrawTriangle(
            //            t.Vertex0 + minCorner,
            //            t.Vertex1 + minCorner,
            //            t.Vertex2 + minCorner,
            //            Color.Red, true, false);
            //        Vector3I cellCoord, voxelCoord;
            //        var worldPosition = result.Value.IntersectionPointInWorldSpace;
            //        BoundingBoxD voxelAabb;
            //        MyVoxelCoordSystems.WorldPositionToVoxelCoord(minCorner, ref worldPosition, out voxelCoord);
            //        MyVoxelCoordSystems.VoxelCoordToWorldAABB(minCorner, ref voxelCoord, out voxelAabb);
            //        MyRenderProxy.DebugDrawAABB(voxelAabb, Vector3.UnitY, 1f, 1f, true);
            //        MyVoxelCoordSystems.WorldPositionToGeometryCellCoord(minCorner, ref worldPosition, out cellCoord);
            //        MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(minCorner, ref cellCoord, out voxelAabb);
            //        MyRenderProxy.DebugDrawAABB(voxelAabb, Vector3.UnitZ, 1f, 1f, true);

            //        bool isEmpty;
            //        MyIsoMesh cell;
            //        if (m_voxelMap.Storage.Geometry.TryGetMesh(new MyCellCoord(0, cellCoord), out isEmpty, out cell) && !isEmpty)
            //        {
            //            MyVoxelVertex tmp;
            //            var triangleBatch = MyRenderProxy.PrepareDebugDrawTriangles();
            //            for (int i = 0; i < cell.VerticesCount; ++i)
            //            {
            //                cell.GetUnpackedVertex(i, out tmp);
            //                triangleBatch.AddVertex(tmp.Position);
            //                tmp.Position += minCorner;
            //                MyRenderProxy.DebugDrawLine3D(tmp.Position, tmp.Position + tmp.Normal * MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF, Color.Gray, Color.White, depthRead);
            //            }
            //            for (int i = 0; i < cell.TrianglesCount; ++i)
            //            {
            //                triangleBatch.AddIndex(cell.Triangles[i].VertexIndex2);
            //                triangleBatch.AddIndex(cell.Triangles[i].VertexIndex1);
            //                triangleBatch.AddIndex(cell.Triangles[i].VertexIndex0);
            //            }
            //            MyRenderProxy.DebugDrawTriangles(triangleBatch, Matrix.CreateTranslation(minCorner), Color.CornflowerBlue, depthRead, false);
            //        }
            //    }
            //}
        }
    }
}

