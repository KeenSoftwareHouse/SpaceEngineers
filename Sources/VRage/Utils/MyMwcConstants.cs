using System.Text;
using VRageMath;

namespace VRage.Utils
{
    public static class MyMathConstants
    {
        //  IMPORTANT: If you change these constants, don't forget to change them also in MyMeshPartSolver
        public const float EPSILON = 0.00001f;
        public const float EPSILON10 = 0.000001f;
        public const float EPSILON_SQUARED = EPSILON * EPSILON;
    }

    public static class MyValidationConstants
    {
        public const int USERNAME_LENGTH_MIN = 3;
        public const int USERNAME_LENGTH_MAX = 15;
        public const int EMAIL_LENGTH_MAX = 50;
        public const int PASSWORD_LENGTH_MIN = 6;
        public const int PASSWORD_LENGTH_MAX = 10;
        public const int POSITION_X_MAX = 5;
        public const int POSITION_Y_MAX = 5;
        public const int POSITION_Z_MAX = 5;
    }

    public static class MySectorConstants
    {
        //  Sector size
        public const float SECTOR_SIZE = 50000.0f;          //  Size of sector cube
        public const float SECTOR_SIZE_HALF = SECTOR_SIZE / 2.0f;
        public static readonly Vector3 SECTOR_SIZE_VECTOR3 = new Vector3(SECTOR_SIZE, SECTOR_SIZE, SECTOR_SIZE);
        public static readonly float SECTOR_DIAMETER = SECTOR_SIZE_VECTOR3.Length();
        public const float SAFE_SECTOR_SIZE = SECTOR_SIZE + 200;                //  Safe area around sector for detecting if we need to switch sectors
        public const float SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF = (SECTOR_SIZE * 0.9f) / 2.0f;
        public const float SAFE_SECTOR_SIZE_HALF = SAFE_SECTOR_SIZE / 2.0f;
        public static readonly BoundingBox SAFE_SECTOR_SIZE_BOUNDING_BOX = new BoundingBox(
            new Vector3(-SAFE_SECTOR_SIZE_HALF, -SAFE_SECTOR_SIZE_HALF, -SAFE_SECTOR_SIZE_HALF),
            new Vector3(SAFE_SECTOR_SIZE_HALF, SAFE_SECTOR_SIZE_HALF, SAFE_SECTOR_SIZE_HALF));
        public static readonly Vector3[] SAFE_SECTOR_SIZE_BOUNDING_BOX_CORNERS = SAFE_SECTOR_SIZE_BOUNDING_BOX.GetCorners();
        public static readonly BoundingBox SECTOR_SIZE_FOR_PHYS_OBJECTS_BOUNDING_BOX = new BoundingBox(
            new Vector3(-SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF, -SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF, -SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF),
            new Vector3(SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF, SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF, SECTOR_SIZE_FOR_PHYS_OBJECTS_SIZE_HALF));
        public static readonly BoundingBox SECTOR_SIZE_BOUNDING_BOX = new BoundingBox(
            new Vector3(-SECTOR_SIZE_HALF, -SECTOR_SIZE_HALF, -SECTOR_SIZE_HALF),
            new Vector3(SECTOR_SIZE_HALF, SECTOR_SIZE_HALF, SECTOR_SIZE_HALF));
    }
}
