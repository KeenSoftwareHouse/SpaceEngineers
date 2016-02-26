using SharpDX;
using SharpDX.Direct3D11;
using System.Diagnostics;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VRageRender
{
    struct MyMapping
    {
        private DeviceContext context;
        private Resource buffer;
        private int bufferSize;
        private DataBox dataBox;
        private System.IntPtr dataPointer;

        internal static MyMapping MapDiscard(DeviceContext context, Buffer buffer)
        {
            return MapDiscard(context, buffer, buffer.Description.SizeInBytes);
        }

        internal static MyMapping MapDiscard(Buffer buffer)
        {
            return MapDiscard(MyRender11.DeviceContext, buffer, buffer.Description.SizeInBytes);
        }
        
        internal static MyMapping MapDiscard(Resource buffer)
        {
            var mapping = MapDiscard(MyRender11.DeviceContext, buffer, 0);
            if (mapping.dataBox.SlicePitch != 0)
                mapping.bufferSize = mapping.dataBox.SlicePitch;
            else if (buffer is Texture2D)
            {
                Texture2D tex = buffer as Texture2D;
                mapping.bufferSize = mapping.dataBox.RowPitch * tex.Description.Height;
            }
            else Debug.Assert(false);
            return mapping;
        }

        internal void WriteAndPosition<T>(ref T data) where T : struct
        {
            dataPointer = Utilities.WriteAndPosition(dataPointer, ref data);
            Debug.Assert((dataPointer.ToInt64() - dataBox.DataPointer.ToInt64()) <= bufferSize);
        }

        internal void WriteAndPosition<T>(T[] data, int offset, int count) where T : struct
        {
            dataPointer = Utilities.Write(dataPointer, data, offset, count);
            Debug.Assert((dataPointer.ToInt64() - dataBox.DataPointer.ToInt64()) <= bufferSize);
        }

        internal void WriteAndPositionByRow<T>(T[] data, int offset, int count) where T : struct
        {
            Debug.Assert(count <= dataBox.RowPitch);
            Utilities.Write(dataPointer, data, offset, count);
            dataPointer += dataBox.RowPitch;
            Debug.Assert((dataPointer.ToInt64() - dataBox.DataPointer.ToInt64()) <= bufferSize);
        }

        internal void Unmap()
        {
            context.UnmapSubresource(buffer, 0);
        }

        private static MyMapping MapDiscard(DeviceContext context, Resource buffer, int bufferSize)
        {
            MyMapping mapping;
            mapping.context = context;
            mapping.buffer = buffer;
            mapping.bufferSize = bufferSize;
            mapping.dataBox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None);

            if (mapping.dataBox.IsEmpty)
                throw new MyRenderException("Resource mapping failed!");
            mapping.dataPointer = mapping.dataBox.DataPointer;

            return mapping;
        }

        /*private void LogState<T>(string msg, Exception ex, MyMapping mapping, IntPtr originalPointer, SharpDX.Direct3D11.Buffer buffer, T data) where T : struct
        {
            MyLog.Default.WriteLine(ex);
            MyLog.Default.WriteLine(string.Format("@ {0} DataPointer {1} Original Pointer {2} dataBox rowpitch {3} / SlicePitch {4} Buffer {5} ",
                msg, mapping.dataBox.DataPointer.ToInt64(), originalPointer.ToInt64(), mapping.dataBox.RowPitch, mapping.dataBox.SlicePitch,
                string.Format("CB Desc (BindFlags {0}, CpuAccessFlags {1}, OptionFlags {2}, Usage {3}, SizeInBytes {4}, StructureByteStride {5})",
                    buffer.Description.BindFlags, buffer.Description.CpuAccessFlags, buffer.Description.OptionFlags, buffer.Description.Usage,
                    buffer.Description.SizeInBytes, buffer.Description.StructureByteStride)));
        }*/
    }
}
