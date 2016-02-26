using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BotDefinition))]
    public class MyBotDefinition : MyDefinitionBase
    {
        public MyDefinitionId BotBehaviorTree;
        public string BehaviorType;
        public string BehaviorSubtype;
        public MyDefinitionId TypeDefinitionId;
        public bool Commandable;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_BotDefinition;
            this.BotBehaviorTree = new MyDefinitionId(ob.BotBehaviorTree.Type, ob.BotBehaviorTree.Subtype);
            this.BehaviorType = ob.BehaviorType;
            this.TypeDefinitionId = new MyDefinitionId(ob.TypeId, ob.SubtypeName);
            if (string.IsNullOrWhiteSpace(ob.BehaviorSubtype))
                this.BehaviorSubtype = ob.BehaviorType;
            else
                this.BehaviorSubtype = ob.BehaviorSubtype;
            Commandable = ob.Commandable;
        }

        public virtual void AddItems(Sandbox.Game.Entities.Character.MyCharacter character)
        {

        }

    }
}
