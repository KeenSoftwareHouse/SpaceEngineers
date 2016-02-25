using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MergeBlockDefinition))]
    public class MyMergeBlockDefinition : MyCubeBlockDefinition
    {
        public float Strength;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var mergeBuilder = builder as MyObjectBuilder_MergeBlockDefinition;
            MyDebug.AssertDebug(mergeBuilder != null, "Initializing thrust definition using wrong object builder.");
            Strength = mergeBuilder.Strength;
            //DeformationRatio = mergeBuilder.DeformationRatio;
            DeformationRatio = 0.5f;
        }
    }
}
