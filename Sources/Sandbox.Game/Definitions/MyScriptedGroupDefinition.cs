using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ScriptedGroupDefinition))]
    public class MyScriptedGroupDefinition : MyDefinitionBase
    {
        public MyStringHash Category;
        public MyStringHash Script;

        private HashSet<MyDefinitionId> m_scriptedObjects;
        private List<MyDefinitionId> m_scriptedObjectsList;

        public HashSetReader<MyDefinitionId> SetReader { get { return new HashSetReader<MyDefinitionId>(m_scriptedObjects); } }
        public ListReader<MyDefinitionId> ListReader 
        { 
            get 
            {
                if (m_scriptedObjectsList == null)
                    m_scriptedObjectsList = new List<MyDefinitionId>(m_scriptedObjects);
                return new ListReader<MyDefinitionId>(m_scriptedObjectsList); 
            } 
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ScriptedGroupDefinition;

            Category = MyStringHash.GetOrCompute(ob.Category);
            Script = MyStringHash.GetOrCompute(ob.Script);

            m_scriptedObjects = new HashSet<MyDefinitionId>();
        }

        public void Add(MyModContext context, MyDefinitionId obj)
        {
            Debug.Assert(context != null);
            if (context == null)
            {
                MyLog.Default.WriteLine("Writing to scripted group definition without context");
                return;
            }
            m_scriptedObjects.Add(obj);
        }
    }
}
