using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FractureComponentBase : MyObjectBuilder_ComponentBase
    {
        [ProtoContract]
        public struct FracturedShape
        {
            [ProtoMember]
            public string Name;

            [ProtoMember, DefaultValue(false)]
            public bool Fixed;
        }

        [ProtoMember]
        public List<FracturedShape> Shapes = new List<FracturedShape>();
    }
}
