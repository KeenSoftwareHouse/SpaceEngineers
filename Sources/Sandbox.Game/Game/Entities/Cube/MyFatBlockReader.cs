using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    public struct MyFatBlockReader<TBlock> : IEnumerator<TBlock>
        where TBlock : MyCubeBlock
    {
        HashSet<MySlimBlock>.Enumerator m_enumerator;

        public MyFatBlockReader(MyCubeGrid grid)
        {
            m_enumerator = grid.GetBlocks().GetEnumerator();
        }

        public MyFatBlockReader<TBlock> GetEnumerator()
        {
            return this;
        }

        public TBlock Current
        {
            get { return (TBlock)m_enumerator.Current.FatBlock; }
        }

        public void Dispose()
        {
            m_enumerator.Dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            while (m_enumerator.MoveNext())
            {
                var block = m_enumerator.Current.FatBlock as TBlock;
                if (block != null)
                    return true;
            }
            return false;
        }

        public void Reset()
        {
            ((IEnumerator<MySlimBlock>)m_enumerator).Reset();
        }
    }
}
