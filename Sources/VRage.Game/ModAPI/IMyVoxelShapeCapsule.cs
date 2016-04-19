using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelShapeCapsule:IMyVoxelShape
    {
        /// <summary>
        /// In world Coordinates
        /// </summary>
        Vector3D A
        {
            get;
            set;
        }

        /// <summary>
        /// In world Coordinates
        /// </summary>
        Vector3D B
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
