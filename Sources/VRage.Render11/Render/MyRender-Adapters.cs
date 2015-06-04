using System;
using System.Collections.Generic;
using System.Management;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace VRageRender
{
	partial class MyRender11
	{
		static MyRefreshRatePriorityComparer m_refreshRatePriorityComparer = new MyRefreshRatePriorityComparer();
        static MyAdapterInfo[] m_adapterInfoList;
        static Dictionary<int, ModeDescription[]> m_adapterModes = new Dictionary<int, ModeDescription[]>();

        static Factory m_factory;
        static Factory GetFactory()
        {
            return m_factory ?? (m_factory = new Factory());
        }

	    private static void LogInfoFromWMI()
        {
            try
            {
                //http://wutils.com/wmi/
                //create a management scope object
                ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

                //create object query
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_VideoController");

                //create object searcher
                ManagementObjectSearcher searcher =
                                        new ManagementObjectSearcher(scope, query);

                //get collection of WMI objects
                ManagementObjectCollection queryCollection = searcher.Get();

                Log.WriteLine("WMI {");
                Log.IncreaseIndent();

                //enumerate the collection.
                foreach (ManagementObject m in queryCollection)
                {
                    Log.WriteLine("{");
                    Log.IncreaseIndent();

                    Log.WriteLine("Caption = " + m["Caption"]);
                    Log.WriteLine("AdapterRam = " + m["AdapterRam"]);
                    Log.WriteLine("DriverVersion = " + m["DriverVersion"]);
                    Log.WriteLine("DriverDate = " + m["DriverDate"]);
                    Log.WriteLine("Description = " + m["Description"]);
                    Log.WriteLine("DeviceID = " + m["DeviceID"]);
                    Log.WriteLine("Name = " + m["Name"]);
                    Log.WriteLine("VideoProcessor = " + m["VideoProcessor"]);
                    Log.WriteLine("VideoArchitecture = " + m["VideoArchitecture"]);
                    Log.WriteLine("PNPDeviceID = " + m["PNPDeviceID"]);
                    Log.WriteLine("InstalledDisplayDrivers = " + m["InstalledDisplayDrivers"]);

                    Log.DecreaseIndent();
                    Log.WriteLine("}");
                }
                Log.DecreaseIndent();
                Log.WriteLine("}");
            }
            catch
            {
            }
        }

        static void LogAdapterInfoBegin(ref MyAdapterInfo info)
        {
            Log.WriteLine("AdapterInfo = {");
            Log.IncreaseIndent();
            Log.WriteLine("Name = " + info.Name);
            Log.WriteLine("Device name = " + info.DeviceName);
            Log.WriteLine("Description = " + info.Description);
            Log.WriteLine("DXGIAdapter id = " + info.AdapterDeviceId);
            Log.WriteLine("SUPPORTED = " + info.IsDx11Supported);
            Log.WriteLine("VRAM = " + info.VRAM);
            Log.WriteLine("Priority = " + info.Priority);
            Log.WriteLine("Fallback display modes = " + info.FallbackDisplayModes);
            Log.WriteLine("Multithreaded rendering supported = " + info.MultithreadedRenderingSupported);
        }

        static void LogAdapterInfoEnd()
        {
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        static void LogOutputDisplayModes(ref MyAdapterInfo info)
        {
            Log.WriteLine("Display modes = {");
            Log.IncreaseIndent();
            Log.WriteLine("DXGIOutput id = " + info.OutputId);
            for(int i=0; i< info.SupportedDisplayModes.Length; i++)
            {
                Log.WriteLine(info.SupportedDisplayModes[i].ToString());
            }
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        static int VendorPriority(int vendorId)
        {
            switch(vendorId)
            {
                case 4098: // amd
                case 4318: // nvidia
                    return 2;
                case 32902: // intel
                    return 1;
                default:
                    return 0;
            }
        }

        unsafe static MyAdapterInfo[] CreateAdaptersList()
        {
            List<MyAdapterInfo> adaptersList = new List<MyAdapterInfo>();

            var factory = GetFactory();
            FeatureLevel[] featureLevels = { FeatureLevel.Level_11_0 };

            int adapterIndex = 0;

            LogInfoFromWMI();

            for(int i=0; i<factory.Adapters.Length; i++)
            {
                var adapter = factory.Adapters[i];
                Device adapterTestDevice = null;
                try
                {
                    adapterTestDevice = new Device(adapter, DeviceCreationFlags.None, featureLevels);
                }
                catch(SharpDXException e)
                {

                }

                bool supportedDevice = adapterTestDevice != null;

                bool supportsConcurrentResources = false;
                bool supportsCommandLists = false;
                if (supportedDevice)
                {
                    adapterTestDevice.CheckThreadingSupport(out supportsConcurrentResources, out supportsCommandLists);
                }

                void* ptr = ((IntPtr)adapter.Description.DedicatedVideoMemory).ToPointer();
                ulong vram = (ulong)ptr;

                supportedDevice = supportedDevice && vram > 500000000;

                var deviceDesc = String.Format("{0}, dev id: {1}, shared mem: {2}, Luid: {3}, rev: {4}, subsys id: {5}, vendor id: {6}",
                    adapter.Description.Description,
                    adapter.Description.DeviceId,
                    vram,
                    adapter.Description.Luid,
                    adapter.Description.Revision,
                    adapter.Description.SubsystemId,
                    adapter.Description.VendorId
                    );

                var info = new MyAdapterInfo
                {
                    Name = adapter.Description.Description,
                    DeviceName = adapter.Description.Description,
                    Description = deviceDesc,
                    IsDx11Supported = supportedDevice,
                    AdapterDeviceId = i,

                    Priority = VendorPriority(adapter.Description.VendorId),
                    Has512MBRam = vram > 500000000,
                    HDRSupported = true,
                    MaxTextureSize = Texture2D.MaximumTexture2DSize,

                    VRAM = vram,
                    MultithreadedRenderingSupported = supportsCommandLists
                };

                if(vram >= 2000000000)
                {
                    info.MaxTextureQualitySupported = MyTextureQuality.HIGH;
                }
                else if (vram >= 1000000000)
                {
                    info.MaxTextureQualitySupported = MyTextureQuality.MEDIUM;
                }
                else
                { 
                    info.MaxTextureQualitySupported = MyTextureQuality.LOW;
                }

                info.MaxAntialiasingModeSupported = MyAntialiasingMode.FXAA;
                if (supportedDevice)
                {
                    if (adapterTestDevice.CheckMultisampleQualityLevels(Format.R11G11B10_Float, 2) > 0)
                    {
                        info.MaxAntialiasingModeSupported = MyAntialiasingMode.MSAA_2;
                    }
                    if (adapterTestDevice.CheckMultisampleQualityLevels(Format.R11G11B10_Float, 4) > 0)
                    {
                        info.MaxAntialiasingModeSupported = MyAntialiasingMode.MSAA_4;
                    }
                    if (adapterTestDevice.CheckMultisampleQualityLevels(Format.R11G11B10_Float, 8) > 0)
                    {
                        info.MaxAntialiasingModeSupported = MyAntialiasingMode.MSAA_8;
                    }
                }

                LogAdapterInfoBegin(ref info);

                if(supportedDevice)
                {
                    for(int j=0; j<factory.Adapters[i].Outputs.Length; j++)
                    {
                        var output = factory.Adapters[i].Outputs[j];

                        info.Name = String.Format("{0} + {1}", adapter.Description.Description, output.Description.DeviceName);
                        info.OutputName = output.Description.DeviceName;
                        info.OutputId = j;

                        var displayModeList = factory.Adapters[i].Outputs[j].GetDisplayModeList(MyRender11Constants.BACKBUFFER_FORMAT, DisplayModeEnumerationFlags.Interlaced);
                        var adapterDisplayModes = new MyDisplayMode[displayModeList.Length];
                        for (int k = 0; k < displayModeList.Length; k++)
                        {
                            var displayMode = displayModeList[k];

                            adapterDisplayModes[k] = new MyDisplayMode 
                            { 
                                Height = displayMode.Height, 
                                Width = displayMode.Width, 
                                RefreshRate = displayMode.RefreshRate.Numerator, 
                                RefreshRateDenominator = displayMode.RefreshRate.Denominator 
                            }; 
                        }
                        Array.Sort(adapterDisplayModes, m_refreshRatePriorityComparer);

                        info.SupportedDisplayModes = adapterDisplayModes;
                        info.CurrentDisplayMode = adapterDisplayModes[adapterDisplayModes.Length - 1];


                        adaptersList.Add(info);
                        m_adapterModes[adapterIndex] = displayModeList;
                        adapterIndex++;

                        LogOutputDisplayModes(ref info);
                    }

                    if(info.SupportedDisplayModes == null)
                    {
                        // FALLBACK MODES

                        MyDisplayMode[] fallbackDisplayModes = {
                            new MyDisplayMode(640, 480, 60000, 1000),
                            new MyDisplayMode(720, 576, 60000, 1000),
                            new MyDisplayMode(800, 600, 60000, 1000),
                            new MyDisplayMode(1024, 768, 60000, 1000),
                            new MyDisplayMode(1152, 864, 60000, 1000),
                            new MyDisplayMode(1280, 720, 60000, 1000),
                            new MyDisplayMode(1280, 768, 60000, 1000),
                            new MyDisplayMode(1280, 800, 60000, 1000),
                            new MyDisplayMode(1280, 960, 60000, 1000),
                            new MyDisplayMode(1280, 1024, 60000, 1000),
                            new MyDisplayMode(1360, 768, 60000, 1000),
                            new MyDisplayMode(1360, 1024, 60000, 1000),
                            new MyDisplayMode(1440, 900, 60000, 1000),
                            new MyDisplayMode(1600, 900, 60000, 1000),
                            new MyDisplayMode(1600, 1024, 60000, 1000),
                            new MyDisplayMode(1600, 1200, 60000, 1000),
                            new MyDisplayMode(1680, 1200, 60000, 1000),
                            new MyDisplayMode(1680, 1050, 60000, 1000),
                            new MyDisplayMode(1920, 1080, 60000, 1000),
                            new MyDisplayMode(1920, 1200, 60000, 1000)
                        };

                        info.SupportedDisplayModes = fallbackDisplayModes;
                        info.FallbackDisplayModes = true;
                    }
                }
                else
                {
                    info.SupportedDisplayModes = new MyDisplayMode[0];
                    adaptersList.Add(info);
                    adapterIndex++;
                }
                LogAdapterInfoEnd();

                if(adapterTestDevice != null)
                {
                    adapterTestDevice.Dispose();
                    adapterTestDevice = null;
                }
            }

            return adaptersList.ToArray();
        }

		internal static MyAdapterInfo[] GetAdaptersList()
		{
		    return m_adapterInfoList ?? (m_adapterInfoList = CreateAdaptersList());
		}
	}
}
