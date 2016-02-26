using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CockpitDefinition))]
    public class MyCockpitDefinition : MyShipControllerDefinition
    {
        public float OxygenCapacity;
        public bool IsPressurized;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var cbuilder = builder as MyObjectBuilder_CockpitDefinition;

            GlassModel = cbuilder.GlassModel;
            InteriorModel = cbuilder.InteriorModel;

            CharacterAnimation = cbuilder.CharacterAnimation ?? cbuilder.CharacterAnimationFile;

            if (!String.IsNullOrEmpty(cbuilder.CharacterAnimationFile))
            {
                MyDefinitionErrors.Add(Context, "<CharacterAnimation> tag must contain animation name (defined in Animations.sbc) not the file: " + cbuilder.CharacterAnimationFile, TErrorSeverity.Error);
            }

            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(CharacterAnimation));

            OxygenCapacity = cbuilder.OxygenCapacity;
            IsPressurized = cbuilder.IsPressurized;
        }
    }
}
