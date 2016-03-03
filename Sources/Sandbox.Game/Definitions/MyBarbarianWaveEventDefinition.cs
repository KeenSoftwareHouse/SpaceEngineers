using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.AI.Bot;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BarbarianWaveEventDefinition))]
    public class MyBarbarianWaveEventDefinition : MyGlobalEventDefinition
    {
        public class Wave
        {
            public List<MyDefinitionId> Bots = new List<MyDefinitionId>();

            public Wave(MyObjectBuilder_BarbarianWaveEventDefinition.WaveDef waveOb)
            {
                foreach (var botDef in waveOb.Bots)
                {
                    MyObjectBuilderType botType;
                    if (!MyObjectBuilderType.TryParse(botDef.TypeName, out botType))
                        botType = typeof(MyObjectBuilder_HumanoidBot);
                    Bots.Add(new MyDefinitionId(botType, botDef.SubtypeName));
                }
            }
        }

        private Dictionary<int, Wave> m_waves = new Dictionary<int, Wave>();
        private int m_lastDay = 0;

        public MyBarbarianWaveEventDefinition() { }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_BarbarianWaveEventDefinition;

            foreach (var waveOb in ob.Waves)
            {
                var wave = new Wave(waveOb);
                m_waves.Add(waveOb.Day, wave);

                m_lastDay = Math.Max(m_lastDay, waveOb.Day);
            }
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var defBuilder = base.GetObjectBuilder() as MyObjectBuilder_BarbarianWaveEventDefinition;

            defBuilder.Waves = new MyObjectBuilder_BarbarianWaveEventDefinition.WaveDef[m_waves.Count];

            int i = 0;
            foreach (var entry in m_waves)
            {
                var waveOb = new MyObjectBuilder_BarbarianWaveEventDefinition.WaveDef();
                waveOb.Day = entry.Key;
                waveOb.Bots = new MyObjectBuilder_BarbarianWaveEventDefinition.BotDef[entry.Value.Bots.Count];

                int j = 0;
                foreach (var botDefId in entry.Value.Bots)
                {
                    var botOb = new MyObjectBuilder_BarbarianWaveEventDefinition.BotDef();
                    botOb.SubtypeName = botDefId.SubtypeName;

                    waveOb.Bots[j] = botOb;
                    j++;
                }

                defBuilder.Waves[i] = waveOb;
                i++;
            }

            return defBuilder;
        }

        public int GetBotCount(int dayNumber)
        {
            int botCountExtra = 0;
            if (m_lastDay > 0 && dayNumber >= m_lastDay)
            {
                botCountExtra = dayNumber - m_lastDay;
                dayNumber = m_lastDay;
            }

            Wave wave = null;
            if (!m_waves.TryGetValue(dayNumber, out wave)) return 0;

            return wave.Bots.Count + botCountExtra;
        }

        public MyDefinitionId GetBotDefinitionId(int dayNumber, int botNumber)
        {
            int index = dayNumber;
            if (m_lastDay > 0 && dayNumber >= m_lastDay)
            {
                index = m_lastDay;
            }

            Wave wave = null;
            if (!m_waves.TryGetValue(index, out wave)) return new MyDefinitionId();

            if (wave.Bots.Count > 0)
            {
                return wave.Bots[botNumber % wave.Bots.Count];
            }
            else
            {
                return new MyDefinitionId();
            }
        }
    }
}
