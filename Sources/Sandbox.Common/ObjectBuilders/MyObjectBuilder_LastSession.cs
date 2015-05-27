using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LastSession : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string Path;

        [ProtoMember]
        public bool IsContentWorlds;
    }
}
