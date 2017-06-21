#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Animations;

#endregion

namespace VRage.Game
{
    public class MyParticleSound
    {
        #region Members

        static readonly int Version = 0;
        string m_name;

        MyParticleEffect m_effect;

        float m_range;
        float m_volume;
        Vector3 m_position = Vector3.Zero;

        public float CurrentVolume { get { return m_volume; } }
        public float CurrentRange { get { return m_range; } }
        public Vector3 Position { get { return m_position; } }

        public uint ParticleSoundId { get { return m_particleSoundId; } }
        private uint m_particleSoundId = 0;
        private static uint m_particleSoundIdGlobal = 1;
        private bool m_newLoop = false;
        public bool NewLoop
        {
            get { return m_newLoop; }
            set { m_newLoop = value; }
        }

        private enum MySoundPropertiesEnum
        {
            Volume,
            VolumeVar,

            Range,
            RangeVar,

            SoundName,

            Enabled
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[Enum.GetValues(typeof(MySoundPropertiesEnum)).Length];
        

        /// <summary>
        /// Public members to easy access
        /// </summary>
        public MyAnimatedPropertyFloat Range
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MySoundPropertiesEnum.Range]; }
            private set { m_properties[(int)MySoundPropertiesEnum.Range] = value; }
        }

        public MyAnimatedPropertyFloat RangeVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MySoundPropertiesEnum.RangeVar]; }
            private set { m_properties[(int)MySoundPropertiesEnum.RangeVar] = value; }
        }

        public MyAnimatedPropertyFloat Volume
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MySoundPropertiesEnum.Volume]; }
            private set { m_properties[(int)MySoundPropertiesEnum.Volume] = value; }
        }

        public MyAnimatedPropertyFloat VolumeVar
        {
            get { return (MyAnimatedPropertyFloat)m_properties[(int)MySoundPropertiesEnum.VolumeVar]; }
            private set { m_properties[(int)MySoundPropertiesEnum.VolumeVar] = value; }
        }

        public MyConstPropertyString SoundName
        {
            get { return (MyConstPropertyString)m_properties[(int)MySoundPropertiesEnum.SoundName]; }
            private set { m_properties[(int)MySoundPropertiesEnum.SoundName] = value; }
        }

        public MyConstPropertyBool Enabled
        {
            get { return (MyConstPropertyBool)m_properties[(int)MySoundPropertiesEnum.Enabled]; }
            private set { m_properties[(int)MySoundPropertiesEnum.Enabled] = value; }
        }
   
        //////////////////////////////

        #endregion

        #region Constructor & Init

        public MyParticleSound()
        {
        }


        public void Init()
        {
            AddProperty(MySoundPropertiesEnum.Range, new MyAnimatedPropertyFloat("Range"));
            AddProperty(MySoundPropertiesEnum.RangeVar, new MyAnimatedPropertyFloat("Range var"));

            AddProperty(MySoundPropertiesEnum.Volume, new MyAnimatedPropertyFloat("Volume"));
            AddProperty(MySoundPropertiesEnum.VolumeVar, new MyAnimatedPropertyFloat("Volume var"));

            AddProperty(MySoundPropertiesEnum.SoundName, new MyConstPropertyString("Sound name"));

            AddProperty(MySoundPropertiesEnum.Enabled, new MyConstPropertyBool("Enabled"));
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

            m_effect = effect;
            m_name = "ParticleSound";
            m_particleSoundId = m_particleSoundIdGlobal++;
      }

        private void InitSound()
        {
            
        }

        public void Close()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }

            m_effect = null;

            CloseSound();
        }

        private void CloseSound()
        {
            

        }

        #endregion

        #region Member properties

        T AddProperty<T>(MySoundPropertiesEnum e, T property) where T : IMyConstProperty
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

        public void Update(bool newLoop = false)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleSound-Update");
            m_newLoop |= newLoop;

            if (Enabled)
            {
                Range.GetInterpolatedValue(m_effect.GetElapsedTime() / m_effect.Duration, out m_range);
                Volume.GetInterpolatedValue(m_effect.GetElapsedTime() / m_effect.Duration, out m_volume);
                m_position = (Vector3)m_effect.WorldMatrix.Translation;
            }
            if (!Enabled)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region Clear & Create

        public MyParticleSound CreateInstance(MyParticleEffect effect)
        {
            MyParticleSound particleSound;
            MyParticlesManager.SoundsPool.AllocateOrCreate(out particleSound);
            
            particleSound.Start(effect);

            particleSound.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                particleSound.m_properties[i] = m_properties[i];
            }

            return particleSound;
        }

        public void InitDefault()
        {
            Range.AddKey(0, 30f);
            Volume.AddKey(0, 1f);
        }

        public MyParticleSound Duplicate(MyParticleEffect effect)
        {
            MyParticleSound particleSound;
            MyParticlesManager.SoundsPool.AllocateOrCreate(out particleSound);
            particleSound.Start(effect);

            particleSound.Name = Name;

            for (int i = 0; i < m_properties.Length; i++)
            {
                particleSound.m_properties[i] = m_properties[i].Duplicate();
            }

            return particleSound;
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
            writer.WriteStartElement("ParticleSound");
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

            writer.WriteEndElement();//particle sound
        }

        public void DeserializeFromObjectBuilder(ParticleSound sound)
        {
            m_name = sound.Name;

            foreach (GenerationProperty property in sound.Properties)
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
