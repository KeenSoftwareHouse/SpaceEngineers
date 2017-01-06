using System;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;
using VRage.Render11.Tools;
using VRageMath;
using VRageRender;
using VRageRender.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using Format = SharpDX.DXGI.Format;
using VRage.Utils;

namespace VRage.Render11.Resources.Buffers
{
    abstract class MyBufferInternal : IBuffer
    {
        internal bool IsReleased;

        int m_elementCount;

        BufferDescription m_description;
        Buffer m_buffer;


        #region IResource overrides

        public string Name { get { return m_buffer.DebugName; } }

        public Vector3I Size3 { get { return new Vector3I(m_elementCount, 1, 1); } }

        public Vector2I Size { get { return new Vector2I(m_elementCount, 1); } }

        public Resource Resource { get { return m_buffer; } }

        #endregion

        #region IBuffer overrides

        public int ByteSize { get { return m_elementCount * m_description.StructureByteStride; } }

        public int ElementCount { get { return m_elementCount; } }

        public BufferDescription Description { get { return m_description; } }

        public Buffer Buffer { get { return m_buffer; } }

        #endregion

        private void LogCreateBuffer(string name, ref BufferDescription description, Exception e)
        {
            MyLog.Default.Error("Error creating buffer: {0}\nName: {1}\nDebug name: {2}\nBufferDescription: [\n", e.ToString(), GetType().Name, Name);
            MyLog.Default.IncreaseIndent();
            MyLog.Default.Error("BindFlags = {0}", description.BindFlags);
            MyLog.Default.Error("CpuAccessFlags = {0}", description.CpuAccessFlags);
            MyLog.Default.Error("OptionFlags = {0}", description.OptionFlags);
            MyLog.Default.Error("SizeInBytes = {0}", description.SizeInBytes);
            MyLog.Default.Error("StructureByteStride = {0}", description.StructureByteStride);
            MyLog.Default.Error("Usage = {0}", description.Usage);
            MyLog.Default.DecreaseIndent();
            MyLog.Default.Error("]");
            MyLog.Default.Flush();

            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "Error creating buffer: {0}\nName: {1}\nDebug name: {2}\nBufferDescription: [\n", e.ToString(), GetType().Name, Name);
            MyRender11.Log.IncreaseIndent();
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "BindFlags = {0}", description.BindFlags);
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "CpuAccessFlags = {0}", description.CpuAccessFlags);
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "OptionFlags = {0}", description.OptionFlags);
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "SizeInBytes = {0}", description.SizeInBytes);
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "StructureByteStride = {0}", description.StructureByteStride);
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "Usage = {0}", description.Usage);
            MyRender11.Log.DecreaseIndent();
            MyRender11.Log.Log(Utils.MyLogSeverity.Error, "]");
            MyRender11.Log.Flush();
        }

        internal void Init(string name, ref BufferDescription description, IntPtr? initData)
        {
            m_description = description;
            m_elementCount = description.SizeInBytes / Math.Max(1, Description.StructureByteStride);

            try
            {
                m_buffer = new Buffer(MyRender11.Device, initData ?? default(IntPtr), description)
                {
                    DebugName = name,
                };
            }
            catch (SharpDXException e)
            {
                if (description.SizeInBytes == 0)
                    MyRenderProxy.Log.WriteLine("Error requesting for buffer with zero size");
                MyRenderProxy.Log.WriteLine("Error during allocation of a directX buffer!");
                LogStuff(e);

                LogCreateBuffer(name, ref description, e);
                throw;
            }
            catch (Exception e)
            {
                LogCreateBuffer(name, ref description, e);
                throw;
            }

            try
            {
                AfterBufferInit();
            }
            catch (SharpDXException e)
            {
                MyRenderProxy.Log.WriteLine("Error during creating a view or an unordered access to a directX buffer!");
                LogStuff(e);
                throw;
            }

            IsReleased = false;
        }

        protected virtual void AfterBufferInit()
        { }

        public virtual void DisposeInternal()
        {
            IsReleased = true;

            m_elementCount = 0;
            m_description = default(BufferDescription);

            if (m_buffer != null)
            {
                m_buffer.Dispose();
                m_buffer = null;
            }
        }


        #region Error logging

        void LogStuff(SharpDXException e)
        {
            if (MyRenderProxy.Log == null)
                return;

            MyRenderProxy.Log.WriteLine("Reason: " + e.Message.Trim());

            if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved)
                MyRenderProxy.Log.WriteLine("Reason: " + MyRender11.Device.DeviceRemovedReason);

            MyRenderProxy.Log.WriteMemoryUsage("");
            MyRenderProxy.Log.WriteLine("Buffer type name: " + GetType().Name);
            MyRenderProxy.Log.WriteLine("Buffer debug name: " + Name);
            MyRenderProxy.Log.WriteLine("Buffer description:\n" + BufferDescriptionToString());


            MyRenderProxy.Log.WriteLine("Exception stack trace: " + e.StackTrace);


            StringBuilder sb = new StringBuilder();
            System.Threading.Thread.Sleep(1000);

            foreach (var column in MyRenderStats.m_stats.Values)
            {
                foreach (var stats in column)
                {
                    sb.Clear();
                    stats.WriteTo(sb);
                    MyRenderProxy.Log.WriteLine(sb.ToString());
                }
            }

            MyStatsUpdater.UpdateStats();
            MyStatsDisplay.WriteTo(sb);
            MyRenderProxy.Log.WriteLine(sb.ToString());
        }

        string BufferDescriptionToString()
        {
            return string.Format(
                "ByteSize:\t{0}\n" +
                "ByteStride:\t{1}\n" +
                "Usage:\t{2}\n" +
                "BindFlags:\t{3}\n" +
                "CpuAccesFlage:\t{4}\n" +
                "OptionsFlags:\t{5}",
                Description.SizeInBytes,
                Description.StructureByteStride,
                Description.Usage,
                Description.BindFlags,
                Description.CpuAccessFlags,
                Description.OptionFlags);
        }

        #endregion
    }

    #region Bindable buffers

    class MySrvBuffer : MyBufferInternal, ISrvBuffer
    {
        ShaderResourceView m_srv;

        public ShaderResourceView Srv
        {
            get { return m_srv; }
        }


        protected override void AfterBufferInit()
        {
            m_srv = new ShaderResourceView(MyRender11.Device, Resource)
            {
                DebugName = Name,
            };
        }

        public override void DisposeInternal()
        {
            if (m_srv != null)
            {
                m_srv.Dispose();
                m_srv = null;
            }

            base.DisposeInternal();
        }
    }

    class MyUavBuffer : MyBufferInternal, IUavBuffer
    {
        UnorderedAccessView m_uav;

        public UnorderedAccessView Uav
        {
            get { return m_uav; }
        }

        public MyUavType UavType { get; set; }


        protected override void AfterBufferInit()
        {
            if (UavType == MyUavType.Default)
                m_uav = new UnorderedAccessView(MyRender11.Device, Resource);
            else
            {
                var description = new UnorderedAccessViewDescription()
                {
                    Buffer = new UnorderedAccessViewDescription.BufferResource()
                    {
                        ElementCount = ElementCount,
                        FirstElement = 0,
                        Flags =
                            UavType == MyUavType.Append
                                ? UnorderedAccessViewBufferFlags.Append
                                : UnorderedAccessViewBufferFlags.Counter
                    },
                    Format = SharpDX.DXGI.Format.Unknown,
                    Dimension = UnorderedAccessViewDimension.Buffer
                };
                m_uav = new UnorderedAccessView(MyRender11.Device, Resource, description);
            }
            m_uav.DebugName = Name + "_Uav";
        }

        public override void DisposeInternal()
        {
            if (m_uav != null)
            {
                m_uav.Dispose();
                m_uav = null;
            }

            base.DisposeInternal();
        }
    }

    class MySrvUavBuffer : MyUavBuffer, ISrvUavBuffer
    {
        ShaderResourceView m_srv;

        public ShaderResourceView Srv
        {
            get { return m_srv; }
        }

        protected override void AfterBufferInit()
        {
            base.AfterBufferInit();

            m_srv = new ShaderResourceView(MyRender11.Device, Resource)
            {
                DebugName = Name + "_Srv"
            };
        }

        public override void DisposeInternal()
        {
            if (m_srv != null)
            {
                m_srv.Dispose();
                m_srv = null;
            }

            base.DisposeInternal();
        }
    }

    class MyIndirectArgsBuffer : MyBufferInternal, IIndirectResourcesBuffer
    {
        UnorderedAccessView m_uav;

        public Format Format { get; set; }

        public UnorderedAccessView Uav
        {
            get { return m_uav; }
        }

        protected override void AfterBufferInit()
        {
            var description = new UnorderedAccessViewDescription()
            {
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = ElementCount,
                    FirstElement = 0,
                    Flags = 0
                },
                Format = Format,
                Dimension = UnorderedAccessViewDimension.Buffer
            };
            m_uav = new UnorderedAccessView(MyRender11.Device, Resource, description)
            {
                DebugName = Name + "_Uav"
            };
        }

        public override void DisposeInternal()
        {
            if (m_uav != null)
            {
                m_uav.Dispose();
                m_uav = null;
            }

            base.DisposeInternal();
        }
    }

    #endregion

    #region Non-bindable buffers

    class MyReadBuffer : MyBufferInternal, IReadBuffer
    { }

    class MyIndexBuffer : MyBufferInternal, IIndexBuffer
    {
        public MyIndexBufferFormat Format { get; set; }
    }

    class MyVertexBuffer : MyBufferInternal, IVertexBuffer
    { }

    class MyConstantBuffer : MyBufferInternal, IConstantBuffer
    { }

    #endregion
}