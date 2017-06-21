using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipControllerDefinition))]
    public class MyShipControllerDefinition : MyCubeBlockDefinition
    {
        public bool EnableFirstPerson;
        public bool EnableShipControl;
        public bool EnableBuilderCockpit;
        public string GlassModel;
        public string InteriorModel;
        public string CharacterAnimation;
        public string GetInSound;
        public string GetOutSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var cbuilder = builder as MyObjectBuilder_ShipControllerDefinition;
            EnableFirstPerson = cbuilder.EnableFirstPerson;
            EnableShipControl = cbuilder.EnableShipControl;
            EnableBuilderCockpit = cbuilder.EnableBuilderCockpit;
            GetInSound = cbuilder.GetInSound;
            GetOutSound = cbuilder.GetOutSound;
        }
    }
}
