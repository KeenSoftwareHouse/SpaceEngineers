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
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicsBodyComponentDefinition))]
    public class MyPhysicsBodyComponentDefinition : MyPhysicsComponentDefinitionBase
    {
        public bool CreateFromCollisionObject;


        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicsBodyComponentDefinition;
            CreateFromCollisionObject = ob.CreateFromCollisionObject;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_PhysicsBodyComponentDefinition;

            ob.CreateFromCollisionObject = CreateFromCollisionObject;

            return ob;
        }
    }
}
