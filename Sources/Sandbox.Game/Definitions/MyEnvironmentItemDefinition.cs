using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentItemDefinition))]
    public class MyEnvironmentItemDefinition : MyPhysicalModelDefinition
    {
        public string[] Models;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EnvironmentItemDefinition;
            MyDebug.AssertDebug(ob != null);

            if (ob.SubModels != null)
            {
                Models = new string[1 + ob.SubModels.Length];
                Models[0] = ob.Model;
                for (int i = 0; i < ob.SubModels.Length; i++)
                    Models[i + 1] = ob.SubModels[i];
            }
            else
            {
                Models = new string[] { ob.Model };
            }
        }
    }
}
