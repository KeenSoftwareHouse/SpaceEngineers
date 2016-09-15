using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    class MyHWResource
    {
        internal SharpDX.Direct3D11.Resource m_resource;

        internal virtual void Release()
        {
            if (m_resource != null)
            {
                m_resource.Dispose();
                m_resource = null;
            }
        }

        internal void SetDebugName(string name)
        {
            m_resource.DebugName = name;
        }
    }

    class MyBindableResource : MyHWResource
    {
        internal static int AutoId = 0;
        internal static int GenerateId() { return AutoId++; }
        internal readonly int ResId;

        internal virtual Vector3I GetSize() { throw new NotImplementedException(); }

        internal MyBindableResource()
        {
            ResId = GenerateId();
        }

        internal virtual int GetID()
        {
            return ResId;
        }

        internal virtual SharpDX.Direct3D11.Resource GetHWResource()
        {
            return m_resource;
        }
    }

    class MyRWStructuredBuffer : MyBindableResource, IUnorderedAccessBindable, IShaderResourceBindable
    {
        internal ShaderResourceView m_srv;
        internal UnorderedAccessView m_uav;

        internal Vector2I m_resolution;

        ShaderResourceView IShaderResourceBindable.Srv
        {
            get { return m_srv; }
        }

        UnorderedAccessView IUnorderedAccessBindable.Uav
        {
            get { return m_uav; }
        }

        internal override void Release()
        {
            if (m_srv != null)
            {
                m_srv.Dispose();
                m_srv = null;
            }
            if (m_uav != null)
            {
                m_uav.Dispose();
                m_uav = null;
            }

            base.Release();
        }

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution.X, m_resolution.Y, 0);
        }
        internal enum UavType
        {
            None,
            Default,
            Append,
            Counter
        }
        internal MyRWStructuredBuffer(int elements, int stride, UavType uav, bool srv, string debugName)
        {
            m_resolution = new Vector2I(elements, 1);

            var bufferDesc = new BufferDescription(elements * stride, ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess, 
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, stride);

            m_resource = new SharpDX.Direct3D11.Buffer(MyRender11.Device, bufferDesc);
            m_resource.DebugName = debugName;
            if (uav != UavType.None)
            {
                if (uav == UavType.Default)
                    m_uav = new UnorderedAccessView(MyRender11.Device, m_resource);
                else
                {
                    var description = new UnorderedAccessViewDescription()
                    {
                        Buffer = new UnorderedAccessViewDescription.BufferResource()
                        {
                            ElementCount = elements,
                            FirstElement = 0,
                            Flags = uav == UavType.Append ? UnorderedAccessViewBufferFlags.Append : UnorderedAccessViewBufferFlags.Counter
                        },
                        Format = Format.Unknown,
                        Dimension = UnorderedAccessViewDimension.Buffer
                    };
                    m_uav = new UnorderedAccessView(MyRender11.Device, m_resource, description);
                }
                m_uav.DebugName = debugName + "Uav";
            }
            if (srv)
            {
                m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                m_srv.DebugName = debugName + "Srv";
            }
        }
    }
    class MyIndirectArgsBuffer : MyBindableResource, IUnorderedAccessBindable
    {
        internal UnorderedAccessView m_uav;

        internal Vector2I m_resolution;

        internal SharpDX.Direct3D11.Buffer Buffer { get { return m_resource as SharpDX.Direct3D11.Buffer; } }
        UnorderedAccessView IUnorderedAccessBindable.Uav
        {
            get { return m_uav; }
        }

        internal override void Release()
        {
            if (m_uav != null)
            {
                m_uav.Dispose();
                m_uav = null;
            }

            base.Release();
        }

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution.X, m_resolution.Y, 0);
        }
        internal MyIndirectArgsBuffer(int elements, int stride, string debugName)
        {
            m_resolution = new Vector2I(elements, 1);

            var bufferDesc = new BufferDescription(elements * stride, ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.DrawIndirectArguments, stride);

            m_resource = new SharpDX.Direct3D11.Buffer(MyRender11.Device, bufferDesc);
            m_resource.DebugName = debugName;

            var description = new UnorderedAccessViewDescription()
            {
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = elements,
                    FirstElement = 0,
                    Flags = 0
                },
                Format = Format.R32_UInt,
                Dimension = UnorderedAccessViewDimension.Buffer
            };
            m_uav = new UnorderedAccessView(MyRender11.Device, m_resource, description);
            m_uav.DebugName = debugName + "Uav";
        }
    }
    class MyReadStructuredBuffer : MyBindableResource
    {
        internal int m_resolution;

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution, 1, 0);
        }

        internal MyReadStructuredBuffer(int elements, int stride, string debugName)
        {
            m_resolution = elements;

            var bufferDesc = new BufferDescription(elements * stride, ResourceUsage.Staging, BindFlags.None, 
                CpuAccessFlags.Read, ResourceOptionFlags.None, stride);

            m_resource = new SharpDX.Direct3D11.Buffer(MyRender11.Device, bufferDesc);
            m_resource.DebugName = debugName;
        }
    }
}
