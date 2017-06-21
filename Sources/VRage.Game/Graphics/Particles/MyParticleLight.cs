#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Animations;
using VRageRender.Messages;

#endregion

namespace VRage.Game
{
    public class MyParticleLight
    {
        #region Members

        static readonly int Version = 0;
        string m_name;

        MyParticleEffect m_effect;

        private uint m_renderObjectID = MyRenderProxy.RENDER_ID_UNASSIGNED;
        
        Vector3D m_position;
        Vector4 m_color;
        float m_range;
        float m_intensity;



        private enum MyLightPropertiesEnum
        {
            Position,
            PositionVar,

            Color,
            ColorVar,

            Range,
            RangeVar,

            Intensity,
            IntensityVar,

            Enabled
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[Enum.GetValues(typeof(MyLightPropertiesEnum)).Length];
        

        /// <summary>
        /// Public members to easy access
        /// </summary>
        public MyAnimatedPropertyVector3 Position 
        {
            get { return (MyAnimatedPropertyVector3)m_properties[(int)MyLightPropertiesEnum.Position]; }
            private set { m_properties[(int)MyLightPropertiesEnum.Position] = value; }
        }

        public MyAnimatedPropertyVector3 PositionVar
        {
            get { return (MyAnimatedPropertyVector3)m_properties[(int)MyLightPropertiesEnum.PositionVar]; }
            private set { m_properties[(int)MyLightPropertiesEnum.PositionVar] = value; }
        }

        public MyAnimatedPropertyVector4 Color
        {
            get { return (MyAnimatedPropertyVector4)m_properties[(int)MyLightPropertiesEnum.Color]; }
            private set { m_properties[(int)MyLightPropertiesEnum.Color] = value; }
        }

        public MyAnimatedPropertyFloat ColorVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyLightPropertiesEnum.ColorVar]; }
            private set { m_properties[(int)MyLightPropertiesEnum.ColorVar] = value; }
        }

        public MyAnimatedPropertyFloat Range
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyLightPropertiesEnum.Range]; }
            private set { m_properties[(int)MyLightPropertiesEnum.Range] = value; }
        }

        public MyAnimatedPropertyFloat RangeVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyLightPropertiesEnum.RangeVar]; }
            private set { m_properties[(int)MyLightPropertiesEnum.RangeVar] = value; }
        }

        public MyAnimatedPropertyFloat Intensity
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyLightPropertiesEnum.Intensity]; }
            private set { m_properties[(int)MyLightPropertiesEnum.Intensity] = value; }
        }

        public MyAnimatedPropertyFloat IntensityVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MyLightPropertiesEnum.IntensityVar]; }
            private set { m_properties[(int)MyLightPropertiesEnum.IntensityVar] = value; }
        }

        public MyConstPropertyBool Enabled
        {
            get { return (MyConstPropertyBool)m_properties[(int)MyLightPropertiesEnum.Enabled]; }
            private set { m_properties[(int)MyLightPropertiesEnum.Enabled] = value; }
        }
   
        //////////////////////////////

        #endregion

        #region Constructor & Init

        public MyParticleLight()
        {
        }


        public void Init()
        {
            System.Diagnostics.Debug.Assert(Position == null);

            AddProperty(MyLightPropertiesEnum.Position, new MyAnimatedPropertyVector3("Position"));
            AddProperty(MyLightPropertiesEnum.PositionVar, new MyAnimatedPropertyVector3("Position var"));

            AddProperty(MyLightPropertiesEnum.Color, new MyAnimatedPropertyVector4("Color"));
            AddProperty(MyLightPropertiesEnum.ColorVar, new MyAnimatedPropertyFloat("Color var"));

            AddProperty(MyLightPropertiesEnum.Range, new MyAnimatedPropertyFloat("Range"));
            AddProperty(MyLightPropertiesEnum.RangeVar, new MyAnimatedPropertyFloat("Range var"));

            AddProperty(MyLightPropertiesEnum.Intensity, new MyAnimatedPropertyFloat("Intensity"));
            AddProperty(MyLightPropertiesEnum.IntensityVar, new MyAnimatedPropertyFloat("Intensity var"));

            AddProperty(MyLightPropertiesEnum.Enabled, new MyConstPropertyBool("Enabled"));
            Enabled.SetValue(true);
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
            System.Diagnostics.Debug.Assert(Position == null);

            m_effect = effect;
            m_name = "ParticleLight";
      }

        private void InitLight()
        {
            m_renderObjectID = VRageRender.MyRenderProxy.CreateRenderLight();
        }

        public void Close()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }

            m_effect = null;

            CloseLight();
        }

        private void CloseLight()
        {
            if (m_renderObjectID != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(m_renderObjectID);
                m_renderObjectID = MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        #endregion

        #region Member properties

        T AddProperty<T>(MyLightPropertiesEnum e, T property) where T : IMyConstProperty
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
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleLight-Update");

            bool created = false;

            if (Enabled)
            {
                if (m_renderObjectID == MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    InitLight();
                    created = true;
                }
            }
            else 
            {
                if (m_renderObjectID != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    CloseLight();
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            Vector3 localPosition;
            Position.GetInterpolatedValue(m_effect.GetElapsedTime(), out localPosition);
            Vector3 localPositionVar;
            PositionVar.GetInterpolatedValue(m_effect.GetElapsedTime(), out localPositionVar);
            Vector3 localPositionVarRnd = new Vector3(
                MyUtils.GetRandomFloat(-localPositionVar.X, localPositionVar.X),
                MyUtils.GetRandomFloat(-localPositionVar.Y, localPositionVar.Y),
                MyUtils.GetRandomFloat(-localPositionVar.Z, localPositionVar.Z));

            localPosition += localPositionVarRnd;


            Vector4 color;
            Color.GetInterpolatedValue(m_effect.GetElapsedTime(), out color);
            float colorVar;
            ColorVar.GetInterpolatedValue(m_effect.GetElapsedTime(), out colorVar);
            float colorVarRnd = MyUtils.GetRandomFloat(1 - colorVar, 1 + colorVar);
            color.X = MathHelper.Clamp(color.X * colorVarRnd, 0, 1);
            color.Y = MathHelper.Clamp(color.Y * colorVarRnd, 0, 1);
            color.Z = MathHelper.Clamp(color.Z * colorVarRnd, 0, 1);

            float range;
            Range.GetInterpolatedValue(m_effect.GetElapsedTime(), out range);
            float rangeVar;
            RangeVar.GetInterpolatedValue(m_effect.GetElapsedTime(), out rangeVar);
            float rangeVarRnd = MyUtils.GetRandomFloat(-rangeVar, rangeVar);
            range += rangeVarRnd;

            float intensity;
            Intensity.GetInterpolatedValue(m_effect.GetElapsedTime(), out intensity);
            float intensityVar;
            IntensityVar.GetInterpolatedValue(m_effect.GetElapsedTime(), out intensityVar);
            float intensityRnd = MyUtils.GetRandomFloat(-intensityVar, intensityVar);
            intensity += intensityRnd;
            if (m_effect.IsStopped)
                intensity = 0;

            Vector3D position = Vector3D.Transform(localPosition * m_effect.GetEmitterScale(), m_effect.WorldMatrix);
            if ((m_position != position) ||
                (m_color != color) ||
                (m_range != range) ||
                (m_intensity != intensity) ||
                created)
            {
                m_color = color;
                m_intensity = intensity;
                m_range = range;
                m_position = position;

                MyLightLayout light = new MyLightLayout()
                {
                    Range = m_range * m_effect.GetEmitterScale(),
                    Color = new Vector3(m_color),
                    Falloff = 1,
                    GlossFactor = 1,
                    DiffuseFactor = 1,
                };

                UpdateRenderLightData renderLightData = new UpdateRenderLightData()
                {
                    ID = m_renderObjectID,
                    Position = m_position,
                    Type = LightTypeEnum.PointLight,
                    ParentID = -1,
                    UseInForwardRender = true,
                    SpecularColor = new Vector3(m_color),
                    PointLightOn = true,
                    PointLightIntensity = m_intensity,
                    PointLight = light,
                };
                MyRenderProxy.UpdateRenderLight(ref renderLightData);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region Clear & Create

        public MyParticleLight CreateInstance(MyParticleEffect effect)
        {
            MyParticleLight particleLight;
            MyParticlesManager.LightsPool.AllocateOrCreate(out particleLight);

            particleLight.Start(effect);

            particleLight.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                particleLight.m_properties[i] = m_properties[i];
            }

            return particleLight;
        }

        public void InitDefault()
        {
            Color.AddKey(0, Vector4.One);
            Range.AddKey(0, 2.5f);
            Intensity.AddKey(0, 10.0f);
        }

        public MyParticleLight Duplicate(MyParticleEffect effect)
        {
            MyParticleLight particleLight;
            MyParticlesManager.LightsPool.AllocateOrCreate(out particleLight);
            particleLight.Start(effect);

            particleLight.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                particleLight.m_properties[i] = m_properties[i].Duplicate();
            }

            return particleLight;
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
    
        #endregion

        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleLight");
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Version", Version.ToString(CultureInfo.InvariantCulture));

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

            writer.WriteEndElement();//particle light
        }

        public void DeserializeFromObjectBuilder(ParticleLight light)
        {
            m_name = light.Name;

            foreach (GenerationProperty property in light.Properties)
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

            reader.ReadStartElement(); 

            foreach (IMyConstProperty property in m_properties)
            {
                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleGeneration
        }

        #endregion

        #region DebugDraw
        
        public void DebugDraw()
        {
        }

        #endregion
    }
}
