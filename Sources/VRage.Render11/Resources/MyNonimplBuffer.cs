//using System;
//using SharpDX.Direct3D11;
//using SharpDX.DXGI;
//using VRage.Generics;
//using VRageMath;
//using VRageRender;
//using Buffer = SharpDX.Direct3D11.Buffer;
//using Resource = SharpDX.Direct3D11.Resource;

//namespace VRageRender
//{
//    internal enum MyUAVType
//    {
//        Default,
//        Append,
//        Counter
//    }

//    struct MyBufferStatistics
//    {
//        public int TotalBuffers;
//        public int TotalBytes;
//    }

//    class MyBufferInternal : IResource
//    {
//        string m_name;
//        int m_elements;
//        int m_stride;
//        BindFlags m_bindFlags;
//        ResourceUsage m_resourceUsage;
//        CpuAccessFlags m_cpuAccessFlags;
//        ResourceOptionFlags m_resourceOptionFlags;

//        Buffer m_resource;

//        public string Name
//        {
//            get { return m_name; }
//        }

//        public Resource Resource
//        {
//            get { return m_resource; }
//        }

//        public Buffer Buffer
//        {
//            get { return m_resource; }
//        }

//        public Vector3I Size3
//        {
//            get { return new Vector3I(m_elements, 1, 1); }
//        }

//        public Vector2I Size
//        {
//            get { return new Vector2I(m_elements, 1); }
//        }

//        public int SizeInBytes
//        {
//            get { return m_elements*m_stride; }
//        }

//        public int GetElementsCount()
//        {
//            return m_elements;
//        }

//        protected void Init(string name, int elements, int stride, 
//            BindFlags bindFlags, 
//            ResourceOptionFlags resourceOptionFlags,
//            ResourceUsage resourceUsage = ResourceUsage.Default, 
//            CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None)
//        {
//            m_name = name;
//            m_elements = elements;
//            m_stride = stride;
//            m_bindFlags = bindFlags;
//            m_resourceUsage = resourceUsage;
//            m_cpuAccessFlags = cpuAccessFlags;
//            m_resourceOptionFlags = resourceOptionFlags;
//        }

//        public void OnDeviceInit()
//        {
//            var bufferDesc = new BufferDescription(m_elements * m_stride, m_resourceUsage, m_bindFlags,
//                m_cpuAccessFlags, m_resourceOptionFlags, m_stride);

//            m_resource = new Buffer(MyRender11.Device, bufferDesc);
//            m_resource.DebugName = m_name;
//        }

//        public void OnDeviceEnd()
//        {
//            if (m_resource != null)
//            {
//                m_resource.Dispose();
//                m_resource = null;
//            }
//        }
//    }

//    class MySrvBuffer : MyBufferInternal, ISrvBindable
//    {
//        ShaderResourceView m_SRV;

//        public ShaderResourceView SRV
//        {
//            get { return m_SRV; }
//        }

//        public void Init(string name, int elements, int stride)
//        {
//            base.Init(name, elements, stride,
//                bindFlags: BindFlags.ShaderResource,
//                resourceUsage: ResourceUsage.Dynamic,
//                resourceOptionFlags: ResourceOptionFlags.BufferStructured);
//        }

//        internal void OnDeviceInit()
//        {
//            base.OnDeviceInit();

//            m_SRV = new ShaderResourceView(MyRender11.Device, Resource);
//            m_SRV.DebugName = Name;
//        }

//        internal void OnDeviceEnd()
//        {
//            if (m_SRV != null)
//            {
//                m_SRV.Dispose();
//                m_SRV = null;
//            }

//            base.OnDeviceEnd();
//        }
//    }

//    class MyUavBuffer : MyBufferInternal, IUavBindable, ISrvBindable
//    {
//        MyUAVType m_uavType;

//        ShaderResourceView m_SRV;
//        UnorderedAccessView m_UAV;

//        public ShaderResourceView SRV
//        {
//            get { return m_SRV; }
//        }

//        public UnorderedAccessView UAV
//        {
//            get { return m_UAV; }
//        }

//        public void Init(string name, int elements, int stride, MyUAVType uavType, bool isDynamic)
//        {
//            base.Init(name, elements, stride,
//                bindFlags: BindFlags.ShaderResource | BindFlags.UnorderedAccess,
//                resourceUsage: isDynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
//                resourceOptionFlags: ResourceOptionFlags.BufferStructured);
//            m_uavType = uavType;
//        }

//        internal void OnDeviceInit()
//        {
//            base.OnDeviceInit();

//            if (m_uavType == MyUAVType.Default)
//                m_UAV = new UnorderedAccessView(MyRender11.Device, Resource);
//            else
//            {
//                var description = new UnorderedAccessViewDescription()
//                {
//                    Buffer = new UnorderedAccessViewDescription.BufferResource()
//                    {
//                        ElementCount = GetElementsCount(),
//                        FirstElement = 0,
//                        Flags =
//                            m_uavType == MyUAVType.Append
//                                ? UnorderedAccessViewBufferFlags.Append
//                                : UnorderedAccessViewBufferFlags.Counter
//                    },
//                    Format = SharpDX.DXGI.Format.Unknown,
//                    Dimension = UnorderedAccessViewDimension.Buffer
//                };
//                m_UAV = new UnorderedAccessView(MyRender11.Device, Resource, description);
//            }
//            m_UAV.DebugName = Name;

//            m_SRV = new ShaderResourceView(MyRender11.Device, Resource);
//            m_SRV.DebugName = Name;
//        }

//        internal void OnDeviceEnd()
//        {
//            if (m_SRV != null)
//            {
//                m_SRV.Dispose();
//                m_SRV = null;
//            }
//            if (m_UAV != null)
//            {
//                m_UAV.Dispose();
//                m_UAV = null;
//            }

//            base.OnDeviceEnd();
//        }
//    }

//    class MyReadBuffer : MyBufferInternal
//    {
//        internal void Init(string debugName, int elements, int stride)
//        {
//            base.Init(debugName, elements, stride, BindFlags.None, ResourceOptionFlags.None, cpuAccessFlags: CpuAccessFlags.Read);
//        }
//    }

//    class MyIndirectArgsBuffer : MyBufferInternal, IUavBindable
//    {
//        UnorderedAccessView m_UAV;

//        Format m_format;

//        public UnorderedAccessView UAV
//        {
//            get { return m_UAV; }
//        }

//        internal void Init(string debugName, int elements, int stride, Format format)
//        {
//            base.Init(debugName, elements, stride, BindFlags.UnorderedAccess,
//                resourceOptionFlags: ResourceOptionFlags.DrawIndirectArguments);

//            m_format = format;
//        }

//        internal void OnDeviceInit()
//        {
//            base.OnDeviceInit();

//            var description = new UnorderedAccessViewDescription()
//            {
//                Buffer = new UnorderedAccessViewDescription.BufferResource()
//                {
//                    ElementCount = GetElementsCount(),
//                    FirstElement = 0,
//                    Flags = 0
//                },
//                Format = m_format,
//                Dimension = UnorderedAccessViewDimension.Buffer
//            };
//            m_UAV = new UnorderedAccessView(MyRender11.Device, Resource, description);
//            m_UAV.DebugName = Name;
//        }

//        internal void OnDeviceEnd()
//        {
//            if (m_UAV != null)
//            {
//                m_UAV.Dispose();
//                m_UAV = null;
//            }

//            base.OnDeviceEnd();
//        }
//    }

//    class MyBufferManager : IResourceManager
//    {
//        MyBufferStatistics m_statistics;

//        MyObjectsPool<MySrvBuffer> m_srvBuffers = new MyObjectsPool<MySrvBuffer>(64);
//        MyObjectsPool<MyUavBuffer> m_uavStructBuffers = new MyObjectsPool<MyUavBuffer>(64);
//        MyObjectsPool<MyReadBuffer> m_readStructBuffers = new MyObjectsPool<MyReadBuffer>(64);
//        MyObjectsPool<MyIndirectArgsBuffer> m_indirectArgsBuffers = new MyObjectsPool<MyIndirectArgsBuffer>(64);
//        bool m_isDeviceInit = false;

//        public MySrvBuffer CreateSrv(string name, int elements, int stride)
//        {
//            MySrvBuffer buffer;
//            m_srvBuffers.AllocateOrCreate(out buffer);
//            buffer.Init(name, elements, stride);
            
//            m_statistics.TotalBuffers++;
//            m_statistics.TotalBytes += elements*stride;

//            return buffer;
//        }
        
//        public MyUavBuffer CreateUav(string name, int elements, int stride, MyUAVType uavType, bool isDynamic)
//        {
//            MyUavBuffer buffer;
//            m_uavStructBuffers.AllocateOrCreate(out buffer);
//            buffer.Init(name, elements, stride, uavType, isDynamic);

//            m_statistics.TotalBuffers++;
//            m_statistics.TotalBytes += elements * stride;

//            return buffer;
//        }

//        public MyReadBuffer CreateRead(string name, int elements, int stride)
//        {
//            MyReadBuffer buffer;
//            m_readStructBuffers.AllocateOrCreate(out buffer);
//            buffer.Init(name, elements, stride);

//            m_statistics.TotalBuffers++;
//            m_statistics.TotalBytes += elements * stride;

//            return buffer;
//        }

//        public MyIndirectArgsBuffer CreateIndirectArgsBuffer(string name, int elements, int stride, Format format)
//        {
//            MyIndirectArgsBuffer buffer;
//            m_indirectArgsBuffers.AllocateOrCreate(out buffer);
//            buffer.Init(name, elements, stride, format);

//            m_statistics.TotalBuffers++;
//            m_statistics.TotalBytes += elements * stride;

//            return buffer;
//        }

//        public void DisposeBuffer(ref MySrvBuffer buffer)
//        {
//            if (buffer == null)
//                return;

//            if (m_isDeviceInit)
//                buffer.OnDeviceEnd();

//            m_statistics.TotalBuffers--;
//            m_statistics.TotalBytes += buffer.SizeInBytes;

//            m_srvBuffers.Deallocate(buffer);
//        }
        
//        public void DisposeBuffer(ref MyUavBuffer buffer)
//        {
//            if (buffer == null)
//                return;

//            if (m_isDeviceInit)
//                buffer.OnDeviceEnd();

//            m_statistics.TotalBuffers--;
//            m_statistics.TotalBytes += buffer.SizeInBytes;

//            m_uavStructBuffers.Deallocate(buffer);
//        }

//        public void DisposeBuffer(ref MyReadBuffer buffer)
//        {
//            if (buffer == null)
//                return;

//            if (m_isDeviceInit)
//                buffer.OnDeviceEnd();

//            m_statistics.TotalBuffers--;
//            m_statistics.TotalBytes += buffer.SizeInBytes;

//            m_readStructBuffers.Deallocate(buffer);
//        }

//        public void DisposeBuffer(ref MyIndirectArgsBuffer buffer)
//        {
//            if (buffer == null)
//                return;

//            if (m_isDeviceInit)
//                buffer.OnDeviceEnd();

//            m_statistics.TotalBuffers--;
//            m_statistics.TotalBytes += buffer.SizeInBytes;

//            m_indirectArgsBuffers.Deallocate(buffer);
//        }

//        public void OnDeviceInit()
//        {
//            m_isDeviceInit = true;
//            foreach (var tex in m_srvBuffers.Active)
//                tex.OnDeviceInit();
//            foreach (var tex in m_uavStructBuffers.Active)
//                tex.OnDeviceInit();
//            foreach (var tex in m_readStructBuffers.Active)
//                tex.OnDeviceInit();
//            foreach (var tex in m_indirectArgsBuffers.Active)
//                tex.OnDeviceInit();
//        }

//        public void OnDeviceReset()
//        {
//            foreach (var tex in m_srvBuffers.Active)
//            {
//                tex.OnDeviceEnd();
//                tex.OnDeviceInit();
//            }
//            foreach (var tex in m_uavStructBuffers.Active)
//            {
//                tex.OnDeviceEnd();
//                tex.OnDeviceInit();
//            }
//            foreach (var tex in m_readStructBuffers.Active)
//            {
//                tex.OnDeviceEnd();
//                tex.OnDeviceInit();
//            }
//            foreach (var tex in m_indirectArgsBuffers.Active)
//            {
//                tex.OnDeviceEnd();
//                tex.OnDeviceInit();
//            }
//        }

//        public void OnDeviceEnd()
//        {
//            m_isDeviceInit = false;
//            foreach (var tex in m_srvBuffers.Active)
//                tex.OnDeviceEnd();
//            foreach (var tex in m_uavStructBuffers.Active)
//                tex.OnDeviceEnd();
//            foreach (var tex in m_readStructBuffers.Active)
//                tex.OnDeviceEnd();
//            foreach (var tex in m_indirectArgsBuffers.Active)
//                tex.OnDeviceEnd();
//        }

//        public void OnUnloadData()
//        { }

//        public MyBufferStatistics GetReport()
//        {
//            return m_statistics;
//        }
//    }
//}