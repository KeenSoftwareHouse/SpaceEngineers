using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Extensions
{
    public struct ArrayOfTypeEnumerator<T, TInner, TOfType> : IEnumerator<TOfType>
        where TInner : struct, IEnumerator<T>
        where TOfType : T
    {
        TInner m_inner;

        public ArrayOfTypeEnumerator(TInner enumerator)
        {
            m_inner = enumerator;
        }

        /// <summary>
        /// So we can put this into foreach
        /// </summary>
        public ArrayOfTypeEnumerator<T, TInner, TOfType> GetEnumerator()
        {
            return this;
        }

        public TOfType Current
        {
            get { return (TOfType)m_inner.Current; }
        }

        public void Dispose()
        {
            m_inner.Dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                Debug.Fail("Possible boxing!");
                return m_inner.Current;
            }
        }

        public bool MoveNext()
        {
            while (m_inner.MoveNext())
            {
                // This generates IL instruction 'box' which gets skipped, because T is always reference type
                // http://stackoverflow.com/questions/20683715/why-is-box-instruction-emitted-for-generic
                if (m_inner.Current is TOfType)
                {
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            m_inner.Reset();
        }
    }
}
