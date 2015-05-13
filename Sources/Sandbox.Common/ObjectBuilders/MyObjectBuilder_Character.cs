using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using Sandbox.Common.ObjectBuilders.VRageData;


namespace Sandbox.Common.ObjectBuilders
{
    public enum MyCharacterModelEnum
    {
        Soldier = 0,
        Astronaut = 1,
        Astronaut_Black = 2,
        Astronaut_Blue = 3,
        Astronaut_Green = 4,
        Astronaut_Red = 5,
        Astronaut_White = 6,
        Astronaut_Yellow = 7,
    }

    public enum MyCharacterMovementEnum
    {
        Standing = 0,

        Walking = 1,
        BackWalking = 2,
        WalkStrafingLeft = 3,
        WalkStrafingRight = 4,
        WalkingRightFront = 5,
        WalkingRightBack = 6,
        WalkingLeftFront = 7,
        WalkingLeftBack = 8,

        Running = 9,
        Backrunning = 10,
        RunStrafingLeft = 11,
        RunStrafingRight = 12,
        RunningRightFront = 13,
        RunningRightBack = 14,
        RunningLeftFront = 15,
        RunningLeftBack = 16,

        Crouching = 17,
        CrouchWalking = 18,
        CrouchBackWalking = 19,
        CrouchStrafingLeft = 20,
        CrouchStrafingRight = 21,
        CrouchWalkingRightFront = 22,
        CrouchWalkingRightBack = 23,
        CrouchWalkingLeftFront = 24,
        CrouchWalkingLeftBack = 25,

        Sprinting = 26,
        Jump = 27,

        Flying = 28,
        Sitting = 29,

        LadderUp = 30,
        LadderDown = 31,

        RotatingLeft = 32,
        RotatingRight = 33,
        CrouchRotatingLeft = 34,
        CrouchRotatingRight = 35,

        Falling = 36,
        Died = 37,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Character : MyObjectBuilder_EntityBase
    {
        public static Dictionary<string, SerializableVector3> CharacterModels = new Dictionary<string, SerializableVector3>()
        {
            {"Soldier",          new SerializableVector3(0f, 0f, 0.05f)},
            {"Astronaut",        new SerializableVector3(0f, -1f, 0f)},
            {"Astronaut_Black",  new SerializableVector3(0f, -0.96f, -0.5f)},
            {"Astronaut_Blue",   new SerializableVector3(0.575f, 0.15f, 0.2f)},
            {"Astronaut_Green",  new SerializableVector3(0.333f, -0.33f, -0.05f)},
            {"Astronaut_Red",    new SerializableVector3(0f, 0f, 0.05f)},
            {"Astronaut_White",  new SerializableVector3(0f, -0.8f, 0.6f)},
            {"Astronaut_Yellow", new SerializableVector3(0.122f, 0.05f, 0.46f)}
        };

        [ProtoMember(1)]
        public string CharacterModel;

        [ProtoMember(2)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(3)]
        [XmlElement("HandWeapon", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        public MyObjectBuilder_EntityBase HandWeapon;

        [ProtoMember(4)]
        public MyObjectBuilder_Battery Battery;

        [ProtoMember(5)]
        public bool LightEnabled;

        [ProtoMember(6), DefaultValue(true)]
        public bool DampenersEnabled = true;

        [ProtoMember(7)]
        public long? UsingLadder;

        [ProtoMember(8)]
        public SerializableVector2 HeadAngle;

        [ProtoMember(9)]
        public SerializableVector3 LinearVelocity;

        [ProtoMember(10)]
        public float AutoenableJetpackDelay;

        [ProtoMember(11)]
        public bool JetpackEnabled;

        [ProtoMember(12)]
        public float? Health;

        [ProtoMember(13), DefaultValue(false)]
        public bool AIMode = false;
        
        [ProtoMember(14)]
        public SerializableVector3 ColorMaskHSV;

        [ProtoMember(15)]
        public float LootingCounter;

        [ProtoMember(16)]
        public string DisplayName;

        [ProtoMember(17)]
        public bool IsInFirstPersonView = true;

        [ProtoMember(18)]
        public bool EnableBroadcasting = true;

        [ProtoMember(19)]
        public float OxygenLevel = 1f;

        [ProtoMember(20)]
        public MyCharacterMovementEnum MovementState = MyCharacterMovementEnum.Standing;
        public bool ShouldSerializeMovementState() { return MovementState != MyCharacterMovementEnum.Standing; }
    }
}
