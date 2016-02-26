using System.Xml.Serialization;
using ProtoBuf;
using System;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MyEnvironmentItemsAttribute : System.Attribute
    {
        public readonly Type ItemDefinitionType;

        public MyEnvironmentItemsAttribute(Type itemDefinitionType)
        {
            /*Debug.Assert(
                typeof(MyObjectBuilder_EnvironmentItem).IsAssignableFrom(itemDefinitionType),
                "MyEnvironmentItemsAttribute should set a subclass of MyObjectBuilder_EnvironmentItem"
            );*/
            ItemDefinitionType = itemDefinitionType;
        }
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_EnvironmentItemDefinition))]
    public class MyObjectBuilder_EnvironmentItems : MyObjectBuilder_EntityBase
    {
        [ProtoContract]
        public struct MyOBEnvironmentItemData
        {
            [ProtoMember]
            public MyPositionAndOrientation PositionAndOrientation;

            [ProtoMember]
            public string SubtypeName;
        }

        [XmlArrayItem("Item")]
        [ProtoMember]
        public MyOBEnvironmentItemData[] Items;

        [ProtoMember]
        public Vector3D CellsOffset;
    }
}