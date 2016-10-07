#define IN_STACK_STACK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
#if !XB1
using VRage.Service;
#endif // !XB1

namespace VRage.Algorithms
{
    /// <summary>
    ///  Fast representation for disjoint sets.
    /// 
    /// This data structure guarantees virtually constant time operations
    /// for union and finding the representative element of disjoint sets.
    /// </summary>
    /// 
    /// Still wondering weather the iterator makes sence
    public class MyUnionFind //: IEnumerable<MyUnionFind.Element>
    {
        private struct UF
        {
            public int Parent;
            public int Rank;
        }

        UF[] m_indices;

        private int m_size;

        private bool IsInRange(int index)
        {
            return index >= 0 && index < m_size;
        }

        public MyUnionFind()
        { }

        public MyUnionFind(int initialSize)
        {
            Resize(initialSize);
        }

        public void Resize(int count = 0)
        {
            if (m_indices == null || m_indices.Length < count)
                m_indices = new UF[count];

            m_size = count;

            Clear();
        }

        public unsafe void Clear()
        {
            fixed (UF* uf = m_indices)
                for (int i = 0; i < m_size; ++i)
                {
                    uf[i].Parent = i;
                    uf[i].Rank = 0;
                }
        }

        public unsafe void Union(int a, int b)
        {
            fixed (UF* uf = m_indices)
            {
                int aRoot = Find(uf, a);
                int bRoot = Find(uf, b);

                if (aRoot == bRoot) return;

                if (uf[aRoot].Rank < uf[bRoot].Rank)
                    uf[aRoot].Parent = bRoot;
                else if (uf[aRoot].Rank > uf[bRoot].Rank)
                    uf[bRoot].Parent = aRoot;
                else
                {
                    uf[bRoot].Parent = aRoot;
                    uf[aRoot].Rank++;
                }
            }
        }

#if IN_STACK_STACK

        // stack entry for each recursion, reduced recursion cost
        private unsafe struct step
        {
            public step* Prev;
            public int Element;
        }

        // This version is about 10% faster than recursive
        private unsafe int Find(UF* uf, int a)
        {
            Debug.Assert(IsInRange(a));

            step* s = null;

            while (uf[a].Parent != a)
            {
                step* c = stackalloc step[1];
                c->Element = a;
                c->Prev = s;
                s = c;

                a = uf[a].Parent;
            }

            while (s != null)
            {
                uf[s->Element].Parent = a;
                s = s->Prev;
            }

            return a;
        }
#elif THREAD_STATIC_STACK
        // This approach did not work, slower than recursion, here for reference
        [ThreadStatic]
        private static Stack<int> s_stack;

        private unsafe int Find(UF* uf, int a)
        {
            Debug.Assert(IsInRange(a));
            if(s_stack == null)
                s_stack = new Stack<int>();

            while (uf[a].Parent != a)
            {
                s_stack.Push(a);
                a = uf[a].Parent;
            }

            while (s_stack.Count > 0)
            {
                int k = s_stack.Pop();
                uf[k].Parent = a;
            }

            return a;
        }

#else // recursive
        private unsafe int Find(UF* uf, int a)
        {
            Debug.Assert(IsInRange(a));
            if (uf[a].Parent != a)
                uf[a].Parent = Find(uf[a].Parent);

            return uf[a].Parent;
        }
#endif
        public unsafe int Find(int a)
        {
            fixed (UF* uf = m_indices)
                return Find(uf, a);
        }

        /*public struct Element
        {
            public int Index;
            public int Parent;
        }

        public struct Iterator : IEnumerator<Element>
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public Element Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        public IEnumerator<Element> GetEnumerator()
        {
            return new Iterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new 
        }*/
    }

    public static class MyUFTest
    {
        public static void Test()
        {
            var watch = new Stopwatch();
            watch.Start();

            int testSize = 10000000;
            var uf = new MyUnionFind();

            uf.Resize(testSize);

            for (int i = 0; i < testSize; ++i)
            {
                uf.Union(i, i >> 1);
            }

            // Check that it works:
            var papa = uf.Find(0);
            //for(int j = 0; j < 10; ++j)
            for (int i = 0; i < testSize; ++i)
            {
                if (papa != uf.Find(i))
                {
                    File.AppendAllText(@"C:\Users\daniel.ilha\Desktop\perf.log", "FAIL!\n");
#if !XB1
                    Environment.Exit(1);
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                }
            }
            var enlapsed = watch.ElapsedMilliseconds;

            File.AppendAllText(@"C:\Users\daniel.ilha\Desktop\perf.log", string.Format("Test took {0:N}ms\n", enlapsed));
        }
    }
}
