using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    static class MyLodUtils
    {
        public const bool LOD_TRANSITION_DISTANCE = true;
        const int MAX_LOD_COUNT = 8;

        const float LodDistanceTransitionThreshold = 4.0f;
        const float LodTransitionTime = 1.0f;

        const float MinTranstionDistance = 4.0f;

        static float[] m_lodTransitionVector; // distance at which transition must end

        static MyLodUtils()
        {
            m_lodTransitionVector = new float[MAX_LOD_COUNT];
            for (int i = 0; i < MAX_LOD_COUNT; i++)
            {
                m_lodTransitionVector[i] = GetLodTransitionBorder(i);
            }
        }

        public static float GetLodTransitionBorder(int lodIndex)
        {
            return Math.Max(LodDistanceTransitionThreshold * (float)Math.Pow(2, lodIndex), MinTranstionDistance);
        }

        public static float GetTransitionDelta(float distanceDelta, float currentState, int lodIndex)
        {
            float state = LOD_TRANSITION_DISTANCE ? (distanceDelta / m_lodTransitionVector[lodIndex]) : 0.0f;
            state = Math.Max(Math.Abs(currentState) + (float)MyRender11.TimeDelta.Seconds / LodTransitionTime, MathHelper.Clamp(state, 0.0f, 1.0f));

            // Transition should be at least 4 frames
            float delta = Math.Abs(Math.Abs(currentState) - state);
            delta = Math.Min(delta, 0.25f);

            return delta;
        }
    }
}
