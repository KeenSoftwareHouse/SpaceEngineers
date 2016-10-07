using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.Utils;
using VRage.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_TreeDefinition), typeof(Postprocessor))]
    //[MyDefinitionType(typeof(MyObjectBuilder_TreeDefinition))]
    public class MyTreeDefinition : MyEnvironmentItemDefinition
    {
        public float BranchesStartHeight;
        public float HitPoints;
        public string CutEffect;
        public string FallSound;
        public string BreakSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_TreeDefinition;
            MyDebug.AssertDebug(ob != null);

            BranchesStartHeight = ob.BranchesStartHeight;
            HitPoints = ob.HitPoints;

            CutEffect = ob.CutEffect;

            FallSound = ob.FallSound;
            BreakSound = ob.BreakSound;
        }
    }
}
