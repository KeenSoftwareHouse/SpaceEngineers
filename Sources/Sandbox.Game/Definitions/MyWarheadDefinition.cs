using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_WarheadDefinition))]
    public class MyWarheadDefinition : MyCubeBlockDefinition
    {
        public float ExplosionRadius;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var warheadBuilder = (MyObjectBuilder_WarheadDefinition)builder;
            ExplosionRadius = warheadBuilder.ExplosionRadius;
        }
    }
}
