using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.World;
using Sandbox.Game.Entities;

using Sandbox.Game.Entities.Planet;
using VRage.Voxels;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentPlanet : MyDebugRenderComponent
    {
        MyPlanet m_planet = null;

        public MyDebugRenderComponentPlanet(MyPlanet voxelMap)
            : base(voxelMap)
        {
            m_planet = voxelMap;
        }

        public override void DebugDraw()
        {
            var minCorner = m_planet.PositionLeftBottomCorner;

            m_planet.Components.Get<MyPlanetEnvironmentComponent>().DebugDraw();

            m_planet.DebugDrawPhysics();

            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_MAP_AABB)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(m_planet.PositionComp.WorldAABB, Color.White, 1f, 1f, true);
                VRageRender.MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(1f, 0f, 0f), Color.Red, Color.Red, true);
                VRageRender.MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(0f, 1f, 0f), Color.Green, Color.Green, true);
                VRageRender.MyRenderProxy.DebugDrawLine3D(minCorner, minCorner + new Vector3(0f, 0f, 1f), Color.Blue, Color.Blue, true);

                VRageRender.MyRenderProxy.DebugDrawAxis(m_planet.PositionComp.WorldMatrix, 2f, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(m_planet.PositionComp.GetPosition(), 1, Color.OrangeRed, 1, false);
            }

            m_planet.Storage.DebugDraw(m_planet, MyDebugDrawSettings.DEBUG_DRAW_VOXELS_MODE);

            m_planet.DebugDrawPhysics();
            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_GEOMETRY_CELL)
            {
                LineD worldLine;
                if (false)
                {
                    var entityMatrix = MySession.Static.ControlledEntity.Entity.WorldMatrix;
                    worldLine = new LineD(entityMatrix.Translation, entityMatrix.Translation + 25f * entityMatrix.Forward);
                }
                else
                {
                    var camera = MySector.MainCamera;
                    worldLine = new LineD(camera.Position, camera.Position + 25f * camera.ForwardVector);
                }
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? result;
                bool depthRead = true;
                if (m_planet.GetIntersectionWithLine(ref worldLine, out result))
                {
                    var t = result.Value.Triangle.InputTriangle;
                    MyRenderProxy.DebugDrawTriangle(
                        t.Vertex0 + minCorner,
                        t.Vertex1 + minCorner,
                        t.Vertex2 + minCorner,
                        Color.Red, true, false);
                    Vector3I cellCoord, voxelCoord;
                    var worldPosition = result.Value.IntersectionPointInWorldSpace;
                    BoundingBoxD voxelAabb;
                    MyVoxelCoordSystems.WorldPositionToVoxelCoord(minCorner, ref worldPosition, out voxelCoord);
                    MyVoxelCoordSystems.VoxelCoordToWorldAABB(minCorner, ref voxelCoord, out voxelAabb);
                    MyRenderProxy.DebugDrawAABB(voxelAabb, Vector3.UnitY, 1f, 1f, true);
                    MyVoxelCoordSystems.WorldPositionToGeometryCellCoord(minCorner, ref worldPosition, out cellCoord);
                    MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(minCorner, ref cellCoord, out voxelAabb);
                    MyRenderProxy.DebugDrawAABB(voxelAabb, Vector3.UnitZ, 1f, 1f, true);

                    bool isEmpty;
                    MyIsoMesh cell;
                    if (m_planet.Storage.Geometry.TryGetMesh(new MyCellCoord(0, cellCoord), out isEmpty, out cell) && !isEmpty)
                    {
                        MyVoxelVertex tmp;
                        var triangleBatch = MyRenderProxy.PrepareDebugDrawTriangles();
                        for (int i = 0; i < cell.VerticesCount; ++i)
                        {
                            cell.GetUnpackedVertex(i, out tmp);
                            triangleBatch.AddVertex(tmp.Position);
                            tmp.Position += minCorner;
                            MyRenderProxy.DebugDrawLine3D(tmp.Position, tmp.Position + tmp.Normal * MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF, Color.Gray, Color.White, depthRead);
                        }
                        for (int i = 0; i < cell.TrianglesCount; ++i)
                        {
                            triangleBatch.AddIndex(cell.Triangles[i].VertexIndex2);
                            triangleBatch.AddIndex(cell.Triangles[i].VertexIndex1);
                            triangleBatch.AddIndex(cell.Triangles[i].VertexIndex0);
                        }
                        MyRenderProxy.DebugDrawTriangles(triangleBatch, Matrix.CreateTranslation(minCorner), Color.CornflowerBlue, depthRead, false);
                    }
                }
            }
        }
    }
}
