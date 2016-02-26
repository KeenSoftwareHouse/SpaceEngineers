using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_FactionDefinition))]
    public class MyFactionDefinition : MyDefinitionBase
    {
        public string Tag;
        public string Name;
        public string Founder;
        public bool AcceptHumans;
        public bool AutoAcceptMember;
        public bool EnableFriendlyFire;
        public bool IsDefault;
        public MyRelationsBetweenFactions DefaultRelation;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_FactionDefinition;

            Tag = ob.Tag;
            Name = ob.Name;
            Founder = ob.Founder;
            AcceptHumans = ob.AcceptHumans;
            AutoAcceptMember = ob.AutoAcceptMember;
            EnableFriendlyFire = ob.EnableFriendlyFire;
            IsDefault = ob.IsDefault;
            DefaultRelation = ob.DefaultRelation;
        }

        public override void Postprocess()
        {
            base.Postprocess();

            MyDefinitionManager.Static.RegisterFactionDefinition(this);
        }
    }
}
