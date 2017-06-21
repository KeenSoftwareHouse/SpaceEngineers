using System.Diagnostics;
using VRageMath;

using Direction = VRageMath.Base6Directions.Direction;

namespace Sandbox.Game.Entities.Planet
{
    public static class MyPlanetCubemapHelper
    {
        // For each face we have 6 operation instructions, one for each cube face.
        // For itself and the opposite face the operation is always ignored, but
        // it has to be on the list to make indexing simpler.
        public static uint[] AdjacentFaceTransforms =
        {
            // Format is axis to use, axis to set, weather to invert x, weather to invert y, weather to subtract or add 2
            
            // Forward Face
            0, // Dummy
            0, // Backward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Right
            0x0 | 0x1 << 1 | 0x0 << 2 | 1 << 3 | 0 << 4, // Up
            0x0 | 0x1 << 1 | 0x0 << 2 | 1 << 3 | 1 << 4, // Down

            // Backward
            0, // Forward
            0, // Dummy
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Right
            0x0 | 0x1 << 1 | 0x1 << 2 | 0 << 3 | 0 << 4, // Up
            0x0 | 0x1 << 1 | 0x1 << 2 | 0 << 3 | 1 << 4, // Down
            
            // Left
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Forward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Backward
            0, // Dummy
            0, // Right
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Up
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 1 << 4, // Down
            
            // Right
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Forward
            0x0 | 0x0 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Backward
            0, // Left
            0, // Dummy
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 0 << 4, // Up
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Down
            
            // Up
            0x1 | 0x0 << 1 | 0x0 << 2 | 1 << 3 | 1 << 4, // Forward
            0x1 | 0x0 << 1 | 0x1 << 2 | 0 << 3 | 0 << 4, // Backward
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 1 << 4, // Left
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 0 << 4, // Right
            0, // Dummy
            0, // Down
            
            // Down
            0x1 | 0x0 << 1 | 0x0 << 2 | 1 << 3 | 0 << 4, // Forward
            0x1 | 0x0 << 1 | 0x1 << 2 | 0 << 3 | 1 << 4, // Backward
            0x1 | 0x1 << 1 | 0x1 << 2 | 1 << 3 | 1 << 4, // Left
            0x1 | 0x1 << 1 | 0x0 << 2 | 0 << 3 | 0 << 4, // Right
            0, // Up
            0, // Dummy
        };

        /**
         * Find the appropriate face and project a position to it's local space.
         * 
         * There was some fishy here but the face transforms depend on this method's behaviour so it will stay for now, I hope I can fix it before release.
         */
        public static void ProjectToCube(ref Vector3D localPos, out int direction, out Vector2D texcoords)
        {
            Vector3D abs;
            Vector3D.Abs(ref localPos, out abs);

            if (abs.X > abs.Y)
            {
                if (abs.X > abs.Z)
                {
                    localPos /= abs.X;
                    texcoords.Y = localPos.Y;

                    if (localPos.X > 0.0f)
                    {
                        texcoords.X = -localPos.Z;
                        direction = (int)Direction.Right;
                    }
                    else
                    {
                        texcoords.X = localPos.Z;
                        direction = (int)Direction.Left;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texcoords.Y = localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texcoords.X = localPos.X;
                        direction = (int)Direction.Backward;
                    }
                    else
                    {
                        texcoords.X = -localPos.X;
                        direction = (int)Direction.Forward;
                    }
                }
            }
            else
            {
                if (abs.Y > abs.Z)
                {
                    localPos /= abs.Y;
                    texcoords.Y = localPos.X;
                    if (localPos.Y > 0.0f)
                    {
                        texcoords.X = localPos.Z;
                        direction = (int)Direction.Up;
                    }
                    else
                    {
                        texcoords.X = -localPos.Z;
                        direction = (int)Direction.Down;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texcoords.Y = localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texcoords.X = localPos.X;
                        direction = (int)Direction.Backward;
                    }
                    else
                    {
                        texcoords.X = -localPos.X;
                        direction = (int)Direction.Forward;
                    }
                }
            }
        }

        /**
         * Find the appropriate face for the projection of a point.
         */
        public static int FindCubeFace(ref Vector3D localPos)
        {
            Vector3D abs;
            Vector3D.Abs(ref localPos, out abs);

            if (abs.X > abs.Y)
                if (abs.X > abs.Z)
                    if (localPos.X > 0.0f)
                        return (int)Direction.Right;
                    else
                        return (int)Direction.Left;
                else
                    if (localPos.Z > 0.0f)
                        return (int)Direction.Backward;
                    else
                        return (int)Direction.Forward;
            else
                if (abs.Y > abs.Z)
                    if (localPos.Y > 0.0f)
                        return (int)Direction.Up;
                    else
                        return (int)Direction.Down;
                else
                    if (localPos.Z > 0.0f)
                        return (int)Direction.Backward;
                    else
                        return (int)Direction.Forward;
        }

        /**
         * Project the position to local space for a provided face.
         */
        public static void ProjectForFace(ref Vector3D localPos, int face, out Vector2D normalCoord)
        {
            Vector3D abs;
            Vector3D.Abs(ref localPos, out abs);

            switch ((Direction)face)
            {
                case Direction.Forward:
                    localPos /= abs.Z;
                    normalCoord.X = -localPos.X;
                    normalCoord.Y = localPos.Y;
                    break;
                case Direction.Backward:
                    localPos /= abs.Z;
                    normalCoord.X = localPos.X;
                    normalCoord.Y = localPos.Y;
                    break;
                case Direction.Left:
                    localPos /= abs.X;
                    normalCoord.X = localPos.Z;
                    normalCoord.Y = localPos.Y;
                    break;
                case Direction.Right:
                    localPos /= abs.X;
                    normalCoord.X = -localPos.Z;
                    normalCoord.Y = localPos.Y;
                    break;
                case Direction.Up:
                    localPos /= abs.Y;
                    normalCoord.X = localPos.Z;
                    normalCoord.Y = localPos.X;
                    break;
                case Direction.Down:
                    localPos /= abs.Y;
                    normalCoord.X = -localPos.Z;
                    normalCoord.Y = localPos.X;
                    break;
                default:
                    Debug.Fail("Bad face number!!!!!");
                    normalCoord = Vector2D.Zero;
                    break;
            }
        }

        /**
         * Get cubemap forward up for a given face.
         */
        public static void GetForwardUp(Direction axis, out Vector3D forward, out Vector3D up)
        {
            forward = Base6Directions.Directions[(int)axis];

            up = Base6Directions.Directions[(int)Base6Directions.GetPerpendicular(axis)];
        }

        /**
         * Translate texcoords from one face to the next.
         */
        public static unsafe void TranslateTexcoordsToFace(ref Vector2D texcoords, int originalFace, int myFace, out Vector2D newCoords)
        {
            var localCoords = texcoords;

            if ((originalFace & ~1) != (myFace & ~1))
            {
                // Fetch our instructions
                uint instructions = AdjacentFaceTransforms[myFace * 6 + originalFace];
                double* tx = (double*)&localCoords;

                // If destination and target don't match we have to swap the coordinates
                if ((instructions & 1) != ((instructions >> 1) & 1))
                {
                    double tmp = tx[0];
                    tx[0] = tx[1];
                    tx[1] = tmp;
                }

                // Which index to change
                uint index = (instructions >> 1) & 1;

                // Invert
                if (((instructions >> 2) & 1) != 0)
                    tx[index] = -tx[index];

                // Invert other component
                if (((instructions >> 3) & 1) != 0)
                    tx[1 ^ index] = -tx[1 ^ index];

                // Add or subtract 2
                if (((instructions >> 4) & 1) != 0)
                    tx[index] -= 2;
                else tx[index] += 2;
            }

            newCoords = localCoords;
        }
    }
}
