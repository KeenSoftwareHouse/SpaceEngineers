using ProtoBuf;
using VRageMath;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Player : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class CameraControllerSettings
        {
            [ProtoMember]
            public bool IsFirstPerson;

            [ProtoMember]
            public double Distance;

            [ProtoMember]
            public SerializableVector2? HeadAngle;

            [XmlAttribute]
            public long EntityId;
        }

		[ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string DisplayName;

        [ProtoMember]
        public long IdentityId;

        [ProtoMember]
        public bool Connected;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public CameraControllerSettings CharacterCameraData;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public List<CameraControllerSettings> EntityCameraData;

		[ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
		public List<Vector3> BuildColorSlots = null;
		public bool ShouldSerializeBuildColorSlots() { return BuildColorSlots != null; }

		#region Obsolete

		//[ProtoMember]
		// Obsolete!
        [NoSerialize]
		public ulong SteamID;
		public bool ShouldSerializeSteamID() { return false; }

		//[ProtoMember]
		// Obsolete! Dont use dictionaries when not needed
        [NoSerialize]
        private SerializableDictionary<long, CameraControllerSettings> m_cameraData;
        [NoSerialize]
        public SerializableDictionary<long, CameraControllerSettings> CameraData
        {
            get { /*Debug.Fail("Obsolete!");*/ return m_cameraData; }
            set { m_cameraData = value; }
        }
		public bool ShouldSerializeCameraData() { return false; }

		//[ProtoMember]
		// Obsolete!
        [NoSerialize]
		public long PlayerEntity;
		public bool ShouldSerializePlayerEntity() { return false; }

		//[ProtoMember]
		// Obsolete!
        [NoSerialize]
		public string PlayerModel;
		public bool ShouldSerializePlayerModel() { return false; }

		//[ProtoMember]
		// Obsolete!
        [NoSerialize]
		public long PlayerId;
		public bool ShouldSerializePlayerId() { return false; }

		//[ProtoMember]
        [NoSerialize]
		public long LastActivity;
		public bool ShouldSerializeLastActivity() { return false; }
		#endregion
    }
}
