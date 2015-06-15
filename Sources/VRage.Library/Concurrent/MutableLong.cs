using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class MutableLong
    {
        /// <summary>
        /// Internal value
        /// </summary>
        public long Value { get; set; }

        ///<summary>
        /// Create a new instance of a mutable long
        ///</summary>
        ///<param name="value"></param>

        public MutableLong(long value)
        {
            Value = value;
        }
    }
}