using System.Collections.Generic;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptStateMachineManager : MyObjectBuilder_Base
    {
        public struct CursorStruct
        {
            public string StateMachineName;
            public MyObjectBuilder_ScriptSMCursor[] Cursors;
        }

        public List<CursorStruct> ActiveStateMachines;
    }
}
