using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VoxelHandDefinition))]
    public class MyVoxelHandDefinition : MyDefinitionBase
    {
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_VoxelHandDefinition;

            MyDebug.AssertDebug(ob != null);
        }
    }
}
