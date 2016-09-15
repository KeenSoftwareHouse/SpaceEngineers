//using IntPtr = System.IntPtr;
//using SharpDX.Direct3D11;
//using VRage.Generics;
//using VRageMath;

//namespace VRageRender
//{
//    class MyUnmannagedBuffer: IResource, ISrvBindable
//    {
//        string m_name;
//        int m_elements;
//        int m_stride;

//        SharpDX.Direct3D11.Buffer m_buffer;
//        ShaderResourceView m_SRV;

//        public string Name { get { return m_name; } }

//        public Vector2I Size { get { return new Vector2I(m_elements*m_stride, 1); } }

//        public Vector3I Size3 { get { return new Vector3I(m_elements*m_stride, 1, 1); } }

//        public int SizeInBytes { get { return m_elements*m_stride; } }

//        public Resource Resource { get { return m_buffer; } }

//        public Buffer Buffer { get { return m_buffer; } }

//        public ShaderResourceView SRV { get { return m_SRV; } }

//        public void Init(string name, int elements, int stride, IntPtr data)
//        {
//            m_name = name;
//            m_elements = elements;
//            m_stride = stride;

//            BufferDescription desc = new BufferDescription(elements*stride, BindFlags.ShaderResource, ResourceUsage.Dynamic);
//            m_buffer = new Buffer(MyRender11.Device, data, desc);
//            m_buffer.DebugName = name;

//            m_SRV = new ShaderResourceView(MyRender11.Device, m_buffer);
//            m_SRV.DebugName = name;
//        }

//        public void Dispose()
//        {
//            if (m_buffer != null)
//            {
//                m_buffer.Dispose();
//                m_buffer = null;
//            }
//            if (m_SRV != null)
//            {
//                m_SRV.Dispose();
//                m_SRV = null;
//            }
//        }
//    }

//    class MyUnmannagedBufferManager: IResourceManager
//    {
//        MyObjectsPool<MyUnmannagedBuffer> m_buffers = new MyObjectsPool<MyUnmannagedBuffer>(16);
//        bool m_isDeviceInit = false;

//        public MyUnmannagedBuffer CreateBuffer(string name, int elements, int stride, IntPtr data)
//        {
//            MyRenderProxy.Assert(m_isDeviceInit, "Buffer is created before DeviceInit");
//            MyUnmannagedBuffer buffer;
//            m_buffers.AllocateOrCreate(out buffer);
//            buffer.Init(name, elements, stride, data);

//            return buffer;
//        }

//        public void DisposeBuffer(ref MyUnmannagedBuffer buffer)
//        {
//            m_buffers.Deallocate(buffer);
//            buffer.Dispose();
//        }

//        public void OnDeviceInit()
//        {
//            m_isDeviceInit = true;
//        }

//        public void OnDeviceEnd()
//        {
//            m_isDeviceInit = false;
//            MyRenderProxy.Assert(m_buffers.ActiveCount != 0,
//                "Unmannaged buffers are still allocated. Dispose all before DeviceEnd");
//        }

//        public void OnDeviceReset()
//        {
            
//        }

//        public void OnUnloadData()
//        {
            
//        }
//    }
//}
