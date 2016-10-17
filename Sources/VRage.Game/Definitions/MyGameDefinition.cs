using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GameDefinition), typeof(Postprocess))]
    public class MyGameDefinition : MyDefinitionBase
    {
        public static readonly MyDefinitionId Default = new MyDefinitionId(typeof(MyObjectBuilder_GameDefinition), "Default");

        public Dictionary<string, MyDefinitionId?> SessionComponents;
        public static readonly MyGameDefinition DefaultDefinition = new MyGameDefinition()
        {
            Id = Default,
            SessionComponents = new Dictionary<string, MyDefinitionId?>()
        };

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_GameDefinition)builder;

            if (ob.InheritFrom != null)
            {
                var parent = MyDefinitionManagerBase.Static.GetLoadingSet().GetDefinition<MyGameDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_GameDefinition), ob.InheritFrom));

                if (parent == null)
                {
                    MyLog.Default.Error("Could not find parent definition {0} for game definition {1}.", ob.InheritFrom, ob.SubtypeId);
                }
                else
                {
                    SessionComponents = new Dictionary<string, MyDefinitionId?>(parent.SessionComponents);
                }
            }

            if (SessionComponents == null) SessionComponents = new Dictionary<string, MyDefinitionId?>();

            foreach (var def in ob.SessionComponents)
            {
                if(def.Type != null)
                SessionComponents[def.ComponentName] = new MyDefinitionId(MyObjectBuilderType.Parse(def.Type), def.Subtype);
                else
                    SessionComponents[def.ComponentName] = default(MyDefinitionId?);
            }

            if (ob.Default)
            {
                // Make the default this
                SetDefault();
            }
        }

        private void SetDefault()
        {
            var deflt = new MyGameDefinition
            {
                SessionComponents = SessionComponents,
                Id = Default
            };

            MyDefinitionManagerBase.Static.GetLoadingSet().AddOrRelaceDefinition(deflt);
        }

        new class Postprocess : MyDefinitionPostprocessor
        {
            public override void AfterLoaded(ref Bundle definitions)
            {
            }

            public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
            {

                if (!set.ContainsDefinition(Default))
                {
                    set.GetDefinitionsOfType<MyGameDefinition>().First().SetDefault();
                }
            }
        }
    }
}
