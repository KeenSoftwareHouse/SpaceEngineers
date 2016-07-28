using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Native;
using System.Diagnostics;

namespace VRageRender
{
    static class MyDirect3D9Extensions
    {
#if !XB1
        /// <summary>
        /// RefCountedPointer must be disposed, that will reduce reference count by one and clean resources correctly
        /// </summary>
        public struct RefCountedPointer : IDisposable
        {
            IntPtr m_pointer;

            public RefCountedPointer(IntPtr pointer)
            {
                m_pointer = pointer;
            }
            
            public void Dispose()
            {
                Marshal.Release(m_pointer);
                m_pointer = IntPtr.Zero;
            }

            public static implicit operator IntPtr(RefCountedPointer ptr)
            {
                return ptr.m_pointer;
            }
        }
#endif // !XB1

        public static void LockAndWrite<T>(this VertexBuffer vb, int vbLockOffset, int vbLockSize, LockFlags vbLockFlags, T[] data, int dataOffset, int dataCount)
            where T : struct
        {
            IntPtr ptr = vb.LockToPointer(vbLockOffset, vbLockSize, vbLockFlags);
            Utilities.Write<T>(ptr, data, dataOffset, dataCount);
            vb.Unlock();
        }

#if !XB1
        public static unsafe RefCountedPointer GetSurface(this Texture texture, int level)
        {
            const int GET_SURFACE_LEVEL = 18;
            IntPtr result = IntPtr.Zero;
            IntPtr resultPointer = new IntPtr((void*)&result); // We need address of pointer, because it's out pointer
#if XB1
			Debug.Assert(false);
#else
            ((Result)NativeCall<int>.Method<int, IntPtr>(texture.NativePointer, GET_SURFACE_LEVEL, level, resultPointer)).CheckError();
#endif
            return new RefCountedPointer(result);
        }
#endif // !XB1

        public static unsafe void SetRenderTarget(this Device device, int index, Texture renderTargetTexture, int level)
        {
#if XB1
            Debug.Assert(false);
#else
            using (var surface = GetSurface(renderTargetTexture, level))
            {

                const int SET_RENDER_TARGET = 37;
                ((Result)NativeCall<int>.Method<int, IntPtr>(device.NativePointer, SET_RENDER_TARGET, index, surface)).CheckError();
			}
#endif
        }

        public static void SetDepthStencil(this Device device, Texture depthTexture, int level)
        {
#if XB1
			Debug.Assert(false);
#else
            using (var surface = GetSurface(depthTexture, level))
            {
                const int SET_DEPTH_STENCIL = 39;
                ((Result)NativeCall<int>.Method<IntPtr>(device.NativePointer, SET_DEPTH_STENCIL, surface)).CheckError();
			}
#endif
        }
    }
}
