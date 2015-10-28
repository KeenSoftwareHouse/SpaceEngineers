using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Game.Entities.Character.Components
{
    public static class MyCharacterComponentTypes
    {
        private static Dictionary<MyStringId, Type> m_types = null;

        public static Dictionary<MyStringId, Type> CharacterComponents
        {
            get
            {
                if (m_types == null)
                {
                    m_types = new Dictionary<MyStringId, Type>() 
                    { 
                    { MyStringId.GetOrCompute("RagdollComponent"), typeof(MyCharacterRagdollComponent)},
                    { MyStringId.GetOrCompute("InventorySpawnComponent"), typeof(MyInventorySpawnComponent)},
                    { MyStringId.GetOrCompute("FeetIKComponent"), typeof(MyCharacterFeetIKComponent)},
                    };                    
                }
                return m_types;
            }
        }
    }
}
