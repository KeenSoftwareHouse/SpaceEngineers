using System.Collections.Generic;
using VRage.Generics;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Rendering;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using Math = System.Math;

namespace VRage.Render11.GeometryStage2.Instancing
{
    struct MyLodStrategyInfo
    {
        public List<float> LodSwitchingDistances;

        public bool IsEmpty
        {
            get { return LodSwitchingDistances == null || LodSwitchingDistances.Count == 0; }
        }

        public void Init(MyLODDescriptor[] lodDescriptors)
        {
            LodSwitchingDistances = new List<float>(lodDescriptors.Length);
            for (int i = 0; i < lodDescriptors.Length; i++)
            {
                LodSwitchingDistances.Add(lodDescriptors[i].Distance);
            }
        }

        public int GetLodsCount()
        {
            if (LodSwitchingDistances == null)
                return 1;
            else
                return LodSwitchingDistances.Count + 1;
        }

        public void ReduceLodsCount(int newLodsCount)
        {
            VRageRender.MyRenderProxy.Assert(LodSwitchingDistances.Count >= newLodsCount - 1);

            // the number of lods needs to be greater than count of distances
            LodSwitchingDistances.SetSize(newLodsCount - 1);
        }
    }

    enum MyTransitionLodType
    {
        // this lod is displayed all the time:
        Current = 0, 

        // if the transition is in progress, this lod is the target transformation lod (Current is the source)
        Transition = 1,
    }

    struct MyLodStrategyCachedResult
    {
        public int LodsCount;
        public int LodNum;
        public MyInstanceLodState State;
        public float StateData;

        public static MyLodStrategyCachedResult Default = new MyLodStrategyCachedResult
        {
            LodNum = 0,
            LodsCount = 1,
            State = MyInstanceLodState.Solid
        };

        public void Clear()
        {
            LodsCount = 0;
        }

        public void Set(int lodsCount, int lodNum, MyInstanceLodState state, float stateData)
        {
            LodsCount = lodsCount;
            LodNum = lodNum;
            State = state;
            StateData = stateData;
        }
    }

    static class MyLodStrategyCachedResultsUtils
    {
        const int ELEMENTS_COUNT = MyPassIdResolver.AllPassesCount * 2;
        public static void InitList(List<MyLodStrategyCachedResult> results)
        {
            results.Clear();
            while (results.Count < ELEMENTS_COUNT)
            {
                results.Add(MyLodStrategyCachedResult.Default);
            }
        }

        public static int GetLodsCount(List<MyLodStrategyCachedResult> results, int passId)
        {
            return results[passId*2].LodsCount;
        }

        public static void UpdateNoneLod(List<MyLodStrategyCachedResult> results, int passId)
        {
            MyLodStrategyCachedResult result = new MyLodStrategyCachedResult();
            result.Clear();
            results[passId * 2] = result;
        }

        public static void UpdateSingleLod(List<MyLodStrategyCachedResult> results, int passId, int currentLod,
            MyInstanceLodState currentState, float currentStateData)
        {
            MyLodStrategyCachedResult result = new MyLodStrategyCachedResult();
            result.Set(1, currentLod, currentState, currentStateData);
            results[passId*2] = result;
        }

        public static void UpdateTwoLods(List<MyLodStrategyCachedResult> results, int passId, int currentLod,
            MyInstanceLodState currentState, float currentStateData, int theOtherLod, MyInstanceLodState theOtherState, float theOtherStateData)
        {
            MyLodStrategyCachedResult result = new MyLodStrategyCachedResult();
            result.Set(2, currentLod, currentState, currentStateData);
            results[passId * 2] = result;
            result.Set(2, theOtherLod, theOtherState, theOtherStateData);
            results[passId*2 + 1] = result;
        }

        public static void GetLod(List<MyLodStrategyCachedResult> results, int passId, int i, out int lod,
            out MyInstanceLodState state, out float stateData)
        {
            MyRenderProxy.Assert(passId < MyPassIdResolver.AllPassesCount);
            MyRenderProxy.Assert(i < 2); // more lods than 2 are not supported
            MyLodStrategyCachedResult result = results[passId*2 + i];
            lod = result.LodNum;
            state = result.State;
            stateData = result.StateData;
        }
    }

    // encapsulate functionality
    struct MyLodStrategyPreprocessor
    {
        public float DistanceMult;

        public MyLodStrategyPreprocessor(float distanceMult)
        {
            DistanceMult = distanceMult;
        }

        public static MyLodStrategyPreprocessor Perform()
        {
            const float REFERENCE_HORIZONTAL_FOV = 70;
            const float REFERENCE_RESOLUTION_HEIGHT = 1080;
            float fovCoefficient = (float)(Math.Tan(MyRender11.Environment.Matrices.FovH / 2) / Math.Tan(MathHelper.ToRadians(REFERENCE_HORIZONTAL_FOV) / 2));
            float resolutionCoefficient = REFERENCE_RESOLUTION_HEIGHT / (float)MyRender11.ViewportResolution.Y;
            return new MyLodStrategyPreprocessor(fovCoefficient * resolutionCoefficient);
        }
    }

    class MyLodStrategy
    {
        const int MISSING_FRAMES_TO_JUMP = 100;
        const float MAX_TRANSITION_TIME_IN_SECS = 1.0f;
        static readonly float[] m_lodTransitionDistances;
        const float MAX_TRANSITION_PER_FRAME = 0.25f;

        //int m_maxLod; // the bending of the system. If MyLodStrategy will be placed to another place than MyInstanceManager, remove it and refactor the class!!!
        int m_currentLod;
        int m_transitionLod;
        float m_transition;
        float m_transitionStartedAtDistance;

        MyInstanceLodState m_explicitState = MyInstanceLodState.Solid;
        float m_explicitStateData;

        List<MyLodStrategyCachedResult> m_cachedResults;

        static MyObjectsPool<List<MyLodStrategyCachedResult>> m_cachedResultsPool = new MyObjectsPool<List<MyLodStrategyCachedResult>>(1);
        static List<int> m_allPassIds;
        
        static MyPassLoddingSetting[] m_loddingSetting = new MyPassLoddingSetting[MyPassIdResolver.AllPassesCount];
        static float m_objectDistanceAdd = 0;
        static float m_objectDistanceMult = 1.0f;

        public static void SetSettings(MyGlobalLoddingSettings globalLodding, 
            MyPassLoddingSetting gbufferLodding,
            MyPassLoddingSetting[] cascadeDepthLoddings,
            MyPassLoddingSetting singleDepthLodding)
        {
            m_objectDistanceAdd = globalLodding.ObjectDistanceAdd;
            m_objectDistanceMult = globalLodding.ObjectDistanceMult;

            for (int i = 0; i < MyPassIdResolver.MaxGBufferPassesCount; i++)
                m_loddingSetting[MyPassIdResolver.DefaultGBufferPassId] = gbufferLodding;

            int cascadeDepthPassesCount = Math.Min(cascadeDepthLoddings.Length, MyPassIdResolver.MaxCascadeDepthPassesCount);
            for (int i = 0; i < cascadeDepthPassesCount; i++)
                m_loddingSetting[MyPassIdResolver.GetCascadeDepthPassId(i)] = cascadeDepthLoddings[i];
            for (int i = cascadeDepthPassesCount; i < MyPassIdResolver.MaxCascadeDepthPassesCount; i++)
                m_loddingSetting[MyPassIdResolver.GetCascadeDepthPassId(i)] = MyPassLoddingSetting.Default;

            for (int i = 0; i < MyPassIdResolver.MaxCascadeDepthPassesCount; i++)
                m_loddingSetting[MyPassIdResolver.GetSingleDepthPassId(i)] = singleDepthLodding;
        }
        
        ulong m_updatedAtFrameId;

        static MyLodStrategy()
        {
            const int MAX_LOD_COUNT = 8;
            const float LOD_TRANSITION_DISTANCE_THRESHOLD = 4;
            const float MIN_TRANSITION_DISTANCE = 4;

            m_lodTransitionDistances = new float[MAX_LOD_COUNT];
            for (int i = 0; i < m_lodTransitionDistances.Length; i++)
            {
                float lodTranstionDistance = Math.Max(LOD_TRANSITION_DISTANCE_THRESHOLD * (float)Math.Pow(2, i), MIN_TRANSITION_DISTANCE);
                m_lodTransitionDistances[i] = lodTranstionDistance;
            }

            m_allPassIds = new List<int>();
            for (int i = 0; i < MyPassIdResolver.AllPassesCount; i++)
                m_allPassIds.Add(i);
        }

        int GetTheBestLodWithHisteresis(MyLodStrategyInfo strategyInfo, float distance, int currentLod)
        {
            for (int i = 0; i < strategyInfo.LodSwitchingDistances.Count; i++)
            {
                float offset = m_lodTransitionDistances[i]/2;
                offset = currentLod > i ? -offset : +offset;

                float lodDistance = strategyInfo.LodSwitchingDistances[i] + offset;
                if (lodDistance > distance)
                    return i;
            }
            return strategyInfo.LodSwitchingDistances.Count;
        }

        int GetTheBestLod(MyLodStrategyInfo strategyInfo, float distance)
        {
            for (int i = 0; i < strategyInfo.LodSwitchingDistances.Count; i++)
            {
                float lodDistance = strategyInfo.LodSwitchingDistances[i];
                if (lodDistance > distance)
                    return i;
            }
            return strategyInfo.LodSwitchingDistances.Count;
        }

        void SmoothTransition(MyLodStrategyInfo strategyInfo, float timeDeltaSeconds, float distance, ref int currentLod, ref int targetLod, ref float transition)
        {
            if (transition == 0) // the transition has not started
            {
                //Check the suitable lod
                targetLod = GetTheBestLodWithHisteresis(strategyInfo, distance, currentLod);

                //Lod is fine, therefore no transition
                if (currentLod == targetLod)
                {
                    targetLod = -1;
                    return;
                }

                //Lod is not found, init transition:
                m_transitionStartedAtDistance = distance;

                //And swap target and current lod
                int swappedLod = targetLod;
                targetLod = currentLod;
                currentLod = swappedLod;
            }

            // The transition should start
            float deltaTransitionTime = timeDeltaSeconds/MAX_TRANSITION_TIME_IN_SECS;
            float deltaTransitionDistance = Math.Abs(m_transitionStartedAtDistance-distance) / m_lodTransitionDistances[Math.Min(currentLod, targetLod)];
            float deltaTransition = Math.Max(deltaTransitionDistance, deltaTransitionTime);
            deltaTransition = Math.Min(deltaTransition, MAX_TRANSITION_PER_FRAME);
            transition = transition + deltaTransition;
            if (transition >= 1.0f)
            {
                transition = 0.0f;
                targetLod = -1;
            }
        }

        void UpdateCachedResults(int maxLod, List<int> activePassIds)
        {
            foreach (var passId in activePassIds)
            {
                MyPassLoddingSetting lodSetting = m_loddingSetting[passId];
                int currentLod = m_currentLod;
                currentLod = Math.Max(lodSetting.MinLod, currentLod + lodSetting.LodShift);
                currentLod = Math.Min(maxLod, currentLod);
                int transitionLod = m_transitionLod;
                transitionLod = Math.Max(lodSetting.MinLod, transitionLod + lodSetting.LodShift);
                transitionLod = Math.Min(maxLod, transitionLod);
                if (passId == 0)
                {
                    if (m_transition == 0 || m_explicitState != MyInstanceLodState.Solid)
                        MyLodStrategyCachedResultsUtils.UpdateSingleLod(m_cachedResults, passId, 
                            currentLod, m_explicitState, m_explicitStateData);
                    else
                        MyLodStrategyCachedResultsUtils.UpdateTwoLods(m_cachedResults, passId,
                            currentLod, MyInstanceLodState.Transition, 1 - m_transition,
                            transitionLod, MyInstanceLodState.Transition, 2 + m_transition); // because of the implementation 
                }
                else
                {
                    if (m_explicitState == MyInstanceLodState.Solid)
                        MyLodStrategyCachedResultsUtils.UpdateSingleLod(m_cachedResults, passId,
                            currentLod, MyInstanceLodState.Solid, 0);
                    else
                        MyLodStrategyCachedResultsUtils.UpdateNoneLod(m_cachedResults, passId);
                }
            }
        }

        public void Init()
        {
            m_currentLod = 0;
            m_transitionLod = -1;
            m_transition = 0;
            m_transitionStartedAtDistance = 0;
            m_explicitState = MyInstanceLodState.Solid;
            m_explicitStateData = 0;
            m_updatedAtFrameId = 0;

            m_cachedResultsPool.AllocateOrCreate(out m_cachedResults);
            MyLodStrategyCachedResultsUtils.InitList(m_cachedResults);
        }

        public void Destroy()
        {
            m_cachedResultsPool.Deallocate(m_cachedResults);
            m_cachedResults = null;
        }

        public void ResolveExplicit(MyLodStrategyInfo strategyInfo, ulong currentFrameId, int lodNum, List<int> activePassIds)
        {
            if (strategyInfo.IsEmpty)
                return;

            if (currentFrameId == m_updatedAtFrameId)
                return;

            int maxLod = strategyInfo.GetLodsCount() - 1;
            m_currentLod = Math.Min(lodNum, maxLod);
            m_transitionLod = -1;
            m_transition = 0;

            UpdateCachedResults(maxLod, activePassIds);
        }

        public void ResolveNoTransition(MyLodStrategyInfo strategyInfo, ulong currentFrameId, Vector3D cameraPos, Vector3D instancePos, List<int> activePassIds, MyLodStrategyPreprocessor preprocessor)
        {
            if (strategyInfo.IsEmpty)
                return;

            float distance = (float)(cameraPos - instancePos).Length();
            distance *= preprocessor.DistanceMult;
            distance += m_objectDistanceAdd;
            distance *= m_objectDistanceMult;

            m_currentLod = GetTheBestLod(strategyInfo, distance);
            m_transitionLod = -1;
            m_transition = 0;

            m_updatedAtFrameId = currentFrameId;
            int maxLod = strategyInfo.GetLodsCount() - 1;

            UpdateCachedResults(maxLod, activePassIds);
        }

        public void ResolveSmoothly(MyLodStrategyInfo strategyInfo, ulong currentFrameId, float timeDeltaSeconds, Vector3D cameraPos, Vector3D instancePos, List<int> activePassIds, MyLodStrategyPreprocessor preprocessor)
        {
            if (strategyInfo.IsEmpty)
                return;

            if (currentFrameId == m_updatedAtFrameId)
                return;

            float distance = (float) (cameraPos - instancePos).Length();
            distance *= preprocessor.DistanceMult;
            distance += m_objectDistanceAdd;
            distance *= m_objectDistanceMult;

            if (MISSING_FRAMES_TO_JUMP < currentFrameId - m_updatedAtFrameId) // the lod was not updated for a long time, we can jump to the lod
            {
                m_currentLod = GetTheBestLod(strategyInfo, distance);
                m_transitionLod = -1;
                m_transition = 0;
            }
            else
                SmoothTransition(strategyInfo, timeDeltaSeconds, distance, ref m_currentLod, ref m_transitionLod, ref m_transition);

            m_updatedAtFrameId = currentFrameId;
            int maxLod = strategyInfo.GetLodsCount() - 1;

            /*m_currentLod = strategyInfo.GetLodsCount() - 1;
            m_transition = 0;*/

            UpdateCachedResults(maxLod, activePassIds);
        }

        public void SetExplicitLodState(MyLodStrategyInfo strategyInfo, MyInstanceLodState state, float stateData)
        {
            m_explicitState = state;
            m_explicitStateData = stateData;
            int maxLod = 0;
            if (!strategyInfo.IsEmpty)
                maxLod = strategyInfo.GetLodsCount() - 1;
            UpdateCachedResults(maxLod, m_allPassIds);
        }

        public int GetLodsCount(int passId)
        {
            return MyLodStrategyCachedResultsUtils.GetLodsCount(m_cachedResults, passId);
        }

        public void GetLod(int passId, int i, out int lodNum, out MyInstanceLodState stateId, out float stateData)
        {
            MyLodStrategyCachedResultsUtils.GetLod(m_cachedResults, passId, i, out lodNum, out stateId, out stateData);
        }
    }
}
