using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Utils;

using BulletXNA;

namespace Sandbox.Engine.Utils
{
    public class MyGridIntersection
    {
        static bool IsPointInside(Vector3 p, Vector3I min, Vector3I max)
        {
            return (p.X >= min.X && p.X < max.X+1 &&
                    p.Y >= min.Y && p.Y < max.Y+1 &&
                    p.Z >= min.Z && p.Z < max.Z+1);
        }

        static bool IntersectionT(double n, double d, ref double tE, ref double tL)
        {
            if (MyUtils.IsZero(d)) return n <= 0;
            double t = n / d;
            if (d > 0)
            {
                if (t > tL) return false;
                if (t > tE) tE = t;
            }
            else
            {
                if (t < tE) return false;
                if (t < tL) tL = t;
            }
            return true;
        }

        
        // Liang-Barsky line clipping. Return true if the line isn't completely clipped.
        static bool ClipLine(ref Vector3D start, ref Vector3D end, Vector3I min, Vector3I max)
        {
            Vector3D dir = end - start;
            if (MyUtils.IsZero(dir)) return IsPointInside(start, min, max);
            double tE = 0, tL = 1;

            if (IntersectionT(min.X - start.X, dir.X, ref tE, ref tL) && IntersectionT(start.X - max.X - 1, -dir.X, ref tE, ref tL) &&
                IntersectionT(min.Y - start.Y, dir.Y, ref tE, ref tL) && IntersectionT(start.Y - max.Y - 1, -dir.Y, ref tE, ref tL) &&
                IntersectionT(min.Z - start.Z, dir.Z, ref tE, ref tL) && IntersectionT(start.Z - max.Z - 1, -dir.Z, ref tE, ref tL))
            {
                if (tL < 1) end = start + tL * dir;
                if (tE > 0) start += tE * dir;
                return true;
            }
            return false;
        }

        // Return +1 if a component of v is non-negative, or -1 if it's negative.
        static Vector3I SignInt(Vector3 v)
        {
            return new Vector3I(v.X >= 0 ? 1 : -1, v.Y >= 0 ? 1 : -1, v.Z >= 0 ? 1 : -1);
        }

        static Vector3 Sign(Vector3 v)
        {
            return new Vector3(v.X >= 0 ? 1 : -1, v.Y >= 0 ? 1 : -1, v.Z >= 0 ? 1 : -1);
        }

        // Get the grid point corresponding to v (in grid coordinates). Guaranteed to lie in the given bounding box (by clamping).
        static Vector3I GetGridPoint(ref Vector3D v, Vector3I min, Vector3I max)
        {
            var r = new Vector3I();
            if (v.X < min.X) { v.X = r.X = min.X; }
            else if (v.X >= max.X + 1) { v.X = MathUtil.NextAfter(max.X + 1, float.NegativeInfinity); r.X = max.X; }
            else r.X = (int)Math.Floor(v.X);

            if (v.Y < min.Y) { v.Y = r.Y = min.Y; }
            else if (v.Y >= max.Y + 1) { v.Y = MathUtil.NextAfter(max.Y + 1, float.NegativeInfinity); r.Y = max.Y; }
            else r.Y = (int)Math.Floor(v.Y);
            
            if (v.Z < min.Z) { v.Z = r.Z = min.Z; }
            else if (v.Z >= max.Z + 1) { v.Z = MathUtil.NextAfter(max.Z + 1, float.NegativeInfinity); r.Z = max.Z; }
            else r.Z = (int)Math.Floor(v.Z);

            return r;
        }

        public static void CalculateHavok(List<Vector3I> result, float gridSize, Vector3D lineStart, Vector3D lineEnd, Vector3I min, Vector3I max)
        {
            var dir = Vector3D.Normalize(lineEnd - lineStart);
            var up = Vector3D.Normalize(Vector3D.CalculatePerpendicularVector(dir)) * 0.06f;
            var right = Vector3D.Normalize(Vector3D.Cross(dir, up)) * 0.06;
            Calculate(result, gridSize, lineStart + up, lineEnd + up, min, max);
            Calculate(result, gridSize, lineStart - up, lineEnd - up, min, max);
            Calculate(result, gridSize, lineStart + right, lineEnd + right, min, max);
            Calculate(result, gridSize, lineStart - right, lineEnd - right, min, max);
        }

        /// <summary>
        /// Calculates intersected cells, note that cells have their centers in the corners
        /// </summary>
        public static void Calculate(List<Vector3I> result, float gridSize, Vector3D lineStart, Vector3D lineEnd, Vector3I min, Vector3I max)
        {
            var dir = lineEnd - lineStart;
            dir.AssertIsValid();

            // handle start==end
            Vector3D start = lineStart / gridSize;

            if (MyUtils.IsZero(dir))
            {
                if (IsPointInside(start, min, max))
                    result.Add(GetGridPoint(ref start, min, max));
                return;
            }

            // start/end in grid coordinates: clip them to the bounding box, return if no intersection
            Vector3D end = lineEnd / gridSize;
            
            //TODO: Bug in min/max coordinates?
            if (ClipLine(ref start, ref end, min, max) == false) return;


            // reflect coordinates so that dir is always positive
            Vector3 sign = Sign(dir); Vector3I signInt = SignInt(dir);

            // current/final grid position
            Vector3I cur = GetGridPoint(ref start, min, max) * signInt;
            Vector3I final = GetGridPoint(ref end, min, max) * signInt;
            dir *= sign;
            start *= sign;

            // dx = increase of t when we increase x by 1
            // nextX = t of the next point on the line with x whole
            double dx = 1 / dir.X, nextX = dx * (Math.Floor(start.X + 1) - start.X);
            double dy = 1 / dir.Y, nextY = dy * (Math.Floor(start.Y + 1) - start.Y);
            double dz = 1 / dir.Z, nextZ = dz * (Math.Floor(start.Z + 1) - start.Z);

            // 3D DDA
            while (true)
            {
                result.Add(cur * signInt);
                
                if (nextX < nextZ)
                {
                    if (nextX < nextY) { nextX += dx; if (++cur.X > final.X) break; }
                    else { nextY += dy; if (++cur.Y > final.Y) break; }
                }
                else
                {
                    if (nextZ < nextY) { nextZ += dz; if (++cur.Z > final.Z) break; }
                    else { nextY += dy; if (++cur.Y > final.Y) break; }
                }
            }
        }
    }
}
