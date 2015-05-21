using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LastLoadedTimes : MyObjectBuilder_Base
    {
        [ProtoMember]
        public SerializableDictionary<string, DateTime> LastLoaded;
    }
}
