using ProtoBuf;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SensorBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public SerializableVector3 FieldMin = new SerializableVector3(-5f, -5f, -5f);

        [ProtoMember]
        public SerializableVector3 FieldMax = new SerializableVector3(5f, 5f, 5f);

        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }

        [ProtoMember]
        public bool PlaySound = true;

        [ProtoMember]
        public bool DetectPlayers = true;

        [ProtoMember]
        public bool DetectFloatingObjects = false;

        [ProtoMember]
        public bool DetectSmallShips = false;

        [ProtoMember]
        public bool DetectLargeShips = false;

        [ProtoMember]
        public bool DetectStations = false;

        [ProtoMember]
        public bool DetectSubgrids = false;

        [ProtoMember]
        public bool IsActive = false;

        [ProtoMember]
        public bool DetectAsteroids = false;

        [ProtoMember]
        public bool DetectOwner = true;

        [ProtoMember]
        public bool DetectFriendly = true;

        [ProtoMember]
        public bool DetectNeutral = true;

        [ProtoMember]
        public bool DetectEnemy = true;
    }
}
