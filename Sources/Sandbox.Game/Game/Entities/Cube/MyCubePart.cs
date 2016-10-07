using System.Diagnostics;
using Sandbox.Engine.Models;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Common;
using VRage;
using VRage.Game.Models;

namespace Sandbox.Game.Entities
{
    // Before: 148 B
    // After: 60 B
    public class MyCubePart
    {
        public MyCubeInstanceData InstanceData;
        public MyModel Model;

        public void Init(MyModel model, Matrix matrix, float rescaleModel = 1.0f)
        {
            Model = model;
            model.Rescale(rescaleModel);
            InstanceData.LocalMatrix = matrix;
            model.LoadData();
        }
    }
}
