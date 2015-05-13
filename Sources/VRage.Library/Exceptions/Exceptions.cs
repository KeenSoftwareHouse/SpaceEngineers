using System;
using System.Diagnostics;

namespace VRage
{
    /// <summary>
    /// Provides a set of methods that help throwing exceptions. This class cannot be inherited.
    /// </summary>
    public static class Exceptions
    {
        /// <summary>
        /// Specifies a condition and throws an exception with the provided message if the condition is true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="condition">The conditional expression to test.</param>
        [DebuggerStepThrough]
        public static void ThrowIf<TException>(bool condition) where TException : Exception
        {
            if (condition)
            {
                throw (Exception)Activator.CreateInstance(typeof(TException));
            }
        }

        /// <summary>
        /// Specifies a condition and throws an exception with the provided message if the condition is true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="condition">The conditional expression to test.</param>
        /// <param name="arg1">The arg1.</param>
        [DebuggerStepThrough]
        public static void ThrowIf<TException>(bool condition, string arg1) where TException : Exception
        {
            if (condition)
            {
                throw (Exception)Activator.CreateInstance(typeof(TException), arg1);
            }
        }

        /// <summary>
        /// Specifies a condition and throws an exception with the provided message if the condition is true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="condition">The conditional expression to test.</param>
        /// <param name="arg1">The arg1.</param>
        /// <param name="arg2">The arg2.</param>
        [DebuggerStepThrough]
        public static void ThrowIf<TException>(bool condition, string arg1, string arg2) where TException : Exception
        {
            if (condition)
            {
                throw (Exception)Activator.CreateInstance(typeof(TException), arg1, arg2 );
            }
        }

        /// <summary>
        /// Specifies a condition and throws an exception with the provided message if the condition is true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="condition">The conditional expression to test.</param>
        /// <param name="args">Exception arguments.</param>
        [DebuggerStepThrough]
        public static void ThrowIf<TException>(bool condition, params object[] args) where TException : Exception
        {
            if (condition)
            {
                throw (Exception)Activator.CreateInstance(typeof(TException), args); 
            }
        }

        /// <summary>
        /// Specifies a conditions and throws an exception with the provided message if any of the conditions is true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="conditions">The conditional expression to test.</param>
        /// <param name="args">Exception arguments.</param>
        [DebuggerStepThrough]
        public static void ThrowAny<TException>(bool[] conditions, params object[] args) where TException : Exception
        {
            for (uint i = 0; i < conditions.Length; ++i)
            {
                if (conditions[i])
                {
                    throw (Exception)Activator.CreateInstance(typeof(TException), args);
                }
            }
        }

        /// <summary>
        /// Specifies a conditions and throws an exception with the provided message if all conditions are true.
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is true.</typeparam>
        /// <param name="conditions">The conditional expression to test.</param>
        /// <param name="args">Exception arguments.</param>
        [DebuggerStepThrough]
        public static void ThrowAll<TException>(bool[] conditions, params object[] args) where TException : Exception
        {
            for (uint i = 0; i < conditions.Length; ++i)
            {
                if (!conditions[i])
                {
                    return;
                }
            }

            throw (Exception)Activator.CreateInstance(typeof(TException), args);
        }
    }
}
