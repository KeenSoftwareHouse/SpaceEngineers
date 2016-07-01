using System;
using System.Collections.Generic;
using VRage.Utils;

namespace VRage.Game.Definitions
{
    // A object used for postprocessing definitions after they are loaded.
    public abstract class MyDefinitionPostprocessor
    {
        // Priority for this postprocessor
        public virtual int Priority {
            get { return 500; }
        }

        // Type of the definition this instace postprocesses
        // This is assigned by the definition manager, don't set.
        public Type DefinitionType;

        // To simplify argument passing.
        public struct Bundle
        {
            public MyModContext Context;
            public MyDefinitionSet Set;
            public Dictionary<MyStringHash, MyDefinitionBase> Definitions;  // MyStringHash key is subtype id
        }

        // Called after all definitions are loaded
        public abstract void AfterLoaded(ref Bundle definitions);

        // Called after all definitions are loaded and postprocessed
        // No definition may be discarded at this phase.
        public abstract void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions);

        // Called when adding definitions from a mod into the game.
        // By default it simply adds any new definitions and overrites any existing ones.
        public virtual void OverrideBy(ref Bundle currentDefinitions, ref Bundle overrideBySet)
        {
            foreach (var def in overrideBySet.Definitions)
            {
                if (def.Value.Enabled)
                    currentDefinitions.Definitions[def.Key] = def.Value;
                else
                    currentDefinitions.Definitions.Remove(def.Key);
            }
        }

        public class PostprocessorComparer : IComparer<MyDefinitionPostprocessor>
        {
            public int Compare(MyDefinitionPostprocessor x, MyDefinitionPostprocessor y)
            {
                return y.Priority - x.Priority;
            }
        }

        public static PostprocessorComparer Comparer = new PostprocessorComparer();
    }

    public class NullDefinitionPostprocessor : MyDefinitionPostprocessor
    {
        public override void AfterLoaded(ref Bundle definitions)
        {}

        public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
        {}
    }
}
