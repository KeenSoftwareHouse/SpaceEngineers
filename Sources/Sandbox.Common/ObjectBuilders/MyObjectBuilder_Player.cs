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
            [ProtoMember(1)]
            public bool IsFirstPerson;

            [ProtoMember(2)]
            public double Distance;

            [ProtoMember(3)]
            public SerializableVector2? HeadAngle;

            [XmlAttribute]
            public long EntityId;
        }

        //[ProtoMember(1)]
        // Obsolete!
        public ulong SteamID;
        public bool ShouldSerializeSteamID() { return false; }

        //[ProtoMember(1)]
        // Obsolete! Dont use dictionaries when not needed
        private SerializableDictionary<long, CameraControllerSettings> m_cameraData;
        public SerializableDictionary<long, CameraControllerSettings> CameraData
        {
            get { Debug.Fail("Obsolete!"); return m_cameraData; }
            set { m_cameraData = value; }
        }
        public bool ShouldSerializeCameraData() { return false; }

        //[ProtoMember(3)]
        // Obsolete!
        public long PlayerEntity;
        public bool ShouldSerializePlayerEntity() { return false; }

        //[ProtoMember(4)]
        // Obsolete!
        public string PlayerModel;
        public bool ShouldSerializePlayerModel() { return false; }

        //[ProtoMember(5)]
        // Obsolete!
        public long PlayerId;
        public bool ShouldSerializePlayerId() { return false; }

        [ProtoMember(1)]
        public string DisplayName;

        [ProtoMember(2)]
        public long IdentityId;

        [ProtoMember(3)]
        public bool Connected;

        [ProtoMember(4)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember(5)]
        public long LastActivity;

        [ProtoMember(6)]
        public CameraControllerSettings CharacterCameraData;

        [ProtoMember(7)]
        public List<CameraControllerSettings> EntityCameraData;
    }
}
