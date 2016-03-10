#region Using

using System;
using System.Diagnostics;
using VRage.Generics;
using VRage.Utils;
using VRageMath;

#endregion

namespace VRage.Game
{
    using VRageRender;
    using Color = VRageMath.Color;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;


    public class MyTransparentGeometry
    {
        public static VRageMath.MatrixD Camera;
        public static VRageMath.MatrixD CameraView;

        public const int MAX_TRANSPARENT_GEOMETRY_COUNT = 50000;

        public const int MAX_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.05f);
        public const int MAX_NEW_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.7f);
        
        #region Fields

        static MyObjectsPool<MyAnimatedParticle> m_animatedParticles = new MyObjectsPool<MyAnimatedParticle>(MAX_NEW_PARTICLES_COUNT);

        public static Color ColorizeColor { get; set; }
        public static Vector3 ColorizePlaneNormal { get; set; }
        public static float ColorizePlaneDistance { get; set; }
        public static bool EnableColorize { get; set; }

        static bool IsEnabled
        {
            get
            {
                return true;
                // TODO: Par
                //return MyRender.IsModuleEnabled(MyRenderStage.AlphaBlend, MyRenderModuleEnum.TransparentGeometry) 
                //    || MyRender.IsModuleEnabled(MyRenderStage.AlphaBlendPreHDR, MyRenderModuleEnum.TransparentGeometry)
                //    || MyRender.IsModuleEnabled(MyRenderStage.AlphaBlend, MyRenderModuleEnum.TransparentGeometryForward);
            }
        }

        #endregion

        #region Load/unload content

      
        public static void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyTransparentGeometry.LoadData");

            MyLog.Default.WriteLine(string.Format("MyTransparentGeometry.LoadData - START"));

            m_animatedParticles.DeallocateAll();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region Adding billboards/particles

        public static MyAnimatedParticle AddAnimatedParticle()
        {
            return m_animatedParticles.Allocate(true);
        }

        public static void DeallocateAnimatedParticle(MyAnimatedParticle particle)
        {
            m_animatedParticles.Deallocate(particle);
        }

        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddLineBillboard(string material,
            Vector4 color, Vector3D origin, Vector3 directionNormalized, float length, float thickness, int priority = 0, bool near = false, int customViewProjection = -1)
        {
            AddLineBillboard(material, color, origin, -1, ref MatrixD.Identity, directionNormalized, length, thickness, priority, near, customViewProjection);
        }

        public static void AddLineBillboard(string material,
            Color color, Vector3D origin, int renderObjectID, ref MatrixD worldToLocal, Vector3 directionNormalized, float length, float thickness, int priority = 0, bool near = false, int customViewProjection = -1)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return;


            MyDebug.AssertIsValid(origin);
            MyDebug.AssertIsValid(length);
            MyDebug.AssertDebug(length > 0);
            MyDebug.AssertDebug(thickness > 0);

            //VRageRender.MyBillboard billboard = m_preallocatedBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard == null)
                return;

            billboard.Priority = priority;

            MyPolyLineD polyLine;
            polyLine.LineDirectionNormalized = directionNormalized;
            polyLine.Point0 = origin;
            polyLine.Point1 = origin + directionNormalized * length;
            polyLine.Thickness = thickness;

            MyQuadD quad;
            Vector3D cameraPosition = customViewProjection == -1 ? MyTransparentGeometry.Camera.Translation : VRageRender.MyRenderProxy.BillboardsViewProjectionWrite[customViewProjection].CameraPosition;
            MyUtils.GetPolyLineQuad(out quad, ref polyLine, cameraPosition);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, false, near, false, customViewProjection);

            if(renderObjectID != -1)
            {
                Vector3D.Transform(ref billboard.Position0, ref worldToLocal, out billboard.Position0);
                Vector3D.Transform(ref billboard.Position1, ref worldToLocal, out billboard.Position1);
                Vector3D.Transform(ref billboard.Position2, ref worldToLocal, out billboard.Position2);
                Vector3D.Transform(ref billboard.Position3, ref worldToLocal, out billboard.Position3);
                billboard.ParentID = renderObjectID;
            }

            billboard.Position0.AssertIsValid();
            billboard.Position1.AssertIsValid();
            billboard.Position2.AssertIsValid();
            billboard.Position3.AssertIsValid();

            VRageRender.MyRenderProxy.AddBillboard(billboard);
        }

        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddPointBillboard(string material,
            Color color, Vector3D origin, float radius, float angle, int priority = 0, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1)
        {
            AddPointBillboard(material, color, origin, -1, ref MatrixD.Identity, radius, angle, priority, colorize, near, lowres, customViewProjection);
        }

        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddPointBillboard(string material,
            Color color, Vector3D origin, int renderObjectID, ref MatrixD worldToLocal, float radius, float angle, int priority = 0, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return;

            MyDebug.AssertIsValid(origin);
            MyDebug.AssertIsValid(angle);

            MyQuadD quad;
            Vector3 diff = MyTransparentGeometry.Camera.Translation - origin;
            if (MyUtils.GetBillboardQuadAdvancedRotated(out quad, origin, radius, radius, angle, (origin + MyTransparentGeometry.Camera.Forward * diff.Length())) != false)
            {
                VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
                if (billboard == null)
                    return;

                billboard.Priority = priority;
                CreateBillboard(billboard, ref quad, material, ref color, ref origin, colorize, near, lowres, customViewProjection);

                if (renderObjectID != -1)
                {
                    Vector3D.Transform(ref billboard.Position0, ref worldToLocal, out billboard.Position0);
                    Vector3D.Transform(ref billboard.Position1, ref worldToLocal, out billboard.Position1);
                    Vector3D.Transform(ref billboard.Position2, ref worldToLocal, out billboard.Position2);
                    Vector3D.Transform(ref billboard.Position3, ref worldToLocal, out billboard.Position3);
                    billboard.ParentID = renderObjectID;
                }

                VRageRender.MyRenderProxy.AddBillboard(billboard);
            }
        }

        public static void AddBillboardOrientedCull(Vector3 cameraPos, string material,
            Vector4 color, Vector3 origin, Vector3 leftVector, Vector3 upVector, float radius, int priority = 0, bool colorize = false, int customViewProjection = -1, float reflection = 0)
        {
            if (Vector3.Dot(Vector3.Cross(leftVector, upVector), origin - cameraPos) > 0)
            {
                AddBillboardOriented(material, color, origin, leftVector, upVector, radius, priority, colorize, customViewProjection, reflection);
            }
        }


        public static void AddTriangleBillboard(
            Vector3 p0, Vector3 p1, Vector3 p2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            string material, int parentID, Vector3 worldPosition,
            int priority = 0, bool colorize = false)
        {
            VRageRender.MyTriangleBillboard billboard = VRageRender.MyRenderProxy.TriangleBillboardsPoolWrite.Allocate();
            if (billboard == null)
                return;

            var materialInstance = MyTransparentMaterials.GetMaterial(material);

            billboard.Position0 = p0;
            billboard.Position1 = p1;
            billboard.Position2 = p2;
            billboard.Position3 = p0;

            billboard.UV0 = uv0;
            billboard.UV1 = uv1;
            billboard.UV2 = uv2;

            billboard.Normal0 = n0;
            billboard.Normal1 = n1;
            billboard.Normal2 = n2;

            billboard.Near = false;
            billboard.Lowres = false;

            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyTransparentGeometry.Camera.Translation, worldPosition);

            billboard.Material = material;
            billboard.Color = materialInstance.Color;
            billboard.ColorIntensity = 1;
            billboard.Priority = priority;
            billboard.EnableColorize = colorize;
            billboard.CustomViewProjection = -1;
            billboard.Reflectivity = materialInstance.Reflectivity;

            billboard.ParentID = parentID;

            VRageRender.MyRenderProxy.AddBillboard(billboard);
        }


        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  This billboard isn't facing the camera. It's always oriented in specified direction. May be used as thrusts, or inner light of reflector.
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
        public static void AddBillboardOriented(string material,
            Color color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float radius, int priority = 0, bool colorize = false, int customViewProjection = -1, float reflection = 0)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return;

            MyUtils.AssertIsValid(origin);
            MyUtils.AssertIsValid(leftVector);
            MyUtils.AssertIsValid(upVector);
            MyUtils.AssertIsValid(radius);
            MyDebug.AssertDebug(radius > 0);

            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();// m_preallocatedBillboards.Allocate();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard == null)
                return;

            billboard.Priority = priority;
            
            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, radius, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, colorize, false, false, customViewProjection, reflection);

            VRageRender.MyRenderProxy.AddBillboard(billboard);
        }


       
       public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material,
           ref Color color, ref Vector3D origin, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1, float reflection = 0)
       {
           CreateBillboard(billboard, ref quad, material, "Test", 0, ref color, ref origin, colorize, near, lowres, customViewProjection, reflection);
       }

       public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material,
           ref Color color, ref Vector3D origin, Vector2 uvOffset, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1, float reflection = 0)
       {
           CreateBillboard(billboard, ref quad, material, "Test", 0, ref color, ref origin, uvOffset, colorize, near, lowres, customViewProjection, reflection);
       }

       public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, string blendMaterial, float textureBlendRatio,
       ref Color color, ref Vector3D origin, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1, float reflection = 0)
       {
           CreateBillboard(billboard, ref quad, material, blendMaterial, textureBlendRatio, ref color, ref origin, Vector2.Zero, colorize, near, lowres, customViewProjection, reflection);
       }

       //  This method is like a constructor (which we can't use because billboards are allocated from a pool).
       //  It starts/initializes a billboard. Refs used only for optimalization
       public static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, string blendMaterial, float textureBlendRatio,
       ref Color color, ref Vector3D origin, Vector2 uvOffset, bool colorize = false, bool near = false, bool lowres = false, int customViewProjection = -1, float reflectivity = 0)
       {
           System.Diagnostics.Debug.Assert(material != null);

           if (string.IsNullOrEmpty(material) || !MyTransparentMaterials.ContainsMaterial(material))
           {
               material = "ErrorMaterial";
               color = Vector4.One;
           }

           billboard.Material = material;
           billboard.BlendMaterial = blendMaterial;
           billboard.BlendTextureRatio = textureBlendRatio;

           MyUtils.AssertIsValid(quad.Point0);
           MyUtils.AssertIsValid(quad.Point1);
           MyUtils.AssertIsValid(quad.Point2);
           MyUtils.AssertIsValid(quad.Point3);
            

           //  Billboard vertices
           billboard.Position0 = quad.Point0;
           billboard.Position1 = quad.Point1;
           billboard.Position2 = quad.Point2;
           billboard.Position3 = quad.Point3;

           billboard.UVOffset = uvOffset;

           EnableColorize = colorize;

           if (EnableColorize)
               billboard.Size = (float)(billboard.Position0 - billboard.Position2).Length();

           //  Distance for sorting
           //  IMPORTANT: Must be calculated before we do color and alpha misting, because we need distance there
           Vector3D cameraPosition = customViewProjection == -1 ? MyTransparentGeometry.Camera.Translation : VRageRender.MyRenderProxy.BillboardsViewProjectionWrite[customViewProjection].CameraPosition;
           billboard.DistanceSquared = (float)Vector3D.DistanceSquared(cameraPosition, origin);
           
           //  Color
           billboard.Color = color;
           billboard.ColorIntensity = 1;
           billboard.Reflectivity = reflectivity;

           billboard.Near = near;
           billboard.Lowres = lowres;
           billboard.CustomViewProjection = customViewProjection;
           billboard.ParentID = -1;

           //  Alpha depends on distance to camera. Very close bilboards are more transparent, so player won't see billboard errors or rotating billboards
           var mat = MyTransparentMaterials.GetMaterial(billboard.Material);
           if (mat.AlphaMistingEnable)
               billboard.Color *= MathHelper.Clamp(((float)Math.Sqrt(billboard.DistanceSquared) - mat.AlphaMistingStart) / (mat.AlphaMistingEnd - mat.AlphaMistingStart), 0, 1);

           billboard.Color *= mat.Color;

           billboard.ContainedBillboards.Clear();
       }
          


        //  Add billboard for one frame only. This billboard isn't particle (it doesn't survive this frame, doesn't have update/draw methods, etc).
        //  This billboard isn't facing the camera. It's always oriented in specified direction. May be used as thrusts, or inner light of reflector.
        //  It's used by other classes when they want to draw some billboard (e.g. rocket thrusts, reflector glare).
       public static void AddBillboardOriented(string material,
            Vector4 color, Vector3 origin, Vector3 leftVector, Vector3 upVector, float width, float height, int priority = 0, bool colorize = false)
        {
            AddBillboardOriented(material, color, origin, leftVector, upVector, width, height, Vector2.Zero, priority, colorize);
        }

        public static void AddBillboardOriented(string material,
            Color color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float width, float height, Vector2 uvOffset, int priority = 0, bool colorize = false, int customViewProjection = -1)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return;

            //VRageRender.MyBillboard billboard = m_preallocatedBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard == null)
                return;

            billboard.Priority = priority;

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, width, height, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, uvOffset, colorize, false, false, customViewProjection);

            VRageRender.MyRenderProxy.AddBillboard(billboard);
        }

        public static bool AddQuad(string material, ref MyQuadD quad, ref Color color, ref Vector3D vctPos, int priority = 0, int customViewProjection = -1)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return false;

            MyUtils.AssertIsValid(quad.Point0);
            MyUtils.AssertIsValid(quad.Point1);
            MyUtils.AssertIsValid(quad.Point2);
            MyUtils.AssertIsValid(quad.Point3);

            //VRageRender.MyBillboard billboard = m_preallocatedBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard == null)
                return false;

            billboard.Priority = priority;

            CreateBillboard(billboard, ref quad, material, ref color, ref vctPos, false, false, false, customViewProjection);

            VRageRender.MyRenderProxy.AddBillboard(billboard);

            return true;
        }

        public static bool AddAttachedQuad(string material, ref MyQuadD quad, ref Color color, ref Vector3D vctPos, int renderObjectID, int priority = 0)
        {
            Debug.Assert(material != null);
            if (!IsEnabled) return false;

            MyUtils.AssertIsValid(quad.Point0);
            MyUtils.AssertIsValid(quad.Point1);
            MyUtils.AssertIsValid(quad.Point2);
            MyUtils.AssertIsValid(quad.Point3);

            //VRageRender.MyBillboard billboard = m_preallocatedBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard == null)
                return false;

            billboard.Priority = priority;

            CreateBillboard(billboard, ref quad, material, ref color, ref vctPos, false, false, false, -1);
            billboard.ParentID = renderObjectID;

            VRageRender.MyRenderProxy.AddBillboard(billboard);

            return true;
        }



        public static VRageRender.MyBillboard AddBillboardParticle(MyAnimatedParticle particle, VRageRender.MyBillboard effectBillboard, bool sort)
        {
            //MyBillboard billboard = m_preallocatedParticleBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("item.Value.Draw");
                if (particle.Draw(billboard) == true)
                {
                    if (!sort)
                        effectBillboard.ContainedBillboards.Add(billboard);

                    MyPerformanceCounter.PerCameraDrawWrite.NewParticlesCount++;
                }

                billboard.CustomViewProjection = -1;

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            return billboard;
        }


        public static VRageRender.MyBillboard AddBillboardEffect(MyParticleEffect effect)
        {
            //VRageRender.MyBillboard billboard = m_preallocatedParticleBillboards.Allocate();
            //VRageRender.MyBillboard billboard = new VRageRender.MyBillboard();
            VRageRender.MyBillboard billboard = VRageRender.MyRenderProxy.BillboardsPoolWrite.Allocate();
            if (billboard != null)
            {
                billboard.ContainedBillboards.Clear();

                MyTransparentGeometry.StartParticleProfilingBlock("AddBillboardEffect");

                billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyTransparentGeometry.Camera.Translation, effect.WorldMatrix.Translation);

                billboard.Lowres = effect.LowRes || VRageRender.MyRenderConstants.RenderQualityProfile.LowResParticles;
                billboard.Near = effect.Near;
                billboard.CustomViewProjection = -1;

                MyTransparentGeometry.EndParticleProfilingBlock();
            }
            return billboard;
        }

        
        #endregion

        [Conditional("PARTICLE_PROFILING")]
        public static void StartParticleProfilingBlock(string name)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock(name);
        }

        [Conditional("PARTICLE_PROFILING")]
        public static void EndParticleProfilingBlock()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        
    }
}
