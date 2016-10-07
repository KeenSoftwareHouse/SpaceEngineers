using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GhostCharacterDefinition))]
    public class MyGhostCharacterDefinition : MyDefinitionBase
    {
        public List<MyDefinitionId> LeftHandWeapons = new List<MyDefinitionId>();
        public List<MyDefinitionId> RightHandWeapons = new List<MyDefinitionId>();


        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ghostCharacterBuilder = builder as MyObjectBuilder_GhostCharacterDefinition;

            if (ghostCharacterBuilder.LeftHandWeapons != null)
            {
                foreach (var defId in ghostCharacterBuilder.LeftHandWeapons)
                {
                    LeftHandWeapons.Add(defId);
                }
            }

            if (ghostCharacterBuilder.RightHandWeapons != null)
            {
                foreach (var defId in ghostCharacterBuilder.RightHandWeapons)
                {
                    RightHandWeapons.Add(defId);
                }
            }
        }

    }
}
