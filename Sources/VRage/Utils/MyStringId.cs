using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    /// <summary>
    /// Generates unique IDs for strings. When used as key for hash tables (Dictionary or HashSet)
    /// always pass in MyStringId.Comparer, otherwise lookups will allocate memory! Never serialize to network or disk!
    /// 
    /// IDs are created sequentially as they get requested so two IDs might be different between sessions or clients and
    /// server. You can safely use ToString() as it will not allocate.
    /// </summary>
    [ProtoBuf.ProtoContract]
    public struct MyStringId
    {
        public static readonly MyStringId NullOrEmpty;

        [ProtoBuf.ProtoMember]
        private readonly int m_id;

        private MyStringId(int id)
        {
            m_id = id;
        }

        public override string ToString()
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_idToString[this];
            }
        }

        public override int GetHashCode()
        {
            return m_id;
        }

        public override bool Equals(object obj)
        {
            return (obj is MyStringId) && Equals((MyStringId)obj);
        }

        public bool Equals(MyStringId id)
        {
            return m_id == id.m_id;
        }

        public static bool operator == (MyStringId lhs, MyStringId rhs) { return lhs.m_id == rhs.m_id; }
        public static bool operator != (MyStringId lhs, MyStringId rhs) { return lhs.m_id != rhs.m_id; }

        public static explicit operator int(MyStringId id) { return id.m_id; }

        #region Comparer
        public class IdComparerType : IComparer<MyStringId>, IEqualityComparer<MyStringId>
        {
            public int Compare(MyStringId x, MyStringId y)
            {
                return x.m_id - y.m_id;
            }

            public bool Equals(MyStringId x, MyStringId y)
            {
                return x.m_id == y.m_id;
            }

            public int GetHashCode(MyStringId obj)
            {
                return obj.m_id;
            }
        }

        public static readonly IdComparerType Comparer = new IdComparerType();
        #endregion

        private static readonly FastResourceLock m_lock;
        private static Dictionary<string, MyStringId> m_stringToId;
        private static Dictionary<MyStringId, string> m_idToString;

        static MyStringId()
        {
            m_lock = new FastResourceLock();
            m_stringToId = new Dictionary<string, MyStringId>(50);
            m_idToString = new Dictionary<MyStringId, string>(50, Comparer);

            NullOrEmpty = GetOrCompute("");
            Debug.Assert(NullOrEmpty == default(MyStringId));
            Debug.Assert(NullOrEmpty.m_id == 0);
        }

        public static MyStringId GetOrCompute(string str)
        {
            MyStringId result;

            using (m_lock.AcquireExclusiveUsing())
            {
                if (str == null)
                {
                    result = NullOrEmpty;
                }
                else if (!m_stringToId.TryGetValue(str, out result))
                {
                    result = new MyStringId(m_stringToId.Count);
                    m_idToString.Add(result, str);
                    m_stringToId.Add(str, result);
                }
            }

            return result;
        }

        public static MyStringId Get(string str)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_stringToId[str];
            }
        }

        public static bool TryGet(string str, out MyStringId id)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_stringToId.TryGetValue(str, out id);
            }
        }

        public static MyStringId TryGet(string str)
        {
            using (m_lock.AcquireSharedUsing())
            {
                MyStringId id;
                m_stringToId.TryGetValue(str, out id);
                return id;
            }
        }

        public static bool IsKnown(MyStringId id)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_idToString.ContainsKey(id);
            }
        }
    }
}
