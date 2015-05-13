using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Library.Utils;


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
        public MyStringId ThrowSound;

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
            ThrowSound = MyStringId.GetOrCompute(ob.ThrowSound);
        }
    }
}