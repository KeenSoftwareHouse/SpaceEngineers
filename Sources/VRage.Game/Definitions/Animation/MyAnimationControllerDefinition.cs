using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Definitions.Animation
{
    [MyDefinitionType(typeof(MyObjectBuilder_AnimationControllerDefinition), typeof(MyAnimationControllerDefinitionPostprocess))]
    public class MyAnimationControllerDefinition : MyDefinitionBase
    {
        // animation layers
        public List<MyObjectBuilder_AnimationLayer> Layers = new List<MyObjectBuilder_AnimationLayer>();
        // state machines (referenced by layers)
        public List<MyObjectBuilder_AnimationSM> StateMachines = new List<MyObjectBuilder_AnimationSM>();

        // init from object builder
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_AnimationControllerDefinition;
            Debug.Assert(ob != null);

            if (ob.Layers != null)
                Layers.AddRange(ob.Layers);
            if (ob.StateMachines != null)
                StateMachines.AddRange(ob.StateMachines);
        }

        // generate object builder
        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var builder = MyDefinitionManagerBase.GetObjectFactory().CreateObjectBuilder<MyObjectBuilder_AnimationControllerDefinition>(this);

            builder.Id = Id;
            builder.Description = (DescriptionEnum.HasValue) ? DescriptionEnum.Value.ToString() : DescriptionString != null ? DescriptionString.ToString() : null;
            builder.DisplayName = (DisplayNameEnum.HasValue) ? DisplayNameEnum.Value.ToString() : DisplayNameString != null ? DisplayNameString.ToString() : null;
            builder.Icons = Icons;
            builder.Public = Public;
            builder.Enabled = Enabled;
            builder.AvailableInSurvival = AvailableInSurvival;

            builder.StateMachines = StateMachines.ToArray();
            builder.Layers = Layers.ToArray();

            return builder;
        }
    }

    internal class MyAnimationControllerDefinitionPostprocess : MyDefinitionPostprocessor
    {
        public override void AfterLoaded(ref Bundle definitions)
        {
        }

        public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
        {
        }

        public override void OverrideBy(ref Bundle currentDefinitions, ref Bundle overrideBySet)
        {
            foreach (var def in overrideBySet.Definitions)
            {
                MyAnimationControllerDefinition modifyingAnimationController = def.Value as MyAnimationControllerDefinition;
                if (def.Value.Enabled && modifyingAnimationController != null)
                {
                    bool justCopy = true;
                    if (currentDefinitions.Definitions.ContainsKey(def.Key))
                    {
                        MyAnimationControllerDefinition originalAnimationController = currentDefinitions.Definitions[def.Key] as MyAnimationControllerDefinition;
                        if (originalAnimationController != null)
                        {
                            foreach (var sm in modifyingAnimationController.StateMachines)
                                if (originalAnimationController.StateMachines.All(x => x.Name != sm.Name))
                                    originalAnimationController.StateMachines.Add(sm);

                            foreach (var layer in modifyingAnimationController.Layers)
                                if (originalAnimationController.Layers.All(x => x.Name != layer.Name))
                                    originalAnimationController.Layers.Add(layer);
                            
                            justCopy = false;
                        }
                    }

                    if (justCopy)
                    {
                        currentDefinitions.Definitions[def.Key] = def.Value;
                    }
                }
            }
        }
    }
}
