using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemMedievalWeapon : MyObjectBuilder_ToolbarItemWeapon
    {
        [ProtoMember]
        public uint? ItemId;
    }
}
