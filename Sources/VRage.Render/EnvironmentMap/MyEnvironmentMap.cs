using System;
using VRage.Utils;
using VRageMath;

using SharpDX;
using SharpDX.Direct3D9;


namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;

    class MyEnvironmentMap
    {
        static MyEnvironmentMap()
        {
            //MySandboxGame.GraphicsDeviceManager.DeviceReset += delegate { Reset(); };
            //MyRenderConstants.OnRenderQualityChange += delegate { Reset(); };
        }

        static MyEnvironmentMapRenderer m_environmentMapRendererMain = new MyEnvironmentMapRenderer();
        static MyEnvironmentMapRenderer m_environmentMapRendererAux = new MyEnvironmentMapRenderer();

        static bool m_renderInstantly = true;

        /// <summary>
        /// Maximal distance MainMap from MainMapPosition where MainMap is rendered.
        /// Further than this distance, AuxMap is set as MainMap.
        /// </summary>
        public static float MainMapMaxDistance = 40;

        /// <summary>
        /// When camera moves more than this distance, cube map is refreshed in one frame to prevent blinking when "teleporting" camera or moving extremelly fast
        /// </summary>
        public static float InstantRefreshDistance = MainMapMaxDistance * 1.5f;

        /// <summary>
        /// Distance from MainMapPosition where MainMap begins to blend with AuxMap.
        /// </summary>
        public static float BlendDistance = MainMapMaxDistance / 2 + 1;

        /// <summary>
        /// To not update maps when on the edge of map distance
        /// </summary>
        public static float Hysteresis = 4;


        public static Vector3D? MainMapPosition { get; private set; }
        public static CubeTexture EnvironmentMainMap
        {
            get
            {
                return m_environmentMapRendererMain.Environment;
            }
        }

        public static Vector3D? AuxMapPosition { get; private set; }
        public static CubeTexture EnvironmentAuxMap
        {
            get
            {
                return m_environmentMapRendererAux.Environment;
            }
        }

        public static CubeTexture AmbientMainMap
        {
            get
            {
                return m_environmentMapRendererMain.Ambient;
            }
        }

        public static CubeTexture AmbientAuxMap
        {
            get
            {
                return m_environmentMapRendererAux.Ambient;
            }
        }

        /// <summary>
        /// Gets or sets the BlendFactor, value is between 0.0f and 1.0f, 0.0f means show only MainMap, 1.0f means show only AuxMap
        /// </summary>
        public static float BlendFactor { get; private set; }

        /// <summary>
        /// Gets or sets duration of last update in miliseconds
        /// </summary>
        public static float LastUpdateTime { get; private set; }

        public static void SetSize(int size)
        {
            MyRender.CreateEnvironmentMapsRT(size);
            Reset();
        }

        public static void SetRenderTargets(CubeTexture envMain, CubeTexture envAux, CubeTexture ambMain, CubeTexture ambAux, Texture fullSizeRT)
        {
            m_environmentMapRendererMain.SetRenderTarget(envMain, ambMain, fullSizeRT);
            m_environmentMapRendererAux.SetRenderTarget(envAux, ambAux, fullSizeRT);
        }

        /// <summary>
        /// Causes maps to be recreated immediately
        /// </summary>
        public static void Reset()
        {
            MainMapPosition = null;
            AuxMapPosition = null;
            m_renderInstantly = true;
        }



        public static void Update()
        {
            //use only for profiling
            /*
            long startTime;
            MyWindowsAPIWrapper.QueryPerformanceCounter(out startTime);
              */


            bool renderEnviromentMaps = MyRender.EnableLights && MyRender.Settings.EnableLightsRuntime && MyRender.Settings.EnableSun && (MyRender.Settings.EnableEnvironmentMapAmbient || MyRender.Settings.EnableEnvironmentMapReflection);

            if (!renderEnviromentMaps)
            {
                return;
            }

            if (BlendDistance > MainMapMaxDistance)
            {
                throw new InvalidOperationException("BlendDistance must be lower than MainMapMaxDistance");
            }

            MyRender.RenderOcclusionsImmediatelly = true;

            var cameraPos = MyRenderCamera.Position;

            if (MainMapPosition.HasValue && (cameraPos - MainMapPosition.Value).Length() > InstantRefreshDistance)
            {
                m_renderInstantly = true;
            }
            
            // Makes evironment camera pos 300m in front of real camera
            //cameraPos += Vector3.Normalize(MyCamera.ForwardVector) * 300

            if (MainMapPosition == null)
            {
                LastUpdateTime = 0;
                MainMapPosition = cameraPos;
                m_environmentMapRendererMain.StartUpdate(MainMapPosition.Value, m_renderInstantly);
                m_environmentMapRendererAux.StartUpdate(MainMapPosition.Value, m_renderInstantly);
                m_renderInstantly = false;

                BlendFactor = 0.0f;
            }
            else
            {
                var mainMapDistance = (MainMapPosition.Value - cameraPos).Length();

                // When behind blend distance
                if (mainMapDistance > BlendDistance)
                {
                    // Create AuxMap if not created
                    if (AuxMapPosition == null)
                    {
                        LastUpdateTime = 0;
                        AuxMapPosition = cameraPos;
                        m_environmentMapRendererAux.StartUpdate(AuxMapPosition.Value, m_renderInstantly);
                        m_renderInstantly = false;
                    }

                    // Wait till rendering done before blending
                    if (m_environmentMapRendererAux.IsDone())
                    {
                        // Set proper blend factor
                        BlendFactor = (float)(mainMapDistance - BlendDistance) / (MainMapMaxDistance - BlendDistance);
                    }
                }
                else if ((mainMapDistance + Hysteresis) < BlendDistance)
                {
                    AuxMapPosition = null;
                }

                // If MainMap should not be even displayed...swap aux and main and display
                if (mainMapDistance > MainMapMaxDistance && m_environmentMapRendererAux.IsDone())
                {
                    var tmp = m_environmentMapRendererAux;
                    m_environmentMapRendererAux = m_environmentMapRendererMain;
                    m_environmentMapRendererMain = tmp;
                    MainMapPosition = cameraPos + MyUtils.Normalize(MainMapPosition.Value - cameraPos) * BlendDistance;
                    AuxMapPosition = null;
                    BlendFactor = 0.0f;
                }
            }


            m_environmentMapRendererMain.ContinueUpdate();
            m_environmentMapRendererAux.ContinueUpdate();

            MyRender.RenderOcclusionsImmediatelly = false;

            /*
            long frq;
            MyWindowsAPIWrapper.QueryPerformanceFrequency(out frq);

            long stopTime;
            MyWindowsAPIWrapper.QueryPerformanceCounter(out stopTime);

            float updateTime = ((float)(stopTime - startTime)) / frq * 1000.0f;
            if(updateTime > LastUpdateTime)
            {
                LastUpdateTime = updateTime;
            }
             * */
        }
    }
}
