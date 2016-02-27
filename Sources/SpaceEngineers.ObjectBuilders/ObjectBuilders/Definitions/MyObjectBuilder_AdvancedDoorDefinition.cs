using ProtoBuf;
using VRageMath;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AdvancedDoorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoContract]
        public class Opening
        {
            public enum MoveType
            {
                Slide,
                Rotate
            }

            public enum Sequence
            {
                Linear
            }

            public enum Rotation
            {
                X,
                Y,
                Z
            }

            [ProtoMember]
            public int ID = -1;

            [ProtoMember]
            public string IDs = "";

            [ProtoMember]
            public MoveType Move = MoveType.Slide;

            [ProtoMember]
            public Sequence SequenceType = Sequence.Linear;

            // How fast opens this Subpart?
            [ProtoMember]
            public float Speed = 1f;

            // In what direction does this Subpart Move? (i.e. Up/Down/Left/Right)
            [ProtoMember]
            public SerializableVector3 SlideDirection = Vector3.Zero;

            [ProtoMember]
            public float OpenDelay = 0f;

            [ProtoMember]
            public float CloseDelay = 0f;

            /// <summary>
            /// For Sliding Parts = value in meter
            /// For Rotating Parts = value in Degrees
            /// </summary>
            [ProtoMember]
            public float MaxOpen = 1f;

            [ProtoMember]
            public Rotation RotationAxis = Rotation.X;

            /// <summary>
            /// override the Pivot/Hinge for this opening
            /// will be read from Model if not defined
            /// </summary>
            [ProtoMember]
            public SerializableVector3? PivotPosition = null;

            [ProtoMember]
            public bool InvertRotation = false;

            [ProtoMember]
            public string OpenSound = "";

            [ProtoMember]
            public string CloseSound = "";
        }

        [ProtoContract]
        public class SubpartDefinition
        {
            /// <summary>
            /// Name of the Subpart Model without extension i.e.:
            /// "DoorLeft" will be "path/to/model/DoorLeft.mwm"
            /// </summary>
            [ProtoMember]
            public string Name;

            /// <summary>
            /// define the Pivot/Hinge position for this Subpart
            /// will be read from Model if not defined
            /// </summary>
            [ProtoMember]
            public SerializableVector3? PivotPosition = null;
        }

        [ProtoMember]
        public SubpartDefinition[] Subparts;

        [ProtoMember]
        public Opening[] OpeningSequence;

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float PowerConsumptionIdle;

        [ProtoMember]
        public float PowerConsumptionMoving;
    }
}
