using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SensorBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public SerializableVector3 FieldMin = new SerializableVector3(-5f, -5f, -5f);

        [ProtoMember(2)]
        public SerializableVector3 FieldMax = new SerializableVector3(5f, 5f, 5f);

        [ProtoMember(3)]
        public MyObjectBuilder_Toolbar Toolbar;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }

        [ProtoMember(4)]
        public bool DetectPlayers = true;

        [ProtoMember(5)]
        public bool DetectFloatingObjects = false;

        [ProtoMember(6)]
        public bool DetectSmallShips = false;

        [ProtoMember(7)]
        public bool DetectLargeShips = false;

        [ProtoMember(8)]
        public bool DetectStations = false;

        [ProtoMember(9)]
        public bool IsActive = false;

        [ProtoMember(10)]
        public bool DetectAsteroids = false;

        [ProtoMember(11)]
        public bool DetectOwner = true;

        [ProtoMember(12)]
        public bool DetectFriendly = true;

        [ProtoMember(13)]
        public bool DetectNeutral = true;

        [ProtoMember(14)]
        public bool DetectEnemy = true;
        
        [ProtoMember(15)]
        public int MaxDistance = 50;
    }
}
