using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_PistonBase : MyObjectBuilder_MechanicalConnectionBlock
    {
        public float Velocity = -0.1f;

        public float? MaxLimit;

        public float? MinLimit;

        public bool Reverse;

        public float CurrentPosition;
    }
}
