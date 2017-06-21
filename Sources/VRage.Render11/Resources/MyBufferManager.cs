using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct3D11;
using VRage.Generics;
using VRage.Library.Collections;
using VRage.Render11.Common;
using VRage.Render11.Resources.Buffers;
using VRageRender;
using Buffer = SharpDX.Direct3D11.Buffer;
using Format = SharpDX.DXGI.Format;

namespace VRage.Render11.Resources
{
    internal enum MyIndexBufferFormat
    {
        UShort = SharpDX.DXGI.Format.R16_UInt,
        UInt = SharpDX.DXGI.Format.R32_UInt,
    }


    #region Bindable buffer interfaces

    internal interface ISrvBuffer : ISrvBindable, IBuffer
    { }

    internal interface IUavBuffer : IUavBindable, IBuffer
    {
        MyUavType UavType { get; }
    }

    internal interface ISrvUavBuffer : ISrvUavBindable, ISrvBuffer, IUavBuffer
    { }

    internal interface IIndirectResourcesBuffer : IUavBindable, IBuffer
    { }

    #endregion

    #region Non-bindable buffer interfaces

    internal interface IReadBuffer : IBuffer
    { }

    internal interface IIndexBuffer : IBuffer
    {
        MyIndexBufferFormat Format { get; }
    }

    internal interface IVertexBuffer : IBuffer
    { }

    internal interface IConstantBuffer : IBuffer
    { }

    #endregion


    internal enum MyUavType
    {
        Default,
        Append,
        Counter
    }

    struct MyBufferStatistics
    {
        public string Name;
        public int TotalBuffers;
        public int TotalBytes;
        public int TotalBuffersPeak;
        public int TotalBytesPeak;
    }

    class MyBufferManager : IManager, IManagerDevice
    {
        #region Pivate pool class

        interface IMyBufferPool
        {
            MyBufferStatistics Statistics { get; }

            int ActiveCount { get; }
            //IEnumerable<IBuffer> ActiveBuffers { get; }
            void DisposeAll();
        }

        class MyBufferPool<T> : MyConcurrentObjectsPool<T>, IMyBufferPool
            where T : class, IBuffer, new()
        {
            private MyBufferStatistics m_statistics;

            public MyBufferPool(string name, int baseCapacity)
                : base(baseCapacity)
            {
                m_statistics.Name = name;
            }


            #region IMyBufferPool overrides

            public MyBufferStatistics Statistics
            {
                get { return m_statistics; }
                private set { m_statistics = value; }
            }

            int IMyBufferPool.ActiveCount { get { return ActiveCount; } }

            Action<T> m_actionDisposeInternal = delegate(T buffer) { buffer.DisposeInternal(); };

            public void DisposeAll()
            {
                ApplyActionOnAllActives(m_actionDisposeInternal);

                DeallocateAll();
            }

            #endregion

            #region Logging

            public void LogAllocate(ref BufferDescription description)
            {
                m_statistics.TotalBuffers++;
                m_statistics.TotalBytes += description.SizeInBytes;
                m_statistics.TotalBuffersPeak = Math.Max(m_statistics.TotalBuffersPeak, m_statistics.TotalBuffers);
                m_statistics.TotalBytesPeak = Math.Max(m_statistics.TotalBytesPeak, m_statistics.TotalBytes);
            }

            public void LogDispose(IBuffer buffer)
            {
                m_statistics.TotalBuffers--;
                m_statistics.TotalBytes -= buffer.Description.SizeInBytes;
            }

            public void LogResize(IBuffer originalBuffer, ref BufferDescription newDescription)
            {
                LogDispose(originalBuffer);
                LogAllocate(ref newDescription);
            }

            #endregion
        }

        #endregion

        #region Fields

        bool m_isDeviceInit;

        readonly MyBufferPool<MySrvBuffer> m_srvBuffers = new MyBufferPool<MySrvBuffer>("Srv", 64);
        readonly MyBufferPool<MyUavBuffer> m_uavBuffers = new MyBufferPool<MyUavBuffer>("Uav", 64);
        readonly MyBufferPool<MySrvUavBuffer> m_srvUavBuffers = new MyBufferPool<MySrvUavBuffer>("SrvUav", 64);
        readonly MyBufferPool<MyIndirectArgsBuffer> m_indirectArgsBuffers = new MyBufferPool<MyIndirectArgsBuffer>("Indirect", 64);

        readonly MyBufferPool<MyReadBuffer> m_readBuffers = new MyBufferPool<MyReadBuffer>("Read", 64);
        readonly MyBufferPool<MyIndexBuffer> m_indexBuffers = new MyBufferPool<MyIndexBuffer>("Index", 64);
        readonly MyBufferPool<MyVertexBuffer> m_vertexBuffers = new MyBufferPool<MyVertexBuffer>("Vertex", 64);
        readonly MyBufferPool<MyConstantBuffer> m_constantBuffers = new MyBufferPool<MyConstantBuffer>("Constant", 64);

        // Mapping from buffer types to their corresponding pools
        readonly TypeSwitchRet<MyBufferInternal, IMyBufferPool> m_poolSwitch = new TypeSwitchRet<MyBufferInternal, IMyBufferPool>();

        #endregion

        public MyBufferManager()
        {
            m_poolSwitch
                .Case<MySrvBuffer>(() => m_srvBuffers)
                .Case<MyUavBuffer>(() => m_uavBuffers)
                .Case<MySrvUavBuffer>(() => m_srvUavBuffers)
                .Case<MyIndirectArgsBuffer>(() => m_indirectArgsBuffers)
                .Case<MyReadBuffer>(() => m_readBuffers)
                .Case<MyIndexBuffer>(() => m_indexBuffers)
                .Case<MyVertexBuffer>(() => m_vertexBuffers)
                .Case<MyConstantBuffer>(() => m_constantBuffers);
        }

        MyBufferPool<T> GetPool<T>()
        where T : MyBufferInternal, new()
        {
            return m_poolSwitch.Switch<T, MyBufferPool<T>>();
        }

        IEnumerable<IMyBufferPool> GetAllPools()
        {
            return m_poolSwitch.Matches.Values.Select(poolFunc => poolFunc());
        }

        #region Creation

        TBuffer CreateInternal<TBuffer>(string name, ref BufferDescription description, IntPtr? initData, Action<TBuffer> initializer = null)
            where TBuffer : MyBufferInternal, new()
        {
            //Debug.Assert(m_isDeviceInit, "Cannot modify buffer resources when the device has not been initialized!");

            var pool = GetPool<TBuffer>();

            TBuffer buffer;
            pool.AllocateOrCreate(out buffer);

            if (initializer != null)
                initializer(buffer);

            buffer.Init(name, ref description, initData);

            pool.LogAllocate(ref description);

            return buffer;
        }

        public ISrvBuffer CreateSrv(string name, int elements, int byteStride, IntPtr? initData = null, ResourceUsage usage = ResourceUsage.Default)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);

            Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should an Srv buffer be a staging resource?");

            BufferDescription description = new BufferDescription(elements * byteStride,
                usage,
                BindFlags.ShaderResource,
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                byteStride);

            return CreateInternal<MySrvBuffer>(name, ref description, initData);
        }

        public IUavBuffer CreateUav(string name, int elements, int byteStride, IntPtr? initData = null, MyUavType uavType = MyUavType.Default, ResourceUsage usage = ResourceUsage.Default)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);

            Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should an Uav buffer be a staging resource?");

            BufferDescription description = new BufferDescription(elements * byteStride,
                usage,
                BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                byteStride);

            return CreateInternal<MyUavBuffer>(name, ref description, initData, b => b.UavType = uavType);
        }

        public ISrvUavBuffer CreateSrvUav(string name, int elements, int byteStride, IntPtr? initData = null, MyUavType uavType = MyUavType.Default, ResourceUsage usage = ResourceUsage.Default)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);

            //Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should an SrvUav buffer be a staging resource?");

            BufferDescription description = new BufferDescription(elements * byteStride,
                usage,
                BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                byteStride);

            return CreateInternal<MySrvUavBuffer>(name, ref description, initData, b => b.UavType = uavType);
        }

        public IIndirectResourcesBuffer CreateIndirectArgsBuffer(string name, int elements, int byteStride, Format format = Format.R32_UInt)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);
            
            BufferDescription description = new BufferDescription(elements * byteStride,
                ResourceUsage.Default,
                BindFlags.UnorderedAccess,
                CpuAccessFlags.None,
                ResourceOptionFlags.DrawIndirectArguments,
                byteStride);

            return CreateInternal<MyIndirectArgsBuffer>(name, ref description, null, b => b.Format = format);
        }

        public IReadBuffer CreateRead(string name, int elements, int byteStride)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);

            BufferDescription description = new BufferDescription(elements * byteStride,
                ResourceUsage.Staging,
                BindFlags.None,
                CpuAccessFlags.Read,
                ResourceOptionFlags.None,
                byteStride);

            return CreateInternal<MyReadBuffer>(name, ref description, null);
        }

        public IIndexBuffer CreateIndexBuffer(string name, int elements, IntPtr? initData = null, MyIndexBufferFormat format = MyIndexBufferFormat.UShort, ResourceUsage usage = ResourceUsage.Default)
        {
            MyRenderProxy.Assert(elements > 0);

            Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should an index buffer be a staging resource?");

            int stride;

            switch (format)
            {
                case MyIndexBufferFormat.UShort:
                    stride = 2;
                    break;
                case MyIndexBufferFormat.UInt:
                    stride = 4;
                    break;
                default:
                    throw new NotImplementedException("Unsupported index buffer format.");
            }

            BufferDescription description = new BufferDescription(elements * stride,
                usage,
                BindFlags.IndexBuffer,
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.None,
                stride);

            return CreateInternal<MyIndexBuffer>(name, ref description, initData, b => b.Format = format);
        }

        public IVertexBuffer CreateVertexBuffer(string name, int elements, int byteStride, IntPtr? initData = null, ResourceUsage usage = ResourceUsage.Default, bool isStreamOutput = false)
        {
            MyRenderProxy.Assert(elements > 0);
            MyRenderProxy.Assert(byteStride > 0);

            Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should a vertex buffer be a staging resource?");

            BufferDescription description = new BufferDescription(elements * byteStride,
                usage,
                BindFlags.VertexBuffer | (isStreamOutput ? BindFlags.StreamOutput : BindFlags.None),
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.None,
                byteStride);

            return CreateInternal<MyVertexBuffer>(name, ref description, initData);
        }

        public IConstantBuffer CreateConstantBuffer(string name, int byteSize, IntPtr? initData = null, ResourceUsage usage = ResourceUsage.Default)
        {
            MyRenderProxy.Assert(byteSize > 0);

            Debug.Assert(byteSize == ((byteSize + 15) / 16) * 16, "CB size not padded");

            Debug.Assert(usage != ResourceUsage.Staging, "Conspicuous ResourceUsage setup: should a constant buffer be a staging resource?");

            BufferDescription description = new BufferDescription(byteSize,
                usage,
                BindFlags.ConstantBuffer,
                usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            return CreateInternal<MyConstantBuffer>(name, ref description, initData);
        }

        #endregion

        #region Resizing

        void ResizeInternal<TBuffer>(TBuffer buffer, int newElements, int newByteStride, IntPtr? newData, Action<TBuffer> initializer = null)
            where TBuffer : MyBufferInternal, new()
        {
            Debug.Assert(m_isDeviceInit, "Cannot modify buffer resources when the device has not been initialized!");
            Debug.Assert(buffer != null, "Invalid buffer to dispose. It is likely a buffer not created through this manager. Use the CreateX methods to create buffers before resizing them.");

            var pool = GetPool<TBuffer>();
            BufferDescription description = buffer.Description;

            if (newByteStride < 0)
                newByteStride = description.StructureByteStride;

            description.SizeInBytes = newElements * newByteStride;
            description.StructureByteStride = newByteStride;

            if (buffer.ByteSize == description.SizeInBytes
                && buffer.Description.StructureByteStride == description.StructureByteStride
                && newData == null)
            {
                return;
            }

            pool.LogResize(buffer, ref description); // We have to log before Init

            string name = buffer.Name;
            buffer.DisposeInternal();

            if (initializer != null)
                initializer(buffer);

            buffer.Init(name, ref description, newData);
        }

        public void Resize(ISrvBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            ResizeInternal(buffer as MySrvBuffer, newElements, newByteStride, newData);
        }

        public void Resize(IUavBindable buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            var uavBuffer = buffer as MyUavBuffer;
            ResizeInternal(uavBuffer, newElements, newByteStride, newData, b => b.UavType = uavBuffer.UavType);
        }

        public void Resize(ISrvUavBindable buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            var srvUavBuffer = buffer as MySrvUavBuffer;
            ResizeInternal(srvUavBuffer, newElements, newByteStride, newData, b => b.UavType = srvUavBuffer.UavType);
        }

        public void Resize(IIndirectResourcesBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            var indirectBuffer = buffer as MyIndirectArgsBuffer;
            ResizeInternal(indirectBuffer, newElements, newByteStride, newData, b => b.Format = indirectBuffer.Format);
        }

        public void Resize(IReadBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            ResizeInternal(buffer as MyReadBuffer, newElements, newByteStride, newData);
        }

        public void Resize(IIndexBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            var indexBuffer = buffer as MyIndexBuffer;
            ResizeInternal(indexBuffer, newElements, newByteStride, newData, b => b.Format = indexBuffer.Format);
        }

        public void Resize(IVertexBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            ResizeInternal(buffer as MyVertexBuffer, newElements, newByteStride, newData);
        }

        public void Resize(IConstantBuffer buffer, int newElements, int newByteStride = -1, IntPtr? newData = null)
        {
            MyRenderProxy.Assert(newElements > 0);
            MyRenderProxy.Assert(newByteStride > 0 || newByteStride == -1);

            ResizeInternal(buffer as MyConstantBuffer, newElements, newByteStride, newData);
        }

        #endregion

        #region Disposal

        public void DisposeInternal<TBuffer>(TBuffer buffer)
            where TBuffer : MyBufferInternal, new()
        {
            //Debug.Assert(m_isDeviceInit, "Cannot modify buffer resources when the device has not been initialized!");

            if (buffer == null || buffer.IsReleased)
                return;

            var pool = GetPool<TBuffer>();

            pool.LogDispose(buffer);

            buffer.DisposeInternal();
            pool.Deallocate(buffer);
        }

        public void Dispose(params ISrvBindable[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MySrvBuffer);
        }

        public void Dispose(params IUavBindable[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyUavBuffer);
        }

        public void Dispose(params ISrvUavBindable[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MySrvUavBuffer);
        }

        public void Dispose(params IIndirectResourcesBuffer[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyIndirectArgsBuffer);
        }

        public void Dispose(params IReadBuffer[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyReadBuffer);
        }

        public void Dispose(params IIndexBuffer[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyIndexBuffer);
        }

        public void Dispose(params IVertexBuffer[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyVertexBuffer);
        }

        public void Dispose(params IConstantBuffer[] buffers)
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers)
                DisposeInternal(buffer as MyConstantBuffer);
        }

        #endregion

        #region IManagerDevice overrides

        public void OnDeviceInit()
        {
            // TODO: Remove Init from OnDeviceReset calls for all components, or better remove OnDeviceReset completely
            //MyRenderProxy.Assert(GetAllPools().All(pool => pool.ActiveCount == 0), "Leftover pool on device init.");
            m_isDeviceInit = true;
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;

            foreach (var myBufferPool in GetAllPools())
                myBufferPool.DisposeAll();
        }

        #endregion


        public IEnumerable<MyBufferStatistics> GetReport()
        {
            MyBufferStatistics totalStatistics = new MyBufferStatistics { Name = "Total" };

            foreach (var myBufferPool in GetAllPools())
            {
                totalStatistics.TotalBytes += myBufferPool.Statistics.TotalBytes;
                totalStatistics.TotalBuffers += myBufferPool.Statistics.TotalBuffers;
                yield return myBufferPool.Statistics;
            }

            yield return totalStatistics;
        }
    }
}
