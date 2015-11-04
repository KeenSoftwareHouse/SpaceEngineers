using Sandbox.Common.ObjectBuilders;
using System.Collections.Generic;
using VRage;
using VRage.ModAPI;
using VRage.Library.Utils;

namespace Sandbox.Game.Entities
{
    class MyEntityIdRemapHelper : IMyRemapHelper
    {
        static int DEFAULT_REMAPPER_SIZE = 512;

        private Dictionary<long, long> m_oldToNewMap = new Dictionary<long, long>(DEFAULT_REMAPPER_SIZE);
        private Dictionary<string, Dictionary<int, int>> m_groupMap = new Dictionary<string, Dictionary<int, int>>();


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

        public int RemapGroupId(string group, int oldValue) 
        {
            Dictionary<int, int> groupOldToNewMap;
            if (!m_groupMap.TryGetValue(group, out groupOldToNewMap))
            {
                groupOldToNewMap = new Dictionary<int,int>();
                m_groupMap.Add(group, groupOldToNewMap);
            }

            int remapId;
            if (!groupOldToNewMap.TryGetValue(oldValue, out remapId))
            {
                remapId = MyRandom.Instance.Next();
                groupOldToNewMap.Add(oldValue, remapId);
            }

            return remapId;
        }

        public void Clear()
        {
            m_oldToNewMap.Clear();
            m_groupMap.Clear();
        }
    }
}
