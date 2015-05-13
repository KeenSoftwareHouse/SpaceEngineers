using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    //  Line defined by two vertexes 'from' and 'to', so it has start and end.
    public struct Line
    {
        public Vector3 From;
        public Vector3 To;
        public Vector3 Direction;
        public float Length;

        //  IMPORTANT: This bounding box is calculated in constructor, but only if needed. So check if you line was made with "calculateBoundingBox = true".  
        //  Do it with true if you want to use this line on MyGuiScreenGameBase.Static intersection testing.
        public BoundingBox BoundingBox;


        //  IMPORTANT: This struct must be initialized using this constructor, or by filling all four fields. It's because
        //  some code may need length or distance, and if they aren't calculated, we can have problems.
        public Line(Vector3 from, Vector3 to, bool calculateBoundingBox = true)
        {
            From = from;
            To = to;
            Direction = to - from;
            Length = Direction.Normalize();

            //  Calculate line's bounding box, but only if we know we will need it
            BoundingBox = BoundingBox.CreateInvalid();
            if (calculateBoundingBox == true)
            {
                //BoundingBoxHelper.AddLine(ref this, ref BoundingBox);
                BoundingBox = BoundingBox.Include(ref from);
                BoundingBox = BoundingBox.Include(ref to);
            }
        }

        public static float GetShortestDistanceSquared(Line line1, Line line2)
        {
            Vector3 res1, res2;
            Vector3 dP = GetShortestVector(ref line1, ref line2, out res1, out res2);

            //return Math.Sqrt(dot(dP, dP));
            return Vector3.Dot(dP, dP);
        }

        public static Vector3 GetShortestVector(ref Line line1, ref Line line2, out Vector3 res1, out Vector3 res2)
        {
            float EPS = 0.000001f;

            Vector3 delta21 = new Vector3();
            delta21.X = line1.To.X - line1.From.X;
            delta21.Y = line1.To.Y - line1.From.Y;
            delta21.Z = line1.To.Z - line1.From.Z;

            Vector3 delta41 = new Vector3();
            delta41.X = line2.To.X - line2.From.X;
            delta41.Y = line2.To.Y - line2.From.Y;
            delta41.Z = line2.To.Z - line2.From.Z;

            Vector3 delta13 = new Vector3();
            delta13.X = line1.From.X - line2.From.X;
            delta13.Y = line1.From.Y - line2.From.Y;
            delta13.Z = line1.From.Z - line2.From.Z;

            float a = Vector3.Dot(delta21, delta21);
            float b = Vector3.Dot(delta21, delta41);
            float c = Vector3.Dot(delta41, delta41);
            float d = Vector3.Dot(delta21, delta13);
            float e = Vector3.Dot(delta41, delta13);
            float D = a * c - b * b;

            float sc, sN, sD = D;
            float tc, tN, tD = D;

            if (D < EPS)
            {
                sN = 0.0f;
                sD = 1.0f;
                tN = e;
                tD = c;
            }
            else
            {
                sN = (b * e - c * d);
                tN = (a * e - b * d);
                if (sN < 0.0)
                {
                    sN = 0.0f;
                    tN = e;
                    tD = c;
                }
                else if (sN > sD)
                {
                    sN = sD;
                    tN = e + b;
                    tD = c;
                }
            }

            if (tN < 0.0)
            {
                tN = 0.0f;

                if (-d < 0.0f)
                    sN = 0.0f;
                else if (-d > a)
                    sN = sD;
                else
                {
                    sN = -d;
                    sD = a;
                }
            }
            else if (tN > tD)
            {
                tN = tD;
                if ((-d + b) < 0.0)
                    sN = 0;
                else if ((-d + b) > a)
                    sN = sD;
                else
                {
                    sN = (-d + b);
                    sD = a;
                }
            }

            if (Math.Abs(sN) < EPS) sc = 0.0f;
            else sc = sN / sD;
            if (Math.Abs(tN) < EPS) tc = 0.0f;
            else tc = tN / tD;

            res1.X = (sc * delta21.X);
            res1.Y = (sc * delta21.Y);
            res1.Z = (sc * delta21.Z);
            
            Vector3 dP = new Vector3();
            dP.X = delta13.X -(tc * delta41.X) + res1.X;
            dP.Y = delta13.Y -(tc * delta41.Y) + res1.Y;
            dP.Z = delta13.Z -(tc * delta41.Z) + res1.Z;

            res2 = res1 - dP;

            return dP;
        }


    }



    public struct MyLineSegmentOverlapResult<T>
    {
        public class MyLineSegmentOverlapResultComparer : IComparer<MyLineSegmentOverlapResult<T>>
        {
            public int Compare(MyLineSegmentOverlapResult<T> x, MyLineSegmentOverlapResult<T> y)
            {
                return x.Distance.CompareTo(y.Distance);
            }
        }

        public static MyLineSegmentOverlapResultComparer DistanceComparer = new MyLineSegmentOverlapResultComparer();

        public double Distance;
        public T Element;
    }
}
