#region Using
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Win32;

#endregion

namespace Sandbox.Graphics.TransparentGeometry.Particles
{
    public class MyParticlesLibrary
    {
        static Dictionary<int, MyParticleEffect> m_libraryEffects = new Dictionary<int, MyParticleEffect>();
        static readonly int Version = 0;

        static MyParticlesLibrary()
        {
            MyLog.Default.WriteLine(string.Format("MyParticlesLibrary.ctor - START"));
            InitDefault();
            MyLog.Default.WriteLine(string.Format("MyParticlesLibrary.ctor - END"));
        }

        public static void InitDefault()
        {
            try
            {
                Deserialize("Particles\\MyParticlesLibrary.mwl");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Loading of particles library failed: " + e.ToString());
            }

            /*
            MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
            effect.Start(666);

            m_libraryEffects.Add(effect.GetID(), effect);
            
            MyParticleGeneration generation = MyParticlesManager.GenerationsPool.Allocate();
            generation.Start(effect);
            generation.Init();
            generation.InitDefault();
            effect.AddGeneration(generation);*/
        }

        public static void AddParticleEffect(MyParticleEffect effect)
        {
            m_libraryEffects.Add(effect.GetID(), effect);
        }

        public static bool EffectExists(int ID)
        {
            return m_libraryEffects.ContainsKey(ID);
        }

        public static MyParticleEffect GetParticleEffect(int particleEffectID)
        {
            return m_libraryEffects[particleEffectID];
        }

        public static void UpdateParticleEffectID(int ID)
        {
            MyParticleEffect effect;
            m_libraryEffects.TryGetValue(ID, out effect);
            if (effect != null)
            {
                m_libraryEffects.Remove(ID);
                m_libraryEffects.Add(effect.GetID(), effect);
            }
        }

        public static void RemoveParticleEffect(int ID)
        {
            MyParticleEffect effect;
            m_libraryEffects.TryGetValue(ID, out effect);
            if (effect != null)
            {
                effect.Close(true);
                MyParticlesManager.EffectsPool.Deallocate(effect);
            }

            m_libraryEffects.Remove(ID);
        }

        public static void RemoveParticleEffect(MyParticleEffect effect)
        {
            RemoveParticleEffect(effect.GetID());
        }

        public static IEnumerable<MyParticleEffect> GetParticleEffects()
        {
            return m_libraryEffects.Values;
        }

        public static IEnumerable<int> GetParticleEffectsIDs()
        {
            return m_libraryEffects.Keys;
        }

        #region Serialization

        static public void Serialize(string file)
        {
            using (FileStream fs = File.Create(file))
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                };
                using (XmlWriter writer = XmlWriter.Create(fs, settings))
                {
                    Serialize(writer);
                    writer.Flush();
                }
            }
        }

        static public void Deserialize(string file)
        {
            try
            {
                var path = Path.Combine(MyFileSystem.ContentPath, file);
                using (var fs = MyFileSystem.OpenRead(path))
                {
                    XmlReaderSettings settings = new XmlReaderSettings()
                    {
                        IgnoreWhitespace = true,
                    };
                    using (XmlReader reader = XmlReader.Create(fs, settings))
                    {
                        Deserialize(reader);
                    }
                }
            }
            catch (IOException ex)
            {
                MyLog.Default.WriteLine("ERROR: Failed to load particles library.");
                MyLog.Default.WriteLine(ex);
                WinApi.MessageBox(new IntPtr(), ex.Message, "Loading Error", 0);
                throw;
            }
        }


        static public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("VRageParticleLibrary");

            writer.WriteElementString("Version", Version.ToString(CultureInfo.InvariantCulture));

            writer.WriteStartElement("ParticleEffects");

            foreach (KeyValuePair<int, MyParticleEffect> pair in m_libraryEffects)
            {
                pair.Value.Serialize(writer);
            }

            writer.WriteEndElement(); //ParticleEffects

            writer.WriteEndElement(); //root
        }

        static void Close()
        {
            foreach (MyParticleEffect effect in m_libraryEffects.Values)
            {
                effect.Close(true);
                MyParticlesManager.EffectsPool.Deallocate(effect);
            }
            
            m_libraryEffects.Clear();
        }

        public static int RedundancyDetected = 0;

        static public void Deserialize(XmlReader reader)
        {
            Close();
            RedundancyDetected = 0;

            reader.ReadStartElement(); //VRageParticleLibrary

            int version = reader.ReadElementContentAsInt();

            reader.ReadStartElement(); //ParticleEffects

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
                effect.Deserialize(reader);
                m_libraryEffects.Add(effect.GetID(), effect);
            }

            reader.ReadEndElement(); //ParticleEffects

            reader.ReadEndElement(); //root
        }

        #endregion

        static public MyParticleEffect CreateParticleEffect(int id)
        {
            return m_libraryEffects[id].CreateInstance();
        }

        static public void RemoveParticleEffectInstance(MyParticleEffect effect)
        {
            effect.Close(false);
            //if (effect.Enabled)
            {
                if (m_libraryEffects[effect.GetID()].GetInstances().Contains(effect))
                {
                    MyParticlesManager.EffectsPool.Deallocate(effect);
                    m_libraryEffects[effect.GetID()].RemoveInstance(effect);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Effect deleted twice!");
                }
            }
        }

        static public void DebugDraw()
        {
           // if (AppCode.ExternalEditor.MyEditorBase.IsEditorActive)
            {
                foreach (MyParticleEffect effect in m_libraryEffects.Values)
                {
                    List<MyParticleEffect> instances = effect.GetInstances();
                    if (instances != null)
                    {
                        foreach (MyParticleEffect instance in instances)
                        {
                            instance.DebugDraw();
                        }
                    }
                }
            }
        }

    }
}
