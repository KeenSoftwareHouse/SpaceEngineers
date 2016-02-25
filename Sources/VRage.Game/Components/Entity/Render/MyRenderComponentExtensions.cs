using VRage.Game.Models;

namespace VRage.Game.Components
{
    public static class MyRenderComponentBaseExtensions
    {
        public static MyModel GetModel(this MyRenderComponentBase obj)
        {
            return (MyModel)obj.ModelStorage;
        }
    }
}
