using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
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
