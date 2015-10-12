#region Using

using Sandbox.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage;
using VRage.Animations;
using VRage.Utils;
using VRageMath;
using VRageRender;


#endregion

namespace Sandbox.Graphics.TransparentGeometry.Particles
{
    public enum MyVelocityDirEnum
    {
        Default,
        FromEmitterCenter
    }



    public class MyParticleGeneration : IComparable
    {
        #region Static

        static string[] MyVelocityDirStrings =
        {
            "Default",
            "FromEmitterCenter"
        };

        static string[] MyParticleTypeStrings =
        {
            "Point",
            "Line",
            "Trail"
        };

        static List<string> s_velocityDirStrings = MyVelocityDirStrings.ToList<string>();
        static List<string> s_particleTypeStrings = MyParticleTypeStrings.ToList<string>();

        #endregion

        #region Members

        static readonly int Version = 0;
        string m_name;

        MyParticleEffect m_effect;
        MyParticleEmitter m_emitter;
        float m_particlesToCreate = 0;
        float m_birthRate = 0;
        List<MyAnimatedParticle> m_particles = new List<MyAnimatedParticle>(64);
        private Vector3D? m_lastEffectPosition;

        FastResourceLock ParticlesLock = new FastResourceLock(); 

        BoundingBoxD m_AABB = new BoundingBoxD();

        private enum MyGenerationPropertiesEnum
        {
            Birth,
            BirthVar,

            Life,
            LifeVar,

            Velocity,
            VelocityDir,

            Angle,
            AngleVar,

            RotationSpeed,
            RotationSpeedVar,

            Radius,
            RadiusVar,

            Color,
            ColorVar,

            Material,

            ParticleType,
            Thickness,
            Enabled,
            BlendTextures,

            EnableCustomRadius,
            EnableCustomVelocity,
            EnableCustomBirth,

            OnDie,
            OnLife,

            LODBirth,
            LODRadius,

            MotionInheritance,

            UseLayerSorting,
            SortLayer,

            AlphaAnisotropic,
            Gravity,

            PivotRotation,
            PivotDistance,
            PivotDistVar,

            Acceleration,
            AccelerationVar            
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[Enum.GetValues(typeof(MyGenerationPropertiesEnum)).Length];
        

        /// <summary>
        /// Public members to easy access
        /// </summary>
        public MyAnimatedPropertyFloat Birth 
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.Birth]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Birth] = value; }
        }

        public MyConstPropertyFloat BirthVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.BirthVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.BirthVar] = value; }
        }

        public MyAnimatedPropertyFloat Life
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.Life]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Life] = value; }
        }

        public MyConstPropertyFloat LifeVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.LifeVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.LifeVar] = value; }
        }

        public MyAnimatedPropertyVector3 Velocity
        {
            get { return (MyAnimatedPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.Velocity]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Velocity] = value; }
        }

        public MyVelocityDirEnum VelocityDir
        {
            get { return (MyVelocityDirEnum)(int)((MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.VelocityDir]); }
            private set { m_properties[(int)MyGenerationPropertiesEnum.VelocityDir].SetValue((int)value); }
        }

        public MyAnimatedPropertyFloat Angle
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.Angle]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Angle] = value; }
        }

        public MyConstPropertyFloat AngleVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.AngleVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.AngleVar] = value; }
        }

        public MyAnimatedPropertyFloat RotationSpeed
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.RotationSpeed]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.RotationSpeed] = value; }
        }

        public MyConstPropertyFloat RotationSpeedVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.RotationSpeedVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.RotationSpeedVar] = value; }
        }

        public MyAnimatedProperty2DFloat Radius
        {
            get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGenerationPropertiesEnum.Radius]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Radius] = value; }
        }

        public MyAnimatedPropertyFloat RadiusVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.RadiusVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.RadiusVar] = value; }
        }

        public MyAnimatedProperty2DVector4 Color
        {
            get { return (MyAnimatedProperty2DVector4)m_properties[(int)MyGenerationPropertiesEnum.Color]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Color] = value; }
        }

        public MyAnimatedPropertyFloat ColorVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.ColorVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ColorVar] = value; }
        }

        public MyAnimatedProperty2DTransparentMaterial Material
        {
            get { return (MyAnimatedProperty2DTransparentMaterial)m_properties[(int)MyGenerationPropertiesEnum.Material]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Material] = value; }
        }

        public MyConstPropertyEnum ParticleType
        {
            get { return (MyConstPropertyEnum)m_properties[(int)MyGenerationPropertiesEnum.ParticleType]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ParticleType] = value; }
        }

        public MyAnimatedPropertyFloat Thickness
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.Thickness]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Thickness] = value; }
        }

        public MyConstPropertyBool Enabled
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.Enabled]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Enabled] = value; }
        }

        public MyConstPropertyBool BlendTextures
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.BlendTextures]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.BlendTextures] = value; }
        }

        public MyConstPropertyBool EnableCustomRadius
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.EnableCustomRadius]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.EnableCustomRadius] = value; }
        }

        public MyConstPropertyBool EnableCustomBirth
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.EnableCustomBirth]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.EnableCustomBirth] = value; }
        }

        public MyConstPropertyGenerationIndex OnDie
        {
            get { return (MyConstPropertyGenerationIndex)m_properties[(int)MyGenerationPropertiesEnum.OnDie]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.OnDie] = value; }
        }

        public MyConstPropertyGenerationIndex OnLife
        {
            get { return (MyConstPropertyGenerationIndex)m_properties[(int)MyGenerationPropertiesEnum.OnLife]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.OnLife] = value; }
        }

        public MyAnimatedPropertyFloat LODBirth
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.LODBirth]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.LODBirth] = value; }
        }

        public MyAnimatedPropertyFloat LODRadius
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.LODRadius]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.LODRadius] = value; }
        }

        public MyAnimatedPropertyFloat MotionInheritance
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.MotionInheritance]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.MotionInheritance] = value; }
        }

        public MyConstPropertyBool UseLayerSorting
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.UseLayerSorting]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.UseLayerSorting] = value; }
        }

        public MyConstPropertyInt SortLayer
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.SortLayer]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.SortLayer] = value; }
        }

        public MyConstPropertyBool AlphaAnisotropic
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGenerationPropertiesEnum.AlphaAnisotropic]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.AlphaAnisotropic] = value; }
        }

        public MyConstPropertyFloat Gravity
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.Gravity]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Gravity] = value; }
        }

        public MyAnimatedProperty2DVector3 PivotRotation
        {
            get { return (MyAnimatedProperty2DVector3)m_properties[(int)MyGenerationPropertiesEnum.PivotRotation]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.PivotRotation] = value; }
        }

        public MyAnimatedProperty2DFloat PivotDistance
        {
            get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGenerationPropertiesEnum.PivotDistance]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.PivotDistance] = value; }
        }

        public MyConstPropertyFloat PivotDistVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.PivotDistVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.PivotDistVar] = value; }
        }

        public MyAnimatedProperty2DVector3 Acceleration
        {
            get { return (MyAnimatedProperty2DVector3)m_properties[(int)MyGenerationPropertiesEnum.Acceleration]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Acceleration] = value; }
        }

        public MyConstPropertyFloat AccelerationVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.AccelerationVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.AccelerationVar] = value; }
        }

        //////////////////////////////

        #endregion

        #region Constructor & Init

        public MyParticleGeneration()
        {
            m_emitter = new MyParticleEmitter(MyParticleEmitterType.Point);
        }

        public void SetVelocityDir(MyVelocityDirEnum val)
        {
            VelocityDir = val;
        }

        public void Init()
        {
            System.Diagnostics.Debug.Assert(Birth == null);

            AddProperty(MyGenerationPropertiesEnum.Birth, new MyAnimatedPropertyFloat("Birth"));
            AddProperty(MyGenerationPropertiesEnum.BirthVar, new MyConstPropertyFloat("Birth var"));

            AddProperty(MyGenerationPropertiesEnum.Life, new MyAnimatedPropertyFloat("Life"));
            AddProperty(MyGenerationPropertiesEnum.LifeVar, new MyConstPropertyFloat("Life var"));

            AddProperty(MyGenerationPropertiesEnum.Velocity, new MyAnimatedPropertyVector3("Velocity"));
            AddProperty(MyGenerationPropertiesEnum.VelocityDir, new MyConstPropertyEnum("Velocity dir", typeof(MyVelocityDirEnum) ,s_velocityDirStrings));

            AddProperty(MyGenerationPropertiesEnum.Angle, new MyAnimatedPropertyFloat("Angle"));
            AddProperty(MyGenerationPropertiesEnum.AngleVar, new MyConstPropertyFloat("Angle var")); 

            AddProperty(MyGenerationPropertiesEnum.RotationSpeed, new MyAnimatedPropertyFloat("Rotation speed"));
            AddProperty(MyGenerationPropertiesEnum.RotationSpeedVar, new MyConstPropertyFloat("Rotation speed var"));

            AddProperty(MyGenerationPropertiesEnum.Radius, new MyAnimatedProperty2DFloat("Radius"));
            AddProperty(MyGenerationPropertiesEnum.RadiusVar, new MyAnimatedPropertyFloat("Radius var"));

            AddProperty(MyGenerationPropertiesEnum.Color, new MyAnimatedProperty2DVector4("Color"));
            AddProperty(MyGenerationPropertiesEnum.ColorVar, new MyAnimatedPropertyFloat("Color var"));

            AddProperty(MyGenerationPropertiesEnum.Material, new MyAnimatedProperty2DTransparentMaterial("Material", MyTransparentMaterialInterpolator.Switch));

            AddProperty(MyGenerationPropertiesEnum.ParticleType, new MyConstPropertyEnum("Particle type", typeof(MyParticleTypeEnum), s_particleTypeStrings));

            AddProperty(MyGenerationPropertiesEnum.Thickness, new MyAnimatedPropertyFloat("Thickness"));

            AddProperty(MyGenerationPropertiesEnum.Enabled, new MyConstPropertyBool("Enabled"));
            Enabled.SetValue(true);

            AddProperty(MyGenerationPropertiesEnum.BlendTextures, new MyConstPropertyBool("Blend textures"));
            BlendTextures.SetValue(true);

            AddProperty(MyGenerationPropertiesEnum.EnableCustomRadius, new MyConstPropertyBool("Enable custom radius"));
            AddProperty(MyGenerationPropertiesEnum.EnableCustomVelocity, new MyConstPropertyBool("Enable custom velocity"));
            AddProperty(MyGenerationPropertiesEnum.EnableCustomBirth, new MyConstPropertyBool("Enable custom birth"));

            AddProperty(MyGenerationPropertiesEnum.OnDie, new MyConstPropertyGenerationIndex("OnDie"));
            OnDie.SetValue(-1);
            AddProperty(MyGenerationPropertiesEnum.OnLife, new MyConstPropertyGenerationIndex("OnLife"));
            OnLife.SetValue(-1);

            AddProperty(MyGenerationPropertiesEnum.LODBirth, new MyAnimatedPropertyFloat("LODBirth"));
            AddProperty(MyGenerationPropertiesEnum.LODRadius, new MyAnimatedPropertyFloat("LODRadius"));

            AddProperty(MyGenerationPropertiesEnum.MotionInheritance, new MyAnimatedPropertyFloat("Motion inheritance"));

            AddProperty(MyGenerationPropertiesEnum.UseLayerSorting, new MyConstPropertyBool("Use layer sorting"));
            AddProperty(MyGenerationPropertiesEnum.SortLayer, new MyConstPropertyInt("Sort layer"));
            AddProperty(MyGenerationPropertiesEnum.AlphaAnisotropic, new MyConstPropertyBool("Alpha anisotropic"));

            AddProperty(MyGenerationPropertiesEnum.Gravity, new MyConstPropertyFloat("Gravity"));

            AddProperty(MyGenerationPropertiesEnum.PivotRotation, new MyAnimatedProperty2DVector3("PivotRotation"));
            AddProperty(MyGenerationPropertiesEnum.PivotDistance, new MyAnimatedProperty2DFloat("PivotDistance"));
            AddProperty(MyGenerationPropertiesEnum.PivotDistVar, new MyConstPropertyFloat("PivotDistVar"));

            AddProperty(MyGenerationPropertiesEnum.Acceleration, new MyAnimatedProperty2DVector3("Acceleration"));
            AddProperty(MyGenerationPropertiesEnum.AccelerationVar, new MyConstPropertyFloat("AccelerationVar"));

            Thickness.AddKey(0, 1.0f);

            LODBirth.AddKey(0, 1.0f);
            LODRadius.AddKey(0, 1.0f);

            UseLayerSorting.SetValue(false);
            SortLayer.SetValue(-1);

            PivotDistVar.SetValue(1);

            AccelerationVar.SetValue(0);

            m_emitter.Init();
        }

        public void Done()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                if (m_properties[i] is IMyAnimatedProperty)
                    (m_properties[i] as IMyAnimatedProperty).ClearKeys();
            }

            m_emitter.Done();
           
            Close();
        }

        public void Start(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(m_effect == null);
            System.Diagnostics.Debug.Assert(m_particles.Count == 0);
            System.Diagnostics.Debug.Assert(Birth == null);

            m_effect = effect;
            m_name = "ParticleGeneration";

            m_emitter.Start();

            m_lastEffectPosition = null;
            IsInherited = false;
            m_birthRate = 0.0f;
            m_particlesToCreate = 0.0f;
            m_AABB = BoundingBoxD.CreateInvalid();
      }

        public void Close()
        {
            Clear();

            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }

            m_emitter.Close();

            m_effect = null;
        }

        /// <summary>
        /// Only for some testing values
        /// </summary>
        public void InitDefault()
        {
            Birth.AddKey(0, 1.0f);
            Life.AddKey(0, 10.0f);
            Thickness.AddKey(0, 1.0f);
            Velocity.AddKey(0, new Vector3(0, 1, 0));


            MyAnimatedPropertyVector4 colorKey = new MyAnimatedPropertyVector4(Color.Name);
            colorKey.AddKey(0, new Vector4(1, 0, 0, 1));
            colorKey.AddKey(1, new Vector4(0, 0, 1, 1));
            Color.AddKey(0, colorKey);

            MyAnimatedPropertyFloat radiusKey = new MyAnimatedPropertyFloat(Radius.Name);
            radiusKey.AddKey(0, 1.0f);
            Radius.AddKey(0, radiusKey);

            MyAnimatedPropertyTransparentMaterial materialKey = new MyAnimatedPropertyTransparentMaterial(Material.Name);
            materialKey.AddKey(0,  MyTransparentMaterials.GetMaterial("Smoke"));
            Material.AddKey(0, materialKey);

            LODBirth.AddKey(0, 1.0f);
            LODRadius.AddKey(0, 1.0f);

            PivotDistVar.SetValue(1.0f);

            AccelerationVar.SetValue(0.0f);

            UseLayerSorting.SetValue(false);
            SortLayer.SetValue(-1);
        }

        #endregion

        #region Member properties

        T AddProperty<T>(MyGenerationPropertiesEnum e, T property) where T : IMyConstProperty
        {
            System.Diagnostics.Debug.Assert(m_properties[(int)e] == null, "Property already assigned!");

            m_properties[(int)e] = property;
            return property;
        }

        public IEnumerable<IMyConstProperty> GetProperties()
        {
            return m_properties;
        }

        #endregion

        #region Update


        private void UpdateParticlesLife()
        {
            int counter = 0;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleGeneration-UpdateParticlesLife");

            MyParticleGeneration inheritedGeneration = null;
            Vector3D previousParticlePosition = m_effect.WorldMatrix.Translation;
            float particlesToCreate = 0;
            m_AABB = BoundingBoxD.CreateInvalid();
            m_AABB = m_AABB.Include(ref previousParticlePosition);

            if (OnDie.GetValue<int>() != -1)
            {
                inheritedGeneration = GetInheritedGeneration(OnDie.GetValue<int>());

                if (inheritedGeneration == null)
                {
                    OnDie.SetValue(-1);
                }
                else
                {
                    inheritedGeneration.IsInherited = true;
                    particlesToCreate = inheritedGeneration.m_particlesToCreate;
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleGeneration-Update01");

            Vector3D previousTrail0 = previousParticlePosition;
            Vector3D previousTrail1 = previousParticlePosition;

            using (ParticlesLock.AcquireExclusiveUsing())
            {
                while (counter < m_particles.Count)
                {
                    float motionInheritance;
                    MotionInheritance.GetInterpolatedValue(m_effect.GetElapsedTime(), out motionInheritance);

                    MyAnimatedParticle particle = m_particles[counter];

                    if (motionInheritance > 0)
                    {
                        m_effect.CalculateDeltaMatrix = true;
                    }

                    if (particle.Update())
                    {
                        if (motionInheritance > 0)
                        {
                            var delta = m_effect.GetDeltaMatrix();
                            particle.AddMotionInheritance(ref motionInheritance, ref delta);
                        }

                        if (counter == 0)
                        {
                            previousParticlePosition = particle.ActualPosition;
                            previousTrail0 = particle.Quad.Point1;
                            previousTrail1 = particle.Quad.Point2;
                            particle.Quad.Point0 = particle.ActualPosition;
                            particle.Quad.Point2 = particle.ActualPosition;
                        }

                        counter++;


                        if (particle.Type == MyParticleTypeEnum.Trail)
                        {
                            if (particle.ActualPosition == previousParticlePosition)
                            {
                                particle.Quad.Point0 = particle.ActualPosition;
                                particle.Quad.Point1 = particle.ActualPosition;
                                particle.Quad.Point2 = particle.ActualPosition;
                                particle.Quad.Point3 = particle.ActualPosition;
                            }
                            else
                            {
                                MyPolyLineD polyLine = new MyPolyLineD();
                                polyLine.Thickness = particle.Thickness;
                                polyLine.Point0 = particle.ActualPosition;
                                polyLine.Point1 = previousParticlePosition;

                                Vector3D direction = polyLine.Point1 - polyLine.Point0;
                                Vector3D normalizedDirection = MyUtils.Normalize(polyLine.Point1 - polyLine.Point0);

                                polyLine.LineDirectionNormalized = normalizedDirection;
                                var camPos = MyTransparentGeometry.Camera.Translation;
                                MyUtils.GetPolyLineQuad(out particle.Quad, ref polyLine, camPos);

                                particle.Quad.Point0 = previousTrail0 + direction * 0.15f;
                                particle.Quad.Point3 = previousTrail1 + direction * 0.15f;
                                previousTrail0 = particle.Quad.Point1;
                                previousTrail1 = particle.Quad.Point2;
                            }
                        }

                        previousParticlePosition = particle.ActualPosition;

                        m_AABB = m_AABB.Include(ref previousParticlePosition);
                        particle.Flags = GetEffect().IsInFrustum ? particle.Flags | MyAnimatedParticle.ParticleFlags.IsInFrustum : particle.Flags & ~MyAnimatedParticle.ParticleFlags.IsInFrustum;
                        continue;
                    }

                    if (inheritedGeneration != null)
                    {
                        inheritedGeneration.m_particlesToCreate = particlesToCreate;
                        inheritedGeneration.EffectMatrix = MatrixD.CreateWorld(particle.ActualPosition, Vector3D.Normalize(particle.Velocity), Vector3D.Cross(Vector3D.Left, particle.Velocity));
                        inheritedGeneration.UpdateParticlesCreation();
                    }

                    m_particles.Remove(particle);
                    MyTransparentGeometry.DeallocateAnimatedParticle(particle);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void UpdateParticlesCreation()
        {
            if (!Enabled.GetValue<bool>())
                return;

            //particles to create in this update
            if (!m_effect.IsStopped)
            {
                float lodBirth = 1.0f;
                if (GetEffect().EnableLods)
                {
                    LODBirth.GetInterpolatedValue<float>(GetEffect().Distance, out lodBirth);
                }

                Birth.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out m_birthRate);
                m_birthRate *=
                    MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS  
                    * (EnableCustomBirth ? m_effect.UserBirthMultiplier : 1.0f)
                    * MyParticlesManager.BirthMultiplierOverall
                    * lodBirth;

                m_particlesToCreate += m_birthRate;
            }

            //If speed of effect is too high, there would be created bunches
            //of particles each frame. By interpolating position, we create
            //seamless particle creation.
            Vector3 positionDelta = Vector3.Zero;

            if (!m_lastEffectPosition.HasValue)
                m_lastEffectPosition = EffectMatrix.Translation;

            //Position delta interpolates particle position at fast flying objects, dont do that while motion inheritance
            if (m_particlesToCreate > 1.0f && !m_effect.CalculateDeltaMatrix)
            {
                positionDelta = (EffectMatrix.Translation - m_lastEffectPosition.Value) / (int)m_particlesToCreate;
            }

            //VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("CreateParticle");


            using (ParticlesLock.AcquireExclusiveUsing())
            {
                int maxParticles = 40;
                while (m_particlesToCreate >= 1.0f && maxParticles-- > 0)
                {
                    if (m_effect.CalculateDeltaMatrix)
                        CreateParticle(EffectMatrix.Translation);
                    else
                        CreateParticle(m_lastEffectPosition.Value + positionDelta * (int)m_particlesToCreate);

                    m_particlesToCreate -= 1.0f;
                }
            }

         //   VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("OnLife");

            if (OnLife.GetValue<int>() != -1)
            {
                MyParticleGeneration inheritedGeneration = GetInheritedGeneration(OnLife.GetValue<int>());

                if (inheritedGeneration == null)
                {
                    OnLife.SetValue(-1);
                }
                else
                {
                    inheritedGeneration.IsInherited = true;

                    float particlesToCreate = inheritedGeneration.m_particlesToCreate;

                    using (ParticlesLock.AcquireSharedUsing())
                    {
                        foreach (MyAnimatedParticle particle in m_particles)
                        {
                            inheritedGeneration.m_particlesToCreate = particlesToCreate;
                            inheritedGeneration.EffectMatrix = MatrixD.CreateWorld(particle.ActualPosition, (Vector3D)particle.Velocity, Vector3D.Cross(Vector3D.Left, particle.Velocity));
                            inheritedGeneration.UpdateParticlesCreation();
                        }
                    }
                }
            }

          //  VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            m_lastEffectPosition = EffectMatrix.Translation;
        }


        public void Update()
        {
            m_birthRate = 0.0f;

            UpdateParticlesLife();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleGeneration-UpdateCreation");
            if (!IsInherited)               
                UpdateParticlesCreation();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private MyParticleGeneration GetInheritedGeneration(int generationIndex)
        {
             if (generationIndex >= m_effect.GetGenerations().Count || generationIndex == m_effect.GetGenerations().IndexOf(this))
                 return null;

             return m_effect.GetGenerations()[generationIndex];
        }

        #endregion

        #region Clear & Create

        public void Clear()
        {
            using (ParticlesLock.AcquireExclusiveUsing())
            {
                int counter = 0;
                while (counter < m_particles.Count)
                {
                    MyAnimatedParticle particle = m_particles[counter];
                    m_particles.Remove(particle);
                    MyTransparentGeometry.DeallocateAnimatedParticle(particle);
                }
            }

            m_particlesToCreate = 0;
            m_lastEffectPosition = m_effect.WorldMatrix.Translation;
        }

        private void CreateParticle(Vector3D interpolatedEffectPosition)
        {
            MyAnimatedParticle particle = MyTransparentGeometry.AddAnimatedParticle();

            if (particle == null)
                return;

            particle.Type = (MyParticleTypeEnum)ParticleType.GetValue<int>();

            MyUtils.AssertIsValid(m_effect.WorldMatrix);

            Vector3D startOffset;
            m_emitter.CalculateStartPosition(m_effect.GetElapsedTime(), MatrixD.CreateWorld(interpolatedEffectPosition, m_effect.WorldMatrix.Forward, m_effect.WorldMatrix.Up), m_effect.UserEmitterScale * m_effect.UserAxisScale, m_effect.UserEmitterScale * m_effect.UserScale, out startOffset, out particle.StartPosition);

            Vector3D particlePosition = particle.StartPosition;
            m_AABB = m_AABB.Include(ref particlePosition);

            Life.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.Life);
            float lifeVar = LifeVar;
            if (lifeVar > 0)
            {
                particle.Life = MathHelper.Max(MyUtils.GetRandomFloat(particle.Life - lifeVar, particle.Life + lifeVar), 0.1f);
            }

            Vector3 vel;
            Velocity.GetInterpolatedValue<Vector3>(m_effect.GetElapsedTime(), out vel);
            vel.X *= m_effect.UserScale;
            vel.Y *= m_effect.UserScale;
            vel.Z *= m_effect.UserScale;
            particle.Velocity = vel;

            if (VelocityDir == MyVelocityDirEnum.FromEmitterCenter)
            {
                if (!MyUtils.IsZero(startOffset - particle.StartPosition))
                {
                    float length = particle.Velocity.Length();
                    particle.Velocity = MyUtils.Normalize(particle.StartPosition - (Vector3D)startOffset) * length;
                }
            }
            particle.Velocity = Vector3D.TransformNormal(particle.Velocity, GetEffect().WorldMatrix);

            Angle.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.Angle);
            float angleVar = AngleVar;
            if (angleVar > 0)
            {
                particle.Angle = MyUtils.GetRandomFloat(particle.Angle - AngleVar, particle.Angle + AngleVar);
            }

            RotationSpeed.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.RotationSpeed);
            float rotationSpeedVar = RotationSpeedVar;
            if (rotationSpeedVar > 0)
            {
                particle.RotationSpeed = MyUtils.GetRandomFloat(particle.RotationSpeed - RotationSpeedVar, particle.RotationSpeed + RotationSpeedVar);
            }

            float radiusVar;
            RadiusVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out radiusVar);
            float lodRadius = 1.0f;
            if (GetEffect().EnableLods)
            {
                LODRadius.GetInterpolatedValue<float>(GetEffect().Distance, out lodRadius);
            }
            
            Radius.GetInterpolatedKeys(m_effect.GetElapsedTime(), 
                radiusVar, 
                (EnableCustomRadius.GetValue<bool>() ? m_effect.UserRadiusMultiplier : 1.0f)
                * lodRadius
                * GetEffect().UserScale, 
                particle.Radius);
                    
            if (particle.Type != MyParticleTypeEnum.Point)
                Thickness.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.Thickness);

            particle.Thickness *= lodRadius;

            float colorVar;
            ColorVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out colorVar);
            Color.GetInterpolatedKeys(m_effect.GetElapsedTime(), colorVar, 1.0f, particle.Color);

            Material.GetInterpolatedKeys(m_effect.GetElapsedTime(), 0, 1.0f, particle.Material);

            particle.Flags = 0;
            particle.Flags |= BlendTextures.GetValue<bool>() ? MyAnimatedParticle.ParticleFlags.BlendTextures : 0;
            particle.Flags |= GetEffect().IsInFrustum ? MyAnimatedParticle.ParticleFlags.IsInFrustum : 0;

            if (PivotRotation.GetKeysCount() > 0 && PivotDistance.GetKeysCount() > 0)
            {
                particle.PivotDistance = new MyAnimatedPropertyFloat(PivotDistance.Name); 
                particle.PivotRotation = new MyAnimatedPropertyVector3(PivotRotation.Name);
                PivotDistance.GetInterpolatedKeys(m_effect.GetElapsedTime(), PivotDistVar, 1.0f, particle.PivotDistance);
                PivotRotation.GetInterpolatedKeys(m_effect.GetElapsedTime(), 1.0f, particle.PivotRotation);
            }

            if (Acceleration.GetKeysCount() > 0)
            {
                particle.Acceleration = new MyAnimatedPropertyVector3(Acceleration.Name);
                float multiplier = MyUtils.GetRandomFloat(1-AccelerationVar, 1+AccelerationVar);
                Acceleration.GetInterpolatedKeys(m_effect.GetElapsedTime(), multiplier, particle.Acceleration);
            }

            particle.Start(this);
                 
            m_particles.Add(particle);
        }

        public MyParticleGeneration CreateInstance(MyParticleEffect effect)
        {
            MyParticleGeneration generation = MyParticlesManager.GenerationsPool.Allocate(true);
            if (generation == null)
                return null;

            generation.Start(effect);

            generation.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                generation.m_properties[i] = m_properties[i];
            }

            generation.m_emitter.CreateInstance(m_emitter);

            return generation;
        }

        public MyParticleGeneration Duplicate(MyParticleEffect effect)
        {
            MyParticleGeneration generation = MyParticlesManager.GenerationsPool.Allocate();
            generation.Start(effect);

            generation.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                generation.m_properties[i] = m_properties[i].Duplicate();
            }

            m_emitter.Duplicate(generation.m_emitter);

            return generation;
        }

        #endregion

        #region Properties

        public MyParticleEmitter GetEmitter()
        {
            return m_emitter;
        }

        public MyParticleEffect GetEffect()
        {
            return m_effect;
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public int GetParticlesCount()
        {
            return m_particles.Count;
        }

        public float GetBirthRate()
        {
            return m_birthRate;
        }

        public MatrixD EffectMatrix { get; set; }

        public bool IsInherited { get; set; }

        public BoundingBoxD GetAABB()
        {
            return m_AABB;
        }

        #endregion

        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleGeneration");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("version", Version.ToString(CultureInfo.InvariantCulture));

            foreach (IMyConstProperty property in m_properties)
            {
                property.Serialize(writer);
            }

            writer.WriteStartElement("Emitter");
            m_emitter.Serialize(writer);
            writer.WriteEndElement();

            writer.WriteEndElement(); //ParticleGeneration
        }

        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);

            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                if (reader.Name == "Emitter")
                    break; //we added new property which is not in xml yet

                property.Deserialize(reader);
            }

            reader.ReadStartElement();
            m_emitter.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadEndElement(); //ParticleGeneration

            //Disable texture blending if it is set but unneccessary
            if (BlendTextures)
            {
                bool someMaterialKeysDifferent = false;
                for (int j = 0; j < Material.GetKeysCount(); j++)
                {
                    MyAnimatedPropertyTransparentMaterial key;
                    float time;
                    Material.GetKey(j, out time, out key);

                    MyTransparentMaterial previousMaterial = null;
                    for (int i = 0; i < key.GetKeysCount(); i++)
                    {
                        float timeMat;
                        MyTransparentMaterial material;
                        key.GetKey(i, out timeMat, out material);

                        if (previousMaterial != null && (previousMaterial != material))
                        {
                            if (previousMaterial.Texture != material.Texture)
                            {
                                someMaterialKeysDifferent = true;
                                break;
                            }
                        }
                        previousMaterial = material;
                    }

                    if (someMaterialKeysDifferent)
                        break;
                }

                if (!someMaterialKeysDifferent)
                {
                    BlendTextures.SetValue(false);
                }
            }

        }

        #endregion

        #region Draw

        List<VRageRender.MyBillboard> m_billboards = new List<VRageRender.MyBillboard>();

        public void PrepareForDraw(ref VRageRender.MyBillboard effectBillboard)
        {
            m_billboards.Clear();

            if (m_particles.Count == 0)
                return;

            if (UseLayerSorting && effectBillboard == null)
            {
                effectBillboard = MyTransparentGeometry.AddBillboardEffect(m_effect);
                if (effectBillboard != null)
                {
                    m_billboards.Add(effectBillboard);
                }
            }

            using (ParticlesLock.AcquireSharedUsing())
            {
                foreach (MyAnimatedParticle particle in m_particles)
                {
                    MyTransparentGeometry.StartParticleProfilingBlock("m_preallocatedBillboards.Allocate()");

                    VRageRender.MyBillboard billboard = MyTransparentGeometry.AddBillboardParticle(particle, effectBillboard, !UseLayerSorting);
                    if (billboard != null)
                    {
                        if (!UseLayerSorting)
                        {
                            billboard.Position0.AssertIsValid();
                            billboard.Position1.AssertIsValid();
                            billboard.Position2.AssertIsValid();
                            billboard.Position3.AssertIsValid();

                            m_billboards.Add(billboard);
                        }
                    }
                    MyTransparentGeometry.EndParticleProfilingBlock();
                    if (billboard == null)
                        break;
                }
            }
        }

        public void Draw(List<VRageRender.MyBillboard> collectedBillboards)
        {
            foreach (VRageRender.MyBillboard billboard in m_billboards)
            {
                //MyTransparentGeometry.AddBillboardToSortingList(billboard);
                collectedBillboards.Add(billboard);
                MyParticlesManager.m_ParticlesTotal += billboard.ContainedBillboards.Count;
            }
            m_billboards.Clear();
        }

        //  For sorting generations if needed
        public int CompareTo(object compareToObject)
        {
            MyParticleGeneration compareToGeneration = (MyParticleGeneration)compareToObject;

            if (UseLayerSorting && compareToGeneration.UseLayerSorting)
                return SortLayer.GetValue<int>().CompareTo(compareToGeneration.SortLayer.GetValue<int>());

            return 0;
        }

        #endregion

        #region DebugDraw
        
        public void DebugDraw()
        {
            m_emitter.DebugDraw(m_effect.GetElapsedTime(), m_effect.WorldMatrix);
        }

        #endregion

        public bool IsValid()
        {
            //System.Diagnostics.Debug.Assert(m_effect == null);
            //System.Diagnostics.Debug.Assert(m_particles.Count == 0);
            //System.Diagnostics.Debug.Assert(Birth == null);
            int keys = Life.GetKeysCount();
            for (int i = 0; i < keys; ++i )
            {
                float time, value;
                Life.GetKey(i, out time, out value);
                if (value <= 0)
                {
                    return false;
                }
            }

            foreach (var particle in m_particles)
            {
                if (!particle.IsValid())
                {
                    return false;
                }
            }

            //return MyUtils.IsValid(m_effect.WorldMatrix);
            return true;
        }
    }
}
