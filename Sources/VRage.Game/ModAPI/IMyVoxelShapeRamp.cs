using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelShapeRamp : IMyVoxelShape
    {
        BoundingBoxD Boundaries
        {
            get;
            set;
        }

        /// <summary>
        /// normal of the sloped plane
        /// </summary>
        Vector3D RampNormal
        {
            get;
            set;
        }

        double RampNormalW
        {
            get;
            set;
        }
    }
}
