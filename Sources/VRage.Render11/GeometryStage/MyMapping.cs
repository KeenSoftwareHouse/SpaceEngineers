using SharpDX;
using SharpDX.Direct3D11;
using System.Diagnostics;
using VRage.Render11.RenderContext;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VRageRender
{
    struct MyMapping
    {
        private MyRenderContext m_rc;
        private Resource m_buffer;
        private int m_bufferSize;
        private DataBox m_dataBox;
        private System.IntPtr m_dataPointer;

        internal static MyMapping MapDiscard(MyRenderContext rc, Buffer buffer)
        {
            return MapDiscard(rc, buffer, buffer.Description.SizeInBytes);
        }

        internal static MyMapping MapDiscard(Buffer buffer)
        {
            return MapDiscard(MyRender11.RC, buffer, buffer.Description.SizeInBytes);
        }

        internal static MyMapping MapDiscard(Resource buffer)
        {
            var mapping = MapDiscard(MyRender11.RC, buffer, 0);
            if (mapping.m_dataBox.SlicePitch != 0)
                mapping.m_bufferSize = mapping.m_dataBox.SlicePitch;
            else if (buffer is Texture2D)
            {
                Texture2D tex = buffer as Texture2D;
                mapping.m_bufferSize = mapping.m_dataBox.RowPitch * tex.Description.Height;
            }
            else MyRenderProxy.Assert(false);
            return mapping;
        }

        internal static MyMapping MapRead(Resource buffer)
        {
            var mapping = MapRead(MyRender11.RC, buffer, 0);
            if (mapping.m_dataBox.SlicePitch != 0)
                mapping.m_bufferSize = mapping.m_dataBox.SlicePitch;
            else if (buffer is Texture2D)
            {
                Texture2D tex = buffer as Texture2D;
                mapping.m_bufferSize = mapping.m_dataBox.RowPitch * tex.Description.Height;
            }
            else MyRenderProxy.Assert(false);
            return mapping;
        }

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

        internal void WriteAndPosition<T>(T[] data, int offset, int count) where T : struct
        {
            m_dataPointer = Utilities.Write(m_dataPointer, data, offset, count);
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void WriteAndPositionByRow<T>(T[] data, int offset, int count) where T : struct
        {
            Debug.Assert(count <= m_dataBox.RowPitch);
            Utilities.Write(m_dataPointer, data, offset, count);
            m_dataPointer += m_dataBox.RowPitch;
            Debug.Assert((m_dataPointer.ToInt64() - m_dataBox.DataPointer.ToInt64()) <= m_bufferSize);
        }

        internal void Unmap()
        {
            m_rc.UnmapSubresource(m_buffer, 0);
        }

        private static MyMapping MapDiscard(MyRenderContext rc, Resource buffer, int bufferSize)
        {
            MyMapping mapping;
            mapping.m_rc = rc;
            mapping.m_buffer = buffer;
            mapping.m_bufferSize = bufferSize;
            mapping.m_dataBox = rc.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None);

            if (mapping.m_dataBox.IsEmpty)
                throw new MyRenderException("Resource mapping failed!");
            mapping.m_dataPointer = mapping.m_dataBox.DataPointer;

            return mapping;
        }

        private static MyMapping MapRead(MyRenderContext rc, Resource buffer, int bufferSize)
        {
            MyMapping mapping;
            mapping.m_rc = rc;
            mapping.m_buffer = buffer;
            mapping.m_bufferSize = bufferSize;
            mapping.m_dataBox = rc.MapSubresource(buffer, 0, MapMode.Read, MapFlags.None);

            if (mapping.m_dataBox.IsEmpty)
                throw new MyRenderException("Resource mapping failed!");
            mapping.m_dataPointer = mapping.m_dataBox.DataPointer;

            return mapping;
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
    }
}
