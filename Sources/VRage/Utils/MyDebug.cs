using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using VRageMath;
using SystemTrace = System.Diagnostics.Trace;

namespace VRage.Utils
{
    public class MyDebug
    {
        /// <summary>
        /// This "assert" is executed in DEBUG and RELEASE modes. Use it in code that that won't suffer from more work (e.g. loading), not in frequently used loops
        /// </summary>
        /// <param name="condition"></param>
        public static void AssertRelease(bool condition)
        {
            AssertRelease(condition, "Assertion failed");
        }

        /// <summary>
        /// This "assert" is executed in DEBUG and RELEASE modes. Use it in code that that won't suffer from more work (e.g. loading), not in frequently used loops
        /// </summary>
        /// <param name="condition"></param>
        public static void AssertRelease(bool condition, string assertMessage)
        {
            if (condition == false)
            {
                MyLog.Default.WriteLine("Assert: " + assertMessage);
                SystemTrace.Fail(assertMessage);
            }
        }

        /// <summary>
        /// Logs the message on release and also displays a message on DEBUG.
        /// </summary>
        /// <param name="message"></param>
        public static void FailRelease(string message)
        {
            MyLog.Default.WriteLine("Assert Fail: " + message);
            SystemTrace.Fail(message);
        }

        public static void FailRelease(string format, params object[] args)
        {
            string message = String.Format(format, args);
            MyLog.Default.WriteLine("Assert Fail: " + message);
            SystemTrace.Fail(message);
        }

        /// <summary>
        /// This "assert" is executed in DEBUG mode. Because people dont know how to use AssertRelease!
        /// </summary>
        /// <param name="condition"></param>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void AssertDebug(bool condition)
        {
            Debug.Assert(condition);
        }

        /// <summary>
        /// This "assert" is executed in DEBUG mode. Because people dont know how to use AssertRelease!
        /// </summary>
        /// <param name="condition"></param>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void AssertDebug(bool condition, string assertMessage)
        {
            Debug.Assert(condition, assertMessage);
        }

        /// <summary>
        /// Returns true if float is valid
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static bool IsValid(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        /// <summary>
        /// Returns true if Vector3 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static bool IsValid(Vector3 vec)
        {
            return IsValid(vec.X) && IsValid(vec.Y) && IsValid(vec.Z);
        }

        public static bool IsValidNormal(Vector3 vec)
        {
            const float epsilon = 0.001f;
            var length = vec.LengthSquared();
            return IsValid(vec) && length > 1 - epsilon && length < 1 + epsilon;
        }

        /// <summary>
        /// Returns true if Vector2 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static bool IsValid(Vector2 vec)
        {
            return IsValid(vec.X) && IsValid(vec.Y);
        }

        /// <summary>
        /// Returns true if Vector3 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static bool IsValid(Vector3? vec)
        {
            return vec == null ? true : IsValid(vec.Value.X) && IsValid(vec.Value.Y) && IsValid(vec.Value.Z);
        }

        public static bool IsValid(Matrix matrix)
        {
            return IsValid(matrix.Up) && IsValid(matrix.Left) && IsValid(matrix.Forward) && IsValid(matrix.Translation) && (matrix != Matrix.Zero);
        }

        public static bool IsValid(Quaternion q)
        {
            return IsValid(q.X) && IsValid(q.Y) && IsValid(q.Z) && IsValid(q.W) &&
                !MyUtils.IsZero(q);
        }

        /// <summary>
        /// Returns true if Vector3 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static void AssertIsValid(Vector3 vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }

        /// <summary>
        /// Returns true if Vector3 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static void AssertIsValid(Vector3? vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }

        /// <summary>
        /// Returns true if Vector2 is valid
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static void AssertIsValid(Vector2 vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }

        /// <summary>
        /// Returns true if float is valid
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static void AssertIsValid(float f)
        {
            System.Diagnostics.Debug.Assert(IsValid(f));
        }

        public static void AssertIsValid(Matrix matrix)
        {
            System.Diagnostics.Debug.Assert(IsValid(matrix));
        }

        public static void AssertIsValid(Quaternion q)
        {
            System.Diagnostics.Debug.Assert(IsValid(q));
        }

    }
}
