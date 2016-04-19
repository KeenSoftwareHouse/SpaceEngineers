using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ConsumableItemDefinition))]
    public class MyConsumableItemDefinition : MyUsableItemDefinition
    {
        public struct StatValue
        {
            public string Name;
            public float Value;
            public float Time;

            public StatValue(string name, float value, float time)
            {
                Name = name;
                Value = value;
                Time = time;
            }
        }

        public List<StatValue> Stats;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ConsumableItemDefinition;

            Stats = new List<StatValue>();
            if (ob.Stats != null)
            {
                foreach (var stat in ob.Stats)
                {
                    Stats.Add(new StatValue(stat.Name, stat.Value, stat.Time));
                }
            }
        }
    }
}
