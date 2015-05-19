using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LightingBlockDefinition))]
    class MyLightingBlockDefinition : MyCubeBlockDefinition
    {
        public MyBounds LightRadius;
        public MyBounds LightFalloff;
        public MyBounds LightIntensity;
        public MyBounds BlinkIntervalSeconds;
        public MyBounds BlinkLenght;
        public MyBounds BlinkOffset;
        public float RequiredPowerInput;
        public string LightGlare;
        public bool HasPhysics;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_LightingBlockDefinition)builder;

            BlinkIntervalSeconds = ob.LightBlinkIntervalSeconds;
            BlinkLenght = ob.LightBlinkLenght;
            BlinkOffset = ob.LightBlinkOffset;
            LightRadius        = ob.LightRadius;
            LightFalloff       = ob.LightFalloff;
            LightIntensity     = ob.LightIntensity;
            RequiredPowerInput = ob.RequiredPowerInput;
            LightGlare         = ob.LightGlare;
            HasPhysics = ob.HasPhysics;
        }
    }
}
