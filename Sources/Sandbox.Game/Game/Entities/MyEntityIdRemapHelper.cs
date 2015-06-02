using Sandbox.Common.ObjectBuilders;
using System.Collections.Generic;
using VRage;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    class MyEntityIdRemapHelper : IMyRemapHelper
    {
        static int DEFAULT_REMAPPER_SIZE = 512;

        private Dictionary<long, long> m_oldToNewMap = new Dictionary<long, long>(DEFAULT_REMAPPER_SIZE);
        
        public long RemapEntityId(long oldEntityId)
        {
            long retval;

            if (!m_oldToNewMap.TryGetValue(oldEntityId, out retval))
            {
                retval = MyEntityIdentifier.AllocateId();
                m_oldToNewMap.Add(oldEntityId, retval);
            }

            return retval;
        }

        public void Clear()
        {
            m_oldToNewMap.Clear();
        }
    }
}
