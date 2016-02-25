using ProtoBuf;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMap : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public string StorageName
        {
            get { return m_storageName ?? base.Name; }
            set { m_storageName = value; }
        }
        private string m_storageName;

        //[ProtoMember]
        public bool MutableStorage = true;
        public bool ShouldSerializeMutableStorage() { return !MutableStorage; }

        [Serialize(MyObjectFlags.Nullable)]
        public bool? ContentChanged;

        public MyObjectBuilder_VoxelMap()
            : base()
        {
            PositionAndOrientation = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up);
        }

        public MyObjectBuilder_VoxelMap(Vector3 position, string storageName)
            : base()
        {
            PositionAndOrientation = new MyPositionAndOrientation(position, Vector3.Forward, Vector3.Up);
            StorageName = storageName;
        }
    }
}