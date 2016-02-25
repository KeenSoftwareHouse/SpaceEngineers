using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BlueprintClassDefinition))]
    public class MyBlueprintClassDefinition : MyDefinitionBase, IEnumerable<MyBlueprintDefinitionBase>
    {
        public string HighlightIcon;
        public string InputConstraintIcon;
        public string OutputConstraintIcon;
        public string ProgressBarSoundCue = null;

        private SortedSet<MyBlueprintDefinitionBase> m_blueprints;

        private class SubtypeComparer : IComparer<MyBlueprintDefinitionBase>
        {
            public static SubtypeComparer Static = new SubtypeComparer();

            public int Compare(MyBlueprintDefinitionBase x, MyBlueprintDefinitionBase y)
            {
                return x.Id.SubtypeName.CompareTo(y.Id.SubtypeName);
            }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var classBuilder = builder as MyObjectBuilder_BlueprintClassDefinition;

            HighlightIcon = classBuilder.HighlightIcon;
            InputConstraintIcon = classBuilder.InputConstraintIcon;
            OutputConstraintIcon = classBuilder.OutputConstraintIcon;
            ProgressBarSoundCue = classBuilder.ProgressBarSoundCue;

            m_blueprints = new SortedSet<MyBlueprintDefinitionBase>(SubtypeComparer.Static);
        }

        public void AddBlueprint(MyBlueprintDefinitionBase blueprint)
        {
            System.Diagnostics.Debug.Assert(!m_blueprints.Contains(blueprint));
            if (m_blueprints.Contains(blueprint)) return;

            m_blueprints.Add(blueprint);
        }

        public bool ContainsBlueprint(MyBlueprintDefinitionBase blueprint)
        {
            return m_blueprints.Contains(blueprint);
        }

        public IEnumerator<MyBlueprintDefinitionBase> GetEnumerator()
        {
            return m_blueprints.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_blueprints.GetEnumerator();
        }
    }
}
