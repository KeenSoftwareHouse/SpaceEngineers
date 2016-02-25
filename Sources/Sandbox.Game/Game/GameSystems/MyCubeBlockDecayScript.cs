using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Systems;
using VRage.Utils;

namespace Sandbox.Game.GameSystems
{
    [MyScriptedSystem("DecayBlocks")]
    public class MyCubeBlockDecayScript : MyGroupScriptBase
    {
        private HashSet<MyStringHash> m_tmpSubtypes;

        public MyCubeBlockDecayScript()
            :
            base()
        {
            m_tmpSubtypes = new HashSet<MyStringHash>(MyStringHash.Comparer);
        }

        public override void ProcessObjects(ListReader<MyDefinitionId> objects)
        {
            var allEntities = MyEntities.GetEntities();

            m_tmpSubtypes.Clear();
            foreach (var obj in objects)
                m_tmpSubtypes.Add(obj.SubtypeId);


            // Find floating objects in all objects
            foreach (var entity in allEntities)
            {

                MyFloatingObject floatingObject = entity as MyFloatingObject;
                if(floatingObject == null)
                    continue;

                // Check if not manipulated (in hand)
                if (MyManipulationTool.IsEntityManipulated(entity))
                    continue;

                MyDefinitionId defId = floatingObject.Item.Content.GetObjectId();

                // Check if they are marked as removable in Decay.sbc script file.
                if (m_tmpSubtypes.Contains(defId.SubtypeId))
                {
                    MyFloatingObjects.RemoveFloatingObject(floatingObject, true);
                }

            }
        }
    }
}
