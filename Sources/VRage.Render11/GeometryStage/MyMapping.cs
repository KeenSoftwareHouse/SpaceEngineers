using System;
using SharpDX;
using SharpDX.Direct3D11;
using System.Diagnostics;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;

namespace VRageRender
{
    struct MyMapping
    {
        private MyRenderContext m_rc;
        private Resource m_resource;
        private int m_bufferSize;
        private DataBox m_dataBox;
        private System.IntPtr m_dataPointer;


        #region Static function

        internal static MyMapping MapDiscard(IBuffer buffer)
        {
            return MapDiscard(MyRender11.RC, buffer);
        }

        internal static MyMapping MapDiscard(MyRenderContext rc, IBuffer buffer)
        {
            return MapDiscard(rc, buffer, buffer.Description.SizeInBytes);
        }

        internal static MyMapping MapDiscard(IResource resource)
        {
            Debug.Assert(!(resource is IBuffer), "Using a buffer as a texture.");

            var mapping = MapDiscard(MyRender11.RC, resource, 0);
            if (mapping.m_dataBox.SlicePitch != 0)
                mapping.m_bufferSize = mapping.m_dataBox.SlicePitch;
            else
            {
                Debug.Assert(resource.Size.Y > 0, "Height of resource must be greater than zero");
                mapping.m_bufferSize = mapping.m_dataBox.RowPitch * resource.Size.Y;
            }
            return mapping;
        }

        private static MyMapping MapDiscard(MyRenderContext rc, IResource resource, int bufferSize)
        {
            MyMapping mapping;
            mapping.m_rc = rc;
            mapping.m_resource = resource.Resource;
            mapping.m_bufferSize = bufferSize;
            mapping.m_dataBox = rc.MapSubresource(resource, 0, MapMode.WriteDiscard, MapFlags.None);

            if (mapping.m_dataBox.IsEmpty)
                throw new MyRenderException("Resource mapping failed!");
            mapping.m_dataPointer = mapping.m_dataBox.DataPointer;

            return mapping;
        }

        internal static MyMapping MapRead(IResource resource)
        {
            var mapping = MapRead(MyRender11.RC, resource, 0);
            if (mapping.m_dataBox.SlicePitch != 0)
                mapping.m_bufferSize = mapping.m_dataBox.SlicePitch;
            else if (resource is Texture2D)
            {
                Texture2D tex = resource as Texture2D;
                mapping.m_bufferSize = mapping.m_dataBox.RowPitch * tex.Description.Height;
            }
            else MyRenderProxy.Assert(false);
            return mapping;
        }

        private static MyMapping MapRead(MyRenderContext rc, IResource resource, int bufferSize)
        {
            MyMapping mapping;
            mapping.m_rc = rc;
            mapping.m_resource = resource.Resource;
            mapping.m_bufferSize = bufferSize;
            mapping.m_dataBox = rc.MapSubresource(resource, 0, MapMode.Read, MapFlags.None);

            if (mapping.m_dataBox.IsEmpty)
                throw new MyRenderException("Resource mapping failed!");
            mapping.m_dataPointer = mapping.m_dataBox.DataPointer;

            return mapping;
        }

        #endregion

        #region Member functions

        internal void ReadAndPosition<T>(ref T data) where T : struct
        {
            m_dataPointer = Utilities.ReadAndPosition(m_dataPointer, ref data);
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void WriteAndPosition<T>(ref T data) where T : struct
        {
            m_dataPointer = Utilities.WriteAndPosition(m_dataPointer, ref data);
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void WriteAndPosition<T>(T[] data, int count, int offset = 0) where T : struct
        {
            m_dataPointer = Utilities.Write(m_dataPointer, data, offset, count);
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void WriteAndPositionByRow<T>(T[] data, int count, int offset = 0) where T : struct
        {
            Debug.Assert(count <= m_dataBox.RowPitch);
            Utilities.Write(m_dataPointer, data, offset, count);
            m_dataPointer += m_dataBox.RowPitch;
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void Unmap()
        {
            m_rc.UnmapSubresource(m_resource, 0);
        }

        /*private void LogState<T>(string msg, Exception ex, MyMapping mapping, IntPtr originalPointer, SharpDX.Direct3D11.Buffer buffer, T data) where T : struct
        {
            MyLog.Default.WriteLine(ex);
            MyLog.Default.WriteLine(string.Format("@ {0} DataPointer {1} Original Pointer {2} m_dataBox rowpitch {3} / SlicePitch {4} Buffer {5} ",
                msg, mapping.m_dataBox.DataPointer.ToInt64(), originalPointer.ToInt64(), mapping.m_dataBox.RowPitch, mapping.m_dataBox.SlicePitch,
                string.Format("CB Desc (BindFlags {0}, CpuAccessFlags {1}, OptionFlags {2}, Usage {3}, SizeInBytes {4}, StructureByteStride {5})",
                    buffer.Description.BindFlags, buffer.Description.CpuAccessFlags, buffer.Description.OptionFlags, buffer.Description.Usage,
                    buffer.Description.SizeInBytes, buffer.Description.StructureByteStride)));
        }*/

        #endregion
    }
}
