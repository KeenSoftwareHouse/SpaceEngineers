using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
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

            public Vector3I Min;
            public Vector3I Max;
            public Base6Directions.Direction Forward;
            public Base6Directions.Direction Up;
        }

        public MyMultiBlockPartDefinition[] BlockDefinitions;

        public Vector3I Min;
        public Vector3I Max;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_MultiBlockDefinition;
            MyDebug.AssertDebug(ob != null);

            if (ob.BlockDefinitions != null && ob.BlockDefinitions.Length > 0)
            {
                BlockDefinitions = new MyMultiBlockPartDefinition[ob.BlockDefinitions.Length];
                for (int i = 0; i < ob.BlockDefinitions.Length; ++i)
                {
                    BlockDefinitions[i] = new MyMultiBlockPartDefinition();

                    var obBlockDef = ob.BlockDefinitions[i];
                    BlockDefinitions[i].Id = obBlockDef.Id;
                    BlockDefinitions[i].Min = obBlockDef.Position;
                    BlockDefinitions[i].Forward = obBlockDef.Orientation.Forward;
                    BlockDefinitions[i].Up = obBlockDef.Orientation.Up;
                }
            }
        }
    }
}
