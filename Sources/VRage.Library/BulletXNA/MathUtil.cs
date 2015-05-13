// *
// * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
// *
// * Bullet Continuous Collision Detection and Physics Library
// * Copyright (c) 2003-2008 Erwin Coumans  http://www.bulletphysics.com/
// *
// * This software is provided 'as-is', without any express or implied warranty.
// * In no event will the authors be held liable for any damages arising from
// * the use of this software.
// * 
// * Permission is granted to anyone to use this software for any purpose, 
// * including commercial applications, and to alter it and redistribute it
// * freely, subject to the following restrictions:
// * 
// * 1. The origin of this software must not be misrepresented; you must not
// *    claim that you wrote the original software. If you use this software
// *    in a product, an acknowledgment in the product documentation would be
// *    appreciated but is not required.
// * 2. Altered source versions must be plainly marked as such, and must not be
// *    misrepresented as being the original software.
// * 3. This notice may not be removed or altered from any source distribution.
// */

using BulletXNA.LinearMath;
using System;
using System.Runtime.InteropServices;

namespace BulletXNA
{
    public static class MathUtil
    {
        public const float SIMD_EPSILON = 1.192092896e-07f;
        public const float SIMD_INFINITY = float.MaxValue;

        public static int MaxAxis(ref IndexedVector3 a)
        {
            return a.X < a.Y ? (a.Y < a.Z ? 2 : 1) : (a.X < a.Z ? 2 : 0);
        }

        public static void VectorMin(ref IndexedVector3 input, ref IndexedVector3 output)
        {
            output.X = Math.Min(input.X, output.X);
            output.Y = Math.Min(input.Y, output.Y);
            output.Z = Math.Min(input.Z, output.Z);
        }

        public static void VectorMax(ref IndexedVector3 input, ref IndexedVector3 output)
        {
            output.X = Math.Max(input.X, output.X);
            output.Y = Math.Max(input.Y, output.Y);
            output.Z = Math.Max(input.Z, output.Z);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)]
            public int i;
            [FieldOffset(0)]
            public float f;
        }

        //  Returns the next floating-point number after x in the direction of y.
        public static float NextAfter(float x, float y)
        {

            if (float.IsNaN(x) || float.IsNaN(y)) return x + y;
            if (x == y) return y;  // nextafter (0.0, -0.0) should return -0.0

            FloatIntUnion u;
            u.i = 0; u.f = x; // shut up the compiler

            if (x == 0)
            {
                u.i = 1;
                return y > 0 ? u.f : -u.f;
            }

            if ((x > 0) == (y > x))
                u.i++;
            else
                u.i--;
            return u.f;
        }
    }
}
