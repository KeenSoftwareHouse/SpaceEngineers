using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct CapsuleD
    {
        public Vector3D P0;
        public Vector3D P1;
        public float Radius;

        public CapsuleD(Vector3D p0, Vector3D p1, float radius)
        {
            P0 = p0;
            P1 = p1;
            Radius = radius;
        }

        public bool Intersect(RayD ray, ref Vector3D p1, ref Vector3D p2, ref Vector3 n1, ref Vector3 n2)
        {
            // Substituting equ. (1) - (6) to equ. (I) and solving for t' gives:
            //
            // t' = (t * dot(AB, d) + dot(AB, AO)) / dot(AB, AB); (7) or
            // t' = t * m + n where 
            // m = dot(AB, d) / dot(AB, AB) and 
            // n = dot(AB, AO) / dot(AB, AB)
            //
            Vector3D AB = P1 - P0;
            Vector3D AO = ray.Position - P0;

            double AB_dot_d = AB.Dot(ray.Direction);
            double AB_dot_AO = AB.Dot(AO);
            double AB_dot_AB = AB.Dot(AB);

            double m = AB_dot_AB > 0 ? AB_dot_d / AB_dot_AB : 0;
            double n = AB_dot_AB > 0 ? AB_dot_AO / AB_dot_AB : 0;

            // Substituting (7) into (II) and solving for t gives:
            //
            // dot(Q, Q)*t^2 + 2*dot(Q, R)*t + (dot(R, R) - r^2) = 0
            // where
            // Q = d - AB * m
            // R = AO - AB * n
            Vector3D Q = ray.Direction - (AB * m);
            Vector3D R = AO - (AB * n);

            double a = Q.Dot(Q);
            double b = 2.0f * Q.Dot(R);
            double c = R.Dot(R) - (Radius * Radius);

            if (a == 0.0)
            {
                // Special case: AB and ray direction are parallel. If there is an intersection it will be on the end spheres...
                // NOTE: Why is that?
                // Q = d - AB * m =>
                // Q = d - AB * (|AB|*|d|*cos(AB,d) / |AB|^2) => |d| == 1.0
                // Q = d - AB * (|AB|*cos(AB,d)/|AB|^2) =>
                // Q = d - AB * cos(AB, d) / |AB| =>
                // Q = d - unit(AB) * cos(AB, d)
                //
                // |Q| == 0 means Q = (0, 0, 0) or d = unit(AB) * cos(AB,d)
                // both d and unit(AB) are unit vectors, so cos(AB, d) = 1 => AB and d are parallel.
                // 
                BoundingSphereD sphereA, sphereB;
                sphereA.Center = P0;
                sphereA.Radius = Radius;
                sphereB.Center = P1;
                sphereB.Radius = Radius;

                double atmin, atmax, btmin, btmax;
                if (!sphereA.IntersectRaySphere(ray, out atmin, out atmax) ||
                    !sphereB.IntersectRaySphere(ray, out btmin, out btmax))
                {
                    // No intersection with one of the spheres means no intersection at all...
                    return false;
                }

                if (atmin < btmin)
                {
                    p1 = ray.Position + (ray.Direction * atmin);
                    n1 = p1 - P0;
                    n1.Normalize();
                }
                else
                {
                    p1 = ray.Position + (ray.Direction * btmin);
                    n1 = p1 - P1;
                    n1.Normalize();
                }

                if (atmax > btmax)
                {
                    p2 = ray.Position + (ray.Direction * atmax);
                    n2 = p2 - P0;
                    n2.Normalize();
                }
                else
                {
                    p2 = ray.Position + (ray.Direction * btmax);
                    n2 = p2 - P1;
                    n2.Normalize();
                }

                return true;
            }

            double discriminant = b * b - 4.0 * a * c;
            if (discriminant < 0.0f)
            {
                // The ray doesn't hit the infinite cylinder defined by (A, B).
                // No intersection.
                return false;
            }

            double tmin = (-b - Math.Sqrt(discriminant)) / (2.0 * a);
            double tmax = (-b + Math.Sqrt(discriminant)) / (2.0 * a);
            if (tmin > tmax)
            {
                double temp = tmin;
                tmin = tmax;
                tmax = temp;
            }

            // Now check to see if K1 and K2 are inside the line segment defined by A,B
            double t_k1 = tmin * m + n;
            if (t_k1 < 0.0f)
            {
                // On sphere (A, r)...
                BoundingSphereD s;
                s.Center = P0;
                s.Radius = Radius;

                double stmin, stmax;
                if (s.IntersectRaySphere(ray, out stmin, out stmax))
                {
                    p1 = ray.Position + (ray.Direction * stmin);
                    n1 = p1 - P0;
                    n1.Normalize();
                }
                else
                    return false;
            }
            else if (t_k1 > 1.0f)
            {
                // On sphere (B, r)...
                BoundingSphereD s;
                s.Center = P1;
                s.Radius = Radius;

                double stmin, stmax;
                if (s.IntersectRaySphere(ray, out stmin, out stmax))
                {
                    p1 = ray.Position + (ray.Direction * stmin);
                    n1 = p1 - P1;
                    n1.Normalize();
                }
                else
                    return false;
            }
            else
            {
                // On the cylinder...
                p1 = ray.Position + (ray.Direction * tmin);

                Vector3 k1 = P0 + AB * t_k1;
                n1 = p1 - k1;
                n1.Normalize();
            }

            double t_k2 = tmax * m + n;
            if (t_k2 < 0.0f)
            {
                // On sphere (A, r)...
                BoundingSphereD s;
                s.Center = P0;
                s.Radius = Radius;

                double stmin, stmax;
                if (s.IntersectRaySphere(ray, out stmin, out stmax))
                {
                    p2 = ray.Position + (ray.Direction * stmax);
                    n2 = p2 - P0;
                    n2.Normalize();
                }
                else
                    return false;
            }
            else if (t_k2 > 1.0f)
            {
                // On sphere (B, r)...
                BoundingSphereD s;
                s.Center = P1;
                s.Radius = Radius;

                double stmin, stmax;
                if (s.IntersectRaySphere(ray, out stmin, out stmax))
                {
                    p2 = ray.Position + (ray.Direction * stmax);
                    n2 = p2 - P1;
                    n2.Normalize();
                }
                else
                    return false;
            }
            else
            {
                p2 = ray.Position + (ray.Direction * tmax);

                Vector3D k2 = P0 + AB * t_k2;
                n2 = p2 - k2;
                n2.Normalize();
            }

            return true;
        }

        public bool Intersect(LineD line, ref Vector3D p1, ref Vector3D p2, ref Vector3 n1, ref Vector3 n2)
        {
            RayD ray = new RayD(line.From, line.Direction);
            if (Intersect(ray, ref p1, ref p2, ref n1, ref n2))
            {
                Vector3D p1Dir = p1 - line.From;
                Vector3D p2Dir = p2 - line.From;
                double p1Len = p1Dir.Normalize();
                double p2Len = p2Dir.Normalize();

                if (Vector3D.Dot(line.Direction, p1Dir) < 0.9)
                    return false;

                if (Vector3D.Dot(line.Direction, p2Dir) < 0.9)
                    return false;

                if (line.Length < p1Len)
                    return false;

                return true;
            }

            return false;
        }
    }
}
