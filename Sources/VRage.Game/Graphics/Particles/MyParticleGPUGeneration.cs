#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage.Animations;
using VRage.Utils;
using VRageMath;
using VRageRender;


#endregion

namespace VRage.Game
{
    
    public class MyParticleGPUGeneration : IMyParticleGeneration
    {
        #region Members

        static readonly int Version = 1;
        string m_name;

        MyParticleEffect m_effect;

        FastResourceLock ParticlesLock = new FastResourceLock();
        bool m_dirty;
        uint m_renderId;

        
        private enum MyGPUGenerationPropertiesEnum
        {
            ArraySize,
            ArrayOffset,
            ArrayModulo,

            Color,

            Bounciness,

            PositionVar,

            Velocity,
            VelocityVar,

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
            VolumetricLight
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[Enum.GetValues(typeof(MyGPUGenerationPropertiesEnum)).Length];
        

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


        public MyConstPropertyFloat Bounciness
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.Bounciness]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Bounciness] = value; }
        }

        public MyConstPropertyVector3 PositionVar
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.PositionVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.PositionVar] = value; }
        }


        public MyConstPropertyVector3 Velocity
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGPUGenerationPropertiesEnum.Velocity]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.Velocity] = value; }
        }


        public MyConstPropertyFloat VelocityVar
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGPUGenerationPropertiesEnum.VelocityVar]; }
            private set { m_properties[(int)MyGPUGenerationPropertiesEnum.VelocityVar] = value; }
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

        public MyConstPropertyInt ParticlesPerSecond
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGPUGenerationPropertiesEnum.ParticlesPerSecond]; }
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
                 
            AddProperty(MyGPUGenerationPropertiesEnum.Bounciness, new MyConstPropertyFloat("Bounciness"));

            AddProperty(MyGPUGenerationPropertiesEnum.PositionVar, new MyConstPropertyVector3("Position var"));

            AddProperty(MyGPUGenerationPropertiesEnum.Velocity, new MyConstPropertyVector3("Velocity"));
            AddProperty(MyGPUGenerationPropertiesEnum.VelocityVar, new MyConstPropertyFloat("Velocity var"));

            AddProperty(MyGPUGenerationPropertiesEnum.Acceleration, new MyConstPropertyVector3("Acceleration"));

            AddProperty(MyGPUGenerationPropertiesEnum.RotationVelocity, new MyConstPropertyFloat("Rotation velocity"));

            AddProperty(MyGPUGenerationPropertiesEnum.Radius, new MyAnimatedProperty2DFloat("Radius"));

            AddProperty(MyGPUGenerationPropertiesEnum.Life, new MyConstPropertyFloat("Life"));

            AddProperty(MyGPUGenerationPropertiesEnum.SoftParticleDistanceScale, new MyConstPropertyFloat("Soft particle distance scale"));
            
            AddProperty(MyGPUGenerationPropertiesEnum.StreakMultiplier, new MyConstPropertyFloat("Streak multiplier"));

            AddProperty(MyGPUGenerationPropertiesEnum.AnimationFrameTime, new MyConstPropertyFloat("Animation frame time"));

            AddProperty(MyGPUGenerationPropertiesEnum.Enabled, new MyConstPropertyBool("Enabled"));

            AddProperty(MyGPUGenerationPropertiesEnum.ParticlesPerSecond, new MyConstPropertyInt("Particles per second"));

            AddProperty(MyGPUGenerationPropertiesEnum.Material, new MyConstPropertyTransparentMaterial("Material"));
            
            AddProperty(MyGPUGenerationPropertiesEnum.OITWeightFactor, new MyConstPropertyFloat("OIT weight factor"));

            AddProperty(MyGPUGenerationPropertiesEnum.Streaks, new MyConstPropertyBool("Streaks"));
            AddProperty(MyGPUGenerationPropertiesEnum.Collide, new MyConstPropertyBool("Collide"));
            AddProperty(MyGPUGenerationPropertiesEnum.SleepState, new MyConstPropertyBool("SleepState"));
            AddProperty(MyGPUGenerationPropertiesEnum.Light, new MyConstPropertyBool("Light"));
            AddProperty(MyGPUGenerationPropertiesEnum.VolumetricLight, new MyConstPropertyBool("VolumetricLight"));


            InitDefault();
        }

        public void Done()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                if (m_properties[i] is IMyAnimatedProperty)
                    (m_properties[i] as IMyAnimatedProperty).ClearKeys();
            }

          
            Close();
        }

        public void Start(MyParticleEffect effect)
        {
            System.Diagnostics.Debug.Assert(m_effect == null);
            System.Diagnostics.Debug.Assert(Life == null);

            m_effect = effect;
            m_name = "ParticleGeneration GPU";
            m_dirty = true;
            m_renderId = MyRenderProxy.CreateGPUEmitter();
      }

        public void Close()
        {
            Clear();

            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }

            m_effect = null;
            
            if (m_renderId != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyRenderProxy.RemoveRenderObject(m_renderId);
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

            Velocity.SetValue(new Vector3(0,0,-1));

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

            ParticlesPerSecond.SetValue(30000);
            Material.SetValue(MyTransparentMaterials.GetMaterial("WhiteBlock"));

            SoftParticleDistanceScale.SetValue(1);
            Bounciness.SetValue(0.5f);

            OITWeightFactor.SetValue(1f);
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
            if (IsDirty)
            {
                MyGPUEmitter emitter = new MyGPUEmitter();
                FillData(ref emitter);
                MyParticlesManager.GPUEmitters.Add(emitter);

                m_dirty = false;
            }
        }

        public bool IsDirty { get { return m_dirty; } }

        public void SetDirty()
        {
            m_dirty = true;
        }

        void FillData(ref MyGPUEmitter emitter)
        {
            float time;
            MyAnimatedPropertyVector4 color;
            MyAnimatedPropertyFloat radius;

            Color.GetKey(0, out time, out color);
            Radius.GetKey(0, out time, out radius);

            // sparks
            emitter.GID = m_renderId;
            emitter.ParticlesPerSecond = (Enabled.GetValue<bool>() && !m_effect.IsStopped) ? ParticlesPerSecond : 0;
            color.GetKey(0, out time, out emitter.Data.Color0);
            color.GetKey(1, out emitter.Data.ColorKey1, out emitter.Data.Color1);
            color.GetKey(2, out emitter.Data.ColorKey2, out emitter.Data.Color2);
            color.GetKey(3, out time, out emitter.Data.Color3);

            emitter.Data.AlphaKey1 = emitter.Data.ColorKey1;
            emitter.Data.AlphaKey2 = emitter.Data.ColorKey2;

            emitter.Data.Bounciness = Bounciness;

            emitter.Data.Velocity = Velocity;
            emitter.Data.VelocityVariance = VelocityVar;

            emitter.Data.NumParticlesToEmitThisFrame = 0;

            emitter.Data.ParticleLifeSpan = Life;

            radius.GetKey(0, out time, out emitter.Data.Size0);
            radius.GetKey(1, out emitter.Data.SizeKeys1, out emitter.Data.Size1);
            radius.GetKey(2, out emitter.Data.SizeKeys2, out emitter.Data.Size2);
            radius.GetKey(3, out time, out emitter.Data.Size3);

            emitter.Data.PositionVariance = PositionVar;
            emitter.Data.RotationVelocity = RotationVelocity;
            emitter.Data.Acceleration = Acceleration;
            emitter.Data.StreakMultiplier = StreakMultiplier;

            GPUEmitterFlags flags = 0;

            flags |= Streaks ? GPUEmitterFlags.Streaks : 0;
            flags |= Collide ? GPUEmitterFlags.Collide : 0;
            flags |= SleepState ? GPUEmitterFlags.SleepState : 0;
            flags |= Light ? GPUEmitterFlags.Light : 0;
            flags |= VolumetricLight ? GPUEmitterFlags.VolumetricLight : 0;

            emitter.Data.Flags = flags;

            emitter.Data.SoftParticleDistanceScale = SoftParticleDistanceScale;
            emitter.Data.AnimationFrameTime = AnimationFrameTime;
            emitter.Data.OITWeightFactor = OITWeightFactor;

            emitter.AtlasTexture = (Material.GetValue<MyTransparentMaterial>()).Texture;
            emitter.AtlasDimension = new Vector2I((int)ArraySize.GetValue<Vector3>().X, (int)ArraySize.GetValue<Vector3>().Y);
            emitter.AtlasFrameOffset = ArrayOffset;
            emitter.AtlasFrameModulo = ArrayModulo;
            emitter.WorldPosition = m_effect.WorldMatrix.Translation;
        }

        #endregion

        #region Clear & Create

        public void Clear()
        {
        }

        public void Deallocate()
        {
            MyParticlesManager.GPUGenerationsPool.Deallocate(this);
        }

   
        public IMyParticleGeneration CreateInstance(MyParticleEffect effect)
        {
            MyParticleGPUGeneration generation = MyParticlesManager.GPUGenerationsPool.Allocate(true);
            if (generation == null)
                return null;

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
            MyParticleGPUGeneration generation = MyParticlesManager.GPUGenerationsPool.Allocate();
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

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleGPUGeneration");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("version", Version.ToString(CultureInfo.InvariantCulture));

            foreach (IMyConstProperty property in m_properties)
            {
                property.Serialize(writer);
            }

            writer.WriteEndElement(); //ParticleGeneration
        }

        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);

            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleGeneration
        }

        #endregion

        #region Draw

        #endregion

        #region DebugDraw
        
        public void DebugDraw()
        {            
        }

        #endregion

    }
}
