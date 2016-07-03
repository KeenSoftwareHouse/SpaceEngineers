using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    // Describes the navigational properties of the cube block
    [MyDefinitionType(typeof(MyObjectBuilder_BlockNavigationDefinition))]
    public class MyBlockNavigationDefinition : MyDefinitionBase
    {
        private MyGridNavigationMesh m_mesh;
        public MyGridNavigationMesh Mesh { get { return m_mesh; } }

        public bool NoEntry { get; private set; }

        private static StringBuilder m_tmpStringBuilder = new StringBuilder();
        private static MyObjectBuilder_BlockNavigationDefinition m_tmpDefaultOb = new MyObjectBuilder_BlockNavigationDefinition();

        private struct SizeAndCenter
        {
            private Vector3I Size;
            private Vector3I Center;

            public SizeAndCenter(Vector3I size, Vector3I center)
            {
                Size = size;
                Center = center;
            }

            public bool Equals(SizeAndCenter other)
            {
                return other.Size == Size && other.Center == Center;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != typeof(SizeAndCenter)) return false;
                return Equals((SizeAndCenter)obj);
            }

            public override int GetHashCode()
            {
                return Size.GetHashCode() * 1610612741 + Center.GetHashCode();
            }
        }

        public MyBlockNavigationDefinition()
        {
            m_mesh = null;
            NoEntry = false;
        }

        public static MyObjectBuilder_BlockNavigationDefinition GetDefaultObjectBuilder(MyCubeBlockDefinition blockDefinition)
        {
            MyObjectBuilder_BlockNavigationDefinition ob = m_tmpDefaultOb;

            m_tmpStringBuilder.Clear();
            m_tmpStringBuilder.Append("Default_");
            m_tmpStringBuilder.Append(blockDefinition.Size.X);
            m_tmpStringBuilder.Append("_");
            m_tmpStringBuilder.Append(blockDefinition.Size.Y);
            m_tmpStringBuilder.Append("_");
            m_tmpStringBuilder.Append(blockDefinition.Size.Z);

            ob.Id = new MyDefinitionId(typeof(MyObjectBuilder_BlockNavigationDefinition), m_tmpStringBuilder.ToString());
            ob.Size = blockDefinition.Size;
            ob.Center = blockDefinition.Center;

            return ob;
        }

        public static void CreateDefaultTriangles(MyObjectBuilder_BlockNavigationDefinition ob)
        {
            Vector3I size = ob.Size;
            Vector3I center = ob.Center;

            int triCount = 4*((size.X*size.Y)+(size.X*size.Z)+(size.Y*size.Z));
            ob.Triangles = new MyObjectBuilder_BlockNavigationDefinition.Triangle[triCount];
            int i = 0;

            // Coords of the block's real center (i.e. origin) relative to blockDef.Center
            Vector3 origin = (size * 0.5f) - center - Vector3.Half;
            for (int d = 0; d < 6; ++d)
            {
                Base6Directions.Direction faceDirection = Base6Directions.EnumDirections[d];
                Base6Directions.Direction rightDir, upDir;
                Vector3 faceOrigin = origin;
                switch (faceDirection)
                {
                    case Base6Directions.Direction.Right:
                        rightDir = Base6Directions.Direction.Forward;
                        upDir = Base6Directions.Direction.Up;
                        faceOrigin += new Vector3(0.5f, -0.5f, 0.5f) * size;
                        break;
                    case Base6Directions.Direction.Left:
                        rightDir = Base6Directions.Direction.Backward;
                        upDir = Base6Directions.Direction.Up;
                        faceOrigin += new Vector3(-0.5f, -0.5f, -0.5f) * size;
                        break;
                    case Base6Directions.Direction.Up:
                        rightDir = Base6Directions.Direction.Right;
                        upDir = Base6Directions.Direction.Forward;
                        faceOrigin += new Vector3(-0.5f, 0.5f, 0.5f) * size;
                        break;
                    case Base6Directions.Direction.Down:
                        rightDir = Base6Directions.Direction.Right;
                        upDir = Base6Directions.Direction.Backward;
                        faceOrigin += new Vector3(-0.5f, -0.5f, -0.5f) * size;
                        break;
                    case Base6Directions.Direction.Backward:
                        rightDir = Base6Directions.Direction.Right;
                        upDir = Base6Directions.Direction.Up;
                        faceOrigin += new Vector3(-0.5f, -0.5f, 0.5f) * size;
                        break;
                    case Base6Directions.Direction.Forward:
                    default:
                        rightDir = Base6Directions.Direction.Left;
                        upDir = Base6Directions.Direction.Up;
                        faceOrigin += new Vector3(0.5f, -0.5f, -0.5f) * size;
                        break;
                }
                
                Vector3 rightVec = Base6Directions.GetVector(rightDir);
                Vector3 upVec = Base6Directions.GetVector(upDir);

                int uMax = size.AxisValue(Base6Directions.GetAxis(upDir));
                int rMax = size.AxisValue(Base6Directions.GetAxis(rightDir));
                for (int u = 0; u < uMax; ++u)
                {
                    for (int r = 0; r < rMax; ++r)
                    {
                        var triangle = new MyObjectBuilder_BlockNavigationDefinition.Triangle();
                        triangle.Points = new SerializableVector3[3];

                        triangle.Points[0] = faceOrigin;
                        triangle.Points[1] = faceOrigin + rightVec;
                        triangle.Points[2] = faceOrigin + upVec;
                        ob.Triangles[i++] = triangle;

                        triangle = new MyObjectBuilder_BlockNavigationDefinition.Triangle();
                        triangle.Points = new SerializableVector3[3];

                        triangle.Points[0] = faceOrigin + rightVec;
                        triangle.Points[1] = faceOrigin + rightVec + upVec;
                        triangle.Points[2] = faceOrigin + upVec;
                        ob.Triangles[i++] = triangle;

                        faceOrigin += rightVec;
                    }

                    faceOrigin -= rightVec * rMax;
                    faceOrigin += upVec;
                }
            }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            var objectBuilder = ob as MyObjectBuilder_BlockNavigationDefinition;
            Debug.Assert(ob != null);
            if (ob == null) return;

            if (objectBuilder.NoEntry || objectBuilder.Triangles == null)
            {
                NoEntry = true;
            }
            else
            {
                NoEntry = false;
                var newMesh = new MyGridNavigationMesh(null, null, objectBuilder.Triangles.Length);

                Vector3I maxPos = objectBuilder.Size - Vector3I.One - objectBuilder.Center;
                Vector3I minPos = - (Vector3I)(objectBuilder.Center);

                foreach (var triOb in objectBuilder.Triangles)
                {
                    Vector3 pa = (Vector3)triOb.Points[0];
                    Vector3 pb = (Vector3)triOb.Points[1];
                    Vector3 pc = (Vector3)triOb.Points[2];

                    var tri = newMesh.AddTriangle(ref pa, ref pb, ref pc);

                    var center = (pa + pb + pc) / 3.0f;

                    // We want to move the triangle vertices more towards the triangle center to ensure correct calculation of containing cube
                    Vector3 cvA = (center - pa) * 0.0001f;
                    Vector3 cvB = (center - pb) * 0.0001f;
                    Vector3 cvC = (center - pc) * 0.0001f;
                    Vector3I gridPosA = Vector3I.Round(pa + cvA);
                    Vector3I gridPosB = Vector3I.Round(pb + cvB);
                    Vector3I gridPosC = Vector3I.Round(pc + cvC);
                    Vector3I.Clamp(ref gridPosA, ref minPos, ref maxPos, out gridPosA);
                    Vector3I.Clamp(ref gridPosB, ref minPos, ref maxPos, out gridPosB);
                    Vector3I.Clamp(ref gridPosC, ref minPos, ref maxPos, out gridPosC);
                    Vector3I min, max;
                    Vector3I.Min(ref gridPosA, ref gridPosB, out min);
                    Vector3I.Min(ref min, ref gridPosC, out min);
                    Vector3I.Max(ref gridPosA, ref gridPosB, out max);
                    Vector3I.Max(ref max, ref gridPosC, out max);

                    Vector3I pos = min;
                    for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out pos))
                    {
                        newMesh.RegisterTriangle(tri, ref pos);
                    }
                }

                m_mesh = newMesh;
            }
        }
    }
}
