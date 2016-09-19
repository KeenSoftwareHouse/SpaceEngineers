#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Animations;
using VRageRender.Messages;
using VRageRender.Utils;

#endregion

namespace VRage.Game
{

    public class MyParticleGPUGeneration : IComparable, IMyParticleGeneration
    {
        #region Static

        static int m_globalCounter;

        #endregion

        #region Members

        static readonly int Version = 2;
        string m_name;

        MyParticleEffect m_effect;

        FastResourceLock ParticlesLock = new FastResourceLock();
        bool m_dirty;
        bool m_positionDirty;
        uint m_renderId = MyRenderProxy.RENDER_ID_UNASSIGNED;

        
        private enum MyGPUGenerationPropertiesEnum
        {
            ArraySize,
            ArrayOffset,
            ArrayModulo,

            Color,
            ColorIntensity,

            Bounciness,

            EmitterSize,
            EmitterSizeMin,

            Direction,
            Velocity,
            VelocityVar,
            DirectionCone,
            DirectionConeVar,

            Acceleration,
            RotationVelocity,

            Radius,

            Life,

            SoftParticleDistanceScale,
        
            StreakMultiplier,

            AnimationFrameTime,

            Enabled,

            ParticlesPerSecond,

            Material,

            OITWeightFactor,

            Streaks,
            Collide,
            SleepState,
            Light,
            VolumetricLight,

            TargetCoverage,

            Gravity,
            Offset,
            RotationVelocityVar,
            ColorVar,
            HueVar,

            NumMembers,
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[(int) MyGPUGenerationPropertiesEnum.NumMembers];
        

        /// <summary>
        /// Public members to easy access
        /// </summary>
        public MyConstPropertyVector3 ArraySize
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.ArraySize]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ArraySize] = value; }
        }

        public MyConstPropertyInt ArrayOffset
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGPUGenerationPropertiesEnum.ArrayOffset]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ArrayOffset] = value; }
        }

        public MyConstPropertyInt ArrayModulo
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGPUGenerationPropertiesEnum.ArrayModulo]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ArrayModulo] = value; }
        }

        public MyAnimatedProperty2DVector4 Color
        {
            get { return (MyAnimatedProperty2DVector4)m_properties[(int)MyGPUGenerationPropertiesEnum.Color]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Color] = value; }
        }

        public MyAnimatedProperty2DFloat ColorIntensity
        {
            get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.ColorIntensity]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ColorIntensity] = value; }
        }

        public MyConstPropertyFloat ColorVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.ColorVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ColorVar] = value; }
        }

        public MyConstPropertyFloat HueVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.HueVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.HueVar] = value; }
        }

        public MyConstPropertyFloat Bounciness
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Bounciness]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Bounciness] = value; }
        }

        public MyAnimatedPropertyVector3 EmitterSize
        {
            get { return (MyAnimatedPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.EmitterSize]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.EmitterSize] = value; }
        }
        public MyAnimatedPropertyFloat EmitterSizeMin
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.EmitterSizeMin]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.EmitterSizeMin] = value; }
        }


        public MyConstPropertyVector3 Offset
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.Offset]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Offset] = value; }
        }

        public MyConstPropertyVector3 Direction
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.Direction]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Direction] = value; }
        }


        public MyAnimatedPropertyFloat Velocity
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Velocity]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Velocity] = value; }
        }

        public MyAnimatedPropertyFloat VelocityVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.VelocityVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.VelocityVar] = value; }
        }

        public MyAnimatedPropertyFloat DirectionCone
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.DirectionCone]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.DirectionCone] = value; }
        }
        public MyAnimatedPropertyFloat DirectionConeVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.DirectionConeVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.DirectionConeVar] = value; }
        }


        public MyConstPropertyVector3 Acceleration
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.Acceleration]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Acceleration] = value; }
        }

        public MyConstPropertyFloat RotationVelocity
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.RotationVelocity]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.RotationVelocity] = value; }
        }

        public MyConstPropertyFloat RotationVelocityVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.RotationVelocityVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.RotationVelocityVar] = value; }
        }

        public MyAnimatedProperty2DFloat Radius
        {
            get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Radius]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Radius] = value; }
        }

        public MyConstPropertyFloat Life
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Life]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Life] = value; }
        }

        public MyConstPropertyFloat SoftParticleDistanceScale
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.SoftParticleDistanceScale]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.SoftParticleDistanceScale] = value; }
        }

        public MyConstPropertyFloat StreakMultiplier
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.StreakMultiplier]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.StreakMultiplier] = value; }
        }

        public MyConstPropertyFloat AnimationFrameTime
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.AnimationFrameTime]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.AnimationFrameTime] = value; }
        }

        public MyConstPropertyBool Enabled
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.Enabled]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Enabled] = value; }
        }

        public MyAnimatedPropertyFloat ParticlesPerSecond
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.ParticlesPerSecond]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.ParticlesPerSecond] = value; }
        }

        public MyConstPropertyTransparentMaterial Material
        {
            get { return (MyConstPropertyTransparentMaterial)m_properties[(int)MyGPUGenerationPropertiesEnum.Material]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Material] = value; }
        }

        public MyConstPropertyBool Streaks
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.Streaks]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Streaks] = value; }
        }

        public MyConstPropertyBool Collide
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.Collide]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Collide] = value; }
        }

        public MyConstPropertyBool SleepState
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.SleepState]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.SleepState] = value; }
        }
           
        public MyConstPropertyBool Light
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.Light]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Light] = value; }
        }

        public MyConstPropertyBool VolumetricLight
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyGPUGenerationPropertiesEnum.VolumetricLight]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.VolumetricLight] = value; }
        }

        public MyConstPropertyFloat OITWeightFactor
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.OITWeightFactor]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.OITWeightFactor] = value; }
        }

        public MyConstPropertyFloat TargetCoverage
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.TargetCoverage]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.TargetCoverage] = value; }
        }

        public MyConstPropertyFloat Gravity
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Gravity]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Gravity] = value; }
        } 
            

        //////////////////////////////

        #endregion

        #region Constructor & Init

        public MyParticleGPUGeneration()
        {            
        }

        
        public void Init()
        {
            System.Diagnostics.Debug.Assert(Life == null);

            AddProperty(MyGPUGenerationPropertiesEnum.ArraySize, new MyConstPropertyVector3("Array size"));
            AddProperty(MyGPUGenerationPropertiesEnum.ArrayOffset, new MyConstPropertyInt("Array offset"));
            AddProperty(MyGPUGenerationPropertiesEnum.ArrayModulo, new MyConstPropertyInt("Array modulo"));

            AddProperty(MyGPUGenerationPropertiesEnum.Color, new MyAnimatedProperty2DVector4("Color"));
            AddProperty(MyGPUGenerationPropertiesEnum.ColorIntensity, new MyAnimatedProperty2DFloat("Color intensity"));
            AddProperty(MyGPUGenerationPropertiesEnum.ColorVar, new MyConstPropertyFloat("Color var"));
            AddProperty(MyGPUGenerationPropertiesEnum.HueVar, new MyConstPropertyFloat("Hue var"));

            AddProperty(MyGPUGenerationPropertiesEnum.Bounciness, new MyConstPropertyFloat("Bounciness"));

            AddProperty(MyGPUGenerationPropertiesEnum.EmitterSize, new MyAnimatedPropertyVector3("Emitter size"));
            AddProperty(MyGPUGenerationPropertiesEnum.EmitterSizeMin, new MyAnimatedPropertyFloat("Emitter inner size"));

            AddProperty(MyGPUGenerationPropertiesEnum.Offset, new MyConstPropertyVector3("Offset"));
            AddProperty(MyGPUGenerationPropertiesEnum.Direction, new MyConstPropertyVector3("Direction"));
            AddProperty(MyGPUGenerationPropertiesEnum.Velocity, new MyAnimatedPropertyFloat("Velocity"));
            AddProperty(MyGPUGenerationPropertiesEnum.VelocityVar, new MyAnimatedPropertyFloat("Velocity var"));
            AddProperty(MyGPUGenerationPropertiesEnum.DirectionCone, new MyAnimatedPropertyFloat("Direction cone"));
            AddProperty(MyGPUGenerationPropertiesEnum.DirectionConeVar, new MyAnimatedPropertyFloat("Direction cone var"));

            AddProperty(MyGPUGenerationPropertiesEnum.Acceleration, new MyConstPropertyVector3("Acceleration"));

            AddProperty(MyGPUGenerationPropertiesEnum.RotationVelocity, new MyConstPropertyFloat("Rotation velocity"));
            AddProperty(MyGPUGenerationPropertiesEnum.RotationVelocityVar, new MyConstPropertyFloat("Rotation velocity var"));

            AddProperty(MyGPUGenerationPropertiesEnum.Radius, new MyAnimatedProperty2DFloat("Radius"));

            AddProperty(MyGPUGenerationPropertiesEnum.Life, new MyConstPropertyFloat("Life"));

            AddProperty(MyGPUGenerationPropertiesEnum.SoftParticleDistanceScale, new MyConstPropertyFloat("Soft particle distance scale"));
            
            AddProperty(MyGPUGenerationPropertiesEnum.StreakMultiplier, new MyConstPropertyFloat("Streak multiplier"));

            AddProperty(MyGPUGenerationPropertiesEnum.AnimationFrameTime, new MyConstPropertyFloat("Animation frame time"));

            AddProperty(MyGPUGenerationPropertiesEnum.Enabled, new MyConstPropertyBool("Enabled"));

            AddProperty(MyGPUGenerationPropertiesEnum.ParticlesPerSecond, new MyAnimatedPropertyFloat("Particles per second"));

            AddProperty(MyGPUGenerationPropertiesEnum.Material, new MyConstPropertyTransparentMaterial("Material"));

            AddProperty(MyGPUGenerationPropertiesEnum.OITWeightFactor, new MyConstPropertyFloat("OIT weight factor"));
            AddProperty(MyGPUGenerationPropertiesEnum.TargetCoverage, new MyConstPropertyFloat("Target coverage"));

            AddProperty(MyGPUGenerationPropertiesEnum.Streaks, new MyConstPropertyBool("Streaks"));
            AddProperty(MyGPUGenerationPropertiesEnum.Collide, new MyConstPropertyBool("Collide"));
            AddProperty(MyGPUGenerationPropertiesEnum.SleepState, new MyConstPropertyBool("SleepState"));
            AddProperty(MyGPUGenerationPropertiesEnum.Light, new MyConstPropertyBool("Light"));
            AddProperty(MyGPUGenerationPropertiesEnum.VolumetricLight, new MyConstPropertyBool("VolumetricLight"));

            AddProperty(MyGPUGenerationPropertiesEnum.Gravity, new MyConstPropertyFloat("Gravity"));


            InitDefault();
        }

        public void Done()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                if (m_properties[i] is IMyAnimatedProperty)
                    (m_properties[i] as IMyAnimatedProperty).ClearKeys();
            }

          
            Stop(true);
        }

        public void Start(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(m_effect == null);
            System.Diagnostics.Debug.Assert(Life == null);

            m_effect = effect;
            m_name = "ParticleGeneration GPU";
            m_dirty = true;
      }

        public void Close()
        {
            Stop(false);
        }

        private void Stop(bool instant)
        {
            Clear();

            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }

            m_effect = null;
            
            if (m_renderId != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyRenderProxy.RemoveGPUEmitter(m_renderId, instant);
                m_renderId = MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        public void InitDefault()
        {
            ArraySize.SetValue(Vector3.One);
            ArrayModulo.SetValue(1);

            var colorAnim = new MyAnimatedPropertyVector4();
            colorAnim.AddKey(0, Vector4.One);
            colorAnim.AddKey(0.33f, Vector4.One);
            colorAnim.AddKey(0.66f, Vector4.One);
            colorAnim.AddKey(1, Vector4.One);
            Color.AddKey(0, colorAnim);

            var colorIntensityAnim = new MyAnimatedPropertyFloat();
            colorIntensityAnim.AddKey(0, 1.0f);
            colorIntensityAnim.AddKey(0.33f, 1.0f);
            colorIntensityAnim.AddKey(0.66f, 1.0f);
            colorIntensityAnim.AddKey(1, 1.0f);
            ColorIntensity.AddKey(0, colorIntensityAnim);

            Offset.SetValue(new Vector3(0, 0, 0));
            Direction.SetValue(new Vector3(0, 0, -1));

            var radiusAnim = new MyAnimatedPropertyFloat();
            radiusAnim.AddKey(0, 0.1f);
            radiusAnim.AddKey(0.33f, 0.1f);
            radiusAnim.AddKey(0.66f, 0.1f);
            radiusAnim.AddKey(1, 0.1f);
            Radius.AddKey(0, radiusAnim);

            Life.SetValue(1);

            StreakMultiplier.SetValue(4);
            AnimationFrameTime.SetValue(1);

            Enabled.SetValue(true);

            EmitterSize.AddKey(0, new Vector3(0.0f, 0.0f, 0.0f));
            EmitterSizeMin.AddKey(0, 0.0f);
            DirectionCone.AddKey(0, 0.0f);
            DirectionConeVar.AddKey(0, 0.0f);

            Velocity.AddKey(0, 1.0f);
            VelocityVar.AddKey(0, 0.0f);

            ParticlesPerSecond.AddKey(0, 1000.0f);
            Material.SetValue(MyTransparentMaterials.GetMaterial("WhiteBlock"));

            SoftParticleDistanceScale.SetValue(1);
            Bounciness.SetValue(0.5f);
            ColorVar.SetValue(0);
            HueVar.SetValue(0);

            OITWeightFactor.SetValue(1f);

            TargetCoverage.SetValue(1f);
        }

        #endregion

        #region Member properties

        T AddProperty<T>(MyGPUGenerationPropertiesEnum e, T property) where T : IMyConstProperty
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


        public void Update()
        {
        }

        public bool IsDirty
        {
            get
            {
                bool animatedTimeValues = 
                    ParticlesPerSecond.GetKeysCount() > 1 || 
                    Velocity.GetKeysCount() > 1 || 
                    VelocityVar.GetKeysCount() > 1 || 
                    DirectionCone.GetKeysCount() > 1 ||
                    DirectionConeVar.GetKeysCount() > 1 || 
                    EmitterSize.GetKeysCount() > 1 || 
                    EmitterSizeMin.GetKeysCount() > 1;

                return m_dirty || animatedTimeValues;
            }
        }
        public bool IsPositionDirty { get { return m_positionDirty; } }

        public void SetDirty()
        {
            m_dirty = true;
        }
        public void SetPositionDirty()
        {
            m_positionDirty = true;
        }

        private MatrixD CalculateWorldMatrix()
        {
            Vector3 pos = Offset;
            return MatrixD.CreateTranslation(pos * m_effect.GetEmitterScale()) * GetEffect().WorldMatrix;
        }

        void FillData(ref MyGPUEmitter emitter)
        {
            float time;
            MyAnimatedPropertyVector4 color;
            MyAnimatedPropertyFloat radius;
            MyAnimatedPropertyFloat colorIntensity;

            Color.GetKey(0, out time, out color);
            Radius.GetKey(0, out time, out radius);
            ColorIntensity.GetKey(0, out time, out colorIntensity);

            emitter.GID = m_renderId;
            if (Enabled.GetValue<bool>() && !m_effect.IsEmittingStopped)
            {
                ParticlesPerSecond.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out emitter.ParticlesPerSecond);
                emitter.ParticlesPerSecond *= m_effect.UserBirthMultiplier;
            }
            else emitter.ParticlesPerSecond = 0;

            float intensity;
            color.GetKey(0, out time, out emitter.Data.Color0);
            color.GetKey(1, out emitter.Data.ColorKey1, out emitter.Data.Color1);
            color.GetKey(2, out emitter.Data.ColorKey2, out emitter.Data.Color2);
            color.GetKey(3, out time, out emitter.Data.Color3);

            // unmultiply colors and factor by intensity
            colorIntensity.GetInterpolatedValue<float>(0, out intensity);
            emitter.Data.Color0.X *= intensity;
            emitter.Data.Color0.Y *= intensity;
            emitter.Data.Color0.Z *= intensity;
            emitter.Data.Color0 *= m_effect.UserColorMultiplier;
            colorIntensity.GetInterpolatedValue<float>(emitter.Data.ColorKey1, out intensity);
            emitter.Data.Color1.X *= intensity;
            emitter.Data.Color1.Y *= intensity;
            emitter.Data.Color1.Z *= intensity;
            emitter.Data.Color1 *= m_effect.UserColorMultiplier;
            colorIntensity.GetInterpolatedValue<float>(emitter.Data.ColorKey2, out intensity);
            emitter.Data.Color2.X *= intensity;
            emitter.Data.Color2.Y *= intensity;
            emitter.Data.Color2.Z *= intensity;
            emitter.Data.Color2 *= m_effect.UserColorMultiplier;
            colorIntensity.GetInterpolatedValue<float>(1.0f, out intensity);
            emitter.Data.Color3.X *= intensity;
            emitter.Data.Color3.Y *= intensity;
            emitter.Data.Color3.Z *= intensity;
            emitter.Data.Color3 *= m_effect.UserColorMultiplier;

            emitter.Data.ColorVar = ColorVar;
            if (emitter.Data.ColorVar > 1.0f)
                emitter.Data.ColorVar = 1.0f;
            else if (emitter.Data.ColorVar < 0)
                emitter.Data.ColorVar = 0;
            emitter.Data.HueVar = HueVar;
            if (emitter.Data.HueVar > 1.0f)
                emitter.Data.HueVar = 1.0f;
            else if (emitter.Data.HueVar < 0)
                emitter.Data.HueVar = 0;

            emitter.Data.Bounciness = Bounciness;

            MatrixD mat = CalculateWorldMatrix(); 
            emitter.Data.RotationMatrix = mat;
            emitter.Data.Direction = Direction;
            Velocity.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out emitter.Data.Velocity);
            VelocityVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out emitter.Data.VelocityVar);
            float cone;
            DirectionCone.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out cone);
            emitter.Data.DirectionCone = MathHelper.ToRadians(cone);
            DirectionConeVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out cone);
            emitter.Data.DirectionConeVar = MathHelper.ToRadians(cone);

            emitter.Data.NumParticlesToEmitThisFrame = 0;

            emitter.Data.ParticleLifeSpan = Life;

            radius.GetKey(0, out time, out emitter.Data.ParticleSize0);
            radius.GetKey(1, out emitter.Data.ParticleSizeKeys1, out emitter.Data.ParticleSize1);
            radius.GetKey(2, out emitter.Data.ParticleSizeKeys2, out emitter.Data.ParticleSize2);
            radius.GetKey(3, out time, out emitter.Data.ParticleSize3);
            emitter.Data.ParticleSize0 *= m_effect.UserRadiusMultiplier;
            emitter.Data.ParticleSize1 *= m_effect.UserRadiusMultiplier;
            emitter.Data.ParticleSize2 *= m_effect.UserRadiusMultiplier;
            emitter.Data.ParticleSize3 *= m_effect.UserRadiusMultiplier;

            EmitterSize.GetInterpolatedValue<Vector3>(m_effect.GetElapsedTime(), out emitter.Data.EmitterSize);
            EmitterSizeMin.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out emitter.Data.EmitterSizeMin);
            emitter.Data.RotationVelocity = RotationVelocity;
            emitter.Data.RotationVelocityVar = RotationVelocityVar;

            emitter.Data.Acceleration = Acceleration;
            emitter.Data.Gravity = m_effect.Gravity * Gravity;
            emitter.Data.StreakMultiplier = StreakMultiplier;

            GPUEmitterFlags flags = 0;

            flags |= Streaks ? GPUEmitterFlags.Streaks : 0;
            flags |= Collide ? GPUEmitterFlags.Collide : 0;
            flags |= SleepState ? GPUEmitterFlags.SleepState : 0;
            flags |= Light ? GPUEmitterFlags.Light : 0;
            flags |= VolumetricLight ? GPUEmitterFlags.VolumetricLight : 0;
            flags |= m_effect.IsSimulationPaused || MyParticlesManager.Paused ? GPUEmitterFlags.FreezeSimulate : 0;
            flags |= MyParticlesManager.Paused ? GPUEmitterFlags.FreezeEmit : 0;

            emitter.Data.Flags = flags;

            emitter.Data.SoftParticleDistanceScale = SoftParticleDistanceScale;
            emitter.Data.AnimationFrameTime = AnimationFrameTime;
            emitter.Data.OITWeightFactor = OITWeightFactor;

            emitter.Data.Scale = m_effect.GetEmitterScale();

            emitter.AtlasTexture = (Material.GetValue<MyTransparentMaterial>()).Texture;
            emitter.AtlasDimension = new Vector2I((int)ArraySize.GetValue<Vector3>().X, (int)ArraySize.GetValue<Vector3>().Y);
            emitter.AtlasFrameOffset = ArrayOffset;
            emitter.AtlasFrameModulo = ArrayModulo;
            emitter.WorldPosition = mat.Translation;
        }

        #endregion

        #region Clear & Create

        public int CompareTo(object compareToObject)
        {
            return 0;
        }

        public void Clear()
        {
        }

        public void Deallocate()
        {
            MyParticlesManager.GPUGenerationsPool.Deallocate(this);
        }

   
        public IMyParticleGeneration CreateInstance(MyParticleEffect effect)
        {
            MyParticleGPUGeneration generation;
            MyParticlesManager.GPUGenerationsPool.AllocateOrCreate(out generation);
            
            generation.Start(effect);

            generation.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                generation.m_properties[i] = m_properties[i];
            }

            return generation;
        }

        public IMyParticleGeneration Duplicate(MyParticleEffect effect)
        {
            MyParticleGPUGeneration generation;
            MyParticlesManager.GPUGenerationsPool.AllocateOrCreate(out generation);
            generation.Start(effect);

            generation.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                generation.m_properties[i] = m_properties[i].Duplicate();
            }

            return generation;
        }

        #endregion

        #region Properties

        public MyParticleEffect GetEffect()
        {
            return m_effect;
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public MatrixD EffectMatrix { get; set; }

        public MyParticleEmitter GetEmitter()
        {
            return null;
        }

        public void MergeAABB(ref BoundingBoxD aabb)
        {
        }

        public int GetParticlesCount()
        {
            return 0;
        }

        public float GetBirthRate()
        {
            return 0;
        }


        #endregion

        #region Serialization
        private void ConvertAlphaColors()
        {
            var colorAnimList = Color;
            for (int i = 0; i < colorAnimList.GetKeysCount(); i++)
            {
                MyAnimatedPropertyVector4 colorAnim;
                float time;
                colorAnimList.GetKey(i, out time, out colorAnim);
                IMyAnimatedProperty anim = colorAnim as IMyAnimatedProperty;
                for (int j = 0; j < colorAnim.GetKeysCount(); j++)
                {
                    Vector4 color;
                    colorAnim.GetKey(j, out time, out color);
                    color = color.UnmultiplyColor();
                    color.W = ColorExtensions.ToLinearRGBComponent(color.W);
                    color = color.PremultiplyColor();
                    color = Vector4.Clamp(color, new Vector4(0, 0, 0, 0), new Vector4(1, 1, 1, 1));
                    anim.SetKey(j, time, color);
                }
            }
        }
        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleGeneration");
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Version", Version.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString("GenerationType", "GPU");

            writer.WriteStartElement("Properties");

            foreach (IMyConstProperty property in m_properties)
            {
                writer.WriteStartElement("Property");

                writer.WriteAttributeString("Name", property.Name);

                writer.WriteAttributeString("Type", property.BaseValueType);

                PropertyAnimationType animType = PropertyAnimationType.Const;
                if (property.Animated)
                    animType = property.Is2D ? PropertyAnimationType.Animated2D : PropertyAnimationType.Animated;
                writer.WriteAttributeString("AnimationType", animType.ToString());

                property.Serialize(writer);

                writer.WriteEndElement();//property
            }
            writer.WriteEndElement();//properties

            writer.WriteEndElement(); //ParticleGeneration
        }

        public void DeserializeFromObjectBuilder(ParticleGeneration generation)
        {
            m_name = generation.Name;

            foreach (GenerationProperty property in generation.Properties)
            {
                for (int i = 0; i < m_properties.Length; i++)
                {
                    if (m_properties[i].Name.Equals(property.Name))
                    {
                        m_properties[i].DeserializeFromObjectBuilder(property);
                    }
                }
            }
        }

        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);

            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                if (reader.GetAttribute("name") == null)
                    break;
                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleGeneration
            if (version == 1)
                ConvertAlphaColors();
        }

        #endregion

        #region Draw

        public void PrepareForDraw(ref VRageRender.MyBillboard effectBillboard)
        {
        }

        public void Draw(List<VRageRender.MyBillboard> collectedBillboards)
        {
            if (m_renderId == MyRenderProxy.RENDER_ID_UNASSIGNED)
                m_renderId = MyRenderProxy.CreateGPUEmitter();
            if (IsDirty)
            {
                MyGPUEmitter emitter = new MyGPUEmitter();
                FillData(ref emitter);
                MyParticlesManager.GPUEmitters.Add(emitter);

                m_dirty = false;
            }
            else if (m_effect.IsPositionDirty)
            {
                var transform = new MyGPUEmitterTransformUpdate()
                {
                    GID = m_renderId,
                    Transform = CalculateWorldMatrix()
                };
                MyParticlesManager.GPUEmitterTransforms.Add(transform);

                m_positionDirty = false;
            }
        }

        #endregion

        #region DebugDraw
        
        public void DebugDraw()
        {            
        }

        #endregion

    }
}
