
using System;
using System.Collections.Generic;

using VRageMath;

using SharpDX.Collections;

using SharpDX.Direct3D9;
using System.Linq.Expressions;

namespace VRageRender
{
    delegate void LockVertexBufferHandler(VertexBuffer buffer, int offsetToLock, int sizeToLock, out IntPtr dataPointer, LockFlags lockFlags);

    public static class SharpDXHelper
    {
        public static SharpDX.Color ToSharpDX(VRageMath.Color xnaColor)
        {
            return new SharpDX.Color(xnaColor.R, xnaColor.G, xnaColor.B, xnaColor.A);
        }

        //public static SharpDX.DrawingRectangle ToSharpDX(VRageMath.Rectangle rectangle)
        //{
        //    return new SharpDX.DrawingRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        //}

        //public static SharpDX.DrawingRectangle? ToSharpDX(VRageMath.Rectangle? rectangle)
        //{
        //    if (rectangle.HasValue)
        //    {
        //        return new SharpDX.DrawingRectangle(rectangle.Value.X, rectangle.Value.Y, rectangle.Value.Width, rectangle.Value.Height);
        //    }
        //    return null;
        //}

        public static SharpDX.Vector2 ToSharpDX(VRageMath.Vector2 vector)
        {
            return new SharpDX.Vector2(vector.X, vector.Y);
        }

        public static SharpDX.Vector3 ToSharpDX(VRageMath.Vector3 vector)
        {
            return new SharpDX.Vector3(vector.X, vector.Y, vector.Z);
        }

        public static SharpDX.Matrix ToSharpDX(VRageMath.Matrix matrix)
        {
            return new SharpDX.Matrix(matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                                      matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                                      matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                                      matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        public static VRageMath.Vector3 ToXNA(SharpDX.Vector3 v3)
        {
            return new VRageMath.Vector3(v3.X, v3.Y, v3.Z);
        }

        public static VRageMath.Vector4 ToXNA(SharpDX.Vector4 v4)
        {
            return new VRageMath.Vector4(v4.X, v4.Y, v4.Z, v4.W);
        }

        public static VRageMath.Matrix ToXNA(SharpDX.Matrix matrix)
        {
            return new VRageMath.Matrix(matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                                      matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                                      matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                                      matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }
    }
}