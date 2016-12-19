using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace VRage.Game
{
    public enum MyCharacterModelEnum
    {
        Soldier          = 0,
        Astronaut        = 1,
        Astronaut_Black  = 2,
        Astronaut_Blue   = 3,
        Astronaut_Green  = 4,
        Astronaut_Red    = 5,
        Astronaut_White  = 6,
        Astronaut_Yellow = 7,
    }

    public static class MyCharacterMovement
    {
        public const ushort MovementTypeMask      = 0x000f; // 4 bits (0 - 3) for movement type should be enough even for the future
        public const ushort MovementDirectionMask = 0x03f0; // 6 bits (4 - 9)
        public const ushort MovementSpeedMask     = 0x0c00; // 2 bits (10 - 11)
        public const ushort RotationMask          = 0x3000; // 2 bits (12 - 13)

        // The movement types are mutually exclusive - i.e. you cannot be sitting and crouching at the same time
        public const ushort Standing   = 0;
        public const ushort Sitting    = 1;
        public const ushort Crouching  = 2;
        public const ushort Flying     = 3;
        public const ushort Falling    = 4;
        public const ushort Jump       = 5;
        public const ushort Died       = 6;
        public const ushort Ladder     = 7;

        // Movement direction
        public const ushort NoDirection = 0;
        public const ushort Forward     = 1 << 4;
        public const ushort Backward    = 1 << 5;
        public const ushort Left        = 1 << 6;
        public const ushort Right       = 1 << 7;
        public const ushort Up          = 1 << 8;
        public const ushort Down        = 1 << 9;

        // Movement speed
        public const ushort NormalSpeed = 0;
        public const ushort Fast        = 1 << 10;
        public const ushort VeryFast    = 1 << 11;

        // Rotation
        public const ushort NotRotating   = 0;
        public const ushort RotatingLeft  = 1 << 12;
        public const ushort RotatingRight = 1 << 13;

        public static ushort GetMode(this MyCharacterMovementEnum value)
        {
            return (ushort)((ushort)value & MovementTypeMask);
        }

        public static ushort GetDirection(this MyCharacterMovementEnum value)
        {
            return (ushort)((ushort)value & MovementDirectionMask);
        }

        public static ushort GetSpeed(this MyCharacterMovementEnum value)
        {
            return (ushort)((ushort)value & MovementSpeedMask); 
        }
    }

    public enum MyCharacterMovementEnum : ushort
    {
        Standing   = MyCharacterMovement.Standing,
        Sitting    = MyCharacterMovement.Sitting,
        Crouching  = MyCharacterMovement.Crouching,
        Flying     = MyCharacterMovement.Flying,
        Falling    = MyCharacterMovement.Falling,
        Jump       = MyCharacterMovement.Jump,
        Died       = MyCharacterMovement.Died,
        Ladder     = MyCharacterMovement.Ladder,

        RotatingLeft = MyCharacterMovement.RotatingLeft,
        RotatingRight = MyCharacterMovement.RotatingRight,

        Walking           = MyCharacterMovement.Forward,
        BackWalking       = MyCharacterMovement.Backward,
        WalkStrafingLeft  = MyCharacterMovement.Left,
        WalkStrafingRight = MyCharacterMovement.Right,
        WalkingRightFront = MyCharacterMovement.Right | MyCharacterMovement.Forward,
        WalkingRightBack  = MyCharacterMovement.Right | MyCharacterMovement.Backward,
        WalkingLeftFront  = MyCharacterMovement.Left | MyCharacterMovement.Forward,
        WalkingLeftBack   = MyCharacterMovement.Left | MyCharacterMovement.Backward,

        Running           = MyCharacterMovement.Forward | MyCharacterMovement.Fast,
        Backrunning       = MyCharacterMovement.Backward | MyCharacterMovement.Fast,
        RunStrafingLeft   = MyCharacterMovement.Left | MyCharacterMovement.Fast,
        RunStrafingRight  = MyCharacterMovement.Right | MyCharacterMovement.Fast,
        RunningRightFront = MyCharacterMovement.Right | MyCharacterMovement.Forward | MyCharacterMovement.Fast,
        RunningRightBack  = MyCharacterMovement.Right | MyCharacterMovement.Backward | MyCharacterMovement.Fast,
        RunningLeftFront  = MyCharacterMovement.Left | MyCharacterMovement.Forward | MyCharacterMovement.Fast,
        RunningLeftBack   = MyCharacterMovement.Left | MyCharacterMovement.Backward | MyCharacterMovement.Fast,

        CrouchWalking           = MyCharacterMovement.Forward | MyCharacterMovement.Crouching,
        CrouchBackWalking       = MyCharacterMovement.Backward | MyCharacterMovement.Crouching,
        CrouchStrafingLeft      = MyCharacterMovement.Left | MyCharacterMovement.Crouching,
        CrouchStrafingRight     = MyCharacterMovement.Right | MyCharacterMovement.Crouching,
        CrouchWalkingRightFront = MyCharacterMovement.Right | MyCharacterMovement.Forward | MyCharacterMovement.Crouching,
        CrouchWalkingRightBack  = MyCharacterMovement.Right | MyCharacterMovement.Backward | MyCharacterMovement.Crouching,
        CrouchWalkingLeftFront  = MyCharacterMovement.Left | MyCharacterMovement.Forward | MyCharacterMovement.Crouching,
        CrouchWalkingLeftBack   = MyCharacterMovement.Left | MyCharacterMovement.Backward | MyCharacterMovement.Crouching,
        CrouchRotatingLeft      = MyCharacterMovement.RotatingLeft | MyCharacterMovement.Crouching,
        CrouchRotatingRight     = MyCharacterMovement.RotatingRight | MyCharacterMovement.Crouching,

        Sprinting = MyCharacterMovement.Forward | MyCharacterMovement.VeryFast,

        LadderUp   = MyCharacterMovement.Ladder | MyCharacterMovement.Up,
        LadderDown = MyCharacterMovement.Ladder | MyCharacterMovement.Down,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Character : MyObjectBuilder_EntityBase
    {
        [ProtoContract]
        public struct StoredGas
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public float FillLevel;
        }
        public static Dictionary<string, SerializableVector3> CharacterModels = new Dictionary<string, SerializableVector3>()
        {
            {"Soldier",          new SerializableVector3(0f, 0f, 0.05f)},
            {"Astronaut",        new SerializableVector3(0f, -1f, 0f)},
            {"Astronaut_Black",  new SerializableVector3(0f, -0.96f, -0.5f)},
            {"Astronaut_Blue",   new SerializableVector3(0.575f, 0.15f, 0.2f)},
            {"Astronaut_Green",  new SerializableVector3(0.333f, -0.33f, -0.05f)},
            {"Astronaut_Red",    new SerializableVector3(0f, 0f, 0.05f)},
            {"Astronaut_White",  new SerializableVector3(0f, -0.8f, 0.6f)},
            {"Astronaut_Yellow", new SerializableVector3(0.122f, 0.05f, 0.46f)},
            {"Engineer_suit_no_helmet", new SerializableVector3(-100.0f, -100.0f, -100.0f)} // invalid color, just reuse existing
        };

        [ProtoMember]
        public string CharacterModel;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember]
        [XmlElement("HandWeapon", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        [Nullable, DynamicObjectBuilder]
        public MyObjectBuilder_EntityBase HandWeapon;

        [ProtoMember]
        public MyObjectBuilder_Battery Battery;

        [ProtoMember]
        public bool LightEnabled;

        [ProtoMember, DefaultValue(true)]
        public bool DampenersEnabled = true;

        [ProtoMember, DefaultValue(1f)]
        public float CharacterGeneralDamageModifier = 1f;

        [ProtoMember]
        public long? UsingLadder;

        [ProtoMember]
        public SerializableVector2 HeadAngle;

        [ProtoMember]
        public SerializableVector3 LinearVelocity;

        [ProtoMember]
        public float AutoenableJetpackDelay;

        [ProtoMember]
        public bool JetpackEnabled;

        [ProtoMember]
        [NoSerialize]
        public float? Health;
        public bool ShouldSerializeHealth() { return false; } // Has been moved to MyEntityStatComponent

        [ProtoMember, DefaultValue(false)]
        public bool AIMode = false;
        
        [ProtoMember]
        public SerializableVector3 ColorMaskHSV;

        [ProtoMember]
        public float LootingCounter;

        [ProtoMember]
        public string DisplayName;

        [ProtoMember]
        public bool IsInFirstPersonView = true;

        [ProtoMember]
        public bool EnableBroadcasting = true;

        [ProtoMember]
        public float OxygenLevel = 1f;

        [ProtoMember]
        public float EnvironmentOxygenLevel = 1f;

        [ProtoMember]
        [Nullable]
        public List<StoredGas> StoredGases;

        [ProtoMember]
        public MyCharacterMovementEnum MovementState = MyCharacterMovementEnum.Standing;
        public bool ShouldSerializeMovementState() { return MovementState != MyCharacterMovementEnum.Standing; }

        [ProtoMember]
        [Nullable]
        public List<string> EnabledComponents = null;

        [ProtoMember]
        public ulong PlayerSteamId = 0;
        [ProtoMember]
        public int PlayerSerialId = 0;
        
        [ProtoMember]
        public bool NeedsOxygenFromSuit;
    }
}