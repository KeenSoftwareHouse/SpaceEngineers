using VRageMath;
using System.Collections.Generic;
using VRageRender.Textures;
using VRageRender.Graphics;

//using VRageMath.Graphics;

using SharpDX;
using SharpDX.Direct3D9;


//  This class creates billboards to stand in for objects in the distance. They are rendered
//  between the background cube and the voxel map impostors to create a more interesting 
//  background
//
//  Every impostor type has its own texture. Therefore for drawing it is needed to sort impostors according to their textures for better performance 
//  While creating impostors they are already sorted - First third is one type, second third another type... and this doesn't change during game so then we dont need to explicitly sort them

namespace VRageRender
{
    using Byte4 = VRageMath.PackedVector.Byte4;
    using HalfVector2 = VRageMath.PackedVector.HalfVector2;
    using HalfVector4 = VRageMath.PackedVector.HalfVector4;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRageRender.Utils;
    using VRageRender.Effects;
    using VRage.Utils;


    struct MyDistantImpostorProperties
    {
        public MyTexture2D Texture;
        public bool LitBySun;
        public int IndexStart;
        public int Count;
    }

    class MyDistantObjectImpostors
    {
        MyVertexFormatPositionTextureColor[] m_vertices;
        List<MyDistantObjectImpostor> m_impostors;
        MyDistantImpostorProperties[] m_impostorProperties;

        public float Scale = 1;

        public void LoadData()
        {
            m_impostorProperties = new MyDistantImpostorProperties[3];
            CreateImpostors();
        }

        public void UnloadData()
        {
            if (m_impostors != null)
            {
                m_impostors.Clear();
                m_impostorProperties = null;
            }
        }

        public void LoadContent()
        {
            MyRender.Log.WriteLine("MyDistantObjectImpostors.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyDistantObjectImpostors::LoadContent");

            m_impostorProperties[0].Texture = MyTextureManager.GetTexture<MyTexture2D>("Textures\\Particles\\Blinker");
            m_impostorProperties[1].Texture = MyTextureManager.GetTexture<MyTexture2D>("Textures\\Particles\\Explosion");
            m_impostorProperties[2].Texture = MyTextureManager.GetTexture<MyTexture2D>("Textures\\Particles\\Mover");
            m_impostorProperties[0].LitBySun = false;
            m_impostorProperties[1].LitBySun = false;
            m_impostorProperties[2].LitBySun = true;

            if (m_vertices == null)
            {
                m_vertices =
                    new MyVertexFormatPositionTextureColor[
                        MyDistantObjectsImpostorsConstants.MAX_NUMBER_DISTANT_OBJECTS *
                        MyDistantObjectsImpostorsConstants.VERTEXES_PER_IMPOSTOR];
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyDistantObjectImpostors.LoadContent() - END");
        }

        public void UnloadContent()
        {
            /*m_impostorProperties[0].Texture.Dispose();
            m_impostorProperties[1].Texture.Dispose();
            m_impostorProperties[2].Texture.Dispose();*/
        }

        void CreateImpostors()
        {
            m_impostors = new List<MyDistantObjectImpostor>();
            MyDistantObjectImpostorTypeEnum lastType = MyDistantObjectImpostorTypeEnum.Blinker;
            for (int i = 0; i < MyDistantObjectsImpostorsConstants.MAX_NUMBER_DISTANT_OBJECTS; i++)
            {

                //  assign some value because something must be assigned. later down in code we decide what impostor type will be this
                MyDistantObjectImpostorTypeEnum type = MyDistantObjectImpostorTypeEnum.Blinker;

                //  First third will be blinkers
                if (i <= MyDistantObjectsImpostorsConstants.MAX_NUMBER_DISTANT_OBJECTS * 0.33f)
                {
                    type = MyDistantObjectImpostorTypeEnum.Blinker;
                    m_impostorProperties[(int)type].IndexStart = 0;
                }
                //  Second third will be explosions
                if (i > MyDistantObjectsImpostorsConstants.MAX_NUMBER_DISTANT_OBJECTS * 0.33f)
                {
                    type = MyDistantObjectImpostorTypeEnum.Explosion;

                    //  If this is first explosion impostor we calculate its start index
                    if (lastType == MyDistantObjectImpostorTypeEnum.Blinker)
                    {
                        m_impostorProperties[(int)type].IndexStart = (MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR * i);
                        lastType = MyDistantObjectImpostorTypeEnum.Explosion;
                    }
                }
                //  Last third will be movers
                if (i > MyDistantObjectsImpostorsConstants.MAX_NUMBER_DISTANT_OBJECTS * 0.66f)
                {
                    type = MyDistantObjectImpostorTypeEnum.Mover;

                    //  If this is first explosion impostor we calculate its start index
                    if (lastType == MyDistantObjectImpostorTypeEnum.Explosion)
                    {
                        m_impostorProperties[(int)type].IndexStart = (MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR * i);
                        lastType = MyDistantObjectImpostorTypeEnum.Mover;
                    }
                }

                //  We calculate how many impostors of each type we have
                m_impostorProperties[(int)type].Count++;
                MyDistantObjectImpostor impostor = new MyDistantObjectImpostor();
                impostor.Start(type);
                m_impostors.Add(impostor);
            }
        }

        void CopyToVertexBuffer()
        {
            int i = 0;
            Vector2 texTopLeft = new Vector2(0, 0);
            Vector2 texTopRight = new Vector2(1, 0);
            Vector2 texBottomLeft = new Vector2(0, 1);
            Vector2 texBottomRight = new Vector2(1, 1);

            foreach (MyDistantObjectImpostor impostor in m_impostors)
            {
                MyQuadD retQuad;
                float ratio = m_impostorProperties[(int)impostor.Type].Texture.Height / (float)m_impostorProperties[(int)impostor.Type].Texture.Width;

                MyUtils.GetBillboardQuadAdvancedRotated(out retQuad, impostor.Position, impostor.Radius * Scale, impostor.Radius * Scale * ratio, impostor.Angle, Vector3.Zero);

                Vector3 position0 = retQuad.Point0;
                Vector3 position1 = retQuad.Point1;
                Vector3 position2 = retQuad.Point2;
                Vector3 position3 = retQuad.Point3;

                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 0] = new MyVertexFormatPositionTextureColor(position0, texTopLeft, impostor.Color);
                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 1] = new MyVertexFormatPositionTextureColor(position3, texBottomLeft, impostor.Color);
                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 2] = new MyVertexFormatPositionTextureColor(position1, texTopRight, impostor.Color);
                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 3] = new MyVertexFormatPositionTextureColor(position3, texBottomLeft, impostor.Color);
                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 4] = new MyVertexFormatPositionTextureColor(position2, texBottomRight, impostor.Color);
                m_vertices[i * MyVoxelMapImpostorsConstants.VERTEXES_PER_IMPOSTOR + 5] = new MyVertexFormatPositionTextureColor(position1, texTopRight, impostor.Color);
                i++;
            }
        }

        public void Update()
        {
            foreach (MyDistantObjectImpostor impostor in m_impostors)
            {
                impostor.Update();
            }
        }

        public void Draw(MyEffectDistantImpostors effect)
        {
            int trianglesCount = m_impostors.Count * MyDistantObjectsImpostorsConstants.TRIANGLES_PER_IMPOSTOR;
            if (trianglesCount <= 0) return;

            CopyToVertexBuffer();

            Device device = MyRender.GraphicsDevice;
            RasterizerState.CullNone.Apply();
            BlendState.NonPremultiplied.Apply();
            DepthStencilState.None.Apply();

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MyRenderCamera.FieldOfView, MyRenderCamera.AspectRatio, 1000, 10000000);

            effect.SetWorldMatrix((Matrix)MatrixD.CreateTranslation(-MyRenderCamera.Position));
            effect.SetViewProjectionMatrix((Matrix)MyRenderCamera.ViewMatrix * projection);
            effect.SetSunDirection(MyRender.Sun.Direction);

            device.VertexDeclaration = MyVertexFormatPositionTextureColor.VertexDeclaration;

            

            for (int i = 0; i < m_impostorProperties.Length; i++)
            {
                effect.SetImpostorTexture(m_impostorProperties[i].Texture);
                effect.SetTechnique(m_impostorProperties[i].LitBySun ? MyEffectDistantImpostors.Technique.ColoredLit : MyEffectDistantImpostors.Technique.Colored);
                effect.Begin();
                device.DrawUserPrimitives(PrimitiveType.TriangleList, m_impostorProperties[i].IndexStart, MyVoxelMapImpostorsConstants.TRIANGLES_PER_IMPOSTOR * m_impostorProperties[i].Count, m_vertices);
                effect.End();
            }
            
        }
    }
}
