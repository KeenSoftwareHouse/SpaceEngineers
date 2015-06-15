using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class AlertException : Exception
    {
        public static AlertException Instance()
        {
            return InstanceHolder.instance;
        }
     
        private AlertException()
        {
        }

        private static class InstanceHolder
        {
            public static readonly AlertException instance = new AlertException();
        }
    }
}
