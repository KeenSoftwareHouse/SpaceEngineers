using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public interface IMyQueue<T>
    {
        bool TryDequeueFront(out T value);
    }
}
