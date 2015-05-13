using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRageMath;


namespace Sandbox.Common.ObjectBuilders
{
    
        [ProtoContract]
        [MyObjectBuilderDefinition]
        public class MyObjectBuilder_Gps : MyObjectBuilder_Base
        {
            [ProtoContract]
            public struct Entry
            {
                [ProtoMember(1)]
                public string name;

                [ProtoMember(2)]
                public string description;

                [ProtoMember(3)]
                public Vector3D coords;

                [ProtoMember(4)]
                public bool isFinal;

                [ProtoMember(4)]
                public bool showOnHud;
            }

            [ProtoMember(1)]
            public List<Entry> Entries;

        }
}

