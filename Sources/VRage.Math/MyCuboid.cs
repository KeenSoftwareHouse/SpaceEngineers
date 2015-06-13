using System;
using System.Collections.Generic;
using VRage;
using VRageMath;


namespace VRageMath
{
    //  6 - 7  
    // /   /|
    //4|- 5 |
    //|2 -|3
    //|/  |/
    //0 - 1                
    public class MyCuboidSide
    {
        public Plane Plane = new Plane();
        public Line[] Lines = new Line[4];

        public MyCuboidSide()
        {
            Lines[0] = new Line();
            Lines[1] = new Line();
            Lines[2] = new Line();
            Lines[3] = new Line();
        }

        public void CreatePlaneFromLines()
        {
            Plane = new Plane(Lines[0].From, Vector3.Cross(Lines[1].Direction, Lines[0].Direction));
        }
    }

    public class MyCuboid
    {
        public MyCuboidSide[] Sides = new MyCuboidSide[6];

        public MyCuboid()
        {
            Sides[0] = new MyCuboidSide();
            Sides[1] = new MyCuboidSide();
            Sides[2] = new MyCuboidSide();
            Sides[3] = new MyCuboidSide();
            Sides[4] = new MyCuboidSide();
            Sides[5] = new MyCuboidSide();
        }

        public IEnumerable<Line> UniqueLines
        {
            get
            {
                yield return Sides[0].Lines[0];
                yield return Sides[0].Lines[1];
                yield return Sides[0].Lines[2];
                yield return Sides[0].Lines[3];

                yield return Sides[1].Lines[0];
                yield return Sides[1].Lines[1];
                yield return Sides[1].Lines[2];
                yield return Sides[1].Lines[3];

                yield return Sides[2].Lines[0];
                yield return Sides[2].Lines[2];
                yield return Sides[4].Lines[1];
                yield return Sides[5].Lines[2];
            }
        }

        public IEnumerable<Vector3> Vertices
        {
            get
            {
                yield return Sides[2].Lines[1].From;
                yield return Sides[2].Lines[1].To;
                yield return Sides[0].Lines[1].From;
                yield return Sides[0].Lines[1].To;

                yield return Sides[1].Lines[2].From;
                yield return Sides[1].Lines[2].To;
                yield return Sides[3].Lines[2].From;
                yield return Sides[3].Lines[2].To;
            }
        }

        public void CreateFromVertices(Vector3[] vertices)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            foreach (Vector3 v in vertices)
            {
                min = Vector3.Min(v, min);
                max = Vector3.Min(v, max);
            }

            Line line02 = new Line(vertices[0], vertices[2], false);
            Line line23 = new Line(vertices[2], vertices[3], false);
            Line line31 = new Line(vertices[3], vertices[1], false);
            Line line10 = new Line(vertices[1], vertices[0], false);

            Line line76 = new Line(vertices[7], vertices[6], false);
            Line line64 = new Line(vertices[6], vertices[4], false);
            Line line45 = new Line(vertices[4], vertices[5], false);
            Line line57 = new Line(vertices[5], vertices[7], false);

            Line line40 = new Line(vertices[4], vertices[0], false);
            Line line01 = new Line(vertices[0], vertices[1], false);
            Line line15 = new Line(vertices[1], vertices[5], false);
            Line line54 = new Line(vertices[5], vertices[4], false);

            Line line32 = new Line(vertices[3], vertices[2], false);
            Line line26 = new Line(vertices[2], vertices[6], false);
            Line line67 = new Line(vertices[6], vertices[7], false);
            Line line73 = new Line(vertices[7], vertices[3], false);

            Line line13 = new Line(vertices[1], vertices[3], false);
            Line line37 = new Line(vertices[3], vertices[7], false);
            Line line75 = new Line(vertices[7], vertices[5], false);
            Line line51 = new Line(vertices[5], vertices[1], false);

            Line line04 = new Line(vertices[0], vertices[4], false);
            Line line46 = new Line(vertices[4], vertices[6], false);
            Line line62 = new Line(vertices[6], vertices[2], false);
            Line line20 = new Line(vertices[2], vertices[0], false);

            Sides[0].Lines[0] = line02;
            Sides[0].Lines[1] = line23;
            Sides[0].Lines[2] = line31;
            Sides[0].Lines[3] = line10;
            Sides[0].CreatePlaneFromLines();

            Sides[1].Lines[0] = line76;
            Sides[1].Lines[1] = line64;
            Sides[1].Lines[2] = line45;
            Sides[1].Lines[3] = line57;
            Sides[1].CreatePlaneFromLines();

            Sides[2].Lines[0] = line40;
            Sides[2].Lines[1] = line01;
            Sides[2].Lines[2] = line15;
            Sides[2].Lines[3] = line54;
            Sides[2].CreatePlaneFromLines();

            Sides[3].Lines[0] = line32;
            Sides[3].Lines[1] = line26;
            Sides[3].Lines[2] = line67;
            Sides[3].Lines[3] = line73;
            Sides[3].CreatePlaneFromLines();

            Sides[4].Lines[0] = line13;
            Sides[4].Lines[1] = line37;
            Sides[4].Lines[2] = line75;
            Sides[4].Lines[3] = line51;
            Sides[4].CreatePlaneFromLines();

            Sides[5].Lines[0] = line04;
            Sides[5].Lines[1] = line46;
            Sides[5].Lines[2] = line62;
            Sides[5].Lines[3] = line20;
            Sides[5].CreatePlaneFromLines();
        }

        public void CreateFromSizes(float width1, float depth1, float width2, float depth2, float length)
        {
            float halfLength = length * 0.5f;
            float halfWidth1 = width1 * 0.5f;
            float halfWidth2 = width2 * 0.5f;
            float halfDepth1 = depth1 * 0.5f;
            float halfDepth2 = depth2 * 0.5f;

            Vector3[] vertices = new Vector3[8];
            vertices[0] = new Vector3(-halfWidth2, -halfLength, -halfDepth2);
            vertices[1] = new Vector3(halfWidth2, -halfLength, -halfDepth2);
            vertices[2] = new Vector3(-halfWidth2, -halfLength, halfDepth2);
            vertices[3] = new Vector3(halfWidth2, -halfLength, halfDepth2);
            vertices[4] = new Vector3(-halfWidth1, halfLength, -halfDepth1);
            vertices[5] = new Vector3(halfWidth1, halfLength, -halfDepth1);
            vertices[6] = new Vector3(-halfWidth1, halfLength, halfDepth1);
            vertices[7] = new Vector3(halfWidth1, halfLength, halfDepth1);

            CreateFromVertices(vertices);
        }

        public BoundingBox GetAABB()
        {
            BoundingBox aabb = BoundingBox.CreateInvalid();

            foreach (Line line in UniqueLines)
            {
                Vector3 from = line.From;
                Vector3 to = line.To;
                aabb = aabb.Include(ref from);
                aabb = aabb.Include(ref to);
            }

            return aabb;
        }

        public BoundingBox GetLocalAABB()
        {
            BoundingBox aabb = GetAABB();

            Vector3 center = aabb.Center;
            aabb.Min -= center;
            aabb.Max -= center;

            return aabb;
        }

        public MyCuboid CreateTransformed(ref Matrix worldMatrix)
        {
            Vector3[] vertices = new Vector3[8];

            int i = 0;
            foreach (Vector3 vertex in Vertices)
            {
                vertices[i] = Vector3.Transform(vertex, worldMatrix);
                i++;
            }

            MyCuboid transformedCuboid = new MyCuboid();
            transformedCuboid.CreateFromVertices(vertices);
            return transformedCuboid;
        }
    }
}
