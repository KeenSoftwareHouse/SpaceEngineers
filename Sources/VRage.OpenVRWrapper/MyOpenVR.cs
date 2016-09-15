using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VRage.Profiler;
#if !XB1 // XB1_NOOPENVRWRAPPER
using Valve.VR;
#endif // !XB1
using VRageMath;
using VRage.Utils;

namespace VRage.OpenVRWrapper
{
    public class LMUEntry
    {
        public MatrixD? LeftMult;
        public MatrixD origin;
        public ControllerRole assignedController;
        public LMUEntry(MatrixD? leftMult, MatrixD matrix, ControllerRole role)
        {
            LeftMult = leftMult;
            origin = matrix;
            assignedController = role;
        }
    }

    public class MyOpenVR
    {
#if XB1 // XB1_NOOPENVRWRAPPER
        static ControllerState m_controllerState = new ControllerState();

        public static MyOpenVR Static
        {
            get { return null; }
        }
        public static Matrix ViewHMD //view matrix of the headset position
        {
            get { return Matrix.Identity; } 
        }
        public static Matrix HeadsetMatrixD
        {
            get { return Matrix.Identity; }
        }
        public static Matrix Controller1Matrix
        {
            get { return Matrix.Identity; }
        }
        public static Matrix Controller2Matrix
        {
            get { return Matrix.Identity; }
        }
        public static float Ipd_2
        {
            get { return 0.0f; }
        }

        public static bool LmuDebugOnOff = false;
        public static bool Debug2DImage = false;//print 2D image from single frame

        public static void FadeToColor(float sec, Color color)
        {
        }
        public static void UnFade(float sec)
        {
        }
        public static void LMUAdd(MatrixD? leftMult, MatrixD origin, ControllerRole role, int reference)
        {
        }
        public static ControllerState GetControllerState(bool secondController)
        {
            return m_controllerState;
        }
        public static Vector2[] GetStencilMask()
        {
            return null;
        }
        public static void WaitForNextStart()
        {
        }
        public void DisplayEye(IntPtr nativePointer)
        {
        }
        public static void LMUMatrixGetOrigin(ref MatrixD origin, int reference)
        {
        }
        public static void LMUMatrixUpdate(ref MatrixD matrix, int reference)//simply said: replaces worldmatrix with new one :-)
        {
        }
        public static void FrameDone()
        {
        }
#else // !XB1
        private static MyOpenVR m_openVR;
        public static MyOpenVR Static
        {
            get { return m_openVR; }
        }
        //
        public static Matrix ViewHMD //view matrix of the headset position
        {
            get { return m_viewHMD; }
        }
        public static Matrix HeadsetMatrixD
        {
            get { return m_headsetPosD; }
        }

        public static Matrix Controller1Matrix
        {
            get { return m_c1pos;}
        }
        public static ControllerState Controller1State
        {
            get { return m_controller1State; }
        }

        public static Matrix Controller2Matrix
        {
            get { return m_c2pos; }
        }
        public static ControllerState Controller2State
        {
            get { return m_controller2State; }
        }

        public static float Ipd_2
        {
            get { return m_ipd_2; }
        }

        public static float FloorOffset = -0.05f;//real floor relative to Vive's calibrated virtual floor

        public static bool LmuDebugOnOff = true;
        public static bool Debug2DImage = false;//print 2D image from single frame
        public static bool SyncWait = true;

        public static ulong FrameCount
        {
            get{return m_currFrame;}
        }
        //privates:
        internal static MyLog Log = new MyLog();
        
        private static CVRCompositor m_vrCompositor;
        private static CVRSystem m_vrSystem;
        private static TrackedDevicePose_t[] renderPose = new TrackedDevicePose_t[16];
        private static TrackedDevicePose_t[] gamePose = new TrackedDevicePose_t[16];
        private static Matrix m_viewHMD = Matrix.Identity;
        private static MatrixD m_headsetPosD = MatrixD.Identity;
        private static Matrix m_c1pos = Matrix.Identity;
        private static Matrix m_c2pos = Matrix.Identity;
        static ControllerState m_controller1State = new ControllerState();
        static ControllerState m_controller2State = new ControllerState();

        private static float m_insideLimit, m_outsideLimit;//cutting from single frame, correct point in front of the eye with correct IPD

        private static float m_ipd_2;//half of IPD

        //for timing & angles prediction:
        static float m_lastRuntimeMs = 0;
        static float m_frameDuration = 1;
        static float m_frameDurationFiltered = 1;
        private static readonly float FILTER_CONST = 0.9f;

        //
        private static uint m_controller1ID = 999, m_controller2ID = 999;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static bool IsActive { get { return m_vrSystem != null; } }

        #region constructors and init
        static MyOpenVR()
        {
            const string logName = "VRage-openVR.log";
            Log.Init(logName, new System.Text.StringBuilder("Version unknown"));
            Log.WriteLine("OpenVR log started");

        }
        public MyOpenVR()
        {
            if (true)
            {
                m_viewHMD = Matrix.Identity;
                m_headsetPosD = MatrixD.Identity;
                m_c1pos = Matrix.Identity;
                m_c2pos = Matrix.Identity;

                //IntPtr Handle = MyRender11.LoadLibrary(@"D:\KeenSWH.VR\Sandbox\Sources\SpaceEngineers\bin\x64\Debug\Bin64\openvr_api.dll");
                //IntPtr Handle = LoadLibrary(@"C:\Program Files (x86)\Steam\SteamApps\common\SteamVR\bin\win64\openvr_api.dll");
                IntPtr Handle = LoadLibrary(@"openvr_api.dll");
                //IntPtr Handle = MyRender11.LoadLibrary(@"c:\Program Files (x86)\Steam\bin\openvr_api.dll");//err code 193 -  not a valid Win32 application
                //Assembly.LoadFile(@"D:\KeenSWH\Sandbox\Sources\SpaceEngineers\bin\x64\Debug\Bin64\openvr_api.dll");
                if (Handle == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Exception(string.Format("Failed to load library (ErrorCode: {0})", errorCode));
                }

                EVRInitError error = EVRInitError.None;
                IntPtr ptr = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
                if (error == EVRInitError.None)
                {
                    m_vrSystem = new CVRSystem(ptr);
                    
                    uint sizeX=0,sizeY=0;
                    m_vrSystem.GetRecommendedRenderTargetSize(ref sizeX, ref sizeY);
                    
                    m_vrSystem.SetDisplayVisibility(true);

                    ETrackedPropertyError pError=0;
                    m_ipd_2=0.5f*m_vrSystem.GetFloatTrackedDeviceProperty(0,ETrackedDeviceProperty.Prop_UserIpdMeters_Float,ref pError);
                    SetIPD(m_ipd_2*2);

                    IntPtr pointer = OpenVR.GetGenericInterface(OpenVR.IVRCompositor_Version, ref error);
                    if (error == EVRInitError.None)
                    {
                        m_vrCompositor = new CVRCompositor(pointer);
                        m_vrCompositor.CompositorBringToFront();
                        m_vrCompositor.ShowMirrorWindow();
                        m_openVR = this;
                    }
                    else
                    {
                        var errString = OpenVR.GetStringForHmdError(error);
                        Log.WriteLineAndConsole(errString);
                        Debug.Fail("No compositor interface");
                        throw new Exception(errString);
                    }
                }
                else
                {
                    var errString = OpenVR.GetStringForHmdError(error);
                    Log.WriteLineAndConsole(errString);
                    //Debug.Fail("OpenVR init failed");
                    throw new Exception(errString);
                }
                InitOverlays();
            }
        }
        #endregion
        #region profiling values
        //returns current frame and time within it
        public static void TimeSinceVsync(ref float m_timeSinceLastVsync, ref ulong m_currFrame)
        {
            m_vrSystem.GetTimeSinceLastVsync(ref m_timeSinceLastVsync, ref m_currFrame);
        }
        public static bool GetFrameTiming(ref Compositor_FrameTiming pTiming, uint unFramesAgo)
        {
            pTiming.m_nSize = (uint)Marshal.SizeOf(pTiming);
            if (m_vrCompositor!=null)
                return m_vrCompositor.GetFrameTiming(ref pTiming, unFramesAgo);
            return false;
        }
        #endregion
        #region poses
        private static void ReadPoses(float seconsdAhead)
        {//to be called from RENDER thread
            if (m_vrCompositor == null)
                return;
            ProfilerShort.Begin("MOVR:ReadPoses");
            //MyLog.Default.WriteLine("   GetPoses");
            var error3 = m_vrCompositor.WaitGetPoses(renderPose, gamePose);//TODO will not initialize correctly without it but maybe not necessary to do all the time
            m_vrSystem.GetDeviceToAbsoluteTrackingPose(m_vrCompositor.GetTrackingSpace(), seconsdAhead, gamePose);

            uint nDevice = 0;
            bool firstControllerFound = false;
            foreach (var rPose in gamePose)
            {
                if (!rPose.bDeviceIsConnected)
                    break;
                if (rPose.bPoseIsValid)
                {
                    switch (m_vrSystem.GetTrackedDeviceClass(nDevice))
                    {
                        case ETrackedDeviceClass.HMD:
                            if (rPose.eTrackingResult != ETrackingResult.Running_OK)
                            {
                                Log.WriteLineAndConsole("Pose: " + nDevice + " not Running OK");
                                continue;
                            }
                            PoseToViewMatrix(ref m_viewHMD, ref gamePose[nDevice].mDeviceToAbsoluteTracking);
                            PoseToTransMatrixD(ref m_headsetPosD, ref gamePose[nDevice].mDeviceToAbsoluteTracking);
                            m_viewHMD.M42 -= FloorOffset;

                            //QS1 axes: X red ship's rear
                            //          Y up
                            //          Z from red ship to platform

                            //translation up-down is inverted
                            break;
                        case ETrackedDeviceClass.Controller:
                            if (!firstControllerFound)
                            {
                                firstControllerFound = true;
                                if (m_controller1ID != nDevice)
                                {
                                    Log.WriteLine("Controller1 is now ID #"+nDevice);
                                    m_controller1ID = nDevice;
                                }
                            }
                            else
                                if (m_controller2ID != nDevice)
                                {
                                    Log.WriteLine("Controller2 is now ID #"+nDevice);
                                    m_controller2ID = nDevice;
                                }
                            if (nDevice==m_controller1ID)
                                PoseToTransMatrix(ref m_c1pos, ref gamePose[nDevice].mDeviceToAbsoluteTracking);
                            else
                                if (nDevice==m_controller2ID)
                                    PoseToTransMatrix(ref m_c2pos, ref gamePose[nDevice].mDeviceToAbsoluteTracking);
                            break;
                    }
                }
                //else
                //    Log.WriteLineAndConsole("Pose: " + nDevice + " not valid");

                nDevice++;
            }
            ProfilerShort.End();
        }
        private static void PoseToViewMatrix(ref Matrix mat, ref HmdMatrix34_t pose)
        {
            mat.M11 = pose.m[0];
            mat.M12 = pose.m[1];
            mat.M13 = pose.m[2];
            mat.M43 = pose.m[3];

            mat.M21 = pose.m[4];
            mat.M22 = pose.m[5];
            mat.M23 = pose.m[6];
            mat.M42 = pose.m[7];

            mat.M31 = pose.m[8];
            mat.M32 = pose.m[9];
            mat.M33 = pose.m[10];
            mat.M41 = pose.m[11];

            mat.M44 = 1;
        }
        private static void PoseToTransMatrix(ref Matrix mat, ref HmdMatrix34_t pose)
        {
            //m[3] + ke dverim, tj doprava, herni X
            //m[7] + nahoru, tj. nahoru, herni Y
            //m[11]+ k telocvicne, tj. dozadu, herni Z
            //koukani dopredu = od telocvicny
            mat.M11 = pose.m[0];
            mat.M12 = pose.m[4];
            mat.M13 = pose.m[8];

            mat.M21 = pose.m[1];
            mat.M22 = pose.m[5];
            mat.M23 = pose.m[9];

            mat.M31 = pose.m[2];
            mat.M32 = pose.m[6];
            mat.M33 = pose.m[10];

            /*X*/
            mat.M41 = pose.m[3];
            /*Y*/
            mat.M42 = pose.m[7] - FloorOffset;
            /*Z*/
            mat.M43 = pose.m[11];

            mat.M44 = 1;

            //mat=Matrix.CreateFromYawPitchRoll(0.0f, 0.5f, 0);
        }
        private static void PoseToTransMatrixD(ref MatrixD mat, ref HmdMatrix34_t pose)
        {
            mat.M11 = pose.m[0];
            mat.M12 = pose.m[4];
            mat.M13 = pose.m[8];

            mat.M21 = pose.m[1];
            mat.M22 = pose.m[5];
            mat.M23 = pose.m[9];

            mat.M31 = pose.m[2];
            mat.M32 = pose.m[6];
            mat.M33 = pose.m[10];

            mat.M41 = pose.m[3];                   
            mat.M42 = pose.m[7] - FloorOffset;  
            mat.M43 = pose.m[11];                  
            
            mat.M44 = 1;
        }
        #endregion
        #region menus
        static CVROverlay m_cvrOverlay;
        private static bool InitOverlays()
        {
            EVRInitError iError = EVRInitError.None;
            IntPtr pointer = OpenVR.GetGenericInterface(OpenVR.IVROverlay_Version, ref iError);
            if (iError!=EVRInitError.None)
            {
                Debug.Fail("Overlays init failed " + iError.ToString());
                Log.WriteLine("Error "+iError);
                return false;
            }
            m_cvrOverlay=new CVROverlay(pointer);
            return true;
        }

        private static bool CreateMenu()
        {
            ulong handle = CreateOverlay("menu");
            Matrix posMatrix = Matrix.Identity;
            SetOverlayTransform("menu", ref posMatrix, null, 3);
            return true;
        }

        public static ulong CreateOverlay(string key)
        {
            Debug.Assert(m_cvrOverlay!=null);

            ulong handle = OpenVR.k_ulOverlayHandleInvalid;

            var oError = m_cvrOverlay.FindOverlay(key, ref handle);
            if (oError == EVROverlayError.None)
                CloseOverlay(handle);

            //create:
            oError = m_cvrOverlay.CreateOverlay(key, key, ref handle);
            if (oError != EVROverlayError.None)
            {
                Log.WriteLine("Error " + oError);
                return OpenVR.k_ulOverlayHandleInvalid;
            }
            return handle;
        }
        /*
            m_cvrOverlay.ShowOverlay(handle);

            var textureBounds = new VRTextureBounds_t();
            textureBounds.uMin = 0;
            textureBounds.uMax = 1;
            textureBounds.vMin = 0;
            textureBounds.vMax = 1;
            m_cvrOverlay.SetOverlayTextureBounds(handle, ref textureBounds);

            HmdMatrix34_t matrix = new HmdMatrix34_t();
            matrix.m = new float[12];
            matrix.m[0] = 1;
            matrix.m[5] = 1;
            matrix.m[10] = 1;
            matrix.m[3] = 0;
            matrix.m[7] = 0;
            matrix.m[11] = -0.2f;
            m_cvrOverlay.SetOverlayTransformTrackedDeviceRelative(handle, m_controller1ID, ref matrix);

            oError = m_cvrOverlay.SetOverlayWidthInMeters(handle, 0.5f);
            oError = m_cvrOverlay.SetOverlayInputMethod(handle, VROverlayInputMethod.Mouse);
        }*/
        public static bool ShowOverlay(string key, bool show)
        {
            Debug.Assert(m_cvrOverlay!=null);
            ulong handle = OpenVR.k_ulOverlayHandleInvalid;
            var oError = m_cvrOverlay.FindOverlay(key, ref handle);
            if (oError != EVROverlayError.None)
            {
                Debug.Fail("Overlay does not exist " + oError.ToString());
                return false;
            }
            if (show)
                m_cvrOverlay.ShowOverlay(handle);
            else
                m_cvrOverlay.HideOverlay(handle);
            return true;
        }

        public static bool SetOverlayTransform(string key, ref Matrix posMatrix, uint? controllerId, float width)
        {
            Debug.Assert(m_cvrOverlay!=null);
            ulong handle = OpenVR.k_ulOverlayHandleInvalid;
            var oError = m_cvrOverlay.FindOverlay(key, ref handle);
            if (oError != EVROverlayError.None)
            {
                Debug.Fail("Overlay does not exist " + oError.ToString());
                return false;
            }
            HmdMatrix34_t hmdMatrix = new HmdMatrix34_t();
            MatrixToHmdMatrix34(ref posMatrix, ref hmdMatrix);

            if (controllerId == null)
                oError = m_cvrOverlay.SetOverlayTransformAbsolute(handle,ETrackingUniverseOrigin.TrackingUniverseStanding,ref hmdMatrix);
            else
                oError = m_cvrOverlay.SetOverlayTransformTrackedDeviceRelative(handle, (uint)controllerId, ref hmdMatrix);
            if (oError != EVROverlayError.None)
            {
                Debug.Fail("Overlay set transform failed " + oError.ToString());
                return false;
            }

            oError = m_cvrOverlay.SetOverlayWidthInMeters(handle, width);
            if (oError != EVROverlayError.None)
            {
                Debug.Fail("Overlay set width failed " + oError.ToString());
                return false;
            }
            return true;
        }

        private static void MatrixToHmdMatrix34(ref Matrix mat, ref HmdMatrix34_t hmdMatrix)
        {
            hmdMatrix.m[0]=mat.M11;
            hmdMatrix.m[4]=mat.M12;
            hmdMatrix.m[8]=mat.M13;

            hmdMatrix.m[1]=mat.M21; 
            hmdMatrix.m[5]=mat.M22;
            hmdMatrix.m[9]=mat.M23;

            hmdMatrix.m[2]=mat.M31;
            hmdMatrix.m[6]=mat.M32;
            hmdMatrix.m[10]=mat.M33;

            hmdMatrix.m[3]=mat.M41;
            hmdMatrix.m[7]=mat.M42;
            hmdMatrix.m[11]= mat.M43;
        }

        private static void CloseOverlay(string key)
        {
            ulong handle = OpenVR.k_ulOverlayHandleInvalid;
            var oError = m_cvrOverlay.FindOverlay(key, ref handle);
            if (oError != EVROverlayError.None)
            {
                CloseOverlay(handle);
            }
            else
                Log.WriteLineAndConsole("Overlay not found when closing");
        }

        private static void CloseOverlay(ulong handle)
        {
            m_cvrOverlay.DestroyOverlay(handle);
        }

        public static void DisplayToOverlay(string key, IntPtr texture)
        {
            ulong handle = OpenVR.k_ulOverlayHandleInvalid;
            var oError = m_cvrOverlay.FindOverlay(key, ref handle);
            if (oError != EVROverlayError.None)
            {
                //Debug.Fail("Overlay does not exist " + oError.ToString());
                return;
            }
            SetOverlayTexture(handle, texture);
        }

        static public void SetOverlayTexture(ulong handle, IntPtr texture)
        {
            Texture_t tex;
            tex.eType = EGraphicsAPIConvention.API_DirectX;
            tex.eColorSpace = EColorSpace.Auto;
            tex.handle = texture;//MyRender11.Backbuffer.m_resource.NativePointer;
            m_cvrOverlay.SetOverlayTexture(handle, ref tex);
        }

        static void PollOverlayEvents(ulong handle)
        {
            if (m_cvrOverlay.HandleControllerOverlayInteractionAsMouse(handle, m_controller2ID))
            { 
                //controller points to the overlay and event was generated. Unsure if the call above must be called every tick to generate event
                VREvent_t oEvent=new VREvent_t();
                uint oEventSize=(uint)Marshal.SizeOf(oEvent);
                while( m_cvrOverlay.PollNextOverlayEvent(handle,ref oEvent, oEventSize))
                {
                    Log.WriteLineAndConsole("OVERLAY event (#"+oEvent.trackedDeviceIndex+"): " + oEvent.eventType.ToString() + " " + Enum.GetName(typeof(EVREventType), oEvent.eventType));
                    switch(oEvent.eventType)
                    {
                        case (uint)EVREventType.VREvent_MouseMove:
                            if (oEvent.trackedDeviceIndex==m_controller2ID)
                                Log.WriteLineAndConsole("  " + oEvent.data.mouse.x + "," + oEvent.data.mouse.y);
                            break;
                    }
                }
            };
        }
        
        #endregion
        #region timing & handoff
        public struct Timer
        {
            static Stopwatch m_timer;
            public static readonly Timer Empty = new Timer();

            static Timer()
            {
                m_timer = new Stopwatch();
                m_timer.Start();
            }
            public static float Ms
            {
                get
                {
                    return (float)(m_timer.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0);
                }
            }
        }

        public static void FrameDone()
        {
            m_frameDuration = (Timer.Ms - m_lastRuntimeMs) * 0.001f;
            m_lastRuntimeMs = Timer.Ms;

            m_frameDurationFiltered = m_frameDurationFiltered * FILTER_CONST + m_frameDuration * (1 - FILTER_CONST);

            m_vrCompositor.PostPresentHandoff();
        }

        private static ulong m_currFrame = 0;
        private static float m_timeSinceLastVsync = 0;

        private static ulong m_previousFrame = 0;
        public static ulong MissedFramesCount = 0;
        public static float WaitTimeMs;

        public static void WaitForNextStart()
        {//active wait for 2ms headstart for next frame + prediction at the end
        //blocking, busy waiting, use in render thread
            if (SyncWait)
            {
                m_vrSystem.GetTimeSinceLastVsync(ref m_timeSinceLastVsync, ref m_currFrame);
                ulong frameAtBegin = m_currFrame;
                float timeAtBegin = m_timeSinceLastVsync;
                while (true)
                {
                    if (m_currFrame - m_previousFrame == 1 && m_timeSinceLastVsync > 0.00911f)
                        break;
                    if (m_currFrame - m_previousFrame > 1)
                        break;//too late, 
                    m_vrSystem.GetTimeSinceLastVsync(ref m_timeSinceLastVsync, ref m_currFrame);
                }
                WaitTimeMs = (m_currFrame - frameAtBegin) * 1000f / 90f + 1000 * (m_timeSinceLastVsync - timeAtBegin);

                if (m_currFrame - m_previousFrame > 1)
                    MissedFramesCount++;
                m_previousFrame = m_currFrame;
                //Debug.WriteLine(m_currFrame);
            }
            ReadPoses(m_frameDurationFiltered * 2f);//*2 is a subjective fudge factor to keep world nice and steady :-)
        }
        #endregion
        #region events & buttons
        public static ControllerState GetControllerState(bool secondController)
        {
            return secondController ? Controller2State : Controller1State;
        }

        public static void ClearButtonStates()
        {
            m_controller1State.Clear();
            m_controller2State.Clear();
        }
        public static string GetButtonName(int buttonId)
        {
            return Enum.GetName(typeof(EVRButtonId), buttonId);
        }

        public unsafe static void PollEvents()//read this from inputs update, otherwise you risk losing some button presses
        {
            if (m_vrSystem == null)
                return;

            ProfilerShort.Begin("MOVR:PollEvents");
            m_controller1State.Update(m_vrSystem, m_controller1ID);
            m_controller2State.Update(m_vrSystem, m_controller2ID);

            unsafe
            {
                VREvent_t pEvent = default(VREvent_t);
                while (m_vrSystem.PollNextEvent(ref pEvent, (uint)Marshal.SizeOf(pEvent)))
                {
                    Log.WriteLineAndConsole("HMD event: " + pEvent.eventType.ToString() + " " + Enum.GetName(typeof(EVREventType),pEvent.eventType));
                    switch (pEvent.eventType)
                    {
                        case (uint)EVREventType.VREvent_IpdChanged:
                            float value=0;
                            if (GetFloatProperty(0, ETrackedDeviceProperty.Prop_UserIpdMeters_Float, ref value))
                            {
                                SetIPD(value);
                                Log.WriteLineAndConsole("  IPD changed to " + value);
                            }
                            break;
                    }
                }
            }
            ProfilerShort.End();
        }

        private static bool GetFloatProperty(uint device, ETrackedDeviceProperty property, ref float outputValue)//convenience wrap with logging
        {
            ETrackedPropertyError pError = 0;
            var val = m_vrSystem.GetFloatTrackedDeviceProperty(device, ETrackedDeviceProperty.Prop_UserIpdMeters_Float, ref pError);
            if (pError == ETrackedPropertyError.TrackedProp_Success)
            {
                outputValue = val;
                return true;
            }
            Log.WriteLine("ERROR  GetFloatTrackedDeviceProperty(" + device + "," + property + "," + outputValue + ") returned  error " + pError);
            return false;
        }

        #endregion
        #region displaying image
        private bool debug_var = false;
        public void DisplayEye(IntPtr nativePointer)
        {
            VRTextureBounds_t tboud = new VRTextureBounds_t();
            Texture_t tex;
            tex.eType = EGraphicsAPIConvention.API_DirectX;
            tex.eColorSpace = EColorSpace.Auto;
            tex.handle = nativePointer;// MyRender11.Backbuffer.m_resource.NativePointer;
            tboud.vMin = 0;
            tboud.vMax = 1;
            
            tboud.uMin = m_outsideLimit / 2;
            tboud.uMax = (1-m_insideLimit) / 2;
            var error = m_vrCompositor.Submit(EVREye.Eye_Left, ref tex, ref tboud, EVRSubmitFlags.Submit_Default);

            tboud.uMin = 0.5f + m_insideLimit / 2;
            tboud.uMax = 0.5f + (1-m_outsideLimit) / 2;
            var error2 = m_vrCompositor.Submit(EVREye.Eye_Right, ref tex, ref tboud, EVRSubmitFlags.Submit_Default);
            //FrameDone();

            if (debug_var)
                m_vrCompositor.CompositorDumpImages();
        }

        public static void FadeToColor(float sec, Color color)
        {
            m_vrCompositor.FadeToColor(sec, color.R, color.G, color.B, color.A, false);
        }
        public static void UnFade(float sec)
        {
            m_vrCompositor.FadeToColor(sec, 0, 0, 0, 0, false);
        }
        #endregion
        #region stencil mask

        protected static void SetIPD(float ipd)
        {
            m_ipd_2 = ipd / 2;
            //vive IPD range approx. 6-7.4cm: 
            //designed for buffer horizontal resolution 1632, hmd display 1512
            //=> total cut of 0.073 split between outside and inside based on current IPD
            float TOTAL_CUT = (1632f-1512f)/1632;
            float IPD_MAX = (1632f-1512f)/1632;

            //ipd = IPD_MAX - (IPD_MAX - ipd) * 4;

            float split= ipd/IPD_MAX;
            m_insideLimit=TOTAL_CUT*split;
            m_outsideLimit = TOTAL_CUT - m_insideLimit;

            //TODO stencil mask may need to be adjusted after IPD change depending on rendering logic we choose in future
        }

        public static Vector2[] GetStencilMask()
        {
            HiddenAreaMesh_t leftMesh = m_vrSystem.GetHiddenAreaMesh(EVREye.Eye_Left);
            HiddenAreaMesh_t rightMesh = m_vrSystem.GetHiddenAreaMesh(EVREye.Eye_Right);
            uint leftVertsCount = leftMesh.unTriangleCount * 3;
            uint rightVertsCount = rightMesh.unTriangleCount * 3;

            Vector2[] verts = new Vector2[leftVertsCount + rightVertsCount];
            
            unsafe
            {
                uint index = 0;
                for (int i = 0, j=0; i < leftMesh.unTriangleCount *3; i++)
                {
                    Vector2 vertex;
                    float x = ((float*)leftMesh.pVertexData.ToPointer())[j++];
                    float y = ((float*)leftMesh.pVertexData.ToPointer())[j++];
                    vertex.X = -1 + 2 * x;
                    vertex.Y = -1 + 2 * y;
                    vertex.X = vertex.X / 2 - 0.5f;
                    verts[index++] = vertex;
                }
                for (int i = 0, j=0; i < rightMesh.unTriangleCount *3; i++)
                {
                    Vector2 vertex;
                    float x = ((float*)rightMesh.pVertexData.ToPointer())[j++];
                    float y = ((float*)rightMesh.pVertexData.ToPointer())[j++];
                    vertex.X = -1 + 2 * x;
                    vertex.Y = -1 + 2 * y;
                    vertex.X = vertex.X / 2 + 0.5f;
                    verts[index++] = vertex;
                }
            }
            return verts;
        }
        #endregion
        #region lastMomentUpdate
        //purpose: some matrixes must be updated from render thread with latest position from controllers and headset
        //matrix = leftMult * matrix_from_controller * origin
        static Dictionary<int,LMUEntry> LMUEntries = new Dictionary<int,LMUEntry>(3);
        public static void LMUAdd( MatrixD? leftMult, MatrixD origin, ControllerRole role, int reference)
        {
            LMUEntries.Remove(reference);
            LMUEntries.Add(reference, new LMUEntry(leftMult, origin, role));
        }
        public static void LMUMatrixGetOrigin(ref MatrixD origin, int reference)
        {//to be called from RENDER thread
            LMUEntry entry;
            if (!LMUEntries.TryGetValue(reference, out entry))
                return;//nothing to update
            origin = entry.origin;
        }
        
        public static void LMUMatrixUpdate(ref MatrixD matrix, int reference)//simply said: replaces worldmatrix with new one :-)
        {//to be called from RENDER thread
            LMUEntry entry;
            if (!LMUEntries.TryGetValue(reference, out entry) || !LmuDebugOnOff)
                return;//nothing to update
            switch (entry.assignedController)
            {
                case ControllerRole.head:
                    matrix = HeadsetMatrixD * entry.origin;
                    break;
                case ControllerRole.leftHand:
                    matrix = Controller1Matrix * entry.origin;
                    break;
                case ControllerRole.rightHand:
                    matrix = Controller2Matrix * entry.origin;
                    break;
                default:
                    Debug.Fail("should not be here");
                    break;
            }
            if (entry.LeftMult!=null)
                matrix = ((MatrixD)entry.LeftMult) * matrix;
        }
        #endregion
#endif // !XB1
    }
    public enum ControllerRole
    {
        INVALID = -1,
        head = 0,
        leftHand = 1,
        rightHand = 2,
    }
}
