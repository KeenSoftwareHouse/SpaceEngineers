using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    /// <summary>
    /// Generates string hashes deterministically and crashes on collisions. When used as key for hash tables (Dictionary or HashSet)
    /// always pass in MyStringHash.Comparer, otherwise lookups will allocate memory! Can be safely used in network but never serialize to disk!
    /// 
    /// IDs are computed as hash from string so there is a risk of collisions. Use only when MyStringId is
    /// not sufficient (eg. sending over network). Because the odds of collision get higher the more hashes are in use, do not use this for
    /// generated strings and make sure hashes are computed deterministically (eg. at startup) and don't require lengthy gameplay. This way
    /// we know about any collision early and not from rare and random crash reports.
    /// </summary>
    [ProtoBuf.ProtoContract]
    public struct MyStringHash
    {
        public static readonly MyStringHash NullOrEmpty;

        [ProtoBuf.ProtoMember]
        private readonly int m_hash;

        private MyStringHash(int hash)
        {
            m_hash = hash;
        }

        public override string ToString()
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_hashToString[this];
            }
        }

        public override int GetHashCode()
        {
            return m_hash;
        }

        public override bool Equals(object obj)
        {
            return (obj is MyStringHash) && Equals((MyStringHash)obj);
        }

        public bool Equals(MyStringHash id)
        {
            return m_hash == id.m_hash;
        }

        public static bool operator == (MyStringHash lhs, MyStringHash rhs) { return lhs.m_hash == rhs.m_hash; }
        public static bool operator != (MyStringHash lhs, MyStringHash rhs) { return lhs.m_hash != rhs.m_hash; }

        public static explicit operator int(MyStringHash id) { return id.m_hash; }

        #region Comparer
        public class HashComparerType : IComparer<MyStringHash>, IEqualityComparer<MyStringHash>
        {
            public int Compare(MyStringHash x, MyStringHash y)
            {
                return x.m_hash - y.m_hash;
            }

            public bool Equals(MyStringHash x, MyStringHash y)
            {
                return x.m_hash == y.m_hash;
            }

            public int GetHashCode(MyStringHash obj)
            {
                return obj.m_hash;
            }
        }

        public static readonly HashComparerType Comparer = new HashComparerType();
        #endregion

        private static readonly FastResourceLock m_lock;
        private static Dictionary<string, MyStringHash> m_stringToHash;
        private static Dictionary<MyStringHash, string> m_hashToString;

        static MyStringHash()
        {
            m_lock = new FastResourceLock();
            m_stringToHash = new Dictionary<string, MyStringHash>(50);
            m_hashToString = new Dictionary<MyStringHash, string>(50, Comparer);

            NullOrEmpty = GetOrCompute("");
            Debug.Assert(NullOrEmpty == default(MyStringHash));
            Debug.Assert(NullOrEmpty.m_hash == MyUtils.GetHash(null, 0));
            Debug.Assert(NullOrEmpty.m_hash == MyUtils.GetHash("", 0));
        }

        public static MyStringHash GetOrCompute(string str)
        {
            MyStringHash result;

            using (m_lock.AcquireExclusiveUsing())
            {
                if (str == null)
                {
                    result = NullOrEmpty;
                }
                else if (!m_stringToHash.TryGetValue(str, out result))
                {
                    result = new MyStringHash(MyUtils.GetHash(str, 0));
                    m_hashToString.Add(result, str);
                    m_stringToHash.Add(str, result);
                }
            }

            return result;
        }

        public static MyStringHash Get(string str)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_stringToHash[str];
            }
        }

        public static bool TryGet(string str, out MyStringHash id)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_stringToHash.TryGetValue(str, out id);
            }
        }

        public static MyStringHash TryGet(string str)
        {
            using (m_lock.AcquireSharedUsing())
            {
                MyStringHash id;
                m_stringToHash.TryGetValue(str, out id);
                return id;
            }
        }

        /// <summary>
        /// Think HARD before using this. Usually you should be able to use MyStringHash as it is without conversion to int.
        /// </summary>
        public static MyStringHash TryGet(int id)
        {
            using (m_lock.AcquireSharedUsing())
            {
                MyStringHash stringId = new MyStringHash(id);
                if (m_hashToString.ContainsKey(stringId))
                    return stringId;
                else
                    return MyStringHash.NullOrEmpty;
            }
        }

        public static bool IsKnown(MyStringHash id)
        {
            using (m_lock.AcquireSharedUsing())
            {
                return m_hashToString.ContainsKey(id);
            }
        }
    }
}
