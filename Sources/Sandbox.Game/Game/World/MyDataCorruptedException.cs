using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.AppCode.Game.Sessions
{
    class MyDataCorruptedException: ApplicationException
    {
        public MyDataCorruptedException()
            :base()
        {
        }

        public MyDataCorruptedException(string message)
            : base(message)
        {
        }

        public MyDataCorruptedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
