using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MultiBlockDefinition))]
    public class MyMultiBlockDefinition : MyDefinitionBase
    {
        public class MyMultiBlockPartDefinition
        {
            public MyDefinitionId Id;

            public Vector3I Position;
            public Base6Directions.Direction Forward;
            public Base6Directions.Direction Up;
        }

        public MyMultiBlockPartDefinition[] BlockDefinitions;

        public Vector3I MinPosition;
        public Vector3I MaxPosition;


        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_MultiBlockDefinition;
            MyDebug.AssertDebug(ob != null);

            if (ob.BlockDefinitions != null && ob.BlockDefinitions.Length > 0)
            {
                MinPosition = Vector3I.MaxValue;
                MaxPosition = Vector3I.MinValue;

                BlockDefinitions = new MyMultiBlockPartDefinition[ob.BlockDefinitions.Length];
                for (int i = 0; i < ob.BlockDefinitions.Length; ++i)
                {
                    BlockDefinitions[i] = new MyMultiBlockPartDefinition();

                    var obBlockDef = ob.BlockDefinitions[i];
                    BlockDefinitions[i].Id = obBlockDef.Id;
                    BlockDefinitions[i].Position = obBlockDef.Position;
                    BlockDefinitions[i].Forward = obBlockDef.Orientation.Forward;
                    BlockDefinitions[i].Up = obBlockDef.Orientation.Up;

                    MinPosition = Vector3I.Min(MinPosition, obBlockDef.Position);
                    MaxPosition = Vector3I.Max(MaxPosition, obBlockDef.Position);
                }
            }
        }

        public int GetMaxSize()
        {
            Vector3I size = MaxPosition - MinPosition + Vector3I.One;
            return Math.Max(Math.Max(size.X, size.Y), size.Z);
        }

    }
}
