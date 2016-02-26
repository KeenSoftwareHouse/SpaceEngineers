using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UseObjectsComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        // Load detectors from model dummies
        [ProtoMember]
        public bool LoadFromModel;

        // Name of detector which is created from entity AABB.
        [ProtoMember, DefaultValue(null)]
        public string UseObjectFromModelBBox = null;
    }
}
