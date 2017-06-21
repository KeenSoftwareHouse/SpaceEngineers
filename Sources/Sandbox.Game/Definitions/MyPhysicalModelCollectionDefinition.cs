using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game.Definitions;
using VRage.Utils;
using System;
using VRage.Game;
using VRage.ObjectBuilders;



namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalModelCollectionDefinition))]
    public class MyPhysicalModelCollectionDefinition : MyDefinitionBase
    {
        public MyDiscreteSampler<MyDefinitionId> Items;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicalModelCollectionDefinition;
            MyDebug.AssertDebug(ob != null);
            MyDebug.AssertDebug(ob.Items != null);

            var definitions = new List<MyDefinitionId>();
            var densities = new List<float>();

            foreach (var item in ob.Items)
            {
                Type type = MyObjectBuilderType.ParseBackwardsCompatible(item.TypeId);
                var itemDef = new MyDefinitionId(type, item.SubtypeId);
                definitions.Add(itemDef);
                densities.Add(item.Weight);
            }

            Items = new MyDiscreteSampler<MyDefinitionId>(definitions, densities);
        }
    }

}
