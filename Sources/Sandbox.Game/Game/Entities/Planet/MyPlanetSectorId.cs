using VRage;
using VRageMath;

namespace Sandbox.Game.Entities.Planet
{
    public class MyPlanetSectorId
    {
        private const long CoordMask = 0XFFFFFF;
        private const int CoordBits = 24;
        private const int FaceOffset = CoordBits * 2;
        private const long FaceMask = 0X7;
        private const int FaceBits = 3;
        private const long LodMask = 0XFF;
        private const int LodBits = 8;
        private const int LodOffset = FaceOffset + FaceBits;

        private MyPlanetSectorId()
        {
        }

        public static long MakeSectorEntityId(int x, int y, int lod, int face, long parentId)
        {
            return MyEntityIdentifier.ConstructIdFromString(MyEntityIdentifier.ID_OBJECT_TYPE.PLANET_ENVIRONMENT_SECTOR, string.Format("P({0})S(x{1}, y{2}, f{3}, l{4})", parentId, x, y, face, lod));
        }

        public static long MakeSectorId(int x, int y, int face, int lod = 0)
        {
            return (x & CoordMask) | (y & CoordMask) << CoordBits | (face & FaceMask) << FaceOffset | (lod & LodMask) << LodOffset;
        }

        public static Vector3I DecomposeSectorId(long sectorID)
        {
            return new Vector3I((int)(sectorID & CoordMask), (sectorID >> CoordBits) & CoordMask, (sectorID >> FaceOffset) & FaceMask);
        }

        public static int GetFace(long packedSectorId)
        {
            return (int)((packedSectorId >> FaceOffset) & FaceMask);
        }
    }
}