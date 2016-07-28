using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

using System.Windows.Forms;

using SharpDX.Direct3D9;
using Microsoft.Win32;
using VRage.Utils;

namespace VRageRender
{
    public class MyGraphicTest
    {
        static int[] GoodGraphicsVendorIDs = new int[]
        {
            0x10DE, //nVidia
            0x1002, //ATI/AMD
        };

        private const string dxRegistryKey = "Software\\Microsoft\\Direct3D";
        private const string dxDebugRuntimeRegistryValue = "LoadDebugRuntime";

        private UInt32 m_VertexShaderVersionMinor;
        private UInt32 m_VertexShaderVersionMajor;
        private UInt32 m_PixelShaderVersionMinor;
        private UInt32 m_PixelShaderVersionMajor;
        private bool m_SeparateAlphaBlend;
        private bool m_DestBlendSrcAlphaSat;
        private UInt32 m_MaxPrimitiveCount;
        private bool m_IndexElementSize32;
        private int m_MaxVertexStreams;
        private int m_MaxStreamStride;
        private int m_MaxTextureSize;
        private int m_MaxVolumeExtent;
        private int m_MaxTextureAspectRatio;
        private int m_MaxVertexSamplers;
        private int m_MaxRenderTargets;
        private bool m_NonPow2Unconditional;
        private bool m_NonPow2Cube;
        private bool m_NonPow2Volume;
        private List<Format> m_ValidTextureFormats;
        private List<Format> m_ValidCubeFormats;
        private List<Format> m_ValidVolumeFormats;
        private List<Format> m_ValidVertexTextureFormats;
        private List<Format> m_InvalidFilterFormats;
        private List<Format> m_InvalidBlendFormats;
        private List<DeclarationType> m_ValidVertexFormats;
        static List<int> m_WMIGraphicsCards = new List<int>();
        static List<int> m_DXGraphicsCards = new List<int>();

        public bool IsBetterGCAvailable = false;

        public MyGraphicTest()
        {
        }

        // Testing function call - creates DX9 device & present test:
        public bool TestDX(Direct3D d3dh, ref MyAdapterInfo[] infos)
        {
#if !XB1
            bool isAnyGraphicsSupported = false;
            MyLog.Default.WriteLine("MyGraphicTest.TestDX() - START");
            MyLog.Default.IncreaseIndent();

            LogInfoFromWMI();

            bool isAnyGoodGCinWMI = IsAnyGoodGCinList(m_WMIGraphicsCards);
            MyLog.Default.WriteLine("Good graphics in WMI detected: " + isAnyGoodGCinWMI);

            //Check debug runtime
            MyLog.Default.WriteLine("Debug runtime enabled: " + IsDebugRuntimeEnabled);

            PresentParameters newPresentParameters;

            try
            {
                MyLog.Default.WriteLine("Adapter count: " + d3dh.AdapterCount);
                for (int i = 0; i < d3dh.AdapterCount; i++)
                {
                    var info = d3dh.GetAdapterIdentifier(i);
                    MyLog.Default.WriteLine(String.Format("Found adapter: {0} ({1})", info.Description, info.DeviceName));
                }
                MyLog.Default.WriteLine("Adapter count: " + d3dh.AdapterCount);

                // DX:
                newPresentParameters = new PresentParameters();
                newPresentParameters.InitDefaults();
                newPresentParameters.Windowed = true;
                newPresentParameters.AutoDepthStencilFormat = Format.D24S8;
                newPresentParameters.EnableAutoDepthStencil = true;
                newPresentParameters.SwapEffect = SwapEffect.Discard;
                newPresentParameters.PresentFlags = PresentFlags.DiscardDepthStencil;

                m_DXGraphicsCards.Clear();

                // Write adapter information to the LOG file:
                MyLog.Default.WriteLine("Adapters count: " + d3dh.AdapterCount);
                MyLog.Default.WriteLine("Adapter array count: " + d3dh.Adapters.Count);

                for (int adapter = 0; adapter < d3dh.AdapterCount; adapter++)
                {
                    bool adapterSupported = false;

                    var adapterIdentifier = d3dh.GetAdapterIdentifier(adapter);
                    MyLog.Default.WriteLine("Adapter " + adapterIdentifier.Description + ": " + adapterIdentifier.DeviceName);

                    Device d3d = null;
                    Form testForm = null;

                    try
                    {
                        //Create window, because other this fails on some ATIs..
                        testForm = new Form();
                        testForm.ClientSize = new System.Drawing.Size(64, 64);
                        testForm.StartPosition = FormStartPosition.CenterScreen;
                        testForm.FormBorderStyle = FormBorderStyle.None;
                        testForm.BackColor = System.Drawing.Color.Black;
                        testForm.Show();

                        newPresentParameters.DeviceWindowHandle = testForm.Handle;
                        d3d = new Device(d3dh, adapter, DeviceType.Hardware, testForm.Handle, CreateFlags.HardwareVertexProcessing | CreateFlags.FpuPreserve, newPresentParameters);

                        if (d3d == null)
                        {
                            throw new Exception("Cannot create Direct3D Device");
                        }
                        else
                            MyLog.Default.WriteLine("d3d handle ok ");
                    }
                    catch (Exception e)
                    {
                        if (testForm != null)
                            testForm.Close();

                        MyLog.Default.WriteLine("Direct3D Device create fail");
                        MyLog.Default.WriteLine(e.ToString());

                        Write(newPresentParameters, MyLog.Default.WriteLine);
                        continue;
                    }

                    adapterSupported |= !TestCapabilities(d3d, d3dh, adapter);

                    infos[adapter].MaxTextureSize = d3d.Capabilities.MaxTextureWidth;

                    bool Rgba1010102Supported = d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.RenderTarget, ResourceType.Surface, Format.A2R10G10B10);
                    MyLog.Default.WriteLine("Rgba1010102Supported: " + Rgba1010102Supported);

                    bool MipmapNonPow2Supported = !d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.Pow2) &&
                        !d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.NonPow2Conditional) &&
                        d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.MipMap);
                    MyLog.Default.WriteLine("MipmapNonPow2Supported: " + MipmapNonPow2Supported);

                    infos[adapter].HDRSupported = Rgba1010102Supported && MipmapNonPow2Supported;
                    MyLog.Default.WriteLine("HDRSupported: " + infos[adapter].HDRSupported);

                    bool QueriesSupported = false;
                    try
                    {
                        MyLog.Default.WriteLine("Create query");
                        Query query = new Query(d3d, QueryType.Event);
                        MyLog.Default.WriteLine("Dispose query");
                        query.Dispose();
                        QueriesSupported = true;
                    }
                    catch
                    {
                        QueriesSupported = false;
                    }

                    //Test sufficient video memory (512MB)
                    bool Has512AvailableVRAM = TestAvailable512VRAM(d3d);

                    //We require queries
                    adapterSupported &= QueriesSupported;

                    infos[adapter].IsDx9Supported = adapterSupported;
                    infos[adapter].Has512MBRam = Has512AvailableVRAM;

                    isAnyGraphicsSupported |= adapterSupported;

                    MyLog.Default.WriteLine("Queries supported: " + QueriesSupported.ToString());

                    m_DXGraphicsCards.Add(adapterIdentifier.VendorId);

                    if (d3d != null)
                    {
                        d3d.Dispose();
                        d3d = null;
                    }

                    if (testForm != null)
                        testForm.Close();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Exception throwed by DX test. Source: " + ex.Source);
                MyLog.Default.WriteLine("Message: " + ex.Message);
                MyLog.Default.WriteLine("Inner exception: " + ex.InnerException);
                MyLog.Default.WriteLine("Exception details" + ex.ToString());
            }

            bool isAnyGoodGCinDX = IsAnyGoodGCinList(m_DXGraphicsCards);
            MyLog.Default.WriteLine("Good graphics in DX detected: " + isAnyGoodGCinDX);

            IsBetterGCAvailable = isAnyGoodGCinWMI && !isAnyGoodGCinDX;
            MyLog.Default.WriteLine("Is better graphics available: " + IsBetterGCAvailable);

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyGraphicTest.TestDX() - END");

            return isAnyGraphicsSupported;
#else // XB1
            System.Diagnostics.Debug.Assert(false, "XB1 TOOD?");
            return false;
#endif // XB1
        }

        public static void Write(PresentParameters parameters, Action<string> lineWriter)
        {
            lineWriter("AutoDepthStencilFormat: " + parameters.AutoDepthStencilFormat.ToString());
            lineWriter("BackBufferCount: " + parameters.BackBufferCount.ToString());
            lineWriter("BackBufferFormat: " + parameters.BackBufferFormat.ToString());
            lineWriter("BackBufferHeight: " + parameters.BackBufferHeight.ToString());
            lineWriter("BackBufferWidth: " + parameters.BackBufferWidth.ToString());
            lineWriter("DeviceWindowHandle: " + (parameters.DeviceWindowHandle == null ? "null" : parameters.DeviceWindowHandle.ToString()));
            lineWriter("EnableAutoDepthStencil: " + parameters.EnableAutoDepthStencil.ToString());
            lineWriter("FullScreenRefreshRateInHz: " + parameters.FullScreenRefreshRateInHz.ToString());
            lineWriter("MultiSampleQuality: " + parameters.MultiSampleQuality.ToString());
            lineWriter("MultiSampleType: " + parameters.MultiSampleType.ToString());
            lineWriter("PresentationInterval: " + parameters.PresentationInterval.ToString());
            lineWriter("PresentFlags: " + parameters.PresentFlags.ToString());
            lineWriter("SwapEffect: " + parameters.SwapEffect.ToString());
            lineWriter("Windowed: " + parameters.Windowed.ToString());
        }

        private static bool IsAnyGoodGCinList(List<int> list)
        {
            //return list.Any(gc => (gc.ToLower().Contains("nvidia") || gc.ToLower().Contains("ati") || gc.ToLower().Contains("amd") || gc.ToLower().Contains("radeon")));
            return list.Any(gc => (GoodGraphicsVendorIDs.Contains(gc)));
        }

        public static bool IsDebugRuntimeEnabled
        {
            get
            {
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(dxRegistryKey, false);
                    if (key == null)
                        return false;
                    var keyValue = key.GetValue(dxDebugRuntimeRegistryValue);
                    return keyValue != null && keyValue.Equals(1);
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void LogInfoFromWMI()
        {
            m_WMIGraphicsCards.Clear();

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

                MyLog.Default.IncreaseIndent();
                MyLog.Default.WriteLine("WMI Data");
                //enumerate the collection.
                foreach (ManagementObject m in queryCollection)
                {

                    // access properties of the WMI object
                    MyLog.Default.WriteLine("Caption:    " + m["Caption"]);
                    MyLog.Default.WriteLine("RAM:    " + m["AdapterRam"]);
                    MyLog.Default.WriteLine("Driver version:    " + m["DriverVersion"]);
                    MyLog.Default.WriteLine("Driver date:    " + m["DriverDate"]);
                    MyLog.Default.WriteLine("Description:    " + m["Description"]);
                    MyLog.Default.WriteLine("DeviceID:    " + m["DeviceID"]);
                    MyLog.Default.WriteLine("Name:    " + m["Name"]);
                    MyLog.Default.WriteLine("VideoProcessor:    " + m["VideoProcessor"]);
                    MyLog.Default.WriteLine("VideoArchitecture:    " + m["VideoArchitecture"]);
                    MyLog.Default.WriteLine("PNPDeviceID:    " + m["PNPDeviceID"]);
                    MyLog.Default.WriteLine("InstalledDisplayDrivers:    " + m["InstalledDisplayDrivers"]);
                    MyLog.Default.WriteLine("-------------------------------------------");

                    m_WMIGraphicsCards.Add(GetVendorID(m["PNPDeviceID"].ToString()));
                }
                MyLog.Default.DecreaseIndent();
            }
            catch
            {
            }
        }

        static int GetVendorID(string pnp)
        {
            string id = pnp.Replace("PCI\\VEN_", "");
            id = id.Remove(4);

            int idInt = Int32.Parse(id, System.Globalization.NumberStyles.HexNumber);

            return idInt;
        }

        public static bool TestAvailable512VRAM(Device d3d)
        {
            return d3d.AvailableTextureMemory >= 448 * 1024 * 1024;
        }

        // Testing values for correct Reach: 
        private void SetReachTestSettings()
        {
            {
                m_VertexShaderVersionMinor = 0;
                m_VertexShaderVersionMajor = 2;
                m_PixelShaderVersionMinor = 0;
                m_PixelShaderVersionMajor = 2;
                m_SeparateAlphaBlend = false;
                m_DestBlendSrcAlphaSat = false;
                m_MaxPrimitiveCount = 65535;
                m_IndexElementSize32 = false;
                m_MaxVertexStreams = 16;
                m_MaxStreamStride = 255;
                m_MaxTextureSize = 2048;
                m_MaxVolumeExtent = 0;
                m_MaxTextureAspectRatio = 2048;
                m_MaxVertexSamplers = 0;
                m_MaxRenderTargets = 1;
                m_NonPow2Unconditional = false;
                m_NonPow2Cube = false;
                m_NonPow2Volume = false;
                m_ValidTextureFormats = new List<Format>();
                m_ValidTextureFormats.Add(Format.A8R8G8B8);
                m_ValidTextureFormats.Add(Format.R5G6B5);
                m_ValidTextureFormats.Add(Format.A1R5G5B5);
                m_ValidTextureFormats.Add(Format.A4R4G4B4);
                m_ValidTextureFormats.Add(Format.Dxt1);
                m_ValidTextureFormats.Add(Format.Dxt3);
                m_ValidTextureFormats.Add(Format.Dxt5);
                m_ValidTextureFormats.Add(Format.Q8W8V8U8);
                /*(SurfaceFormat.Color,SurfaceFormat.Bgr565,SurfaceFormat.Bgra5551,SurfaceFormat.Bgra4444,
                    SurfaceFormat.Dxt1, SurfaceFormat.Dxt3, SurfaceFormat.Dxt5,
                    SurfaceFormat.NormalizedByte2, SurfaceFormat.NormalizedByte4);*/
                m_ValidCubeFormats = new List<Format>();
                m_ValidCubeFormats.Add(Format.A8R8G8B8);
                m_ValidCubeFormats.Add(Format.R5G6B5);
                m_ValidCubeFormats.Add(Format.A1R5G5B5);
                m_ValidCubeFormats.Add(Format.A4R4G4B4);
                m_ValidCubeFormats.Add(Format.Dxt1);
                m_ValidCubeFormats.Add(Format.Dxt3);
                m_ValidCubeFormats.Add(Format.Dxt5);
                /*(SurfaceFormat.Color,SurfaceFormat.Bgr565,SurfaceFormat.Bgra5551,SurfaceFormat.Bgra4444,
                    SurfaceFormat.Dxt1, SurfaceFormat.Dxt3, SurfaceFormat.Dxt5);*/
                m_ValidVolumeFormats = new List<Format>();
                m_ValidVertexTextureFormats = new List<Format>();
                m_InvalidFilterFormats = new List<Format>();
                m_InvalidBlendFormats = new List<Format>();
                m_ValidVertexFormats = new List<DeclarationType>();
                m_ValidVertexFormats.Add(DeclarationType.Color);
                m_ValidVertexFormats.Add(DeclarationType.Float1);
                m_ValidVertexFormats.Add(DeclarationType.Float2);
                m_ValidVertexFormats.Add(DeclarationType.Float3);
                m_ValidVertexFormats.Add(DeclarationType.Float4);
                m_ValidVertexFormats.Add(DeclarationType.UByte4N);
                m_ValidVertexFormats.Add(DeclarationType.Short2);
                m_ValidVertexFormats.Add(DeclarationType.Short4);
                m_ValidVertexFormats.Add(DeclarationType.Short2N);
                m_ValidVertexFormats.Add(DeclarationType.Short4N);
                m_ValidVertexFormats.Add(DeclarationType.HalfTwo);
                m_ValidVertexFormats.Add(DeclarationType.HalfFour);
            }
        }

        // Same settings as Reach but with pixel & vertex shaders v 3_0 and above:
        private void SetTestSettings()
        {
            // same as Reach but with pixel & vertex sahder >=3.0:
            SetReachTestSettings();
            {
                m_VertexShaderVersionMinor = 0;
                m_VertexShaderVersionMajor = 3;
                m_PixelShaderVersionMinor = 0;
                m_PixelShaderVersionMajor = 3;
                m_MaxPrimitiveCount = 1000000;
            }
        }

        // Test profile:
        private bool TestCapabilities(Device d3d, Direct3D d3dh, int adapter)
        {
            SetTestSettings();
            return TestCurrentSettings(d3d, d3dh, adapter);
        }

        // Own DX capability testing function:
        private bool TestCurrentSettings(Device d3d, Direct3D d3dh, int adapter)
        {
#if !XB1
            MyLog.Default.WriteLine("MyGraphicTest.TestCurrentSettings() - START");
            MyLog.Default.IncreaseIndent();

            bool isError = false;


            MyLog.Default.IncreaseIndent();
            var detail = d3dh.GetAdapterIdentifier(adapter);
            if (detail != null)
            {
                MyLog.Default.WriteLine("Ordinal ID: 0x" + detail.DeviceId.ToString("X4"));
                MyLog.Default.WriteLine("Description: " + detail.Description);
                MyLog.Default.WriteLine("Vendor ID: 0x" + detail.VendorId.ToString("X4"));
                MyLog.Default.WriteLine("Device name: " + detail.DeviceName);
                MyLog.Default.WriteLine("Device identifier: " + detail.DeviceIdentifier.ToString());
                MyLog.Default.WriteLine("Driver name: " + detail.Driver);
                MyLog.Default.WriteLine("DirectX Driver version: " + detail.DriverVersion);
                MyLog.Default.WriteLine("Identifier of the adapter chip: 0x" + detail.DeviceId.ToString("X4"));
                MyLog.Default.WriteLine("Adapter certified: " + (detail.Certified ? "YES" : "NO"));
                if (detail.Certified) MyLog.Default.WriteLine("Certification date: " + detail.CertificationDate);
                MyLog.Default.WriteLine("Adapter revision: " + detail.Revision);
                MyLog.Default.WriteLine("Subsystem ID: 0x" + detail.SubsystemId.ToString("X8"));
                MyLog.Default.WriteLine("WHQL level: " + detail.WhqlLevel);
                MyLog.Default.WriteLine("Vertex shader version: " + d3d.Capabilities.PixelShaderVersion.Major + "." + d3d.Capabilities.PixelShaderVersion.Minor);
                MyLog.Default.WriteLine("Pixel shader version:  " + d3d.Capabilities.PixelShaderVersion.Major + "." + d3d.Capabilities.PixelShaderVersion.Minor);
                MyLog.Default.WriteLine("Max primitives count:  " + d3d.Capabilities.MaxPrimitiveCount);
                MyLog.Default.WriteLine("Max texture width:     " + d3d.Capabilities.MaxTextureWidth);
                MyLog.Default.WriteLine("Max texture height:    " + d3d.Capabilities.MaxTextureHeight);
                MyLog.Default.WriteLine("Max vertex streams:    " + d3d.Capabilities.MaxStreams);
                MyLog.Default.WriteLine("Max render targets:    " + d3d.Capabilities.SimultaneousRTCount);
            }

            MyLog.Default.DecreaseIndent();


            if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.DepthStencil, ResourceType.Surface, Format.D24S8))
            {
                MyLog.Default.WriteLine("ERROR: Device does not support D24S8 depth format!");
                isError = true;
            }

            // Test only shared versions from now:
            // Test vertex shader version:
            if (!(d3d.Capabilities.VertexShaderVersion.Major >= m_VertexShaderVersionMajor &&
                d3d.Capabilities.VertexShaderVersion.Minor >= m_VertexShaderVersionMinor))
            {
                MyLog.Default.WriteLine("PixelShader 3.0 is not available");
                isError = true;
            }
            // Test pixel shader version:
            if (!(d3d.Capabilities.PixelShaderVersion.Major >= m_PixelShaderVersionMajor &&
                d3d.Capabilities.PixelShaderVersion.Minor >= m_PixelShaderVersionMinor))
            {
                MyLog.Default.WriteLine("Vertex shader 3.0 is not available");
                isError = true;
            }
            // Test basic rendering caps:
            if (d3d.Capabilities.MaxPrimitiveCount < m_MaxPrimitiveCount)
            {
                MyLog.Default.WriteLine("MaxPrimitiveCount smaller than needed");
                isError = true;
            }
            if (d3d.Capabilities.MaxStreams < m_MaxVertexStreams)
            {
                MyLog.Default.WriteLine("MaxVertexStreams smaller than needed");
                isError = true;
            }
            if (d3d.Capabilities.MaxStreamStride < m_MaxStreamStride)
            {
                MyLog.Default.WriteLine("MaxStreamStride smaller than needed");
                isError = true;
            }
            if (d3d.Capabilities.MaxVertexIndex < (m_IndexElementSize32 ? 16777214 : 65634))
            {
                MyLog.Default.WriteLine("MaxVertexIndex smaller than needed");
                isError = true;
            }
            if (!(d3d.Capabilities.DeviceCaps2.HasFlag(DeviceCaps2.CanStretchRectFromTextures)))
            {
                MyLog.Default.WriteLine("Device doesn't have  RectFromTextures");
                isError = true;
            }
            if (!(d3d.Capabilities.DeviceCaps2.HasFlag(DeviceCaps2.StreamOffset)))
            {
                MyLog.Default.WriteLine("Device doesn't have StreamOffset ability");
                isError = true;
            }
            if (!(d3d.Capabilities.RasterCaps.HasFlag(RasterCaps.DepthBias)))
            {
                MyLog.Default.WriteLine("Device doesn't have DepthBias ability in RasterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.RasterCaps.HasFlag(RasterCaps.MipMapLodBias)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipMapLodBias ability in RasterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.RasterCaps.HasFlag(RasterCaps.ScissorTest)))
            {
                MyLog.Default.WriteLine("Device doesn't have ScissorTest ability in RasterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.RasterCaps.HasFlag(RasterCaps.SlopeScaleDepthBias)))
            {
                MyLog.Default.WriteLine("Device doesn't have SlopeScaleDepthBias ability in RasterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.ShadeCaps.HasFlag(ShadeCaps.ColorGouraudRgb)))
            {
                MyLog.Default.WriteLine("Device doesn't have ColorGouraudRgb ability in ShadeCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.ShadeCaps.HasFlag(ShadeCaps.AlphaGouraudBlend)))
            {
                MyLog.Default.WriteLine("Device doesn't have AlphaGouraudBlend ability in ShadeCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.MaskZ)))
            {
                MyLog.Default.WriteLine("Device doesn't have MaskZ ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.CullNone)))
            {
                MyLog.Default.WriteLine("Device doesn't have CullNone ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.CullCW)))
            {
                MyLog.Default.WriteLine("Device doesn't have CullCW ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.CullCCW)))
            {
                MyLog.Default.WriteLine("Device doesn't have CullCCW ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.ColorWriteEnable)))
            {
                MyLog.Default.WriteLine("Device doesn't have ColorWriteEnable ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.BlendOperation)))
            {
                MyLog.Default.WriteLine("Device doesn't have BlendOperation ability in PrimitiveMiscCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.LineCaps.HasFlag(LineCaps.Blend)))
            {
                MyLog.Default.WriteLine("Device doesn't have Blend ability in LineCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.LineCaps.HasFlag(LineCaps.Texture)))
            {
                MyLog.Default.WriteLine("Device doesn't have Texture ability in LineCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.LineCaps.HasFlag(LineCaps.DepthTest)))
            {
                MyLog.Default.WriteLine("Device doesn't have DepthTest ability in LineCaps");
                isError = true;
            }
            // Test depth test:
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.Always)))
            {
                MyLog.Default.WriteLine("Device doesn't have Always ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.Equal)))
            {
                MyLog.Default.WriteLine("Device doesn't have Equal ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.Greater)))
            {
                MyLog.Default.WriteLine("Device doesn't have Greater ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.GreaterEqual)))
            {
                MyLog.Default.WriteLine("Device doesn't have GreaterEqual ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.Less)))
            {
                MyLog.Default.WriteLine("Device doesn't have Less ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.LessEqual)))
            {
                MyLog.Default.WriteLine("Device doesn't have LessEqual ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.Never)))
            {
                MyLog.Default.WriteLine("Device doesn't have Never ability in DepthCompareCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DepthCompareCaps.HasFlag(CompareCaps.NotEqual)))
            {
                MyLog.Default.WriteLine("Device doesn't have NotEqual ability in DepthCompareCaps");
                isError = true;
            }
            // Test stencil test:
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Decrement)))
            {
                MyLog.Default.WriteLine("Device doesn't have Decrement ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.DecrementClamp)))
            {
                MyLog.Default.WriteLine("Device doesn't have DecrementClamp ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Increment)))
            {
                MyLog.Default.WriteLine("Device doesn't have Increment ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.IncrementClamp)))
            {
                MyLog.Default.WriteLine("Device doesn't have IncrementClamp ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Invert)))
            {
                MyLog.Default.WriteLine("Device doesn't have Invert ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Keep)))
            {
                MyLog.Default.WriteLine("Device doesn't have Keep ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Replace)))
            {
                MyLog.Default.WriteLine("Device doesn't have Replace ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.TwoSided)))
            {
                MyLog.Default.WriteLine("Device doesn't have TwoSided ability in StencilCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.StencilCaps.HasFlag(StencilCaps.Zero)))
            {
                MyLog.Default.WriteLine("Device doesn't have Zero ability in StencilCaps");
                isError = true;
            }
            // Test blending caps:
            // source:
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.BlendFactor)))
            {
                MyLog.Default.WriteLine("Device doesn't have BlendFactor ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.DestinationAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have DestinationAlpha ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.DestinationColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have DestinationColor ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.InverseDestinationAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseDestinationAlpha ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.InverseDestinationColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseDestinationColor ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.InverseSourceAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseSourceAlpha ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.InverseSourceColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseSourceColor ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.One)))
            {
                MyLog.Default.WriteLine("Device doesn't have One ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.SourceAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have SourceAlpha ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.SourceAlphaSaturated)))
            {
                MyLog.Default.WriteLine("Device doesn't have SourceAlphaSaturated ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.SourceColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have SourceColor ability in SourceBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.SourceBlendCaps.HasFlag(BlendCaps.Zero)))
            {
                MyLog.Default.WriteLine("Device doesn't have Zero ability in SourceBlendCaps");
                isError = true;
            }
            // destination:
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.BlendFactor)))
            {
                MyLog.Default.WriteLine("Device doesn't have BlendFactor ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.DestinationAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have DestinationAlpha ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.DestinationColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have DestinationColor ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.InverseDestinationAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseDestinationAlpha ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.InverseDestinationColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseDestinationColor ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.InverseSourceAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseSourceAlpha ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.InverseSourceColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have InverseSourceColor ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.One)))
            {
                MyLog.Default.WriteLine("Device doesn't have One ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.SourceAlpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have SourceAlpha ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.SourceColor)))
            {
                MyLog.Default.WriteLine("Device doesn't have SourceColor ability in DestinationBlendCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.Zero)))
            {
                MyLog.Default.WriteLine("Device doesn't have Zero ability in DestinationBlendCaps");
                isError = true;
            }
            // simply test blend source alpha saturation:
            if (m_DestBlendSrcAlphaSat)
            {
                if (!(d3d.Capabilities.DestinationBlendCaps.HasFlag(BlendCaps.SourceAlphaSaturated)))
                {
                    MyLog.Default.WriteLine("Device doesn't have BlendSourceAlphaSaturated ability in DestinationBlendCaps");
                    isError = true;
                }
            }
            // Simply test separate alpha blend:
            if (m_SeparateAlphaBlend)
            {
                if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.SeparateAlphaBlend)))
                {
                    MyLog.Default.WriteLine("Device doesn't have SeparateAlphaBlend ability in PrimitiveMiscCaps");
                    isError = true;
                }
            }
            // Test multiple render targets:
            if (d3d.Capabilities.SimultaneousRTCount < m_MaxRenderTargets)
            {
                MyLog.Default.WriteLine("MaxRenderTargets smaller than needed");
                isError = true;
            }
            if (m_MaxRenderTargets > 1)
            {
                if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.IndependentWriteMasks)))
                {
                    MyLog.Default.WriteLine("Device doesn't have IndependentWriteMasks ability in PrimitiveMiscCaps for more than 1 render targets");
                    isError = true;
                }
                if (!(d3d.Capabilities.PrimitiveMiscCaps.HasFlag(PrimitiveMiscCaps.MrtPostPixelShaderBlending)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MrtPostPixelShaderBlending ability in PrimitiveMiscCaps for more than 1 render targets");
                    isError = true;
                }
            }
            // Test texturing abilities:
            if (d3d.Capabilities.MaxTextureWidth < m_MaxTextureSize)
            {
                MyLog.Default.WriteLine("MaxTextureWidth smaller than needed");
                isError = true;
            }
            if (d3d.Capabilities.MaxTextureHeight < m_MaxTextureSize)
            {
                MyLog.Default.WriteLine("MaxTextureHeight smaller than needed");
                isError = true;
            }
            // Test aspect ration:
            if (d3d.Capabilities.MaxTextureAspectRatio > 0)
            {
                if (d3d.Capabilities.MaxTextureAspectRatio < m_MaxTextureAspectRatio)
                {
                    MyLog.Default.WriteLine("MaxTextureAspectRatio smaller than needed");
                    isError = true;
                }
            }
            // Test textures abilities:
            if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.Alpha)))
            {
                MyLog.Default.WriteLine("Device doesn't have Alpha ability in TextureCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.MipMap)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipMap ability in TextureCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.CubeMap)))
            {
                MyLog.Default.WriteLine("Device doesn't have CubeMap ability in TextureCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.MipCubeMap)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipCubeMap ability in TextureCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.Perspective)))
            {
                MyLog.Default.WriteLine("Device doesn't have Perspective ability in TextureCaps");
                isError = true;
            }
            if (d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.SquareOnly))
            {
                MyLog.Default.WriteLine("Device doesn't have SquareOnly ability in TextureCaps");
                isError = true;
            }
            // Test texture address caps:
            if (!(d3d.Capabilities.TextureAddressCaps.HasFlag(TextureAddressCaps.Clamp)))
            {
                MyLog.Default.WriteLine("Device doesn't have Clamp ability in TextureAddressCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureAddressCaps.HasFlag(TextureAddressCaps.Wrap)))
            {
                MyLog.Default.WriteLine("Device doesn't have Wrap ability in TextureAddressCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureAddressCaps.HasFlag(TextureAddressCaps.Mirror)))
            {
                MyLog.Default.WriteLine("Device doesn't have Mirror ability in TextureAddressCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureAddressCaps.HasFlag(TextureAddressCaps.IndependentUV)))
            {
                MyLog.Default.WriteLine("Device doesn't have IndependentUV ability in TextureAddressCaps");
                isError = true;
            }
            // Test texture filter caps:
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MagPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MagPoint ability in TextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MagLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MagLinear ability in TextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MinPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MinPoint ability in TextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MinLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MinLinear ability in TextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MipPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipPoint ability in TextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.TextureFilterCaps.HasFlag(FilterCaps.MipLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipLinear ability in TextureFilterCaps");
                isError = true;
            }
            // test cube texture filter caps:
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MagPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MagPoint ability in CubeTextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MagLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MagLinear ability in CubeTextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MinPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MinPoint ability in CubeTextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MinLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MinLinear ability in CubeTextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MipPoint)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipPoint ability in CubeTextureFilterCaps");
                isError = true;
            }
            if (!(d3d.Capabilities.CubeTextureFilterCaps.HasFlag(FilterCaps.MipLinear)))
            {
                MyLog.Default.WriteLine("Device doesn't have MipLinear ability in CubeTextureFilterCaps");
                isError = true;
            }
            // test volume texures:
            if (m_MaxVolumeExtent > 0)
            {
                if (d3d.Capabilities.MaxVolumeExtent < m_MaxVolumeExtent)
                {
                    MyLog.Default.WriteLine("MaxVolumeExtent smaller than needed");
                    isError = true;
                }

                // test volume maps:
                if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.VolumeMap)))
                {
                    MyLog.Default.WriteLine("Device doesn't have VolumeMap ability in TextureCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.MipVolumeMap)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MipVolumeMap ability in TextureCaps");
                    isError = true;
                }
                // test volume texture address caps:
                if (!(d3d.Capabilities.VolumeTextureAddressCaps.HasFlag(TextureAddressCaps.Clamp)))
                {
                    MyLog.Default.WriteLine("Device doesn't have Clamp ability in VolumeTextureAddressCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureAddressCaps.HasFlag(TextureAddressCaps.Wrap)))
                {
                    MyLog.Default.WriteLine("Device doesn't have Wrap ability in VolumeTextureAddressCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureAddressCaps.HasFlag(TextureAddressCaps.Mirror)))
                {
                    MyLog.Default.WriteLine("Device doesn't have Mirror ability in VolumeTextureAddressCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureAddressCaps.HasFlag(TextureAddressCaps.IndependentUV)))
                {
                    MyLog.Default.WriteLine("Device doesn't have IndependentUV ability in VolumeTextureAddressCaps");
                    isError = true;
                }
                // test volume texture filter caps:
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MagPoint)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MagPoint ability in VolumeTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MagLinear)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MagLinear ability in VolumeTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MinPoint)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MinPoint ability in VolumeTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MinLinear)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MinLinear ability in VolumeTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MipPoint)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MipPoint ability in VolumeTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VolumeTextureFilterCaps.HasFlag(FilterCaps.MipLinear)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MipLinear ability in VolumeTextureFilterCaps");
                    isError = true;
                }
            }
            // test non power of two textures:
            if (m_NonPow2Unconditional)
            {
                if (d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.Pow2))
                {
                    MyLog.Default.WriteLine("Device doesn't have Pow2textures ability in TextureCaps");
                    isError = true;
                }
            }
            else
            {
                if (d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.Pow2))
                {
                    if (!(d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.NonPow2Conditional)))
                    {
                        MyLog.Default.WriteLine("Device doesn't have NonPow2Conditional ability in TextureCaps");
                        isError = true;
                    }
                }
            }
            // test non power of two cube textures:
            if (m_NonPow2Cube)
            {
                if (d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.CubeMapPow2))
                {
                    MyLog.Default.WriteLine("Device doesn't have CubeMapPow2 ability in TextureCaps");
                    isError = true;
                }
            }
            // test non power of two volume textures:
            if (m_NonPow2Volume)
            {
                if (d3d.Capabilities.TextureCaps.HasFlag(TextureCaps.VolumeMapPow2))
                {
                    MyLog.Default.WriteLine("Device doesn't have VolumeMapPow2 ability in TextureCaps");
                    isError = true;
                }
            }
            // Test vertex texturing:
            if (m_MaxVertexSamplers > 0)
            {
                if (!(d3d.Capabilities.VertexTextureFilterCaps.HasFlag(FilterCaps.MagPoint)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MagPoint ability in VertexTextureFilterCaps");
                    isError = true;
                }
                if (!(d3d.Capabilities.VertexTextureFilterCaps.HasFlag(FilterCaps.MinPoint)))
                {
                    MyLog.Default.WriteLine("Device doesn't have MinPoint ability in VertexTextureFilterCaps");
                    isError = true;
                }
            }
            // Test vertex element formats:
            if (m_ValidVertexFormats != null)
            {
                foreach (DeclarationType format in m_ValidVertexFormats)
                {
                    switch (format)
                    {
                        case DeclarationType.Color:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.UByte4N)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have UByte4N as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                        case DeclarationType.UByte4N:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.UByte4)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have UByte4 as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                        case DeclarationType.Short2N:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.Short2N)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have NormalizedShort2 as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                        case DeclarationType.Short4N:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.Short4N)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have Short4N as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                        case DeclarationType.HalfTwo:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.HalfTwo)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have HalfTwo as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                        case DeclarationType.HalfFour:
                            if (!(d3d.Capabilities.DeclarationTypes.HasFlag(DeclarationTypeCaps.HalfFour)))
                            {
                                MyLog.Default.WriteLine("Device doesn't have UByte4N as VertexElementFormat type in Declaration");
                                isError = true;
                            }
                            break;
                    }
                }
            }
            // Test texture formats:
            if (m_ValidTextureFormats != null)
            {
                foreach (Format format in m_ValidTextureFormats)
                {
                    // format supported?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, 0, ResourceType.Texture, format))
                    {
                        string text = String.Format("Device doesn't support DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support mipmapping?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryWrapAndMip, ResourceType.Texture, format))
                    {
                        string text = String.Format("Device doesn't support MipMapping for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support filtering?
                    if (!m_InvalidFilterFormats.Contains(format))
                    {
                        if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryFilter, ResourceType.Texture, format))
                        {
                            string text = String.Format("Device doesn't support QueryFiltering for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                            MyLog.Default.WriteLine(text);
                            isError = true;
                        }
                    }
                }
            }
            // Test cubemap formats:
            if (m_ValidCubeFormats != null)
            {
                foreach (Format format in m_ValidCubeFormats)
                {
                    // format supported?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, 0, ResourceType.CubeTexture, format))
                    {
                        string text = String.Format("Device doesn't support DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support mipmapping?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryWrapAndMip, ResourceType.CubeTexture, format))
                    {
                        string text = String.Format("Device doesn't support MipMapping for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support filtering?
                    if (!m_InvalidFilterFormats.Contains(format))
                    {
                        if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryFilter, ResourceType.CubeTexture, format))
                        {
                            string text = String.Format("Device doesn't support QueryFiltering for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                            MyLog.Default.WriteLine(text);
                            isError = true;
                        }
                    }
                }
            }
            // Test volume texture formats:
            if (m_ValidVolumeFormats != null)
            {
                foreach (Format format in m_ValidVolumeFormats)
                {
                    // format supported?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, 0, ResourceType.VolumeTexture, format))
                    {
                        string text = String.Format("Device doesn't support DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support mipmapping?
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryWrapAndMip, ResourceType.VolumeTexture, format))
                    {
                        string text = String.Format("Device doesn't support MipMapping for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // does this format support filtering?
                    if (!m_InvalidFilterFormats.Contains(format))
                    {
                        if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, Usage.QueryFilter, ResourceType.VolumeTexture, format))
                        {
                            string text = String.Format("Device doesn't support QueryFiltering for texture DX format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                            MyLog.Default.WriteLine(text);
                            isError = true;
                        }
                    }
                }
            }
            // Test vertex texture format:
            if (m_ValidVertexTextureFormats != null)
            {
                foreach (Format format in m_ValidVertexTextureFormats)
                {
                    Usage usage = Usage.QueryVertexTexture | Usage.QueryWrapAndMip;
                    if (!m_InvalidBlendFormats.Contains(format))
                    {
                        usage |= Usage.QueryFilter;
                    }
                    // 2D vertex texture:
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, usage, ResourceType.Texture, format))
                    {
                        string text = String.Format("Device doesn't support VertexTextureFormat for DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // Cubemap vertex texture:
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, usage, ResourceType.Texture, format))
                    {
                        string text = String.Format("Device doesn't support VertexCubemapTextureFormat for DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                    // Volume vertex texture:
                    if (!d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, usage, ResourceType.Texture, format))
                    {
                        string text = String.Format("Device doesn't support VertexVolumeTextureFormat for DX texture format { 0 } [XNA format: { 0 }]", format.ToString(), format.ToString());
                        MyLog.Default.WriteLine(text);
                        isError = true;
                        continue;
                    }
                }
            }
            // Test render target format:
            if (m_InvalidBlendFormats != null)
            {
                Usage usage = Usage.RenderTarget;
                if (!m_InvalidBlendFormats.Contains(Format.A8R8G8B8))
                {
                    usage |= Usage.QueryPostPixelShaderBlending;
                }
                if (!(d3dh.CheckDeviceFormat(adapter, DeviceType.Hardware, Format.X8R8G8B8, usage, ResourceType.Surface, Format.A8R8G8B8)))
                {
                    string text = String.Format("Device doesn't support RenderTarget for DX texture format { 0 }", Format.A8R8G8B8.ToString());
                    MyLog.Default.WriteLine(text);
                    isError = true;
                }
            }

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyGraphicTest.TestCurrentSettings() - END");

            return isError;
#else // XB1
            System.Diagnostics.Debug.Assert(false, "XB1 TOOD?");
            return true;
#endif // XB1
        }
    }
}
