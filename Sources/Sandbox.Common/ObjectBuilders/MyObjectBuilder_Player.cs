using ProtoBuf;
using System;
using VRageMath;
using System.ComponentModel;
using System.Collections.Generic;
using VRage.Serialization;
using Sandbox.Common.ObjectBuilders.VRageData;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders
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

        //[ProtoMember]
        // Obsolete!
        public ulong SteamID;
        public bool ShouldSerializeSteamID() { return false; }

        //[ProtoMember]
        // Obsolete! Dont use dictionaries when not needed
        private SerializableDictionary<long, CameraControllerSettings> m_cameraData;
        public SerializableDictionary<long, CameraControllerSettings> CameraData
        {
            get { Debug.Fail("Obsolete!"); return m_cameraData; }
            set { m_cameraData = value; }
        }
        public bool ShouldSerializeCameraData() { return false; }

        //[ProtoMember]
        // Obsolete!
        public long PlayerEntity;
        public bool ShouldSerializePlayerEntity() { return false; }

        //[ProtoMember]
        // Obsolete!
        public string PlayerModel;
        public bool ShouldSerializePlayerModel() { return false; }

        //[ProtoMember]
        // Obsolete!
        public long PlayerId;
        public bool ShouldSerializePlayerId() { return false; }

        [ProtoMember]
        public string DisplayName;

        [ProtoMember]
        public long IdentityId;

        [ProtoMember]
        public bool Connected;

        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember]
        public long LastActivity;

        [ProtoMember]
        public CameraControllerSettings CharacterCameraData;

        [ProtoMember]
        public List<CameraControllerSettings> EntityCameraData;
    }
}
