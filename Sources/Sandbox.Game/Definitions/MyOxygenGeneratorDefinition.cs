using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenGeneratorDefinition))]
    public class MyOxygenGeneratorDefinition : MyProductionBlockDefinition
    {
        public float IceToOxygenRatio;
        public float OxygenProductionPerSecond;

        public MySoundPair GenerateSound;
        public MySoundPair IdleSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obDefinition = builder as MyObjectBuilder_OxygenGeneratorDefinition;

            this.IceToOxygenRatio = obDefinition.IceToOxygenRatio;
            this.OxygenProductionPerSecond = obDefinition.OxygenProductionPerSecond;

            this.GenerateSound = new MySoundPair(obDefinition.GenerateSound);
            this.IdleSound = new MySoundPair(obDefinition.IdleSound);
        }
    }
}
