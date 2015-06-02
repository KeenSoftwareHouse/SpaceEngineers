using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    [ProtoBuf.ProtoContract]
    public struct MyStringId
    {
        public static readonly MyStringId NullOrEmpty;

        [ProtoBuf.ProtoMember]
        private readonly int m_id;

        private MyStringId(int hash)
        {
            m_id = hash;
        }

        public override string ToString()
        {
            return m_stringById[this];
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

        private static Dictionary<string, MyStringId> m_idByString;
        private static Dictionary<MyStringId, string> m_stringById;

        static MyStringId()
        {
            m_idByString = new Dictionary<string, MyStringId>(50);
            m_stringById = new Dictionary<MyStringId, string>(50, Comparer);

            NullOrEmpty = GetOrCompute("");
            Debug.Assert(NullOrEmpty == default(MyStringId));
            Debug.Assert(NullOrEmpty.m_id == MyUtils.GetHash(null, 0));
            Debug.Assert(NullOrEmpty.m_id == MyUtils.GetHash("", 0));
        }

        public static MyStringId GetOrCompute(string str)
        {
            MyStringId result;
            if (str == null)
            {
                result = NullOrEmpty;
            }
            else if (!m_idByString.TryGetValue(str, out result))
            {
                result = new MyStringId(MyUtils.GetHash(str, 0));
                m_stringById.Add(result, str);
                m_idByString.Add(str, result);
            }

            return result;
        }

        public static MyStringId Get(string str)
        {
            return m_idByString[str];
        }

        public static bool TryGet(string str, out MyStringId id)
        {
            return m_idByString.TryGetValue(str, out id);
        }

        public static MyStringId TryGet(string str)
        {
            MyStringId id;
            m_idByString.TryGetValue(str, out id);
            return id;
        }

        /// <summary>
        /// Think HARD before using this. Usually you should be able to use MyStringId as it is without conversion to int.
        /// </summary>
        public static MyStringId TryGet(int id)
        {
            MyStringId stringId = new MyStringId(id);
            if (m_stringById.ContainsKey(stringId))
                return stringId;
            else
                return MyStringId.NullOrEmpty;
        }

        public static bool IsKnown(MyStringId id)
        {
            return m_stringById.ContainsKey(id);
        }
    }
}
