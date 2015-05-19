using ProtoBuf;
using System.Diagnostics;
using System.IO;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Voxels
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMap : MyObjectBuilder_EntityBase
    {
        public string Filename
        {
            get { Debug.Fail("Obsolete."); return null; }
            set { StorageName = Path.GetFileNameWithoutExtension(value); }
        }
        public bool ShouldSerializeFilename() { return false; }

        // Obsolete, but because this property has the same name as in base class (by accident),
        // it makes backwards compatibility somewhat confusing.
        public new string Name
        {
            get { return base.Name; }
            set { m_storageName = value; }
        }
        public bool ShouldSerializeName() { return false; }

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