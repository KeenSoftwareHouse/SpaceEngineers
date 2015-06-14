﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;
using VRageMath;
using Sandbox.Common.ObjectBuilders.VRageData;
using VRage.ObjectBuilders;
using VRage;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
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

            public enum Rotation
            {
                X,
                Y,
                Z
            }

            [ProtoMember]
            public string IDs = "";

            [ProtoMember]
            public MoveType Move = MoveType.Slide;

            [ProtoMember]
            public float Speed = 1f;

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

            [ProtoMember]
            public bool HasPhysics = true;
        }

        [ProtoMember]
        public SubpartDefinition[] Subparts;

        [ProtoMember]
        public Opening[] OpeningSequence;

        [ProtoMember]
        public float PowerConsumptionIdle;

        [ProtoMember]
        public float PowerConsumptionMoving;

        [ProtoMember]
        public bool Autoclose = false;

        [ProtoMember]
        public SerializableBounds AutocloseInterval = new SerializableBounds(1f, 10f, 2f);
    }
}
