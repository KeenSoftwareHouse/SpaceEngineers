#region Using

using VRage.Utils;

using SharpDX.Direct3D9;

using VRageRender.Textures;
using VRageRender.Graphics;
using VRageRender.Effects;

using Vector3 = VRageMath.Vector3;
using Matrix = VRageMath.Matrix;
using Quaternion = VRageMath.Quaternion;

#endregion

namespace VRageRender
{
    

    class MyBackgroundCube : MyRenderComponentBase
    {
        public static MyBackgroundCube Static;

        static MyTextureCube m_textureCube;        
        static VertexBuffer m_boxVertexBuffer;
        static bool m_loaded = false;
        const int BOX_TRIANGLES_COUNT = 12;
        static Matrix m_backgroundProjectionMatrix;
        static Quaternion m_backgroundOrientation;
        static bool m_backgroundOrientationDirty;

        public static Quaternion BackgroundOrientation
        {
            get { return m_backgroundOrientation; }
            set
            {
                if (m_backgroundOrientation != value)
                {
                    m_backgroundOrientation = value;
                    m_backgroundOrientationDirty = true;
                }
            }
        }

        public override int GetID()
        {
            return (int)MyRenderComponentID.BackgroundCube;
        }

        static MyBackgroundCube()
        {
            MyRender.RegisterRenderModule(MyRenderModuleEnum.BackgroundCube, "Background cube", Draw, MyRenderStage.Background, 1, true);
        }

        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyBackgroundCube.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyBackgroundCube");

            Static = this;

            UpdateTexture();
         
            m_loaded = false;

            //  Projection matrix according to zoom level
            m_backgroundProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(1.0f, MyRenderCamera.AspectRatio,
                50,
                100000);

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyBackgroundCube.LoadContent() - END");
        }

        internal static string Filename
        {
            get;
            set;
        }

        internal static Vector3 BackgroundColor
        {
            get;
            set;
        }

        public override void ReloadContent()
        {
            UpdateTexture();
        }

        static void UpdateTexture()
        {     
            //  This texture should be in DDS file extension and must be DXT1 compressed (use Photoshop and DDS tool from NVIDIA)
            //  We don't use for it dxt compression from XNA's content processor because we don't want huge (over 100 Mb) files in SVN.
            if (string.IsNullOrEmpty(Filename))
                m_textureCube = null;
            else
                m_textureCube = MyTextureManager.GetTexture<MyTextureCube>(Filename, "", null, LoadingMode.Immediate);
        }

        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("MyBackgroundCube.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            if (m_boxVertexBuffer != null)
            {
                m_boxVertexBuffer.Dispose();
                m_boxVertexBuffer = null;
            }

            if (m_textureCube != null)
            {
                MyTextureManager.UnloadTexture(m_textureCube);
                m_textureCube = null;
            }
            m_textureCube = null;
            m_loaded = false;

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyBackgroundCube.UnloadContent - END");
        }

        //	Special method that loads data into GPU, and can be called only from Draw method, never from LoadContent or from background thread.
        //	Because that would lead to empty vertex/index buffers if they are filled/created while game is minimized (remember the issue - alt-tab during loading screen)
        static void LoadInDraw()
        {
            if (m_backgroundOrientationDirty)
                Static.UnloadContent();

            if (m_loaded) return;

            //  In fact it doesn't matter how large is cube, it will always look same as we are always in its middle
            //  I changed it from 1.0 to 100.0 only because will small length I had problems with near frustum plane and crazy aspect ratios.
            const float CUBE_LENGTH_HALF = 100;

            Vector3 shapeSize = Vector3.One * CUBE_LENGTH_HALF;
            Vector3 shapePosition = Vector3.Zero;

            MyVertexFormatPositionTexture3[] boxVertices = new MyVertexFormatPositionTexture3[36];

            Vector3 topLeftFront = shapePosition + new Vector3(-1.0f, 1.0f, -1.0f) * shapeSize;
            Vector3 bottomLeftFront = shapePosition + new Vector3(-1.0f, -1.0f, -1.0f) * shapeSize;
            Vector3 topRightFront = shapePosition + new Vector3(1.0f, 1.0f, -1.0f) * shapeSize;
            Vector3 bottomRightFront = shapePosition + new Vector3(1.0f, -1.0f, -1.0f) * shapeSize;
            Vector3 topLeftBack = shapePosition + new Vector3(-1.0f, 1.0f, 1.0f) * shapeSize;
            Vector3 topRightBack = shapePosition + new Vector3(1.0f, 1.0f, 1.0f) * shapeSize;
            Vector3 bottomLeftBack = shapePosition + new Vector3(-1.0f, -1.0f, 1.0f) * shapeSize;
            Vector3 bottomRightBack = shapePosition + new Vector3(1.0f, -1.0f, 1.0f) * shapeSize;

            Vector3 textureTopLeftFront = MyUtils.Normalize(topLeftFront);
            Vector3 textureBottomLeftFront = MyUtils.Normalize(bottomLeftFront);
            Vector3 textureTopRightFront = MyUtils.Normalize(topRightFront);
            Vector3 textureBottomRightFront = MyUtils.Normalize(bottomRightFront);
            Vector3 textureTopLeftBack = MyUtils.Normalize(topLeftBack);
            Vector3 textureTopRightBack = MyUtils.Normalize(topRightBack);
            Vector3 textureBottomLeftBack = MyUtils.Normalize(bottomLeftBack);
            Vector3 textureBottomRightBack = MyUtils.Normalize(bottomRightBack);
            textureTopLeftFront.Z *= -1;
            textureBottomLeftFront.Z *= -1;
            textureTopRightFront.Z *= -1;
            textureBottomRightFront.Z *= -1;
            textureTopLeftBack.Z *= -1;
            textureTopRightBack.Z *= -1;
            textureBottomLeftBack.Z *= -1;
            textureBottomRightBack.Z *= -1;

            // Front face.
            boxVertices[0] = new MyVertexFormatPositionTexture3(topLeftFront, textureTopLeftFront);
            boxVertices[1] = new MyVertexFormatPositionTexture3(bottomLeftFront, textureBottomLeftFront);
            boxVertices[2] = new MyVertexFormatPositionTexture3(topRightFront, textureTopRightFront);
            boxVertices[3] = new MyVertexFormatPositionTexture3(bottomLeftFront, textureBottomLeftFront);
            boxVertices[4] = new MyVertexFormatPositionTexture3(bottomRightFront, textureBottomRightFront);
            boxVertices[5] = new MyVertexFormatPositionTexture3(topRightFront, textureTopRightFront);

            // Back face.
            boxVertices[6] = new MyVertexFormatPositionTexture3(topLeftBack, textureTopLeftBack);
            boxVertices[7] = new MyVertexFormatPositionTexture3(topRightBack, textureTopRightBack);
            boxVertices[8] = new MyVertexFormatPositionTexture3(bottomLeftBack, textureBottomLeftBack);
            boxVertices[9] = new MyVertexFormatPositionTexture3(bottomLeftBack, textureBottomLeftBack);
            boxVertices[10] = new MyVertexFormatPositionTexture3(topRightBack, textureTopRightBack);
            boxVertices[11] = new MyVertexFormatPositionTexture3(bottomRightBack, textureBottomRightBack);

            // Top face.
            boxVertices[12] = new MyVertexFormatPositionTexture3(topLeftFront, textureTopLeftFront);
            boxVertices[13] = new MyVertexFormatPositionTexture3(topRightBack, textureTopRightBack);
            boxVertices[14] = new MyVertexFormatPositionTexture3(topLeftBack, textureTopLeftBack);
            boxVertices[15] = new MyVertexFormatPositionTexture3(topLeftFront, textureTopLeftFront);
            boxVertices[16] = new MyVertexFormatPositionTexture3(topRightFront, textureTopRightFront);
            boxVertices[17] = new MyVertexFormatPositionTexture3(topRightBack, textureTopRightBack);

            // Bottom face.
            boxVertices[18] = new MyVertexFormatPositionTexture3(bottomLeftFront, textureBottomLeftFront);
            boxVertices[19] = new MyVertexFormatPositionTexture3(bottomLeftBack, textureBottomLeftBack);
            boxVertices[20] = new MyVertexFormatPositionTexture3(bottomRightBack, textureBottomRightBack);
            boxVertices[21] = new MyVertexFormatPositionTexture3(bottomLeftFront, textureBottomLeftFront);
            boxVertices[22] = new MyVertexFormatPositionTexture3(bottomRightBack, textureBottomRightBack);
            boxVertices[23] = new MyVertexFormatPositionTexture3(bottomRightFront, textureBottomRightFront);

            // Left face.
            boxVertices[24] = new MyVertexFormatPositionTexture3(topLeftFront, textureTopLeftFront);
            boxVertices[25] = new MyVertexFormatPositionTexture3(bottomLeftBack, textureBottomLeftBack);
            boxVertices[26] = new MyVertexFormatPositionTexture3(bottomLeftFront, textureBottomLeftFront);
            boxVertices[27] = new MyVertexFormatPositionTexture3(topLeftBack, textureTopLeftBack);
            boxVertices[28] = new MyVertexFormatPositionTexture3(bottomLeftBack, textureBottomLeftBack);
            boxVertices[29] = new MyVertexFormatPositionTexture3(topLeftFront, textureTopLeftFront);

            // Right face.
            boxVertices[30] = new MyVertexFormatPositionTexture3(topRightFront, textureTopRightFront);
            boxVertices[31] = new MyVertexFormatPositionTexture3(bottomRightFront, textureBottomRightFront);
            boxVertices[32] = new MyVertexFormatPositionTexture3(bottomRightBack, textureBottomRightBack);
            boxVertices[33] = new MyVertexFormatPositionTexture3(topRightBack, textureTopRightBack);
            boxVertices[34] = new MyVertexFormatPositionTexture3(topRightFront, textureTopRightFront);
            boxVertices[35] = new MyVertexFormatPositionTexture3(bottomRightBack, textureBottomRightBack);
            
            // if we've loaded the cube from DDS, orient it towards the sun
            for (int i = 0; i < boxVertices.Length; i++)
            {
                boxVertices[i].Position = Vector3.Transform(boxVertices[i].Position, m_backgroundOrientation);
            }

            m_boxVertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, MyVertexFormatPositionTexture3.Stride * boxVertices.Length, Usage.WriteOnly, VertexFormat.None, Pool.Default);
            m_boxVertexBuffer.SetData(boxVertices);
            m_boxVertexBuffer.DebugName = "BackgroundCube";

            UpdateTexture();

            m_backgroundOrientationDirty = false;
            m_loaded = true;
        }

        public static void Draw()
        {      
            //  We can fill vertex buffer only when in Draw
            LoadInDraw();

            //RasterizerState.CullClockwise.Apply();
            RasterizerState.CullNone.Apply();
            DepthStencilState.None.Apply();
            BlendState.Opaque.Apply();

            m_backgroundProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio,
                MyRenderCamera.NEAR_PLANE_DISTANCE,
                100000);
        
            if (MyRender.CurrentRenderSetup.BackgroundColor != null)
            {
                MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(MyRender.CurrentRenderSetup.BackgroundColor.Value.R, MyRender.CurrentRenderSetup.BackgroundColor.Value.G, MyRender.CurrentRenderSetup.BackgroundColor.Value.B, MyRender.CurrentRenderSetup.BackgroundColor.Value.A), 1, 0);
            }
            else
                if (m_textureCube != null)
                {
                    MyEffectBackgroundCube effect = MyRender.GetEffect(MyEffects.BackgroundCube) as MyEffectBackgroundCube;
                    effect.SetViewProjectionMatrix(MyRenderCamera.ViewMatrixAtZero * m_backgroundProjectionMatrix);
                    effect.SetBackgroundTexture(m_textureCube);
                    effect.SetBackgroundColor(BackgroundColor);
                    MyRender.GraphicsDevice.VertexDeclaration = MyVertexFormatPositionTexture3.VertexDeclaration;
                    MyRender.GraphicsDevice.SetStreamSource(0, m_boxVertexBuffer, 0, MyVertexFormatPositionTexture3.Stride);

                    effect.Begin();

                    MyRender.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, BOX_TRIANGLES_COUNT);

                    effect.End();

                    MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
                }
                else                
                {
                    MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1), 1, 0);
                }
        }
    }
}
