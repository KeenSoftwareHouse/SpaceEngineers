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
    public enum MyVelocityDirEnum
    {
        Default,
        FromEmitterCenter
    }
    public enum MyRotationReference
    {
        Camera,
        Local,        
        Velocity,
        VelocityAndCamera,
        LocalAndCamera
    }
    public enum MyAccelerationReference
    {
        Local,
        Camera,
        Velocity,
        Gravity
    }

    public enum MyGenerationPropertiesEnum
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
        //PivotDistance,
        //PivotDistVar,

        Acceleration,
        AccelerationVar,

        AlphaCutout,
        BirthPerFrame,
        RadiusBySpeed,

        ColorIntensity,

        Pivot,
        PivotVar,
        PivotRotationVar,

        RotationReference,

        ArraySize,
        ArrayIndex,
        ArrayOffset,
        ArrayModulo,

        AccelerationReference,

        SoftParticleDistanceScale,

        VelocityVar
    }
    
    public class MyParticleGeneration : IComparable, IMyParticleGeneration
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

        static string[] MyRotationReferenceStrings =
        {
            "Camera",            
            "Local",            
            "Velocity",
            "Velocity and camera",
            "Local and camera"
        };

        static string[] MyAccelerationReferenceStrings =
        {
            "Local",            
            "Camera",                        
            "Velocity",
            "Gravity"
        };


        static List<string> s_velocityDirStrings = MyVelocityDirStrings.ToList<string>();
        static List<string> s_particleTypeStrings = MyParticleTypeStrings.ToList<string>();
        static List<string> s_rotationReferenceStrings = MyRotationReferenceStrings.ToList<string>();
        static List<string> s_accelerationReferenceStrings = MyAccelerationReferenceStrings.ToList<string>();

        #endregion

        #region Members

        static readonly int Version = 4;
        string m_name;

        MyParticleEffect m_effect;
        MyParticleEmitter m_emitter;
        float m_particlesToCreate = 0;
        float m_birthRate = 0;
        float m_birthPerFrame = 0;
        List<MyAnimatedParticle> m_particles = new List<MyAnimatedParticle>(64);
        private Vector3D? m_lastEffectPosition;

        FastResourceLock ParticlesLock = new FastResourceLock(); 

        BoundingBoxD m_AABB = new BoundingBoxD();

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

        public MyAnimatedPropertyFloat BirthPerFrame
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.BirthPerFrame]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.BirthPerFrame] = value; }
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

        public MyAnimatedPropertyFloat VelocityVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.VelocityVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.VelocityVar] = value; }
        }

        public MyAnimatedPropertyVector3 Angle
        {
            get { return (MyAnimatedPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.Angle]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Angle] = value; }
        }

        public MyConstPropertyVector3 AngleVar
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.AngleVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.AngleVar] = value; }
        }

        public MyAnimatedProperty2DVector3 RotationSpeed
        {
            get { return (MyAnimatedProperty2DVector3)m_properties[(int)MyGenerationPropertiesEnum.RotationSpeed]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.RotationSpeed] = value; }
        }

        public MyConstPropertyVector3 RotationSpeedVar
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.RotationSpeedVar]; }
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

        public MyConstPropertyFloat RadiusBySpeed
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.RadiusBySpeed]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.RadiusBySpeed] = value; }
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

        //public MyAnimatedProperty2DFloat PivotDistance
        //{
        //    get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGenerationPropertiesEnum.PivotDistance]; }
        //    private set { m_properties[(int)MyGenerationPropertiesEnum.PivotDistance] = value; }
        //}

        //public MyConstPropertyFloat PivotDistVar
        //{
        //    get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.PivotDistVar]; }
        //    private set { m_properties[(int)MyGenerationPropertiesEnum.PivotDistVar] = value; }
        //}

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

        public MyAnimatedProperty2DFloat AlphaCutout
        {
            get { return (MyAnimatedProperty2DFloat)m_properties[(int)MyGenerationPropertiesEnum.AlphaCutout]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.AlphaCutout] = value; }
        }

        public MyAnimatedPropertyFloat ColorIntensity
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.ColorIntensity]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ColorIntensity] = value; }
        }

        public MyConstPropertyFloat SoftParticleDistanceScale
        {
            get { return (MyConstPropertyFloat)m_properties[(int)MyGenerationPropertiesEnum.SoftParticleDistanceScale]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.SoftParticleDistanceScale] = value; }
        }

        public MyAnimatedProperty2DVector3 Pivot
        {
            get { return (MyAnimatedProperty2DVector3)m_properties[(int)MyGenerationPropertiesEnum.Pivot]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.Pivot] = value; }
        }

        public MyConstPropertyVector3 PivotVar
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.PivotVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.PivotVar] = value; }
        }

        public MyConstPropertyVector3 PivotRotationVar
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.PivotRotationVar]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.PivotRotationVar] = value; }
        }

        public MyRotationReference RotationReference
        {
            get { return (MyRotationReference)(int)((MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.RotationReference]); }
            set { m_properties[(int)MyGenerationPropertiesEnum.RotationReference].SetValue((int)value); }
        }

        public MyConstPropertyVector3 ArraySize
        {
            get { return (MyConstPropertyVector3)m_properties[(int)MyGenerationPropertiesEnum.ArraySize]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ArraySize] = value; }
        }

        public MyAnimatedProperty2DInt ArrayIndex
        {
            get { return (MyAnimatedProperty2DInt)m_properties[(int)MyGenerationPropertiesEnum.ArrayIndex]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ArrayIndex] = value; }
        }

        public MyConstPropertyInt ArrayOffset
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.ArrayOffset]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ArrayOffset] = value; }
        }

        public MyConstPropertyInt ArrayModulo
        {
            get { return (MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.ArrayModulo]; }
            private set { m_properties[(int)MyGenerationPropertiesEnum.ArrayModulo] = value; }
        }

        public MyAccelerationReference AccelerationReference
        {
            get { return (MyAccelerationReference)(int)((MyConstPropertyInt)m_properties[(int)MyGenerationPropertiesEnum.AccelerationReference]); }
            set { m_properties[(int)MyGenerationPropertiesEnum.RotationReference].SetValue((int)value); }
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
            AddProperty(MyGenerationPropertiesEnum.VelocityVar, new MyAnimatedPropertyFloat("Velocity var"));

            AddProperty(MyGenerationPropertiesEnum.Angle, new MyAnimatedPropertyVector3("Angle"));
            AddProperty(MyGenerationPropertiesEnum.AngleVar, new MyConstPropertyVector3("Angle var")); 

            AddProperty(MyGenerationPropertiesEnum.RotationSpeed, new MyAnimatedProperty2DVector3("Rotation speed"));
            AddProperty(MyGenerationPropertiesEnum.RotationSpeedVar, new MyConstPropertyVector3("Rotation speed var"));

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

            AddProperty(MyGenerationPropertiesEnum.PivotRotation, new MyAnimatedProperty2DVector3("Pivot rotation"));
            //AddProperty(MyGenerationPropertiesEnum.PivotDistance, new MyAnimatedProperty2DFloat("Pivot distance"));
            //AddProperty(MyGenerationPropertiesEnum.PivotDistVar, new MyConstPropertyFloat("Pivot distance var"));

            AddProperty(MyGenerationPropertiesEnum.Acceleration, new MyAnimatedProperty2DVector3("Acceleration"));
            AddProperty(MyGenerationPropertiesEnum.AccelerationVar, new MyConstPropertyFloat("Acceleration var"));

            AddProperty(MyGenerationPropertiesEnum.AlphaCutout, new MyAnimatedProperty2DFloat("Alpha cutout"));

            AddProperty(MyGenerationPropertiesEnum.BirthPerFrame, new MyAnimatedPropertyFloat("Birth per frame"));

            AddProperty(MyGenerationPropertiesEnum.RadiusBySpeed, new MyConstPropertyFloat("Radius by speed"));

            AddProperty(MyGenerationPropertiesEnum.SoftParticleDistanceScale, new MyConstPropertyFloat("Soft particle distance scale"));

            AddProperty(MyGenerationPropertiesEnum.ColorIntensity, new MyAnimatedPropertyFloat("Color intensity"));

            AddProperty(MyGenerationPropertiesEnum.Pivot, new MyAnimatedProperty2DVector3("Pivot"));
            AddProperty(MyGenerationPropertiesEnum.PivotVar, new MyConstPropertyVector3("Pivot var"));
            AddProperty(MyGenerationPropertiesEnum.PivotRotationVar, new MyConstPropertyVector3("Pivot rotation var"));

            AddProperty(MyGenerationPropertiesEnum.RotationReference, new MyConstPropertyEnum("Rotation reference", typeof(MyRotationReference), s_rotationReferenceStrings));

            AddProperty(MyGenerationPropertiesEnum.ArraySize, new MyConstPropertyVector3("Array size"));
            AddProperty(MyGenerationPropertiesEnum.ArrayIndex, new MyAnimatedProperty2DInt("Array index"));
            AddProperty(MyGenerationPropertiesEnum.ArrayOffset, new MyConstPropertyInt("Array offset"));
            AddProperty(MyGenerationPropertiesEnum.ArrayModulo, new MyConstPropertyInt("Array modulo"));

            AddProperty(MyGenerationPropertiesEnum.AccelerationReference, new MyConstPropertyEnum("Acceleration reference", typeof(MyAccelerationReference), s_accelerationReferenceStrings));

            LODBirth.AddKey(0, 1.0f);
            LODRadius.AddKey(0, 1.0f);

            MyAnimatedPropertyVector3 pivotKey = new MyAnimatedPropertyVector3(Pivot.Name);
            pivotKey.AddKey(0, new Vector3(0, 0, 0));
            Pivot.AddKey(0, pivotKey);

            UseLayerSorting.SetValue(false);
            SortLayer.SetValue(-1);

            AccelerationVar.SetValue(0);

            ColorIntensity.AddKey(0, 1.0f);
            SoftParticleDistanceScale.SetValue(1.0f);

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

        public void Deallocate()
        {
            MyParticlesManager.GenerationsPool.Deallocate(this);
        }

        public void InitDefault()
        {
            Birth.AddKey(0, 1.0f);
            Life.AddKey(0, 10.0f);
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
            LODBirth.AddKey(MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT / 2, 1.0f);
            LODBirth.AddKey(MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT, 0f);
            LODRadius.AddKey(0, 1.0f);

            MyAnimatedPropertyVector3 pivotKey = new MyAnimatedPropertyVector3(Pivot.Name);
            pivotKey.AddKey(0, new Vector3(0, 0, 0));
            Pivot.AddKey(0, pivotKey);

            AccelerationVar.SetValue(0.0f);
            SoftParticleDistanceScale.SetValue(1.0f);

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
                inheritedGeneration = GetInheritedGeneration(OnDie.GetValue<int>()) as MyParticleGeneration;

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
                                float thickness;
                                particle.Radius.GetInterpolatedValue(particle.NormalizedTime, out thickness);
                                polyLine.Thickness = thickness;
                                polyLine.Point0 = particle.ActualPosition;
                                polyLine.Point1 = previousParticlePosition;

                                Vector3D direction = polyLine.Point1 - polyLine.Point0;
                                Vector3D normalizedDirection = MyUtils.Normalize(polyLine.Point1 - polyLine.Point0);

                                polyLine.LineDirectionNormalized = normalizedDirection;
                                var camPos = MyTransparentGeometry.Camera.Translation;
                                MyUtils.GetPolyLineQuad(out particle.Quad, ref polyLine, camPos);

                                particle.Quad.Point0 = previousTrail0;// +0.15 * direction;
                                particle.Quad.Point3 = previousTrail1;// +0.15 * direction;
                                previousTrail0 = particle.Quad.Point1;
                                previousTrail1 = particle.Quad.Point2;
                            }
                        }

                        previousParticlePosition = particle.ActualPosition;

                        m_AABB = m_AABB.Include(ref previousParticlePosition);
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

            if (m_effect.CalculateDeltaMatrix == false)
            {
                float motionInheritance;
                MotionInheritance.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out motionInheritance);
                if (motionInheritance > 0)
                    m_effect.CalculateDeltaMatrix = true;
            }

            //particles to create in this update
            if (!m_effect.IsEmittingStopped)
            {
                float lodBirth = 1.0f;

                if (GetEffect().EnableLods && LODBirth.GetKeysCount() > 0)
                {
                    LODBirth.GetInterpolatedValue<float>(GetEffect().Distance, out lodBirth);
                }

                Birth.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out m_birthRate);
                m_birthRate *=
                    VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS
                    * (EnableCustomBirth ? m_effect.UserBirthMultiplier : 1.0f)
                    * lodBirth;

                if (BirthPerFrame.GetKeysCount() > 0)
                {
                    float keyTime, diff;
                    BirthPerFrame.GetNextValue(m_effect.GetElapsedTime() - VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, out m_birthPerFrame, out keyTime, out diff);
                    if (
                        (keyTime >= (m_effect.GetElapsedTime() - VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS)) &&
                        (keyTime < m_effect.GetElapsedTime())
                        )
                    {
                        m_birthPerFrame *= (EnableCustomBirth ? m_effect.UserBirthMultiplier : 1.0f)
                            * lodBirth;
                    }
                    else
                        m_birthPerFrame = 0;
                }

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

                while (m_birthPerFrame >= 1.0f && maxParticles-- > 0)
                {
                    if (m_effect.CalculateDeltaMatrix)
                        CreateParticle(EffectMatrix.Translation);
                    else
                        CreateParticle(m_lastEffectPosition.Value + positionDelta * (int)m_birthPerFrame);

                    m_birthPerFrame -= 1.0f;
                }
                
            }

         //   VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("OnLife");

            if (OnLife.GetValue<int>() != -1)
            {
                MyParticleGeneration inheritedGeneration = GetInheritedGeneration(OnLife.GetValue<int>()) as MyParticleGeneration;

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

        private IMyParticleGeneration GetInheritedGeneration(int generationIndex)
        {
             if (generationIndex >= m_effect.GetGenerations().Count || generationIndex == m_effect.GetGenerations().IndexOf(this))
                 return null;

             return m_effect.GetGenerations()[generationIndex];
        }


        public bool IsDirty { get { return true; } }

        public void SetDirty() { }

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
            m_emitter.CalculateStartPosition(m_effect.GetElapsedTime(), MatrixD.CreateWorld(interpolatedEffectPosition, m_effect.WorldMatrix.Forward, m_effect.WorldMatrix.Up),
                m_effect.GetEmitterAxisScale(), m_effect.GetEmitterScale(), out startOffset, out particle.StartPosition);

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

            if (VelocityVar.GetKeysCount() > 0)
            {
                float velVar;
                VelocityVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out velVar);

                float min = 1 / velVar;
                float max = velVar;

                velVar = MyUtils.GetRandomFloat(min, max);

                vel *= m_effect.GetScale() * velVar;
            }
            else
            {
                vel *= m_effect.GetScale();
            }

            particle.Velocity = vel;

            particle.Velocity = Vector3D.TransformNormal(particle.Velocity, GetEffect().WorldMatrix);

            if (VelocityDir == MyVelocityDirEnum.FromEmitterCenter)
            {
                if (!MyUtils.IsZero(startOffset - particle.StartPosition))
                {
                    float length = particle.Velocity.Length();
                    particle.Velocity = MyUtils.Normalize(particle.StartPosition - (Vector3D)startOffset) * length;
                }
            }

            Angle.GetInterpolatedValue<Vector3>(m_effect.GetElapsedTime(), out particle.Angle);
            Vector3 angleVar = AngleVar;
            if (angleVar.LengthSquared() > 0)
            {
                particle.Angle = new Vector3(
                    MyUtils.GetRandomFloat(particle.Angle.X - angleVar.X, particle.Angle.X + angleVar.X),
                    MyUtils.GetRandomFloat(particle.Angle.Y - angleVar.Y, particle.Angle.Y + angleVar.Y),
                    MyUtils.GetRandomFloat(particle.Angle.Z - angleVar.Z, particle.Angle.Z + angleVar.Z));
            }

            if (RotationSpeed.GetKeysCount() > 0)
            {
                particle.RotationSpeed = new MyAnimatedPropertyVector3(RotationSpeed.Name);
                Vector3 rotationSpeedVar = RotationSpeedVar;
                RotationSpeed.GetInterpolatedKeys(m_effect.GetElapsedTime(), rotationSpeedVar, 1, particle.RotationSpeed);
            }
            else
                particle.RotationSpeed = null;

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
                * m_effect.GetScale(), 
                particle.Radius);
                    
            Thickness.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.Thickness);

            particle.Thickness *= lodRadius;

            float colorVar;
            ColorVar.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out colorVar);
            Color.GetInterpolatedKeys(m_effect.GetElapsedTime(), colorVar, 1.0f, particle.Color);
            ColorIntensity.GetInterpolatedValue<float>(m_effect.GetElapsedTime(), out particle.ColorIntensity);
            particle.SoftParticleDistanceScale = SoftParticleDistanceScale;

            Material.GetInterpolatedKeys(m_effect.GetElapsedTime(), 0, 1.0f, particle.Material);

            particle.Flags = 0;
            particle.Flags |= BlendTextures.GetValue<bool>() ? MyAnimatedParticle.ParticleFlags.BlendTextures : 0;

            if (Pivot.GetKeysCount() > 0)
            {
                particle.Pivot = new MyAnimatedPropertyVector3(Pivot.Name);
                Pivot.GetInterpolatedKeys(m_effect.GetElapsedTime(), (Vector3)PivotVar, 1.0f, particle.Pivot);
            }
            else
                particle.Pivot = null;

            if (PivotRotation.GetKeysCount() > 0)
            {
                particle.PivotRotation = new MyAnimatedPropertyVector3(PivotRotation.Name);
                PivotRotation.GetInterpolatedKeys(m_effect.GetElapsedTime(), (Vector3)PivotRotationVar, 1.0f, particle.PivotRotation);
            }
            else
                particle.PivotRotation = null;

            if (Acceleration.GetKeysCount() > 0)
            {
                particle.Acceleration = new MyAnimatedPropertyVector3(Acceleration.Name);
                float multiplier = MyUtils.GetRandomFloat(1 - AccelerationVar, 1 + AccelerationVar);
                Acceleration.GetInterpolatedKeys(m_effect.GetElapsedTime(), multiplier, particle.Acceleration);
            }
            else
                particle.Acceleration = null;

            if (AlphaCutout.GetKeysCount() > 0)
            {
                particle.AlphaCutout = new MyAnimatedPropertyFloat(AlphaCutout.Name);
                AlphaCutout.GetInterpolatedKeys(m_effect.GetElapsedTime(), 0, 1, particle.AlphaCutout);
            }
            else
                particle.AlphaCutout = null;

            if (ArrayIndex.GetKeysCount() > 0)
            {
                particle.ArrayIndex = new MyAnimatedPropertyInt(ArrayIndex.Name);
                ArrayIndex.GetInterpolatedKeys(m_effect.GetElapsedTime(), 0, 1, particle.ArrayIndex);
            }
            else
                particle.ArrayIndex = null;


            particle.Start(this);
                 
            m_particles.Add(particle);
        }

        public IMyParticleGeneration CreateInstance(MyParticleEffect effect)
        {
            MyParticleGeneration generation;
            MyParticlesManager.GenerationsPool.AllocateOrCreate(out generation);

            generation.Start(effect);

            generation.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                generation.m_properties[i] = m_properties[i];
            }

            generation.m_emitter.CreateInstance(m_emitter);

            return generation;
        }

        public IMyParticleGeneration Duplicate(MyParticleEffect effect)
        {
            MyParticleGeneration generation;
            MyParticlesManager.GenerationsPool.AllocateOrCreate(out generation);
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

        public void MergeAABB(ref BoundingBoxD aabb)
        {
            aabb.Include(ref m_AABB);
        }


        #endregion

        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleGeneration");
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Version", Version.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString("GenerationType", "CPU");

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

            writer.WriteStartElement("Emitter");
            m_emitter.Serialize(writer);
            writer.WriteEndElement();//emitter

            writer.WriteEndElement(); //ParticleGeneration
        }


        public void DeserializeV0(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                if (reader.Name == "Emitter")
                    break; //we added new property which is not in xml yet

                IMyConstProperty dProperty = property;

                if ((dProperty.Name == "Angle") || (dProperty.Name == "Rotation speed"))
                {
                    dProperty = new MyAnimatedPropertyFloat();
                }
                if ((dProperty.Name == "Angle var") || (dProperty.Name == "Rotation speed var"))
                {
                    dProperty = new MyConstPropertyFloat();
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "PivotDistance")
                {
                    var tProperty = new MyAnimatedProperty2DFloat("temp");
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "PivotDistVar")
                {
                    var tProperty = new MyConstPropertyFloat();
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "Pivot distance")
                {
                    var tProperty = new MyAnimatedProperty2DFloat("temp");
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "Pivot distance var")
                {
                    var tProperty = new MyConstPropertyFloat();
                    tProperty.Deserialize(reader);
                }

                dProperty.Deserialize(reader);
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
            float dist;
            float val;
            if (LODBirth.GetKeysCount() > 0)
            {
                LODBirth.GetKey(LODBirth.GetKeysCount() - 1, out dist, out val);
                if (val > 0f)
                {
                    LODBirth.AddKey(Math.Max(dist + MyConstants.MAX_PARTICLE_DISTANCE_EXTENSION, MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT), 0f);
                }
            }

            if ((int)ParticleType != (int)MyParticleTypeEnum.Line)
                Thickness.ClearKeys();

        }

        public void DeserializeV1(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                if (reader.Name == "Emitter")
                    break; //we added new property which is not in xml yet

                IMyConstProperty dProperty = property;

                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "PivotDistance")
                {
                    var tProperty = new MyAnimatedProperty2DFloat("temp");
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "PivotDistVar")
                {
                    var tProperty = new MyConstPropertyFloat();
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "Pivot distance")
                {
                    var tProperty = new MyAnimatedProperty2DFloat("temp");
                    tProperty.Deserialize(reader);
                }
                if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "Pivot distance var")
                {
                    var tProperty = new MyConstPropertyFloat();
                    tProperty.Deserialize(reader);
                }

                dProperty.Deserialize(reader);
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
            float dist;
            float val;
            if (LODBirth.GetKeysCount() > 0)
            {
                LODBirth.GetKey(LODBirth.GetKeysCount() - 1, out dist, out val);
                if (val > 0f)
                {
                    LODBirth.AddKey(Math.Max(dist + MyConstants.MAX_PARTICLE_DISTANCE_EXTENSION, MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT), 0f);
                }
            }

            if ((int)ParticleType != (int)MyParticleTypeEnum.Line)
                Thickness.ClearKeys();

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

            m_emitter.DeserializeFromObjectBuilder(generation.Emitter);
            
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
            float dist;
            float val;
            if (LODBirth.GetKeysCount() > 0)
            {
                LODBirth.GetKey(LODBirth.GetKeysCount() - 1, out dist, out val);
                if (val > 0f)
                {
                    LODBirth.AddKey(Math.Max(dist + MyConstants.MAX_PARTICLE_DISTANCE_EXTENSION, MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT), 0f);
                }
            }
        }

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
                    color.W = Vector4.ToLinearRGBComponent(color.W);
                    color = color.PremultiplyColor();
                    color = Vector4.Clamp(color, new Vector4(0, 0, 0, 0), new Vector4(1, 1, 1, 1));
                    anim.SetKey(j, time, color);
                }
            }
        }
        private void ConvertSRGBColors()
        {
            var colorAnimList = Color;
            for(int i = 0; i < colorAnimList.GetKeysCount(); i++)
            {
                MyAnimatedPropertyVector4 colorAnim;
                float time;
                colorAnimList.GetKey(i, out time, out colorAnim);
                IMyAnimatedProperty anim = colorAnim as IMyAnimatedProperty;
                for(int j = 0; j < colorAnim.GetKeysCount(); j++)
                {
                    Vector4 color;
                    colorAnim.GetKey(j, out time, out color);
                    color = color.UnmultiplyColor().ToLinearRGB().PremultiplyColor();
                    color = Vector4.Clamp(color, new Vector4(0, 0, 0, 0), new Vector4(1, 1, 1, 1));
                    anim.SetKey(j, time, color);
                }
            }
        }
        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);


            if (version == 0)
            {
                DeserializeV0(reader);
                ConvertSRGBColors();
                return;
            }

            if (version == 1)
            {
                DeserializeV1(reader);
                ConvertSRGBColors();
                return;
            }


            reader.ReadStartElement(); //ParticleGeneration

            foreach (IMyConstProperty property in m_properties)
            {
                if (reader.Name == "Emitter")
                    break; //we added new property which is not in xml yet

                property.Deserialize(reader);
                if (property.Name == "Target coverage")
                    property.Name = "Soft particle distance scale";
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
            float dist;
            float val;
            if (LODBirth.GetKeysCount() > 0)
            {
                LODBirth.GetKey(LODBirth.GetKeysCount() - 1, out dist, out val);
                if (val > 0f)
                {
                    LODBirth.AddKey(Math.Max(dist + MyConstants.MAX_PARTICLE_DISTANCE_EXTENSION, MyConstants.MAX_PARTICLE_DISTANCE_DEFAULT), 0f);
                }
            }
            if (version == 2)
                ConvertSRGBColors();
            if (version == 3)
                ConvertAlphaColors();
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
                        billboard.Position0.AssertIsValid();
                        billboard.Position1.AssertIsValid();
                        billboard.Position2.AssertIsValid();
                        billboard.Position3.AssertIsValid();

                        if (!UseLayerSorting)
                        {
                            m_billboards.Add(billboard);
                        }
                    }
                    MyTransparentGeometry.EndParticleProfilingBlock();
                    if (billboard == null)
                        continue;
                }
            }
        }

        public void Draw(List<VRageRender.MyBillboard> collectedBillboards)
        {
            foreach (VRageRender.MyBillboard billboard in m_billboards)
            {
                collectedBillboards.Add(billboard);
            }
            m_billboards.Clear();
        }

        //  For sorting generations if needed
        public int CompareTo(object compareToObject)
        {
            MyParticleGeneration compareToGeneration = compareToObject as MyParticleGeneration;

            if (compareToGeneration == null)
                return 0;

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



    }
}
