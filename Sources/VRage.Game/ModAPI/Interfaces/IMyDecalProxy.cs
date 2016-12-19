using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Game.ModAPI.Interfaces
{
    public interface IMyDecalProxy
    {
        /// <param name="hitInfo">Hithinfo on world coordinates</param>
        void AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material);
    }

    public interface IMyDecalHandler
    {
        /// <param name="renderData">Position and normal on local coordinates for regular actors.
        /// World position on voxel maps</param>
        uint? AddDecal(ref MyDecalRenderInfo renderInfo);
    }
}
