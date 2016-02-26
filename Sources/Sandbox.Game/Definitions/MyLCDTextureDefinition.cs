using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LCDTextureDefinition))]
    public class MyLCDTextureDefinition : MyDefinitionBase
    {
        public string TexturePath;
 
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var LCDCategoryBuilder = builder as MyObjectBuilder_LCDTextureDefinition;
            if (LCDCategoryBuilder != null)
            {
                this.TexturePath = LCDCategoryBuilder.TexturePath;
            }
        }
    }
}
