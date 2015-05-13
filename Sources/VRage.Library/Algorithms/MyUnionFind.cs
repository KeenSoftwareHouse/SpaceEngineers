using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Algorithms
{
    public class MyUnionFind
    {
        List<int> m_indices = new List<int>();

        public void Init(int count = 0)
        {
            Clear();
            for (int i = 0; i < count; ++i)
                m_indices.Add(i);
        }

        public void Clear()
        {
            m_indices.Clear();
        }

        public int MakeSet()
        {
            int newIndex = m_indices.Count;
            m_indices.Add(newIndex);
            return newIndex;
        }

        public void Union(int a, int b)
        {
            int aRoot = Find(a);
            int bRoot = Find(b);

            m_indices[aRoot] = bRoot;
        }

        public int Find(int a)
        {
            if (m_indices[a] != a)
                m_indices[a] = Find(m_indices[a]);

            return m_indices[a];
        }
    }
}
