using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library;

namespace ParallelTasks
{
    /// <summary>
    /// An exception thrown when an unhandled exception is thrown within a task.
    /// </summary>
#if WINDOWS
    [global::System.Serializable]
#endif
    public class TaskException : Exception
    {
        /// <summary>
        /// Gets an array containing any unhandled exceptions that were thrown by the task.
        /// </summary>
        public Exception[] InnerExceptions { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="TaskException"/> class.
        /// </summary>
        /// <param name="inner">The unhandled exceptions thrown by the task.</param>
        public TaskException(Exception[] inner) 
            : base("An exception(s) was thrown while executing a task.", null) 
        {
            InnerExceptions = inner;
        }

        public override string ToString()
        {
            string result = base.ToString() + MyEnvironment.NewLine;
            for (int i = 0; i < InnerExceptions.Length; i++)
            {
                result += String.Format("Task exception, inner exception {0}:", i) + MyEnvironment.NewLine;
                result += InnerExceptions[i].ToString();
            }
            return result;
        }
    }
}
