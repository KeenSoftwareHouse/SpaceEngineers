using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using SharpDX.DXGI;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Direct3D;
using System.Management;

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
            if(m_factory == null)
            {
                m_factory = new Factory();
            }
            return m_factory;
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

                MyRender11.Log.WriteLine("WMI {");
                MyRender11.Log.IncreaseIndent();

                //enumerate the collection.
                foreach (ManagementObject m in queryCollection)
                {
                    MyRender11.Log.WriteLine("{");
                    MyRender11.Log.IncreaseIndent();

                    MyRender11.Log.WriteLine("Caption = " + m["Caption"]);
                    MyRender11.Log.WriteLine("AdapterRam = " + m["AdapterRam"]);
                    MyRender11.Log.WriteLine("DriverVersion = " + m["DriverVersion"]);
                    MyRender11.Log.WriteLine("DriverDate = " + m["DriverDate"]);
                    MyRender11.Log.WriteLine("Description = " + m["Description"]);
                    MyRender11.Log.WriteLine("DeviceID = " + m["DeviceID"]);
                    MyRender11.Log.WriteLine("Name = " + m["Name"]);
                    MyRender11.Log.WriteLine("VideoProcessor = " + m["VideoProcessor"]);
                    MyRender11.Log.WriteLine("VideoArchitecture = " + m["VideoArchitecture"]);
                    MyRender11.Log.WriteLine("PNPDeviceID = " + m["PNPDeviceID"]);
                    MyRender11.Log.WriteLine("InstalledDisplayDrivers = " + m["InstalledDisplayDrivers"]);

                    MyRender11.Log.DecreaseIndent();
                    MyRender11.Log.WriteLine("}");
                }
                MyRender11.Log.DecreaseIndent();
                MyRender11.Log.WriteLine("}");
            }
            catch
            {
            }
        }

        static void LogAdapterInfoBegin(ref MyAdapterInfo info)
        {
            MyRender11.Log.WriteLine("AdapterInfo = {");
            MyRender11.Log.IncreaseIndent();
            MyRender11.Log.WriteLine("Name = " + info.Name);
            MyRender11.Log.WriteLine("Device name = " + info.DeviceName);
            MyRender11.Log.WriteLine("Description = " + info.Description);
            MyRender11.Log.WriteLine("DXGIAdapter id = " + info.AdapterDeviceId);
            MyRender11.Log.WriteLine("SUPPORTED = " + info.IsDx11Supported);
            MyRender11.Log.WriteLine("VRAM = " + info.VRAM);
            MyRender11.Log.WriteLine("Multithreaded rendering supported = " + info.MultithreadedRenderingSupported);
        }

        static void LogAdapterInfoEnd()
        {
            MyRender11.Log.DecreaseIndent();
            MyRender11.Log.WriteLine("}");
        }

        static void LogOutputDisplayModes(ref MyAdapterInfo info)
        {
            MyRender11.Log.WriteLine("Display modes = {");
            MyRender11.Log.IncreaseIndent();
            MyRender11.Log.WriteLine("DXGIOutput id = " + info.OutputId);
            for(int i=0; i< info.SupportedDisplayModes.Length; i++)
            {
                MyRender11.Log.WriteLine(info.SupportedDisplayModes[i].ToString());
            }
            MyRender11.Log.DecreaseIndent();
            MyRender11.Log.WriteLine("}");
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

                    Has512MBRam = vram > 500000000,
                    HDRSupported = true,
                    MaxTextureSize = SharpDX.Direct3D11.Texture2D.MaximumTexture2DSize,

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

                if(supportedDevice && vram > 0)
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
                }
                /* nope, not supported
                else
                {
                    info.SupportedDisplayModes = new MyDisplayMode[0];
                    adaptersList.Add(info);
                    adapterIndex++;
                }
                */
                LogAdapterInfoEnd();

                if(adapterTestDevice != null)
                {
                    adapterTestDevice.Dispose();
                    adapterTestDevice = null;
                }
            }

            return adaptersList.ToArray();
        }

		internal unsafe static MyAdapterInfo[] GetAdaptersList()
		{
            if(m_adapterInfoList == null)
            {
                m_adapterInfoList = CreateAdaptersList();
            }

            return m_adapterInfoList;
		}
	}
}
