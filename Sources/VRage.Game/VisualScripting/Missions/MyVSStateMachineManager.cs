using System.Collections.Generic;
using VRage.Collections;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Missions;
using VRage.ObjectBuilders;

namespace VRage.Game.VisualScripting
{
    public class MyVSStateMachineManager
    {
        private readonly CachingList<MyVSStateMachine> m_runningMachines = new CachingList<MyVSStateMachine>();
        private readonly Dictionary<string, MyObjectBuilder_ScriptSM> m_machineDefinitions = new Dictionary<string, MyObjectBuilder_ScriptSM>();

        public IEnumerable<MyVSStateMachine> RunningMachines
        {
            get { return m_runningMachines; }
        }

        public void Update()
        {
            m_runningMachines.ApplyChanges();
            foreach (var machine in m_runningMachines)
            {
                machine.Update();
                if(machine.ActiveCursorCount == 0)
                {
                    m_runningMachines.Remove(machine);
                    if(MyVisualScriptLogicProvider.MissionFinished != null)
                        MyVisualScriptLogicProvider.MissionFinished(machine.Name);
                }
            }
        }

        public string AddMachine(string filePath)
        {
            MyObjectBuilder_VSFiles ob;
            if(!MyObjectBuilderSerializer.DeserializeXML(filePath, out ob) || ob.StateMachine == null)
                return null;

            if(m_machineDefinitions.ContainsKey(ob.StateMachine.Name))
                return null;

            m_machineDefinitions.Add(ob.StateMachine.Name, ob.StateMachine);
            return ob.StateMachine.Name;
        }

        public bool Run(string machineName, long ownerId = 0)
        {
            MyObjectBuilder_ScriptSM ob;
            if (m_machineDefinitions.TryGetValue(machineName, out ob))
            {
                var machine = new MyVSStateMachine();
                machine.Init(ob, ownerId);
                m_runningMachines.Add(machine);
                if(MyVisualScriptLogicProvider.MissionStarted != null)
                    MyVisualScriptLogicProvider.MissionStarted(machine.Name);

                return true;
            }

            return false;
        }

        public bool Restore(string machineName, IEnumerable<MyObjectBuilder_ScriptSMCursor> cursors)
        {
            MyObjectBuilder_ScriptSM machineDefinition;
            if(!m_machineDefinitions.TryGetValue(machineName, out machineDefinition))
                return false;

            var definitionWithoutCursors = new MyObjectBuilder_ScriptSM
            {
                Name = machineDefinition.Name,
                Nodes = machineDefinition.Nodes,
                Transitions = machineDefinition.Transitions
            };

            var newMachine = new MyVSStateMachine();
            newMachine.Init(definitionWithoutCursors);

            foreach (var newCursorData in cursors)
                if(newMachine.RestoreCursor(newCursorData.NodeName) == null)
                    return false;

            m_runningMachines.Add(newMachine);
            return true;
        }

        public void Dispose()
        {
            foreach (var machine in m_runningMachines)
            {
                machine.Dispose();
            }

            m_runningMachines.Clear();
        }

        public MyObjectBuilder_ScriptStateMachineManager GetObjectBuilder()
        {
            var ob = new MyObjectBuilder_ScriptStateMachineManager
            {
                ActiveStateMachines = new List<MyObjectBuilder_ScriptStateMachineManager.CursorStruct>()
            };

            foreach (var runningMachine in m_runningMachines)
            {
                var activeCursors = runningMachine.ActiveCursors;
                var cursorStorage = new MyObjectBuilder_ScriptSMCursor[activeCursors.Count];
                for (var i = 0; i < activeCursors.Count; i++)
                    cursorStorage[i] = new MyObjectBuilder_ScriptSMCursor { NodeName = activeCursors[i].Node.Name };

                ob.ActiveStateMachines.Add(new MyObjectBuilder_ScriptStateMachineManager.CursorStruct{Cursors = cursorStorage, StateMachineName = runningMachine.Name});
            }

            return ob;
        }
    }
}
