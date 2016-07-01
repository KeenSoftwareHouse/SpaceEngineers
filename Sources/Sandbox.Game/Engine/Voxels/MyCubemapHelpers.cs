using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;
using SharpDX.Toolkit.Graphics;
using SharpDXImage = SharpDX.Toolkit.Graphics.Image;

namespace Sandbox.Engine.Voxels
{

    static class MyCubemapHelpers
    {
        public const int NUM_MAPS = 6;

        internal enum Faces : byte
        {
            XPositive,
            XNegative,
            YPositive,
            YNegative,
            ZPositive,
            ZNegative,
        }

        public static string GetNameForFace(int i)
        {
            switch (i)
            {
                case (int)Faces.XPositive:
                     return "left";
                    break;
                case (int)Faces.XNegative:
                    return "right";
                    break;
                case (int)Faces.YPositive:
                    return "up";
                    break;
                case (int)Faces.YNegative:
                    return "down";
                    break;
                case (int)Faces.ZPositive:
                    return "back";
                    break;
                case (int)Faces.ZNegative:
                    return "front";
                    break;
            }
            return "";
        }

        public static void CalculateSamplePosition(ref Vector3 localPos, out Vector3I samplePosition, ref Vector2 texCoord, int resolution)
        {
            Vector3 abs = Vector3.Abs(localPos);

            if (abs.X > abs.Y)
            {
                if (abs.X > abs.Z)
                {
                    localPos /= abs.X;
                    texCoord.Y = -localPos.Y;

                    if (localPos.X > 0.0f)
                    {
                        texCoord.X = -localPos.Z;
                        samplePosition.X = (int)Faces.XPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.Z;
                        samplePosition.X = (int)Faces.XNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.ZNegative;
                    }
                }
            }
            else
            {
                if (abs.Y > abs.Z)
                {
                    localPos /= abs.Y;
                    texCoord.Y = -localPos.Z;
                    if (localPos.Y > 0.0f)
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.YPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.YNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.ZNegative;
                    }
                }
            }

            texCoord = ((texCoord + 1f) * .5f * resolution);

            samplePosition.Y = (int)Math.Round(texCoord.X);
            samplePosition.Z = (int)Math.Round(texCoord.Y);
        }

        public static void CalculateSampleTexcoord(ref Vector3 localPos, out int face, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            if (abs.X > abs.Y)
            {
                if (abs.X > abs.Z)
                {
                    localPos /= abs.X;
                    texCoord.Y = -localPos.Y;

                    if (localPos.X > 0.0f)
                    {
                        texCoord.X = -localPos.Z;
                        face = (int)Faces.XPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.Z;
                        face = (int)Faces.XNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        face = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        face = (int)Faces.ZNegative;
                    }
                }
            }
            else
            {
                if (abs.Y > abs.Z)
                {
                    localPos /= abs.Y;
                    texCoord.Y = -localPos.Z;
                    if (localPos.Y > 0.0f)
                    {
                        texCoord.X = -localPos.X;
                        face = (int)Faces.YPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.X;
                        face = (int)Faces.YNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        face = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        face = (int)Faces.ZNegative;
                    }
                }
            }

            texCoord = (texCoord + 1f) * .5f;

            if (texCoord.X == 1) texCoord.X = 0.999999f;
            if (texCoord.Y == 1) texCoord.Y = 0.999999f;
        }

        public static void CalculateTexcoordForFace(ref Vector3 localPos, int face, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            switch (face)
            {
                case (int)Faces.XPositive:
                    localPos /= abs.X;
                    texCoord.Y = -localPos.Y;
                    texCoord.X = -localPos.Z;
                    break;
                case (int)Faces.XNegative:
                    localPos /= abs.X;
                    texCoord.Y = -localPos.Y;
                    texCoord.X = localPos.Z;
                    break;
                case (int)Faces.YPositive:
                    localPos /= abs.Y;
                    texCoord.Y = -localPos.Z;
                    texCoord.X = -localPos.X;
                    break;
                case (int)Faces.YNegative:
                    localPos /= abs.Y;
                    texCoord.Y = -localPos.Z;
                    texCoord.X = localPos.X;
                    break;
                case (int)Faces.ZPositive:
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    texCoord.X = localPos.X;
                    break;
                case (int)Faces.ZNegative:
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    texCoord.X = -localPos.X;
                    break;
                default:
                    Debug.Fail("Bad face number!!!!!");
                    texCoord = Vector2.Zero;
                    break;
            }

            texCoord = ((texCoord + 1f) * .5f);

            if (texCoord.X == 1) texCoord.X = 0.999999f;
            if (texCoord.Y == 1) texCoord.Y = 0.999999f;
        }

        private static readonly BoundingBoxD s_unitsBox = new BoundingBoxD(new Vector3(-1), new Vector3(1));

        public static void GetForwardUp(int face, out Vector3 forward, out Vector3 up)
        {
            switch (face)
            {
                case (int)Faces.XPositive:
                    up = Vector3.Up;
                    forward = Vector3.Right;
                    break;
                case (int)Faces.XNegative:
                    up = Vector3.Up;
                    forward = Vector3.Left;
                    break;
                case (int)Faces.YPositive:
                    up = Vector3.Forward;
                    forward = Vector3.Left;
                    break;
                case (int)Faces.YNegative:
                    up = Vector3.Forward;
                    forward = Vector3.Right;
                    break;
                case (int)Faces.ZPositive:
                    up = Vector3.Up;
                    forward = Vector3.Right;
                    break;
                case (int)Faces.ZNegative:
                    up = Vector3.Up;
                    forward = Vector3.Left;
                    break;
                default:
                    Debug.Fail("Bad face number!!!!!");
                    forward = up = Vector3.Zero;
                    break;
            }
        }

        /**
         * Project a position to a face of the (-1, -1, -1)(1, 1, 1) cube.
         */
        public static void ProjectToNearestFace(ref Vector3D localPos, out Vector3 faceCoords)
        {
            Vector3D gravity;
            Vector3D.Normalize(ref localPos, out gravity);
            gravity = -gravity;

            RayD r = new RayD(localPos, gravity);

            double? travel = s_unitsBox.Intersects(r);

            Debug.Assert(travel.HasValue, "Ray does not intersect with planet!");

            faceCoords = localPos + (Vector3D)(gravity * travel);
        }

        /**
         * Given a position in 3D space find which face of cubemap it falls on.
         */
        public static void GetCubeFace(ref Vector3 position, out int face)
        {
            Vector3 abs = Vector3.Abs(position);

            if (abs.X > abs.Y)
                if (abs.X > abs.Z)
                    if (position.X > 0.0f)
                        face = (int)Faces.XPositive;
                    else
                        face = (int)Faces.XNegative;
                else
                    if (position.Z > 0.0f)
                        face = (int)Faces.ZPositive;
                    else
                        face = (int)Faces.ZNegative;
            else
                if (abs.Y > abs.Z)
                    if (position.Y > 0.0f)
                        face = (int)Faces.YPositive;
                    else
                        face = (int)Faces.YNegative;
                else
                    if (position.Z > 0.0f)
                        face = (int)Faces.ZPositive;
                    else
                        face = (int)Faces.ZNegative;
        }

        /**
         * Given a position in 3D space find the direction of the face of cubemap it falls on.
         */
        public static void GetCubeFaceDirection(ref Vector3 position, out Vector3B face)
        {
            Vector3 abs = Vector3.Abs(position);

            if (abs.X > abs.Y)
                if (abs.X > abs.Z)
                    if (position.X > 0.0f)
                        face = Vector3B.Right;
                    else
                        face = Vector3B.Left;
                else
                    if (position.Z > 0.0f)
                        face = Vector3B.Backward;
                    else
                        face = Vector3B.Forward;
            else
                if (abs.Y > abs.Z)
                    if (position.Y > 0.0f)
                        face = Vector3B.Up;
                    else
                        face = Vector3B.Down;
                else
                    if (position.Z > 0.0f)
                        face = Vector3B.Backward;
                    else
                        face = Vector3B.Forward;
        }

        public static Base6Directions.Direction GetDirForFace(int face)
        {
            switch (face)
            {
                case (int)Faces.XPositive:
                    return Base6Directions.Direction.Right;
                    break;
                case (int)Faces.XNegative:
                    return Base6Directions.Direction.Left;
                    break;
                case (int)Faces.YPositive:
                    return Base6Directions.Direction.Up;
                    break;
                case (int)Faces.YNegative:
                    return Base6Directions.Direction.Down;
                    break;
                case (int)Faces.ZPositive:
                    return Base6Directions.Direction.Backward;
                    break;
                case (int)Faces.ZNegative:
                    return Base6Directions.Direction.Forward;
                    break;
            }

            Debug.Fail("Bad face number.");
            return Base6Directions.Direction.Forward;
        }

        public delegate void TexcoordCalculator(ref Vector3 local, out Vector2 texcoord);

        public static readonly TexcoordCalculator[] TexcoordCalculators = new  TexcoordCalculator[]{
            CalcLeftTexcoord,
            CalcRightTexcoord,
            CalcUpTexcoord,
            CalcDownTexcoord,
            CalcBackTexcoord,
            CalcFrontTexcoord
        };

        public static void CalcUpTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.Y;
            texCoord.Y = -localPos.Z;
            texCoord.X = -localPos.X;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public static void CalcDownTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.Y;
            texCoord.Y = -localPos.Z;
            texCoord.X = localPos.X;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public static void CalcLeftTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.X;
            texCoord.Y = -localPos.Y;
            texCoord.X = -localPos.Z;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public static void CalcRightTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.X;
            texCoord.Y = -localPos.Y;
            texCoord.X = localPos.Z;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public static void CalcBackTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.Z;
            texCoord.Y = -localPos.Y;
            texCoord.X = localPos.X;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public static void CalcFrontTexcoord(ref Vector3 localPos, out Vector2 texCoord)
        {
            Vector3 abs = Vector3.Abs(localPos);

            localPos /= abs.Z;
            texCoord.Y = -localPos.Y;
            texCoord.X = -localPos.X;

            texCoord = ((texCoord + 1f) * .5f);
        }

        public const float USHORT_RECIP = 1f / ushort.MaxValue;
        public const float USHORT2_RECIP = 2f / ushort.MaxValue;
        public const float BYTE_RECIP = 1f / byte.MaxValue;

        public static Vector2I GetStep(ref Vector2I start, ref Vector2I end)
        {
            if (start.X > end.X)
                return -Vector2I.UnitX;

            if (start.X < end.X)
                return Vector2I.UnitX;

            if (start.Y > end.Y)
                return -Vector2I.UnitY;

            if (start.Y < end.Y)
                return Vector2I.UnitY;

            return Vector2I.Zero;
        }
    }
}
