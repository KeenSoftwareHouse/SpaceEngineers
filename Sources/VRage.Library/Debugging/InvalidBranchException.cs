using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public class InvalidBranchException : Exception
    {
        public InvalidBranchException()
        {
        }

        public InvalidBranchException(string message)
            :base(message)
        {
        }
    }
}
