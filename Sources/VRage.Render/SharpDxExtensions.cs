using SharpDX.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDX
{
    public static class SharpDxExtensions
    {
        static string[] ignoredTypes = new string[] 
        {
            "SharpDX.XAPO.ParameterProviderNative", // This never gets disposed, bug in SharpDX? Not a big problem, it's in audio and it's created once.
        };

        public static ObjectReference[] IgnoreKnownIssues(this List<ObjectReference> activeObjects)
        {
            return activeObjects.Where(s => !ignoredTypes.Contains(s.Object.Target.GetType().FullName)).ToArray();
        }
    }
}

namespace SharpDX.Direct3D9
{
    public static class SharpDxExtensions
    {
        /// <summary>
        /// Discard is valid only for dynamic buffers
        /// </summary>
        public static void SetData<T>(this VertexBuffer vertexBuffer, T[] data, LockFlags flags = LockFlags.None, int numDataToWrite = -1)
            where T : struct
        {
            if (numDataToWrite == -1)
                numDataToWrite = data.Length;

            int itemSize = Utilities.SizeOf<T>();
            var ptr = vertexBuffer.LockToPointer(0, itemSize * numDataToWrite, flags);
            Utilities.Write(ptr, data, 0, numDataToWrite);
            vertexBuffer.Unlock();
        }

        /// <summary>
        /// Discard is valid only for dynamic buffers
        /// </summary>
        public static void SetData<T>(this IndexBuffer indexBuffer, T[] data, LockFlags flags = LockFlags.None)
            where T : struct
        {
            int itemSize = Utilities.SizeOf<T>();
            var ptr = indexBuffer.LockToPointer(0, itemSize * data.Length, flags);
            Utilities.Write(ptr, data, 0, data.Length);
            indexBuffer.Unlock();
        }
    }
}
