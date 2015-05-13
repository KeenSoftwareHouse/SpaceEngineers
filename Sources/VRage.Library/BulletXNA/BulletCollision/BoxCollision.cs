/*
 * 
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
This source file is part of GIMPACT Library.

For the latest info, see http://gimpact.sourceforge.net/

Copyright (c) 2007 Francisco Leon Najera. C.C. 80087371.
email: projectileman@yahoo.com


This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using BulletXNA.LinearMath;
using System;

namespace BulletXNA.BulletCollision
{
    static class BoxCollision
    {
        public static bool BT_GREATER(float x, float y)
        {
            return Math.Abs(x) > y;
        }

        public static float BT_MAX(float a, float b)
        {
            return Math.Max(a, b);
        }

        public static float BT_MIN(float a, float b)
        {
            return Math.Min(a, b);
        }
    }

    //! Axis aligned box
    public struct AABB
    {
        public IndexedVector3 m_min;
        public IndexedVector3 m_max;

        public AABB(ref IndexedVector3 min, ref IndexedVector3 max)
        {
            m_min = min;
            m_max = max;
        }

        public AABB(IndexedVector3 min, IndexedVector3 max)
        {
            m_min = min;
            m_max = max;
        }

        public void Invalidate()
        {
            m_min.X = MathUtil.SIMD_INFINITY;
            m_min.Y = MathUtil.SIMD_INFINITY;
            m_min.Z = MathUtil.SIMD_INFINITY;
            m_max.X = -MathUtil.SIMD_INFINITY;
            m_max.Y = -MathUtil.SIMD_INFINITY;
            m_max.Z = -MathUtil.SIMD_INFINITY;
        }

        //! Merges a Box
        public void Merge(AABB box)
        {
            Merge(ref box);
        }

        public void Merge(ref AABB box)
        {
            m_min.X = BoxCollision.BT_MIN(m_min.X, box.m_min.X);
            m_min.Y = BoxCollision.BT_MIN(m_min.Y, box.m_min.Y);
            m_min.Z = BoxCollision.BT_MIN(m_min.Z, box.m_min.Z);

            m_max.X = BoxCollision.BT_MAX(m_max.X, box.m_max.X);
            m_max.Y = BoxCollision.BT_MAX(m_max.Y, box.m_max.Y);
            m_max.Z = BoxCollision.BT_MAX(m_max.Z, box.m_max.Z);
        }

        //! Gets the extend and center
        public void GetCenterExtend(out IndexedVector3 center, out IndexedVector3 extend)
        {
            center = new IndexedVector3((m_max + m_min) * 0.5f);
            extend = new IndexedVector3(m_max - center);
        }

        /*! \brief Finds the Ray intersection parameter.
        \param aabb Aligned box
        \param vorigin A vec3f with the origin of the ray
        \param vdir A vec3f with the direction of the ray
        */
        public bool CollideRay(ref IndexedVector3 vorigin, ref IndexedVector3 vdir)
        {
            IndexedVector3 extents, center;
            GetCenterExtend(out center, out extents);

            float Dx = vorigin.X - center.X;
            if (BoxCollision.BT_GREATER(Dx, extents.X) && Dx * vdir.X >= 0.0f) return false;
            float Dy = vorigin.Y - center.Y;
            if (BoxCollision.BT_GREATER(Dy, extents.Y) && Dy * vdir.Y >= 0.0f) return false;
            float Dz = vorigin.Z - center.Z;
            if (BoxCollision.BT_GREATER(Dz, extents.Z) && Dz * vdir.Z >= 0.0f) return false;


            float f = vdir.Y * Dz - vdir.Z * Dy;
            if (Math.Abs(f) > extents.Y * Math.Abs(vdir.Z) + extents.Z * Math.Abs(vdir.Y)) return false;
            f = vdir.Z * Dx - vdir.X * Dz;
            if (Math.Abs(f) > extents.X * Math.Abs(vdir.Z) + extents.Z * Math.Abs(vdir.X)) return false;
            f = vdir.X * Dy - vdir.Y * Dx;
            if (Math.Abs(f) > extents.X * Math.Abs(vdir.Y) + extents.Y * Math.Abs(vdir.X)) return false;
            return true;
        }

        public float? CollideRayDistance(ref IndexedVector3 origin, ref IndexedVector3 direction)
        {
            // r.dir is unit direction vector of ray
            IndexedVector3 dirfrac = new IndexedVector3(1.0f / direction.X, 1.0f / direction.Y, 1.0f / direction.Z);

            // m_min is the corner of AABB with minimal coordinates - left bottom, m_max is maximal corner
            // r.org is origin of ray
            float t1 = (m_min.X - origin.X) * dirfrac.X;
            float t2 = (m_max.X - origin.X) * dirfrac.X;
            float t3 = (m_min.Y - origin.Y) * dirfrac.Y;
            float t4 = (m_max.Y - origin.Y) * dirfrac.Y;
            float t5 = (m_min.Z - origin.Z) * dirfrac.Z;
            float t6 = (m_max.Z - origin.Z) * dirfrac.Z;

            float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            float t;

            // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
            if (tmax < 0)
            {
                t = tmax;
                return null;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                t = tmax;
                return null;
            }

            t = tmin;
            return t;
        }
    }
}
