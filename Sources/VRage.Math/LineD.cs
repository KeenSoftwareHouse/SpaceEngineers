using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    //  Line defined by two vertexes 'from' and 'to', so it has start and end.
    public struct LineD
    {
        public Vector3D From;
        public Vector3D To;
        public Vector3D Direction;
        public double Length;

        //  IMPORTANT: This struct must be initialized using this constructor, or by filling all four fields. It's because
        //  some code may need length or distance, and if they aren't calculated, we can have problems.
        public LineD(Vector3D from, Vector3D to)
        {
            From = from;
            To = to;
            Direction = to - from;
            Length = Direction.Normalize();
        }

        public static double GetShortestDistanceSquared(LineD line1, LineD line2)
        {
            Vector3D res1, res2;
            Vector3D dP = GetShortestVector(ref line1, ref line2, out res1, out res2);

            //return Math.Sqrt(dot(dP, dP));
            return Vector3D.Dot(dP, dP);
        }

        public static Vector3D GetShortestVector(ref LineD line1, ref LineD line2, out Vector3D res1, out Vector3D res2)
        {
            double EPS = 0.000001f;

            Vector3D delta21 = new Vector3D();
            delta21.X = line1.To.X - line1.From.X;
            delta21.Y = line1.To.Y - line1.From.Y;
            delta21.Z = line1.To.Z - line1.From.Z;

            Vector3D delta41 = new Vector3D();
            delta41.X = line2.To.X - line2.From.X;
            delta41.Y = line2.To.Y - line2.From.Y;
            delta41.Z = line2.To.Z - line2.From.Z;

            Vector3D delta13 = new Vector3D();
            delta13.X = line1.From.X - line2.From.X;
            delta13.Y = line1.From.Y - line2.From.Y;
            delta13.Z = line1.From.Z - line2.From.Z;

            double a = Vector3D.Dot(delta21, delta21);
            double b = Vector3D.Dot(delta21, delta41);
            double c = Vector3D.Dot(delta41, delta41);
            double d = Vector3D.Dot(delta21, delta13);
            double e = Vector3D.Dot(delta41, delta13);
            double D = a * c - b * b;

            double sc, sN, sD = D;
            double tc, tN, tD = D;

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
            
            Vector3D dP = new Vector3D();
            dP.X = delta13.X -(tc * delta41.X) + res1.X;
            dP.Y = delta13.Y -(tc * delta41.Y) + res1.Y;
            dP.Z = delta13.Z -(tc * delta41.Z) + res1.Z;

            res2 = res1 - dP;

            return dP;
        }

        public static explicit operator Line(LineD b)
        {
            return new Line((Vector3)b.From, (Vector3)b.To);
        }

        public static explicit operator LineD(Line b)
        {
            return new LineD((Vector3D)b.From, (Vector3D)b.To);
        }

        public BoundingBoxD GetBoundingBox()
        {
            return new BoundingBoxD(Vector3D.Min(From, To), Vector3D.Max(From, To));
        }
    }
}
