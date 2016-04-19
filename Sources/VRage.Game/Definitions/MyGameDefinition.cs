using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace VRage.Game.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GameDefinition), typeof(Postprocess))]
    public class MyGameDefinition : MyDefinitionBase
    {
        public static readonly MyDefinitionId Default = new MyDefinitionId(typeof(MyObjectBuilder_GameDefinition), "Default");

        public HashSet<string> SessionComponents;
        public static readonly MyGameDefinition DefaultDefinition = new MyGameDefinition()
        {
            Id = Default,
            SessionComponents = new HashSet<string>()
        };

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_GameDefinition)builder;

            SessionComponents = ob.SessionComponents;

            if (ob.InheritFrom != null)
            {
                var parent = MyDefinitionManagerBase.Static.GetLoadingSet().GetDefinition<MyGameDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_GameDefinition), ob.InheritFrom));

                if (parent == null)
                {
                    Debug.Fail("");
                    //TODO: Use MyLog.Error from planets branch.
                    //MyLog.Default.
                }
                else
                {
                    SessionComponents.UnionWith(parent.SessionComponents);
                }
            }

            if (ob.Default)
            {
                // Make the default this
                SetDefault();
            }
        }

        void SetDefault()
        {
            var deflt = new MyGameDefinition();
            deflt.SessionComponents = SessionComponents;
            deflt.Id = Default;

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
