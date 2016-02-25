using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_UseObjectsComponentDefinition))]
    public class MyUseObjectsComponentDefinition : MyComponentDefinitionBase
    {
        public bool LoadFromModel;
        public string UseObjectFromModelBBox;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_UseObjectsComponentDefinition;
            LoadFromModel = ob.LoadFromModel;
            UseObjectFromModelBBox = ob.UseObjectFromModelBBox;
        }
    }
}
