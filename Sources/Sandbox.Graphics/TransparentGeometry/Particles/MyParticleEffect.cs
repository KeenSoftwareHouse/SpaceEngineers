using Sandbox.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Graphics.TransparentGeometry.Particles
{
    public enum LastFrameVisibilityEnum
    {
        AlwaysVisible,
        NotVisibleLastFrame,
        VisibleLastFrame
    }

    public class MyParticleEffect
    {
        public static readonly uint FRAMES_TO_SKIP = 20;

        public event EventHandler OnDelete = null;
        public event EventHandler OnUpdate = null;

        #region Members

        //Version of the effect for serialization
        static readonly int Version = 0;

        int m_particleID; //ID of the particle stored in particles library
        float m_elapsedTime = 0; //Time elapsed from start of the effect
        string m_name; //Name of the effect
        float m_length = 90; //Length of the effect in seconds
        float m_preload; //Time in seconds to preload
        bool m_isPreloading;
        bool m_wasPreloaded;

        float m_birthRate = 0;
        bool m_hasShownSomething = false;
        bool m_isStopped = false;

        MatrixD m_worldMatrix;
        MatrixD m_lastWorldMatrix;
        int m_particlesCount;
        float m_distance;

        List<MyParticleGeneration> m_generations = new List<MyParticleGeneration>();
        List<MyParticleGeneration> m_sortedGenerations = new List<MyParticleGeneration>();
        List<MyParticleEffect> m_instances;
        BoundingBoxD m_AABB = new BoundingBoxD();


        public bool AutoDelete;
        public bool EnableLods;
        public float UserEmitterScale;
        public float UserBirthMultiplier;
        public float UserRadiusMultiplier;
        public float UserScale;
        public Vector4 UserColorMultiplier;
        public bool UserDraw;

        public bool CalculateDeltaMatrix;
        public bool Near;
        public Matrix DeltaMatrix;
        //public LastFrameVisibilityEnum WasVisibleLastFrame = LastFrameVisibilityEnum.AlwaysVisible;
        public uint RenderCounter = 0;
        public Vector3 Velocity;
        
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

            m_isPreloading = false;
            m_wasPreloaded = false;
            m_isStopped = false;
            m_hasShownSomething = false;
            m_distance = 0;

            UserEmitterScale = 1.0f;
            UserBirthMultiplier = 1.0f;
            UserRadiusMultiplier = 1.0f;
            UserScale = 1.0f;
            UserColorMultiplier = Vector4.One;
            UserDraw = false;
            LowRes = false;

            Enabled = true;
            AutoDelete = true;
            EnableLods = true;
            Near = false;

            //For assigment check
            Velocity = Vector3.Zero;
            WorldMatrix = MatrixD.Zero;
            DeltaMatrix = MatrixD.Identity;
            CalculateDeltaMatrix = false;
            RenderCounter = 0;
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

            foreach (MyParticleGeneration generation in m_generations)
            {
                if (done)
                    generation.Done();
                else
                    generation.Close();
                MyParticlesManager.GenerationsPool.Deallocate(generation);
            }

            m_generations.Clear();

            if (m_instances != null)
            {
                while (m_instances.Count > 0)
                {
                    MyParticlesManager.RemoveParticleEffect(m_instances[0]);
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
            m_wasPreloaded = false;
            m_hasShownSomething = false;

            foreach (MyParticleGeneration generation in m_generations)
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
                effect.SetLength(GetLength());
                effect.SetPreload(GetPreload());
                effect.LowRes = LowRes;

                foreach (MyParticleGeneration generation in m_generations)
                {
                    MyParticleGeneration gen = generation.CreateInstance(effect);
                    if (gen != null)
                    {
                        effect.AddGeneration(gen);
                    }
                }

                if (m_instances == null)
                    m_instances = new List<MyParticleEffect>();

                m_instances.Add(effect);
            }

            return effect;
        }

        /// <summary>
        /// This methods stops generating any new particles
        /// </summary>
        public void Stop(bool autodelete = true)
        {
            m_isStopped = true;
            AutoDelete = autodelete ? true : AutoDelete;
        }

        public void RemoveInstance(MyParticleEffect effect)
        {
            if (m_instances != null)
            {
                if (m_instances.Contains(effect))
                    m_instances.Remove(effect);
            }
        }

        public List<MyParticleEffect> GetInstances()
        {
            return m_instances;
        }

        public MyParticleEffect Duplicate()
        {
            MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
            effect.Start(0);

            effect.Name = Name;
            effect.m_preload = m_preload;
            effect.m_length = m_length;

            foreach (MyParticleGeneration generation in m_generations)
            {
                MyParticleGeneration duplicatedGeneration = generation.Duplicate(effect);
                effect.AddGeneration(duplicatedGeneration);
            }

            return effect;
        }

        #endregion

        #region Update

        public MatrixD GetDeltaMatrix()
        {
            DeltaMatrix = MatrixD.Invert(m_lastWorldMatrix) * m_worldMatrix;
            return DeltaMatrix;
        }

        public bool Update()
        {
            if (!Enabled)
                return AutoDelete; //efect is not enabled at all and must be deleted

            System.Diagnostics.Debug.Assert(WorldMatrix != MatrixD.Zero, "Effect world matrix was not set!");

            if (!m_isPreloading && !m_wasPreloaded && m_preload > 0)
            {
                m_isPreloading = true;

                // TODO: Optimize (preload causes lags, depending on preload size, it's from 0 ms to 85 ms)
                while (m_elapsedTime < m_preload)
                {
                    Update();
                }

                m_isPreloading = false;
                m_wasPreloaded = true;
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleEffect-Update");

            if (!m_isPreloading && IsInFrustum)
            {
                MyPerformanceCounter.PerCameraDrawWrite.ParticleEffectsDrawn++;
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ParticleEffect-UpdateGen");

            m_elapsedTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            //m_distance = MySector.MainCamera.GetDistanceWithFOV(WorldMatrix.Translation) / (100.0f); //precalculate for LODs
            m_distance = (float)Vector3D.Distance(MyTransparentGeometry.Camera.Translation, WorldMatrix.Translation) / (100.0f); //precalculate for LODs
            m_particlesCount = 0;
            m_birthRate = 0;
            m_AABB = BoundingBoxD.CreateInvalid();


            //if (CalculateDeltaMatrix)
            //{
                //DeltaMatrix = Matrix.Invert(m_lastWorldMatrix) * m_worldMatrix;
            //}
           

            if (Velocity != Vector3.Zero)
            {
                var position = m_worldMatrix.Translation;
                position.X += Velocity.X * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                position.Y += Velocity.Y * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                position.Z += Velocity.Z * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                m_worldMatrix = MatrixD.CreateWorld(position, Vector3D.Normalize(Velocity), m_worldMatrix.Up);
            }

            //if (RenderCounter == 0 || ((MyRender.RenderCounter - RenderCounter) < FRAMES_TO_SKIP)) //more than FRAMES_TO_SKIP frames consider effect as invisible
            {
                foreach (MyParticleGeneration generation in m_generations)
                {
                    generation.EffectMatrix = WorldMatrix;
                    generation.Update();
                    m_particlesCount += generation.GetParticlesCount();
                    m_birthRate += generation.GetBirthRate();

                    BoundingBoxD bbox = generation.GetAABB();
                    m_AABB = m_AABB.Include(ref bbox);
                }


                if (m_particlesCount > 0)
                    m_hasShownSomething = true;

                //TODO
                IsInFrustum = true; // MySector.MainCamera.IsInFrustum(ref m_AABB);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            m_lastWorldMatrix = m_worldMatrix;

            if (((m_particlesCount == 0 && HasShownSomething())
                || (m_particlesCount == 0 && m_birthRate == 0.0f))
                && AutoDelete && !m_isPreloading)
            {   //Effect was played and has to be deleted
                return true;
            }

            if (!m_isPreloading && OnUpdate != null)
                OnUpdate(this, null);

            return false;
        }

        #endregion

        #region Properties

        public float Preload { get { return m_preload; } set { m_preload = value; } }

        public bool Enabled { get; set; }

        public bool LowRes { get; set; }

        public int ID { get { return m_particleID; } set { m_particleID = value; } }

        public float Length { get { return GetLength(); } set { SetLength(value); } }

        [Browsable(false)]
        public bool IsInFrustum { get; private set; }

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
            m_particleID = id;
        }

        public string GetName()
        {
            return m_name;
        }

        public void SetName(string name)
        {
            m_name = name;
        }

        public float GetLength()
        {
            return m_length;
        }

        public void SetLength(float length)
        {
            m_length = length;
        }

        public bool HasShownSomething()
        {
            return m_hasShownSomething;
        }

        [Browsable(false)]
        public MatrixD WorldMatrix
        {
            get { return m_worldMatrix; }
            set 
            {
                MyUtils.AssertIsValidOrZero(value);
                m_worldMatrix = value;                
            }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public float GetPreload()
        {
            return m_preload;
        }

        public void SetPreload(float preload)
        {
            m_preload = preload;

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.SetPreload(preload);
                }
            }
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

        public void AddGeneration(MyParticleGeneration generation)
        {
            m_generations.Add(generation);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.AddGeneration(generation.CreateInstance(effect));
                }
            }
        }

        public void RemoveGeneration(int index)
        {
            MyParticleGeneration generation = m_generations[index];
            m_generations.Remove(generation);

            generation.Close();
            MyParticlesManager.GenerationsPool.Deallocate(generation);

            if (m_instances != null)
            {
                foreach (MyParticleEffect effect in m_instances)
                {
                    effect.RemoveGeneration(index);
                }
            }
        }

        public void RemoveGeneration(MyParticleGeneration generation)
        {
            int index = m_generations.IndexOf(generation);
            RemoveGeneration(index);
        }

        public List<MyParticleGeneration> GetGenerations()
        {
            return m_generations;
        }

        [Browsable(false)]
        public bool IsStopped
        {
            get { return m_isStopped; }
        }

        public BoundingBoxD GetAABB()
        {
            return m_AABB;
        }
    

        #endregion
        
        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteStartElement("ParticleEffect");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("version", Version.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("ID", m_particleID.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("Length", m_length.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("Preload", m_preload.ToString(CultureInfo.InvariantCulture));

            writer.WriteElementString("LowRes", LowRes.ToString(CultureInfo.InvariantCulture).ToLower());
            
            writer.WriteStartElement("Generations");

            foreach (MyParticleGeneration generation in m_generations)
            {
                generation.Serialize(writer);
            }

            writer.WriteEndElement(); //Generations

            writer.WriteEndElement(); //ParticleEffect
        }

        public void Deserialize(XmlReader reader)
        {
            m_name = reader.GetAttribute("name");
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);

            reader.ReadStartElement(); //ParticleEffect
            
            m_particleID = reader.ReadElementContentAsInt();

            m_length = reader.ReadElementContentAsFloat();

            m_preload = reader.ReadElementContentAsFloat();

            if (reader.Name == "LowRes")
                LowRes = reader.ReadElementContentAsBoolean();

            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement(); //Generations

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                MyParticleGeneration generation = MyParticlesManager.GenerationsPool.Allocate();
                generation.Start(this);
                generation.Init();

                generation.Deserialize(reader);

                AddGeneration(generation);
            }

            if (!isEmpty)
                reader.ReadEndElement(); //Generations

            reader.ReadEndElement(); //ParticleEffect
        }

        #endregion

        #region Draw

        public void PrepareForDraw()
        {
            //if (WasVisibleLastFrame != LastFrameVisibilityEnum.NotVisibleLastFrame)
           // if (RenderCounter == 0 || ((MyRender.RenderCounter - RenderCounter) < FRAMES_TO_SKIP)) //more than FRAMES_TO_SKIP frames consider effect as invisible
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Sort generations");
                m_sortedGenerations.Clear();

                foreach (MyParticleGeneration generation in m_generations)
                {
                    m_sortedGenerations.Add(generation);
                }

                m_sortedGenerations.Sort();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyBillboard effectBillboard = null;

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("PrepareForDraw generations");
                foreach (MyParticleGeneration generation in m_sortedGenerations)
                {
                    generation.PrepareForDraw(ref effectBillboard);
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        public void Draw(List<VRageRender.MyBillboard> collectedBillboards)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Draw generations");
            foreach (MyParticleGeneration generation in m_sortedGenerations)
            {
                generation.Draw(collectedBillboards);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        #region DebugDraw

        public void DebugDraw()
        {
            // TODO: Par
            //MyDebugDraw.DrawAxis(WorldMatrix, 1.0f, 1.0f);
            //MyDebugDraw.DrawSphereWireframe(WorldMatrix.Translation, 0.1f, Vector3.One, 1.0f);

            //foreach (MyParticleGeneration generation in m_generations)
            //{
            //    generation.DebugDraw();
            //}

            //Color color = !m_isStopped ? Color.White : Color.Red;
            //MyDebugDraw.DrawText(WorldMatrix.Translation, new System.Text.StringBuilder(GetID().ToString() + " [" + GetParticlesCount().ToString() + "]") , color, 1.0f);

            //// Vector4 colorV = color.ToVector4();
            //// MyDebugDraw.DrawAABB(ref m_AABB, ref colorV, 1.0f);
        }

        #endregion
    }


}
