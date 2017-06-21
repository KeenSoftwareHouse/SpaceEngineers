using System;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.WorldEnvironment;
using VRage.Input;
using VRageMath;
using VRageRender;

using Direction = VRageMath.Base6Directions.Direction;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {

        // For each face we have 5 indices, one for each other face in the cube,
        // one will always be null for the opposite face.
        // We also need to include the face itself for continuous lookup
        private static uint[] AdjacentFaceTransforms =
        {
            // Format is axis to use, axis to set, weather to invert x, weather to invert y, weather to subtract or add 2
            
            // Forward Face
            0, // Dummy
            0, // Backward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Right
            0x0 | 0x1 << 1 | 0x0 << 2 | 1 << 3 | 0 << 4, // Up
            0x0 | 0x1 << 1 | 0x0 << 2 | 1 << 3 | 1 << 4, // Down

            // Backward
            0, // Forward
            0, // Dummy
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Right
            0x0 | 0x1 << 1 | 0x1 << 2 | 0 << 3 | 0 << 4, // Up
            0x0 | 0x1 << 1 | 0x1 << 2 | 0 << 3 | 1 << 4, // Down
            
            // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Forward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Backward
            0, // Dummy
            0, // Right
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Up
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 1 << 4, // Down
            
            // Right
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Forward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Backward
            0, // Left
            0, // Dummy
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 0 << 4, // Up
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Down
            
            // Up
            0x1 | 0x0 << 1 | 0x0 << 2 | 1 << 3 | 1 << 4, // Forward
            0x1 | 0x0 << 1 | 0x1 << 2 | 0 << 3 | 0 << 4, // Backward
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Left
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 0 << 4, // Right
            0, // Dummy
            0, // Down
            
            // Down
            0x1 | 0x0 << 1 | 0x0 << 2 | 1 << 3 | 0 << 4, // Forward
            0x1 | 0x0 << 1 | 0x1 << 2 | 0 << 3 | 1 << 4, // Backward
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 1 << 4, // Left
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Right
            0, // Up
            0, // Dummy
        };

        private static void ProjectToCube(ref Vector3D localPos, out int direction, out Vector2D texcoords)
        {
            Vector3D abs;
            Vector3D.Abs(ref localPos, out abs);

            if (abs.X > abs.Y)
            {
                if (abs.X > abs.Z)
                {
                    localPos /= abs.X;
                    texcoords.Y = localPos.Y;

                    if (localPos.X > 0.0f)
                    {
                        texcoords.X = -localPos.Z;
                        direction = (int)Direction.Right;
                    }
                    else
                    {
                        texcoords.X = localPos.Z;
                        direction = (int)Direction.Left;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texcoords.Y = localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texcoords.X = localPos.X;
                        direction = (int)Direction.Backward;
                    }
                    else
                    {
                        texcoords.X = -localPos.X;
                        direction = (int)Direction.Forward;
                    }
                }
            }
            else
            {
                if (abs.Y > abs.Z)
                {
                    localPos /= abs.Y;
                    texcoords.Y = localPos.X;
                    if (localPos.Y > 0.0f)
                    {
                        texcoords.X = localPos.Z;
                        direction = (int)Direction.Up;
                    }
                    else
                    {
                        texcoords.X = -localPos.Z;
                        direction = (int)Direction.Down;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texcoords.Y = localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texcoords.X = localPos.X;
                        direction = (int)Direction.Backward;
                    }
                    else
                    {
                        texcoords.X = -localPos.X;
                        direction = (int)Direction.Forward;
                    }
                }
            }
        }

        private class SectorTreeComponent : MyDebugComponent, IMy2DClipmapManager
        {
            private MyPlanetsDebugInputComponent m_comp;

            #region Clipmap Handling

            private readonly HashSet<DebugDrawHandler> m_handlers = new HashSet<DebugDrawHandler>();
            private List<DebugDrawHandler> m_sortedHandlers = new List<DebugDrawHandler>();

            private struct DebugDrawSorter : IComparer<DebugDrawHandler>
            {
                public int Compare(DebugDrawHandler x, DebugDrawHandler y)
                {
                    return x.Lod - y.Lod;
                }
            }

            private class DebugDrawHandler : IMy2DClipmapNodeHandler
            {
                private SectorTreeComponent m_parent;

                public Vector2I Coords;

                public BoundingBoxD Bounds;
                public int Lod;

                public Vector3D[] FrustumBounds;

                public void Init(IMy2DClipmapManager parent, int x, int y, int lod, ref BoundingBox2D bounds)
                {
                    m_parent = (SectorTreeComponent)parent;

                    Bounds = new BoundingBoxD(new Vector3D(bounds.Min, 0), new Vector3D(bounds.Max, 50));
                    Lod = lod;

                    var matrix = m_parent.m_tree[m_parent.m_activeClipmap].WorldMatrix;

                    Bounds = Bounds.TransformFast(matrix);

                    Coords = new Vector2I(x, y);

                    m_parent.m_handlers.Add(this);

                    var center = Bounds.Center;

                    // Sector Frustum
                    Vector3D[] v = new Vector3D[8];

                    v[0] = Vector3D.Transform(new Vector3D(bounds.Min.X, bounds.Min.Y, 0), matrix);
                    v[1] = Vector3D.Transform(new Vector3D(bounds.Max.X, bounds.Min.Y, 0), matrix);
                    v[2] = Vector3D.Transform(new Vector3D(bounds.Min.X, bounds.Max.Y, 0), matrix);
                    v[3] = Vector3D.Transform(new Vector3D(bounds.Max.X, bounds.Max.Y, 0), matrix);

                    for (int i = 0; i < 4; ++i)
                    {
                        //v[i] -= WorldMatrix.Translation;
                        v[i].Normalize();
                        v[i + 4] = v[i] * m_parent.Radius;
                        v[i] *= m_parent.Radius + 50;

                        //v[i] += WorldMatrix.Translation;
                        //v[i + 4] += WorldMatrix.Translation;
                    }

                    FrustumBounds = v;
                }

                public void Close()
                {
                    m_parent.m_handlers.Remove(this);
                    //var lx = new LodIndex(Coords.X, Coords.Y, Lod);
                    //m_parent.m_indices.Remove(lx);
                }

                public void InitJoin(IMy2DClipmapNodeHandler[] children)
                {
                    var fChild = (DebugDrawHandler)children[0];

                    Lod = fChild.Lod + 1;
                    Coords = new Vector2I(fChild.Coords.X >> 1, fChild.Coords.Y >> 1);

                    m_parent.m_handlers.Add(this);
                }

                public unsafe void Split(BoundingBox2D* childBoxes, ref IMy2DClipmapNodeHandler[] children)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        children[i].Init(m_parent, (Coords.X << 1) + (i & 1), (Coords.Y << 1) + ((i >> 1) & 1), Lod - 1, ref childBoxes[i]);
                    }
                }
            }

            private My2DClipmap<DebugDrawHandler>[] m_tree;
            private int m_allocs;
            private int m_activeClipmap;

            #endregion

            private Vector3D Origin = Vector3D.Zero;

            private double Radius = 60000;
            private double Size;

            public SectorTreeComponent(MyPlanetsDebugInputComponent comp)
            {
                Size = Radius * Math.Sqrt(2);
                double sectorSize = 64;

                m_comp = comp;

                m_tree = new My2DClipmap<DebugDrawHandler>[6];

                for (m_activeClipmap = 0; m_activeClipmap < m_tree.Length; m_activeClipmap++)
                {
                    var forward = Base6Directions.Directions[m_activeClipmap];

                    var up = Base6Directions.Directions[(int)Base6Directions.GetPerpendicular((Base6Directions.Direction)m_activeClipmap)];

                    // Forward is -z so it inverts the orientation of the map which we do not want.
                    MatrixD wm = MatrixD.CreateFromDir(-forward, up);

                    wm.Translation = (Vector3D)forward * Size / 2;

                    m_tree[m_activeClipmap] = new My2DClipmap<DebugDrawHandler>();
                    m_tree[m_activeClipmap].Init(this, ref wm, sectorSize, Size);
                }

                AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Toggle clipmap update", () => m_update = !m_update);
            }

            private bool m_update = true;

            private Vector3D m_lastUpdate;

            private int m_activeFace;

            public override void Update10()
            {
                base.Update10();

                if (MySession.Static == null) return;

                if (m_update)
                    m_lastUpdate = MySector.MainCamera.Position;

                var camPos = m_lastUpdate;

                int dir; Vector2D texcoords;

                ProjectToCube(ref camPos, out dir, out texcoords);
                m_activeFace = dir;

                // Magic of evil for calculating the distance:
                var dirvec = (Vector3D)Base6Directions.Directions[dir];
                dirvec.X *= m_lastUpdate.X;
                dirvec.Y *= m_lastUpdate.Y;
                dirvec.Z *= m_lastUpdate.Z;
                double distance = Math.Abs(m_lastUpdate.Length() - Radius);


                m_allocs = 0;
                m_activeClipmap = 0;
                for (; m_activeClipmap < m_tree.Length; m_activeClipmap++)
                {
                    var activeClipmap = m_tree[m_activeClipmap];

                    Vector2D localTexcoords = texcoords;
                    var localDistance = distance;

                    int direction;
                    MyPlanetCubemapHelper.TranslateTexcoordsToFace(ref texcoords, dir, m_activeClipmap, out localTexcoords);

                    Vector3D pos;
                    pos.X = localTexcoords.X * activeClipmap.FaceHalf;
                    pos.Y = localTexcoords.Y * activeClipmap.FaceHalf;

                    if ((m_activeClipmap ^ dir) == 1)
                        pos.Z = distance + Radius * 2;
                    else
                        pos.Z = distance;

                    m_tree[m_activeClipmap].NodeAllocDeallocs = 0;
                    m_tree[m_activeClipmap].Update(pos);
                    m_allocs += m_tree[m_activeClipmap].NodeAllocDeallocs;
                }

                m_sortedHandlers = m_handlers.ToList();
                m_sortedHandlers.Sort(new DebugDrawSorter());
            }

            public override void Draw()
            {
                base.Draw();

                Text("Node Allocs/Deallocs from last update: {0}", m_allocs);

                foreach (var handler in m_sortedHandlers)
                {
                    MyRenderProxy.DebugDraw6FaceConvex(handler.FrustumBounds, new Color(My2DClipmapHelpers.LodColors[handler.Lod], 1), 0.2f, true, true);
                }

                m_activeClipmap = 0;
                for (; m_activeClipmap < m_tree.Length; m_activeClipmap++)
                {
                    var t = m_tree[m_activeClipmap];

                    var wpos = Vector3.Transform(m_tree[m_activeClipmap].LastPosition, m_tree[m_activeClipmap].WorldMatrix);
                    MyRenderProxy.DebugDrawSphere(wpos, 500, Color.Red, 1, true);
                    MyRenderProxy.DebugDrawText3D(wpos, ((Base6Directions.Direction)m_activeClipmap).ToString(), Color.Blue, 1, true);

                    // local space basis
                    var center = t.WorldMatrix.Translation;

                    Vector3D right = Vector3D.Transform(Vector3D.Right * 10000, t.WorldMatrix);
                    Vector3D up = Vector3D.Transform(Vector3D.Up * 10000, t.WorldMatrix);


                    MyRenderProxy.DebugDrawText3D(center, ((Base6Directions.Direction)m_activeClipmap).ToString(), Color.Blue, 1, true);

                    MyRenderProxy.DebugDrawLine3D(center, up, Color.Green, Color.Green, true);
                    MyRenderProxy.DebugDrawLine3D(center, right, Color.Red, Color.Red, true);
                }
            }

            public override string GetName()
            {
                return "Sector Tree";
            }
        }
    }
}
