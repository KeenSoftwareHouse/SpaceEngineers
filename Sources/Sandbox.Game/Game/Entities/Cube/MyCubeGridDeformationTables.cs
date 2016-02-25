using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    using Sandbox.Engine.Utils;
    using Table = Dictionary<Vector3I, Vector3>;

    static class MyCubeGridDeformationTables
    {
        public class DeformationTable
        {
            public Table OffsetTable = new Table();
            public Vector3I Normal;
            public Vector3I MinOffset = Vector3I.MaxValue;
            public Vector3I MaxOffset = Vector3I.MinValue;
        }

        // Final result in these tables should be multiplied by Random(1, 1.25) to achieve random results
        public static DeformationTable[] ThinUpper = new DeformationTable[]
        {
            CreateTable(new Vector3I(1, 0, 0)),
            CreateTable(new Vector3I(0, 1, 0)),
            CreateTable(new Vector3I(0, 0, 1)),
        };

        // Final result in these tables should be multiplied by Random(1, 1.25) to achieve random results
        public static DeformationTable[] ThinLower = new DeformationTable[]
        {
            CreateTable(new Vector3I(-1, 0, 0)),
            CreateTable(new Vector3I( 0,-1, 0)),
            CreateTable(new Vector3I( 0, 0,-1)),
        };

        static DeformationTable CreateTable(Vector3I normal)
        {
            DeformationTable result = new DeformationTable();
            result.Normal = normal;

            Vector3I centerBone = new Vector3I(1, 1, 1);
            var absNormal = Vector3I.Abs(normal);
            var mask = new Vector3I(1, 1, 1) - absNormal;
            mask *= 2;

            for (int x = -mask.X; x <= mask.X; x++)
            {
                for (int y = -mask.Y; y <= mask.Y; y++)
                {
                    for (int z = -mask.Z; z <= mask.Z; z++)
                    {
                        var offset = new Vector3I(x, y, z);

                        float maxOffset = Math.Max(Math.Abs(z), Math.Max(Math.Abs(x), Math.Abs(y)));
                        float ratio = 1;
                        if (maxOffset > 1)
                            ratio = 0.3f;

                        float moveDist = ratio * MyGridConstants.DEFORMATION_TABLE_BASE_MOVE_DIST;
                        Vector3I offsetA = centerBone + new Vector3I(x, y, z) + normal;
                        result.OffsetTable.Add(offsetA, -normal * moveDist);

                        result.MinOffset = Vector3I.Min(result.MinOffset, offset);
                        result.MaxOffset = Vector3I.Max(result.MaxOffset, offset);
                    }
                }
            }
            return result;
        }
    }
}
