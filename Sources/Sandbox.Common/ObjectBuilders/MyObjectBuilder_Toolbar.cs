using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using System.ComponentModel;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    public enum MyToolbarType
    {
        Character,
        SmallCockpit,
        LargeCockpit,
        Ship,
        Seat,
        ButtonPanel,
        /// <summary>
        /// This is character toolbar that allows building everything.
        /// </summary>
        Spectator,
        None,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Toolbar : MyObjectBuilder_Base
    {
        [ProtoContract]
        public struct Slot
        {
            [ProtoMember]
            public int Index;

            [ProtoMember]
            public string Item;

            [ProtoMember]
            public MyObjectBuilder_ToolbarItem Data;
        }

        [ProtoMember]
        public MyToolbarType ToolbarType = MyToolbarType.Character;

        [ProtoMember, DefaultValue(null)]
        public int? SelectedSlot = null;

        [ProtoMember]
        public List<Slot> Slots;

        [ProtoMember]
        public List<Vector3> ColorMaskHSVList;

        public void Remap(IMyRemapHelper remapHelper)
        {
            if (Slots != null)
            {
                foreach (var slot in Slots)
                {
                    slot.Data.Remap(remapHelper);
                }
            }
        }
    }
}
