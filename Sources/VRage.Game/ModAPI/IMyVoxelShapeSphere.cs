using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelShapeSphere:IMyVoxelShape
    {
        /// <summary>
        /// In World Space
        /// </summary>
        Vector3D Center
        {
            get;
            set;
        }

        float Radius
        {
            get;
            set;
        }
    }
}
