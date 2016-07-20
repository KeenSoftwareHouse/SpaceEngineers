using System;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace Sandbox.Engine.Utils
{
    public static class MyDirectXHelper
    {
        static Factory m_factory;
        static Factory GetFactory()
        {
            if(m_factory == null)
            {
                m_factory = new Factory1();
            }
            return m_factory;
        }

        public static bool IsDx11Supported()
        {
            var factory = GetFactory();
            FeatureLevel[] featureLevels = {FeatureLevel.Level_11_0};

            for (int i = 0; i < factory.Adapters.Length; i++)
            {
                var adapter = factory.Adapters[i];
                Device adapterTestDevice = null;
                try
                {
                    adapterTestDevice = new Device(adapter, DeviceCreationFlags.None, featureLevels);
                }
                catch (Exception)
                {
                    continue;
                }

                UInt64 vram;
                UInt64 svram;
                GetRamSizes(out vram, adapter, out svram);

                // microsoft software renderer allocates 256MB shared memory, cpu integrated graphic on notebooks has 0 preallocated, all shared
                return (vram > 500000000 || svram > 500000000);
            }
            return false;
        }

        private static unsafe void GetRamSizes(out UInt64 vram, Adapter adapter, out UInt64 svram)
        {
            // DedicatedSystemMemory = bios or DVMT preallocated video memory, that cannot be used by OS - need retest on pc with only cpu/chipset based graphic
            // DedicatedVideoMemory = discrete graphic video memory
            // SharedSystemMemory = aditional video memory, that can be taken from OS RAM when needed
            void* vramptr = ((IntPtr) (adapter.Description.DedicatedSystemMemory != 0
                ? adapter.Description.DedicatedSystemMemory
                : adapter.Description.DedicatedVideoMemory)).ToPointer();
            vram = (UInt64) vramptr;
            void* svramptr = ((IntPtr) adapter.Description.SharedSystemMemory).ToPointer();
            svram = (UInt64) svramptr;
        }
    }
}
