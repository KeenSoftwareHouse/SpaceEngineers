using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class NoCallbackExceptionHandler : IExceptionHandler
    {
        private NoCallbackExceptionHandler()
        {
        }
        public void OnException(Exception e, long Sequence)
        {
            Trace.TraceError(e.Message);
            //do nothing
        }

        public static NoCallbackExceptionHandler Instance()
        {
            return InstanceHolder.instance;
        }

        private static class InstanceHolder
        {
            public static readonly NoCallbackExceptionHandler instance = new NoCallbackExceptionHandler();
        }
    }
}
