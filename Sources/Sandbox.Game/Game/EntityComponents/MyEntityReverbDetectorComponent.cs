using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    //class used for detection of environment with air - important for realistic sounds
    [MyComponentBuilder(typeof(MyObjectBuilder_EntityReverbDetectorComponent))]
    public class MyEntityReverbDetectorComponent : MyEntityComponentBase
    {
        #region Basic

        public enum ReverbDetectedType
        {
            None,
            Voxel,
            Grid
        }

        //constants
        const float RAYCAST_LENGTH = 25;
        const float INFINITY_PENALTY = 50;
        const float REVERB_THRESHOLD_SMALL = 3;
        const float REVERB_THRESHOLD_MEDIUM = 7;
        const float REVERB_THRESHOLD_LARGE = 12;
        const int REVERB_NO_OBSTACLE_LIMIT = 3;

        //static
        static Vector3[] m_directions = new Vector3[26];
        static bool m_systemInitialized = false;
        static int m_currentReverbPreset = -1;

        //non-static
        float[] m_detectedLengths;
        ReverbDetectedType[] m_detectedObjects;
        MyEntity m_entity = null;
        int m_currentDirectionIndex = 0;
        bool m_componentInitialized = false;
        bool m_sendInformationToAudio = false;

        //properties
        public bool Initialized { get { return m_componentInitialized && m_systemInitialized; } }
        public static string CurrentReverbPreset
        {
            get
            {
                if (m_currentReverbPreset == 1)
                    return "Cave";
                else if(m_currentReverbPreset == 0)
                    return "Ship or station";
                else
                    return "None (reverb is off)";
            }
        }

        //Init
        public void InitComponent(MyEntity entity, bool sendInformationToAudio)
        {
            int index = 0;
            if (!m_systemInitialized)
            {
                //initialization of static variables
                int x, y, z;
                for (x = -1; x <= 1; x++)
                {
                    for (z = -1; z <= 1; z++)
                    {
                        for (y = -1; y <= 1; y++)
                        {
                            if (x != 0 || y != 0 || z != 0)
                            {
                                m_directions[index] = Vector3.Normalize(new Vector3(x, y, z));
                                index++;
                            }
                        }
                    }
                }
                m_systemInitialized = true;
            }

            //initialization of local variables
            m_entity = entity;
            m_detectedLengths = new float[m_directions.Length];
            m_detectedObjects = new ReverbDetectedType[m_directions.Length];
            for (index = 0; index < m_directions.Length; index++)
            {
                m_detectedLengths[index] = -1;
                m_detectedObjects[index] = ReverbDetectedType.None;
            }
            m_sendInformationToAudio = sendInformationToAudio && MyPerGameSettings.UseReverbEffect;
            m_componentInitialized = true;
        }

        #endregion


        #region Update

        public void Update()
        {
            if (Initialized && m_entity != null)
            {
                Vector3 entityPosition = (Vector3)m_entity.PositionComp.WorldAABB.Center;
                Vector3 targetPos = entityPosition + m_directions[m_currentDirectionIndex] * RAYCAST_LENGTH;
                LineD line = new LineD(entityPosition, targetPos);
                MyPhysics.HitInfo? hitInfo = MyPhysics.CastRay(line.From, line.To, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter);

                IMyEntity entity = null;
                Vector3D hitPosition = Vector3D.Zero;
                Vector3 hitNormal = Vector3.Zero;
                if (hitInfo.HasValue)
                {
                    entity = hitInfo.Value.HkHitInfo.GetHitEntity() as MyEntity;
                    hitPosition = hitInfo.Value.Position;
                    hitNormal = hitInfo.Value.HkHitInfo.Normal;
                }

                if (entity != null)
                {
                    float dist = Vector3.Distance(entityPosition, hitPosition);
                    m_detectedLengths[m_currentDirectionIndex] = dist;
                    m_detectedObjects[m_currentDirectionIndex] = (entity is MyCubeGrid || entity is MyCubeBlock) ? ReverbDetectedType.Grid : ReverbDetectedType.Voxel;
                }
                else
                {
                    m_detectedLengths[m_currentDirectionIndex] = -1;
                    m_detectedObjects[m_currentDirectionIndex] = ReverbDetectedType.None;
                }

                m_currentDirectionIndex++;
                if (m_currentDirectionIndex >= m_directions.Length)
                {
                    m_currentDirectionIndex = 0;
                    if (m_sendInformationToAudio)
                    {
                        float average = GetDetectedAverage();
                        int grids = GetDetectedNumberOfObjects(ReverbDetectedType.Grid);
                        int voxels = GetDetectedNumberOfObjects(ReverbDetectedType.Voxel);
                        SetReverb(average, grids, voxels);
                    }
                }
            }
        }

        #endregion


        #region Reverb

        private static void SetReverb(float distance, int grids, int voxels)
        {
            if (MyAudio.Static != null)
            {
                int noObstacles = m_directions.Length - grids - voxels;

                //reverb preset evaluation
                int reverbPreset = -1;
                bool isThereAir = !MySession.Static.Settings.RealisticSound || MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.AtmosphereDetectorComp != null
                    && (MySession.Static.LocalCharacter.AtmosphereDetectorComp.InShipOrStation || MySession.Static.LocalCharacter.AtmosphereDetectorComp.InAtmosphere);
                if (isThereAir && distance <= REVERB_THRESHOLD_LARGE && noObstacles <= REVERB_NO_OBSTACLE_LIMIT)
                {
                    if (voxels > grids)
                        reverbPreset = 1;
                    else
                        reverbPreset = 0;
                }

                //set new reverb preset
                if (reverbPreset != m_currentReverbPreset)
                {
                    m_currentReverbPreset = reverbPreset;
                    if (m_currentReverbPreset <= -1)//normal
                    {
                        MyAudio.Static.ApplyReverb = false;
                        MySessionComponentPlanetAmbientSounds.SetAmbientOn();
                    }
                    else if (m_currentReverbPreset == 0)//ship
                    {
                        MyAudio.Static.ApplyReverb = false;
                        MySessionComponentPlanetAmbientSounds.SetAmbientOff();
                    }
                    else //cave
                    {
                        MyAudio.Static.ApplyReverb = true;
                        MySessionComponentPlanetAmbientSounds.SetAmbientOff();
                    }
                }
            }
        }

        #endregion


        #region OtherMethods

        public float GetDetectedAverage(bool onlyDetected = false)
        {
            float result = 0f;
            int division = 0;
            for (int i = 0; i < m_detectedLengths.Length; i++)
            {
                if (m_detectedLengths[i] >= 0)
                {
                    result += m_detectedLengths[i];
                    division++;
                }
                else if (!onlyDetected)
                {
                    result += INFINITY_PENALTY;
                }
            }
            if (onlyDetected)
            {
                result = division > 0 ? result / division : INFINITY_PENALTY;
            }
            else
                result /= m_detectedLengths.Length;

            return result;
        }

        public int GetDetectedNumberOfObjects(ReverbDetectedType type = ReverbDetectedType.Grid)
        {
            int result = 0;
            for (int i = 0; i < m_detectedObjects.Length; i++)
            {
                if (m_detectedObjects[i] == type)
                    result++;
            }
            return result;
        }

        public override string ComponentTypeDebugString
        {
            get { return "EntityReverbDetector"; }
        }

        #endregion
    }
}