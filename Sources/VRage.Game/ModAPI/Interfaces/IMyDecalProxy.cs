using VRage.Utils;
using VRageMath;

namespace VRage.Game.ModAPI.Interfaces
{
    public interface IMyDecalProxy
    {
        /// <param name="hitInfo">Hithinfo on world coordinates</param>
        /// <param name="renderable">Position and normal on local coordinates for regular actors.
        /// World position on voxel maps. NB: To be changed</param>
        void GetDecalRenderData(MyHitInfo hitInfo, out MyDecalRenderData renderable);
        void OnAddDecal(uint decalId, ref MyDecalRenderData renderable);
    }

    public struct MyDecalRenderData
    {
        public bool Skip;
        public Vector3 Position;
        public Vector3 Normal;
        public int RenderObjectId;
        public MyStringHash Material;
    }
}
