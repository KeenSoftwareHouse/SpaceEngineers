using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public static class VoxelBaseExtensions
    {

        public static MyVoxelMaterialDefinition GetMaterialAt(this MyVoxelBase self, ref Vector3D worldPosition)
        {
            Vector3D localHitPos;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(worldPosition, self.PositionComp.WorldMatrix, self.PositionComp.WorldMatrixInvScaled, self.SizeInMetresHalf, out localHitPos);
            Vector3I voxelPosition = new Vector3I(localHitPos / MyVoxelConstants.VOXEL_SIZE_IN_METRES) + self.StorageMin;

            return self.Storage.GetMaterialAt(ref voxelPosition);
        }

    }
}
