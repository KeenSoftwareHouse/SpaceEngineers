#region Using
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Linq;
using VRage.Animations;
using VRage.FileSystem;
using VRage.Utils;
using VRage.Win32;

#endregion

namespace VRage.Game
{
    public class MyParticlesLibrary
    {
        static Dictionary<int, MyParticleEffect> m_libraryEffects = new Dictionary<int, MyParticleEffect>();
        static Dictionary<string, MyParticleEffect> m_libraryEffectsString = new Dictionary<string, MyParticleEffect>();
        static readonly int Version = 0;
        static string m_loadedFile;

        public static string LoadedFile
        {
            get { return m_loadedFile; }
        }

        static MyParticlesLibrary()
        {
            MyLog.Default.WriteLine("MyParticlesLibrary.ctor - START");
            InitDefault();
            MyLog.Default.WriteLine("MyParticlesLibrary.ctor - END");
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
            m_libraryEffectsString[effect.Name] = effect;
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
                m_libraryEffectsString.Remove(effect.Name);

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

        public static bool GetParticleEffectsID(string name, out int id)
        {
            MyParticleEffect effect;
            if (m_libraryEffectsString.TryGetValue(name, out effect))
            {
                id = effect.GetID();
                return true;
            }

            id = -1;
            return false;
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

                m_loadedFile = file;
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

                    m_loadedFile = path;
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
            m_libraryEffectsString.Clear();
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
                AddParticleEffect(effect);
            }

            reader.ReadEndElement(); //ParticleEffects

            reader.ReadEndElement(); //root
        }

        #endregion

        static public MyParticleEffect CreateParticleEffect(int id)
        {
            if (m_libraryEffects.ContainsKey(id))
            {
                return m_libraryEffects[id].CreateInstance();
            }
            return null;
        }

        static public void RemoveParticleEffectInstance(MyParticleEffect effect)
        {
            effect.Close(false);
            //if (effect.Enabled)
            if (m_libraryEffects.ContainsKey(effect.GetID()))
            {
                var instances = m_libraryEffects[effect.GetID()].GetInstances();
                if (instances != null)
                {
                    if (instances.Contains(effect))
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
        }

        static public void DebugDraw()
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

        //static public void AddKey(int effectID, int generation, string property, float time, object value)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        AddKey(effect, generation, property, time, value);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            AddKey(instance, generation, property, time, value);
        //        }
        //    }
        //}

        //static public void AddKey(int effectID, int generation, string property, int parentKeyID, float time, object value)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        AddKey(effect, generation, property, parentKeyID, time, value);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            AddKey(instance, generation, property, parentKeyID, time, value);
        //        }
        //    }
        //}

        //private static void AddKey(MyParticleEffect effect, int generationIndex, string propertyName, float time, object value)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty)
        //        {
        //            var propAnim = property as IMyAnimatedProperty;
        //            propAnim.AddKey(time, value);
        //        }
        //        else
        //            property.SetValue(value);
        //    }
        //}

        //private static void AddKey(MyParticleEffect effect, int generationIndex, string propertyName, int parentKeyID, float time, object value)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty2D)
        //        {
        //            var propAnim2D = property as IMyAnimatedProperty2D;
        //            object animValue; float animTime;
        //            propAnim2D.GetKeyByID(parentKeyID, out animTime, out animValue);

        //            if (animValue is IMyAnimatedProperty)
        //            {
        //                var propAnim = animValue as IMyAnimatedProperty;
        //                propAnim.AddKey(time, value);
        //            }
        //            else
        //                System.Diagnostics.Debug.Fail("Unknown key value in 2D property");
        //        }
        //        else
        //            System.Diagnostics.Debug.Fail("Only 2D properties can have parent key");
        //    }
        //}

        //static public void RemoveKey(int effectID, int generation, string property, int keyID)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        RemoveKey(effect, generation, property, keyID);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            RemoveKey(instance, generation, property, keyID);
        //        }
        //    }
        //}

        //static public void RemoveKey(int effectID, int generation, string property, int parentKeyID, int keyID)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        RemoveKey(effect, generation, property, parentKeyID, keyID);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            RemoveKey(instance, generation, property, parentKeyID, keyID);
        //        }
        //    }
        //}

        //private static void RemoveKey(MyParticleEffect effect, int generationIndex, string propertyName, int keyID)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty)
        //        {
        //            var propAnim = property as IMyAnimatedProperty;
        //            propAnim.RemoveKeyByID(keyID);
        //        }
        //    }
        //}

        //private static void RemoveKey(MyParticleEffect effect, int generationIndex, string propertyName, int parentKeyID, int keyID)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty2D)
        //        {
        //            var propAnim2D = property as IMyAnimatedProperty2D;
        //            object animValue; float animTime;
        //            propAnim2D.GetKeyByID(parentKeyID, out animTime, out animValue);

        //            if (animValue is IMyAnimatedProperty)
        //            {
        //                var propAnim = animValue as IMyAnimatedProperty;
        //                propAnim.RemoveKeyByID(keyID);
        //            }
        //            else
        //                System.Diagnostics.Debug.Fail("Unknown key value in 2D property");
        //        }
        //        else
        //            System.Diagnostics.Debug.Fail("Only 2D properties can have parent key");
        //    }
        //}

        //static public void SetKey(int effectID, int generation, string property, int keyID, float time, object value)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        SetKey(effect, generation, property, keyID, time, value);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            SetKey(instance, generation, property, keyID, time, value);
        //        }
        //    }
        //}

        //static public void SetKey(int effectID, int generation, string property, int parentKeyID, int keyID, float time, object value)
        //{
        //    MyParticleEffect effect;
        //    if (m_libraryEffects.TryGetValue(effectID, out effect))
        //    {
        //        SetKey(effect, generation, property, parentKeyID, keyID, time, value);

        //        foreach (var instance in effect.GetInstances())
        //        {
        //            SetKey(instance, generation, property, parentKeyID, keyID, time, value);
        //        }
        //    }
        //}

        //private static void SetKey(MyParticleEffect effect, int generationIndex, string propertyName, int keyID, float time, object value)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty)
        //        {
        //            var propAnim = property as IMyAnimatedProperty;
        //            propAnim.SetKeyByID(keyID, time, value);
        //        }
        //        else
        //            property.SetValue(value);
        //    }
        //}

        //private static void SetKey(MyParticleEffect effect, int generationIndex, string propertyName, int parentKeyID, int keyID, float time, object value)
        //{
        //    IMyParticleGeneration generation = effect.GetGenerations()[generationIndex];

        //    IMyConstProperty property = generation.GetProperties().First(x => x.Name == propertyName);
        //    System.Diagnostics.Debug.Assert(property != null, "Invalid effect instance");

        //    if (property != null)
        //    {
        //        if (property is IMyAnimatedProperty2D)
        //        {
        //            var propAnim2D = property as IMyAnimatedProperty2D;
        //            object animValue; float animTime;
        //            propAnim2D.GetKeyByID(parentKeyID, out animTime, out animValue);

        //            if (animValue is IMyAnimatedProperty)
        //            {
        //                var propAnim = animValue as IMyAnimatedProperty;
        //                propAnim.SetKeyByID(keyID, time, value);
        //            }
        //            else
        //                System.Diagnostics.Debug.Fail("Unknown key value in 2D property");
        //        }
        //        else
        //            System.Diagnostics.Debug.Fail("Only 2D properties can have parent key");
        //    }
        //}


    }
}
