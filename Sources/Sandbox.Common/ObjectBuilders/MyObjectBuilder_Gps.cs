using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using System.Collections.Generic;


namespace Sandbox.Common.ObjectBuilders
{
    
        [ProtoContract]
        [MyObjectBuilderDefinition]
        public class MyObjectBuilder_Gps : MyObjectBuilder_Base
        {
            [ProtoContract]
            public struct Entry
            {
                [ProtoMember]
                public string name;

                [ProtoMember]
                public string description;

                [ProtoMember]
                public Vector3D coords;

                [ProtoMember]
                public bool isFinal;

                [ProtoMember]
                public bool showOnHud;
            }

            [ProtoMember]
            public List<Entry> Entries;

        }
}

