using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Game
{
    public enum LastFrameVisibilityEnum
    {
        AlwaysVisible,
        NotVisibleLastFrame,
        VisibleLastFrame
    }

    public class MyParticleEffect
    {
        public event EventHandler OnDelete = null;
        public event EventHandler OnUpdate = null;

        #region Members

        //Version of the effect for serialization
        static readonly int Version = 0;

        int m_particleID; //ID of the particle stored in particles library
        float m_elapsedTime = 0; //Time elapsed from start of the effect
        string m_name; //Name of the effect
        float m_length = 90; //Length of the effect in seconds

        float m_birthRate = 0;
        
        bool m_isStopped = false;
        bool m_isSimulationPaused = false;
        bool m_isEmittingStopped = false;

        bool m_loop = false;
        float m_durationActual = 0f;
        float m_durationMin = 0f;
        float m_durationMax = 0f;

        MatrixD m_worldMatrix = MatrixD.Identity;
        MatrixD m_lastWorldMatrix;
        int m_particlesCount;
        float m_distance;

        readonly List<IMyParticleGeneration> m_generations = new List<IMyParticleGeneration>();
        List<IMyParticleGeneration> m_drawGenerations;
        List<MyParticleEffect> m_instances;
        readonly List<MyParticleLight> m_particleLights = new List<MyParticleLight>();
        readonly List<MyParticleSound> m_particleSounds = new List<MyParticleSound>();

        BoundingBoxD m_AABB = new BoundingBoxD();

        const int GRAVITY_UPDATE_DELAY = 100;
        int m_updateCounter = 0;

        public bool EnableLods;

        private float m_userEmitterScale;
        public float UserEmitterScale
        {
            get { return m_userEmitterScale; }
            set
            {
                m_userEmitterScale = value;
                SetPositionDirty();
            } 
        }

        private float m_userScale;
        public float UserScale
        {
            get { return m_userScale; }
            set
            {
                m_userScale = value;
                SetPositionDirty();
            }
        }
        public Vector3 UserAxisScale;
        


        private float m_userBirthMultiplier;
        public float UserBirthMultiplier
        {
            get { return m_userBirthMultiplier; }
            set
            {
                m_userBirthMultiplier = value;
                SetAnimDirty();
            } 
        }

        private float m_userRadiusMultiplier;
        public float UserRadiusMultiplier
        {
            get { return m_userRadiusMultiplier; }
            set
            {
                m_userRadiusMultiplier = value;
                SetDirty();
            } 
        }
        
        private Vector4 m_userColorMultiplier;
        public Vector4 UserColorMultiplier 
        {
            get { return m_userColorMultiplier; } 
            set
            {
                m_userColorMultiplier = value;
                SetDirty();
            } 
        }

        public bool UserDraw;
        private int m_showOnlyThisGeneration = -1;
        [Browsable(false)]
        public int ShowOnlyThisGeneration { get { return m_showOnlyThisGeneration; } }

        public void SetShowOnlyThisGeneration(IMyParticleGeneration generation)
        {
            SetDirty();
            for (int i=0; i < m_generations.Count;i++)
            {
                if (m_generations[i] == generation)
                {
                    SetShowOnlyThisGeneration(i);
                    return;
                }
            }
            SetShowOnlyThisGeneration(-1);
        }

        public void SetShowOnlyThisGeneration(int generationIndex)
        {
            m_showOnlyThisGeneration = generationIndex;
            for (int i = 0; i < m_generations.Count; i++)
                m_generations[i].Show = (generationIndex < 0 || i == generationIndex);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.SetShowOnlyThisGeneration(generationIndex);
                }
            }
        }

        public bool CalculateDeltaMatrix;
        public MatrixD DeltaMatrix;
        //public LastFrameVisibilityEnum WasVisibleLastFrame = LastFrameVisibilityEnum.AlwaysVisible;
        public uint RenderCounter = 0;
        public Vector3 Velocity;

        private Vector3 m_gravity;
        public Vector3 Gravity
        {
            get { return m_gravity; }
            set
            {
                m_gravity = value;
                SetPositionDirty();
            }
        }

        private bool m_newLoop = false;

        #endregion

        #region Start & Close

        public MyParticleEffect()
        {
            Enabled = true;
        }

        public void Start(int particleID)
        {
            System.Diagnostics.Debug.Assert(m_particlesCount == 0);
            System.Diagnostics.Debug.Assert(m_elapsedTime == 0);

            m_particleID = particleID;
            m_name = "ParticleEffect";

            m_isStopped = false;
            m_isEmittingStopped = false;
            m_isSimulationPaused = false;
            m_distance = 0;

            UserEmitterScale = 1.0f;
            UserBirthMultiplier = 1.0f;
            UserRadiusMultiplier = 1.0f;
            UserScale = 1.0f;
			UserAxisScale = Vector3.One;
            UserColorMultiplier = Vector4.One;
            UserDraw = false;

            Enabled = true;
            EnableLods = true;
            
            //For assigment check
            Velocity = Vector3.Zero;
            WorldMatrix = MatrixD.Identity;
            DeltaMatrix = MatrixD.Identity;
            CalculateDeltaMatrix = false;
            RenderCounter = 0;
            m_updateCounter = 0;
        }

        public void Restart()
        {
            m_elapsedTime = 0;
        }

        public void Close(bool done)
        {
            if (!done && OnDelete != null)
                OnDelete(this, null);

            Clear();

            m_name = "ParticleEffect";

            //lock (m_lock)
            {
                foreach (IMyParticleGeneration generation in m_generations)
                {
                    if (done)
                        generation.Done();
                    else
                        generation.Close();

                    generation.Deallocate();
                }

                m_generations.Clear();

                foreach (MyParticleLight particleLight in m_particleLights)
                {
                    if (done)
                        particleLight.Done();
                    else
                        particleLight.Close();
                    MyParticlesManager.LightsPool.Deallocate(particleLight);
                }

                m_particleLights.Clear();


                foreach (MyParticleSound particleSound in m_particleSounds)
                {
                    if (done)
                        particleSound.Done();
                    else
                        particleSound.Close();
                    MyParticlesManager.SoundsPool.Deallocate(particleSound);
                }

                m_particleSounds.Clear();

                if (m_instances != null)
                {
                    while (m_instances.Count > 0)
                    {
                        MyParticlesManager.RemoveParticleEffect(m_instances[0]);
                    }
                }
            }

            OnDelete = null;
            OnUpdate = null;

            Tag = null;
        }

        public void Clear()
        {
            m_elapsedTime = 0;
            m_birthRate = 0;
            m_particlesCount = 0;

            foreach (IMyParticleGeneration generation in m_generations)
            {
                generation.Clear();
            }

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.Clear();
                }
            }
        }

        public MyParticleEffect CreateInstance()
        {
            MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate(true);
            if (effect != null)
            {
                effect.Start(m_particleID);

                effect.Name = Name;
                effect.Enabled = Enabled;
                effect.Length = Length;
                effect.Loop = m_loop;
                effect.DurationMin = m_durationMin;
                effect.DurationMax = m_durationMax;
                effect.SetRandomDuration();

                foreach (IMyParticleGeneration generation in m_generations)
                {
                    IMyParticleGeneration gen = generation.CreateInstance(effect);
                    if (gen != null)
                    {
                        effect.AddGeneration(gen);
                    }
                }

                foreach (MyParticleLight particleLight in m_particleLights)
                {
                    MyParticleLight pl = particleLight.CreateInstance(effect);
                    if (pl != null)
                    {
                        effect.AddParticleLight(pl);
                    }
                }

                foreach (MyParticleSound particleSound in m_particleSounds)
                {
                    MyParticleSound ps = particleSound.CreateInstance(effect);
                    if (ps != null)
                    {
                        effect.AddParticleSound(ps);
                    }
                }

                if (m_instances == null)
                    m_instances = new List<MyParticleEffect>();

                m_instances.Add(effect);
            }

            return effect;
        }

        /// <summary>
        /// This method stops & deletes effect completely
        /// </summary>
        public void Stop()
        {
            m_isStopped = true;
            m_isEmittingStopped = true;
            SetDirty();
        }

        /// <summary>
        /// This method restores effect
        /// </summary>
        public void Play()
        {
            m_isSimulationPaused = false;
            m_isEmittingStopped = false;
            SetDirty();
        }

        /// <summary>
        /// This methods freezes effect and particles
        /// </summary>
        public void Pause()
        {
            m_isSimulationPaused = true;
            m_isEmittingStopped = true;
            SetDirty();
        }

        /// <summary>
        /// This method stops generating any new particles
        /// </summary>
        public void StopEmitting()
        {
            m_isEmittingStopped = true;
            SetDirty();
        }

        public void SetDirty()
        {
            foreach (IMyParticleGeneration generation in m_generations)
            {
                generation.SetDirty();
            }
        }

        public void SetAnimDirty()
        {
            foreach (IMyParticleGeneration generation in m_generations)
            {
                generation.SetAnimDirty();
            }
        }

        public void SetPositionDirty()
        {
            foreach (IMyParticleGeneration generation in m_generations)
            {
                generation.SetPositionDirty();
            }
            if (m_instances != null)
            {
                foreach (var i in m_instances)
                {
                    i.SetPositionDirty();
                }
            }
        }

        private void SetDirtyInstances()
        {
            foreach (var generation in m_generations)
            {
                generation.SetDirty();
            }

            if (m_instances != null)
            {
                foreach (var i in m_instances)
                {
                    i.SetDirtyInstances();
                }
            }
        }

        public void RemoveInstance(MyParticleEffect effect)
        {
            if (m_instances != null)
            {
                if (m_instances.Contains(effect))
                    m_instances.Remove(effect);
            }
        }

        internal List<MyParticleEffect> GetInstances()
        {
            return m_instances;
        }

        public MyParticleEffect Duplicate()
        {
            MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
            effect.Start(0);

            effect.Name = Name;
            effect.m_length = m_length;
            effect.DurationMin = m_durationMin;
            effect.DurationMax = m_durationMax;
            effect.Loop = m_loop;

            foreach (IMyParticleGeneration generation in m_generations)
            {
                IMyParticleGeneration duplicatedGeneration = generation.Duplicate(effect);
                effect.AddGeneration(duplicatedGeneration);
            }

            foreach (var particleLight in m_particleLights)
            {
                var newParticleLight = (MyParticleLight)particleLight.Duplicate(effect);
                effect.AddParticleLight(newParticleLight);
            }

            foreach (var particleSound in m_particleSounds)
            {
                var newParticleSound = (MyParticleSound)particleSound.Duplicate(effect);
                effect.AddParticleSound(newParticleSound);
            }

            return effect;
        }

        #endregion

        #region Update

        public MatrixD GetDeltaMatrix()
        {
            var lastWorldInv = MatrixD.Invert(m_lastWorldMatrix);
            MatrixD.Multiply(ref lastWorldInv, ref m_worldMatrix, out DeltaMatrix);
            return DeltaMatrix;
        }

        public bool Update()
        {
            if (!Enabled)
                return m_isStopped; //efect is not enabled at all and must be deleted
            if (WorldMatrix == MatrixD.Zero)
                return true;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleEffect-Update");

            System.Diagnostics.Debug.Assert(WorldMatrix != MatrixD.Zero, "Effect world matrix was not set!");

            if (MyParticlesManager.CalculateGravityInPoint != null && m_updateCounter == 0)
                Gravity = MyParticlesManager.CalculateGravityInPoint(WorldMatrix.Translation);

            m_updateCounter++;

            if (m_updateCounter > GRAVITY_UPDATE_DELAY)
            {
                m_updateCounter = 0;
            }

            //m_distance = MySector.MainCamera.GetDistanceWithFOV(WorldMatrix.Translation) / (100.0f); //precalculate for LODs
            m_distance = (float)Vector3D.Distance(MyTransparentGeometry.Camera.Translation, WorldMatrix.Translation) / (100.0f); //precalculate for LODs
            m_particlesCount = 0;
            m_birthRate = 0;
            m_AABB = BoundingBoxD.CreateInvalid();

            if (Velocity != Vector3.Zero)
            {
                var position = m_worldMatrix.Translation;
                position.X += Velocity.X * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                position.Y += Velocity.Y * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                position.Z += Velocity.Z * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                m_worldMatrix = MatrixD.CreateWorld(position, Vector3D.Normalize(Velocity), m_worldMatrix.Up);
            }

            for (int i = 0; i < m_generations.Count;i++ )
            {
                if (m_showOnlyThisGeneration >= 0 && i != m_showOnlyThisGeneration)
                    continue;
                m_generations[i].EffectMatrix = WorldMatrix;
                m_generations[i].Update();

                m_particlesCount += m_generations[i].GetParticlesCount();
                m_birthRate += m_generations[i].GetBirthRate();

                m_generations[i].MergeAABB(ref m_AABB);
            }


            if (!MyParticlesManager.Paused)
            {
                foreach (var particleLight in m_particleLights)
                {
                    particleLight.Update();
                }

                foreach (var particleSound in m_particleSounds)
                {
                    particleSound.Update();
                }
            }

            m_elapsedTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            if (m_loop && m_elapsedTime >= m_durationActual)
            {
                m_elapsedTime = 0;
                SetRandomDuration();
            }


            m_lastWorldMatrix = m_worldMatrix;

            if (OnUpdate != null)
                OnUpdate(this, null);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (m_isStopped)
            {
                // if the effect is stopped, kill it after all particles will die off
                return m_particlesCount == 0;
            }
            else
            {
                // remove particles after set duration time (duration 0 means infinite duration - has to be stopped from code)
                return m_durationActual > 0 && m_elapsedTime > m_durationActual;
            }
        }

        #endregion

        #region Properties


        public bool Enabled { get; set; }

        public int ID { get { return m_particleID; } set { SetID(value); } }

        public float Length 
        { 
            get { return m_length; } 
            set 
            {
                m_length = value;

                if (m_instances != null)
                {
                    foreach (MyParticleEffect effect in m_instances)
                    {
                        effect.Length = value;
                    }
                }
            }
        }

        [Browsable(false)]
        public float Duration { get { return m_durationActual; } }
        public float DurationMin { get { return m_durationMin; } set { SetDurationMin(value); } }
        public float DurationMax { get { return m_durationMax; } set { SetDurationMax(value); } }

        public bool Loop { get { return m_loop; } set { SetLoop(value); } }

        public void SetRandomDuration()
        {
            m_durationActual = m_durationMax > m_durationMin ? MyUtils.GetRandomFloat(m_durationMin, m_durationMax) : m_durationMin;
        }


        void SetDurationMin(float duration)
        {
            m_durationMin = duration;

            SetRandomDuration();

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.SetDurationMin(duration);
                }
            }
        }

        void SetDurationMax(float duration)
        {
            m_durationMax = duration;

            SetRandomDuration();

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.SetDurationMax(duration);
                }
            }
        }

        void SetLoop(bool loop)
        {
            m_loop = loop;

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.SetLoop(loop);
                }
            }
        }

        public float GetScale()
        {
            return UserScale;
        }
        public float GetEmitterScale()
        {
            return UserScale * UserEmitterScale;
        }
        public Vector3 GetEmitterAxisScale()
        {
            return UserAxisScale * UserEmitterScale;
        }

        public float GetElapsedTime()
        {
            return m_elapsedTime;
        }

        public int GetID()
        {
            return m_particleID;
        }

        public int GetParticlesCount()
        {
            return m_particlesCount;
        }

        public void SetID(int id)
        {
            if (m_particleID != id)
            {
                var oldId = m_particleID;
                m_particleID = id;
                MyParticlesLibrary.UpdateParticleEffectID(oldId);                
            }
        }

        public string GetName()
        {
            return m_name;
        }

        public void SetName(string name)
        {
            m_name = name;
        }

        [Browsable(false)]
        public MatrixD WorldMatrix
        {
            get { return m_worldMatrix; }
            set 
            {
                //MyUtils.AssertIsValid(value);

                if (!value.EqualsFast(ref m_worldMatrix, 0.001))
                {
                    SetPositionDirty();
                    m_worldMatrix = value;
                }
            }
        }

        public string Name
        {
            get { return m_name; }
            set { SetName(value); }
        }

        [Browsable(false)]
        public float Distance
        {
            get { return m_distance; }
        }

        [Browsable(false)]
        public object Tag { get; set; }

        #endregion

        #region Generations

        public void AddGeneration(IMyParticleGeneration generation)
        {
            m_generations.Add(generation);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    var gen = generation.CreateInstance(effect);
                    if (gen != null)
                        effect.AddGeneration(gen);
                }
            }
        }


        public void RemoveGeneration(int index)
        {
            IMyParticleGeneration generation = m_generations[index];
            m_generations.Remove(generation);

            generation.Close();
            generation.Deallocate();

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.RemoveGeneration(index);
                }
            }
        }

        public void RemoveGeneration(IMyParticleGeneration generation)
        {
            int index = m_generations.IndexOf(generation);
            RemoveGeneration(index);
        }

        public List<IMyParticleGeneration> GetGenerations()
        {
            return m_generations;
        }      

        [Browsable(false)]
        public bool IsStopped
        {
            get { return m_isStopped; }
        }
        public bool IsSimulationPaused { get { return m_isSimulationPaused; } }
        public bool IsEmittingStopped { get { return m_isEmittingStopped; } }

        public BoundingBoxD GetAABB()
        {
            return m_AABB;
        }


        #endregion

        #region Particle lights

        public void AddParticleLight(MyParticleLight particleLight)
        {
            m_particleLights.Add(particleLight);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.AddParticleLight(particleLight.CreateInstance(effect));
                }
            }
        }

        public void RemoveParticleLight(int index)
        {
            MyParticleLight particleLight = m_particleLights[index];
            m_particleLights.Remove(particleLight);

            particleLight.Close();
            MyParticlesManager.LightsPool.Deallocate(particleLight);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.RemoveParticleLight(index);
                }
            }
        }

        public void RemoveParticleLight(MyParticleLight particleLight)
        {
            int index = m_particleLights.IndexOf(particleLight);
            RemoveParticleLight(index);
        }

        public List<MyParticleLight> GetParticleLights()
        {
            return m_particleLights;
        }

        #endregion

        #region Particle sounds

        public void AddParticleSound(MyParticleSound particleSound)
        {
            m_particleSounds.Add(particleSound);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.AddParticleSound(particleSound.CreateInstance(effect));
                }
            }
        }

        public void RemoveParticleSound(int index)
        {
            MyParticleSound particleSound = m_particleSounds[index];
            m_particleSounds.Remove(particleSound);

            particleSound.Close();
            MyParticlesManager.SoundsPool.Deallocate(particleSound);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.RemoveParticleSound(index);
                }
            }
        }

        public void RemoveParticleSound(MyParticleSound particleSound)
        {
            int index = m_particleSounds.IndexOf(particleSound);
            RemoveParticleSound(index);
        }

        public List<MyParticleSound> GetParticleSounds()
        {
            return m_particleSounds;
        }

        #endregion
        
        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleEffect");

            writer.WriteAttributeString("xsi", "type", null, "MyObjectBuilder_ParticleEffect");

            writer.WriteStartElement("Id");

            writer.WriteElementString("TypeId", "ParticleEffect");
            writer.WriteElementString("SubtypeId", Name);

            writer.WriteEndElement();//Id

            writer.WriteElementString("Version", Version.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("ParticleId", m_particleID.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("Length", m_length.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("DurationMin", m_durationMin.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("DurationMax", m_durationMax.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("Loop", m_loop.ToString(CultureInfo.InvariantCulture).ToLower());

            writer.WriteStartElement("ParticleGenerations");

            foreach (IMyParticleGeneration generation in m_generations)
            {
                generation.Serialize(writer);
            }

            writer.WriteEndElement(); //Generations

            writer.WriteStartElement("ParticleLights");

            foreach (MyParticleLight particleLight in m_particleLights)
            {
                particleLight.Serialize(writer);
            }

            writer.WriteEndElement(); //Particle lights

            writer.WriteStartElement("ParticleSounds");

            foreach (MyParticleSound particleSound in m_particleSounds)
            {
                particleSound.Serialize(writer);
            }

            writer.WriteEndElement(); //Particle sounds

            writer.WriteEndElement(); //ParticleEffect
        }

        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);

            reader.ReadStartElement(); //ParticleEffect
            
            m_particleID = reader.ReadElementContentAsInt();

            m_length = reader.ReadElementContentAsFloat();

            if (reader.Name == "LowRes")
            {
                bool lowres = reader.ReadElementContentAsBoolean();
            }
            if (reader.Name == "Scale")
            {
                float globalScale = reader.ReadElementContentAsFloat();
            }

            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement(); //Generations

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (isEmpty)
                    break;

                if (reader.Name == "ParticleGeneration" && MyParticlesManager.EnableCPUGenerations)
                {
                    MyParticleGeneration generation;
                    MyParticlesManager.GenerationsPool.AllocateOrCreate(out generation);
                    generation.Start(this);
                    generation.Init();

                    generation.Deserialize(reader);

                    AddGeneration(generation);
                }
                else if (reader.Name == "ParticleGPUGeneration")
                {
                    MyParticleGPUGeneration generation;
                    MyParticlesManager.GPUGenerationsPool.AllocateOrCreate(out generation);
                    generation.Start(this);
                    generation.Init();

                    generation.Deserialize(reader);

                    AddGeneration(generation);
                }
                else
                    reader.Read();
            }

            if (!isEmpty)
                reader.ReadEndElement(); //Generations

            if (reader.NodeType != XmlNodeType.EndElement)
            {

                isEmpty = reader.IsEmptyElement;
                if (isEmpty)
                {
                    reader.Read();
                }
                else
                {
                    reader.ReadStartElement(); //Particle lights

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        MyParticleLight particleLight;
                        MyParticlesManager.LightsPool.AllocateOrCreate(out particleLight);
                        particleLight.Start(this);
                        particleLight.Init();

                        particleLight.Deserialize(reader);

                        AddParticleLight(particleLight);
                    }

                    reader.ReadEndElement(); //Particle lights
                }
            }

            if (reader.NodeType != XmlNodeType.EndElement)
            {

                isEmpty = reader.IsEmptyElement;
                if (isEmpty)
                {
                    reader.Read();
                }
                else
                {
                    reader.ReadStartElement(); //Particle sounds

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        MyParticleSound particleSound;
                        MyParticlesManager.SoundsPool.AllocateOrCreate(out particleSound);
                        particleSound.Start(this);
                        particleSound.Init();

                        particleSound.Deserialize(reader);

                        AddParticleSound(particleSound);
                    }

                    reader.ReadEndElement(); //Particle sounds
                }
            }

            reader.ReadEndElement(); //ParticleEffect
        }

        public void DeserializeFromObjectBuilder(MyObjectBuilder_ParticleEffect builder)
        {
            m_name = builder.Id.SubtypeName;
            m_particleID = builder.ParticleId;
            m_length = builder.Length;
            m_loop = builder.Loop;
            m_durationMin = builder.DurationMin;
            m_durationMax = builder.DurationMax;
            SetRandomDuration();

            foreach (ParticleGeneration generation in builder.ParticleGenerations)
            {
                switch (generation.GenerationType)
                {
                    case "CPU":
                        if (MyParticlesManager.EnableCPUGenerations)
                        {
                            MyParticleGeneration genCPU;
                            MyParticlesManager.GenerationsPool.AllocateOrCreate(out genCPU);
                            genCPU.Start(this);
                            genCPU.Init();
                            genCPU.DeserializeFromObjectBuilder(generation);
                            AddGeneration(genCPU);
                        }
                        break;

                    case "GPU":
                        MyParticleGPUGeneration genGPU;
                        MyParticlesManager.GPUGenerationsPool.AllocateOrCreate(out genGPU);
                        genGPU.Start(this);
                        genGPU.Init();
                        genGPU.DeserializeFromObjectBuilder(generation);
                        AddGeneration(genGPU);
                        break;
                }
            }

            foreach (ParticleLight particleLight in builder.ParticleLights)
            {
                MyParticleLight light;
                MyParticlesManager.LightsPool.AllocateOrCreate(out light);
                light.Start(this);
                light.Init();
                light.DeserializeFromObjectBuilder(particleLight);
                AddParticleLight(light);
            }

            foreach (ParticleSound particleSound in builder.ParticleSounds)
            {
                MyParticleSound sound;
                MyParticlesManager.SoundsPool.AllocateOrCreate(out sound);
                sound.Start(this);
                sound.Init();
                sound.DeserializeFromObjectBuilder(particleSound);
                AddParticleSound(sound);
            }
        }

        #endregion

        #region Draw

        public void PrepareForDraw()
        {
            m_drawGenerations = m_generations;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("PrepareForDraw generations");
            foreach (IMyParticleGeneration generation in m_drawGenerations)
            {
                generation.PrepareForDraw();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public void Draw(List<VRageRender.MyBillboard> collectedBillboards)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Draw generations");
            foreach (IMyParticleGeneration generation in m_drawGenerations)
            {
                generation.Draw(collectedBillboards);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region DebugDraw

        public void DebugDraw()
        {
            VRageRender.MyRenderProxy.DebugDrawAxis(WorldMatrix, 1.0f, false);
            //MyDebugDraw.DrawSphereWireframe(WorldMatrix.Translation, 0.1f, Vector3.One, 1.0f);

            foreach (var generation in m_generations)
            {
                if (generation is MyParticleGeneration)
                    (generation as MyParticleGeneration).DebugDraw();
            }

            Color color = !m_isStopped ? Color.White : Color.Red;
            VRageRender.MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation, Name + "(" + GetID().ToString() + ") [" + GetParticlesCount().ToString() + "]", color, 0.8f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            VRageRender.MyRenderProxy.DebugDrawAABB(m_AABB, color);
        }

        #endregion
       
    }


}
