using System.Diagnostics;
using Sandbox.Engine.Models;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Common;
using VRage.Game.Models;

namespace Sandbox.Game.Entities
{
    // Before: 148 B
    // After: 60 B
    public class MyCubePart
    {
        public MyCubeInstanceData InstanceData;
        public MyModel Model;

        public void Init(MyModel model, Matrix matrix)
        {
            Model = model;
            InstanceData.LocalMatrix = matrix;
            model.LoadData();
        }
    }
}
