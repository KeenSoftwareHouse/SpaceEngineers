using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_DestructionDefinition))]
    public class MyDestructionDefinition : MyDefinitionBase
    {
        public float DestructionDamage;
        public string[] Icons;
        public float ConvertedFractureIntegrityRatio;

        public class MyFracturedPieceDefinition
        {
            public MyDefinitionId Id;
            public int Age; //[s]
        }

        public MyFracturedPieceDefinition[] FracturedPieceDefinitions;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_DestructionDefinition;

            DestructionDamage = ob.DestructionDamage;
            Icons = ob.Icons;
            ConvertedFractureIntegrityRatio = ob.ConvertedFractureIntegrityRatio;

            if (ob.FracturedPieceDefinitions != null && ob.FracturedPieceDefinitions.Length > 0)
            {
                FracturedPieceDefinitions = new MyFracturedPieceDefinition[ob.FracturedPieceDefinitions.Length];
                for (int i = 0; i < ob.FracturedPieceDefinitions.Length; ++i)
                {
                    MyFracturedPieceDefinition def = new MyFracturedPieceDefinition();
                    def.Id = ob.FracturedPieceDefinitions[i].Id;
                    def.Age = ob.FracturedPieceDefinitions[i].Age;

                    FracturedPieceDefinitions[i] = def;
                }
            }
        }

        public void Merge(MyDestructionDefinition src)
        {
            DestructionDamage = src.DestructionDamage;
            Icons = src.Icons;
            ConvertedFractureIntegrityRatio = src.ConvertedFractureIntegrityRatio;
            FracturedPieceDefinitions = src.FracturedPieceDefinitions;
        }
    }
}
