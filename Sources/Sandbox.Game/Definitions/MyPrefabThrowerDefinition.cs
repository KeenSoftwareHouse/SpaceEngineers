using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PrefabThrowerDefinition))]
    public class MyPrefabThrowerDefinition : MyDefinitionBase
    {
        public float? Mass;
        public float MaxSpeed;
        public float MinSpeed;
        public float PushTime;
        public string PrefabToThrow;
        public MyCueId ThrowSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PrefabThrowerDefinition;
            if (ob.Mass.HasValue)
                Mass = ob.Mass;

            MaxSpeed = ob.MaxSpeed;
            MinSpeed = ob.MinSpeed;
            PushTime = ob.PushTime;
            PrefabToThrow = ob.PrefabToThrow;
            ThrowSound = new MyCueId(MyStringHash.GetOrCompute(ob.ThrowSound));
        }
    }
}
