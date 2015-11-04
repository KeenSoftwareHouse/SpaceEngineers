using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Voxels;
using Sandbox.Engine.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public static class VoxelBaseExtensions
    {

        public static MyVoxelMaterialDefinition GetMaterialAt(this MyVoxelBase self,ref Vector3D worldPosition)
        {
            Vector3D localHitPos;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(worldPosition, self.PositionComp.WorldMatrix, MatrixD.Invert(self.PositionComp.WorldMatrix), self.SizeInMetresHalf, out localHitPos);
            Vector3I voxelPosition = new Vector3I(localHitPos / MyVoxelConstants.VOXEL_SIZE_IN_METRES) + self.StorageMin;

            return self.Storage.GetMaterialAt(ref voxelPosition);
        }

    }
}
