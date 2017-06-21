using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRageMath
{
    public static class HyperSphereHelpers
    {

        public static double DistanceToTangentProjected(ref Vector3D center, ref Vector3D point, double radius, out double distance)
        {
            double hypotenuse;
            Vector3D.Distance(ref point, ref center, out hypotenuse);

            // using herons formula:

            var r2 = radius * radius;

            var a = hypotenuse;
            var b = radius;
            var c = Math.Sqrt(a * a - r2);

            var s = (a + b + c) / 2; // Semiperimeter

            // area squared
            var asq = s * (s - a) * (s - b) * (s - c);

            var projected = 2 * Math.Sqrt(asq) / a;

            distance = a - Math.Sqrt(r2 - projected * projected);

            return projected;
        }

        public static double DistanceToTangent(ref Vector3D center, ref Vector3D point, double radius)
        {
            double s;
            Vector3D.Distance(ref point, ref center, out s);

            return Math.Sqrt(s * s - radius * radius);
        }

        public static double DistanceToTangent(ref Vector2D center, ref Vector2D point, double radius)
        {
            double s;
            Vector2D.Distance(ref point, ref center, out s);

            return Math.Sqrt(s * s - radius * radius);
        }
    }
}
