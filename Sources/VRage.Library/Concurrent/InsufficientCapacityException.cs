using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    /// <summary>
    /// Used to alert sequence barrier of status changes.
    /// </summary>
    public class InsufficientCapacityException : Exception
    {
        public static InsufficientCapacityException Instance()
        {
            return InstanceHolder.instance;
        }
        /// <summary>
        /// Singleton so not to generate too much garbage.
        /// </summary>
        private InsufficientCapacityException()
        {
        }

        private static class InstanceHolder
        {
            public static readonly InsufficientCapacityException instance = new InsufficientCapacityException();
        }
    }
}
