using System.Diagnostics;
using VRage.Game.ObjectBuilders;

namespace VRage.Game.Definitions.Animation
{
    [MyDefinitionType(typeof(MyObjectBuilder_AnimationControllerDefinition))]
    public class MyAnimationControllerDefinition : MyDefinitionBase
    {
        // animation layers
        public MyObjectBuilder_AnimationLayer[] Layers;
        // state machines (referenced by layers)
        public MyObjectBuilder_AnimationSM[] StateMachines;

        // init from object builder
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_AnimationControllerDefinition;
            Debug.Assert(ob != null);

            Layers = ob.Layers;
            StateMachines = ob.StateMachines;
        }

        // generate object builder
        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var builder = MyDefinitionManagerBase.GetObjectFactory().CreateObjectBuilder<MyObjectBuilder_AnimationControllerDefinition>(this);

            builder.Id = Id;
            builder.Description = (DescriptionEnum.HasValue) ? DescriptionEnum.Value.ToString() : DescriptionString != null ? DescriptionString.ToString() : null;
            builder.DisplayName = (DisplayNameEnum.HasValue) ? DisplayNameEnum.Value.ToString() : DisplayNameString != null ? DisplayNameString.ToString() : null;
            builder.Icon = Icon;
            builder.Public = Public;
            builder.Enabled = Enabled;
            builder.AvailableInSurvival = AvailableInSurvival;

            builder.StateMachines = StateMachines;
            builder.Layers = Layers;

            return builder;
        }
    }
}
