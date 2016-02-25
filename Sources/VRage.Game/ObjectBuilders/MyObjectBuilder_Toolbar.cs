using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace VRage.Game
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
        BuildCockpit
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
            [DynamicObjectBuilder]
            [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ToolbarItem>))]
            public MyObjectBuilder_ToolbarItem Data;
        }

        [ProtoMember]
        public MyToolbarType ToolbarType = MyToolbarType.Character;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public int? SelectedSlot = null;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public List<Slot> Slots;

        #region Obsolete

        [ProtoMember, DefaultValue(null)]
        [NoSerialize]
        // Obsolete
        public List<Vector3> ColorMaskHSVList = null;
        public bool ShouldSerializeColorMaskHSVList() { return false; }	// Moved to MyPlayer

        #endregion

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