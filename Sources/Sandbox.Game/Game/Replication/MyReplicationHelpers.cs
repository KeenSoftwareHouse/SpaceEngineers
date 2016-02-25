using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Replication
{
    public static class MyReplicationHelpers
    {
        /// <summary>
        /// Ramps the priority up or down based on how often it should send updates and when it was last sent.
        /// Returns zero when it was sent recently (not more than 'updateOncePer' before).
        /// </summary>
        /// <param name="priority">Original priority.</param>
        /// <param name="frameCountWithoutSync">Number of frames without sync</param>
        /// <param name="updateOncePer">How often it should be normally updated</param>
        /// <param name="rampAmount">How much to ramp (0.5f means increase priority by 50% for each 'updateOncePer' number of frame late)</param>
        /// <param name="alsoRampDown">Ramps priority also down (returns zero priority when sent less than 'updateOncePer' frames before)</param>
        public static float RampPriority(float priority, int frameCountWithoutSync, float updateOncePer, float rampAmount = 0.5f, bool alsoRampDown = true)
        {
            // Ramp-up priority when without sync for a longer time than it should be
            if (frameCountWithoutSync >= updateOncePer)
            {
                // 0 is on time, 1 is delayed by a regular update time, 2 is delayed by twice the regular update time
                float lateRatio = (frameCountWithoutSync - updateOncePer) / updateOncePer;

                // When object is delayed by more than regular update time, start ramping priority by 50% per regular update time
                // E.g. object should be update once per 4 frame
                // 8 frames without update, priority *= 1.0f (stays same)
                // 12 frames without update, priority *= 1.5f
                // 16 frames without update, priority *= 2.0f
                if (lateRatio > 1)
                {
                    float ramp = (lateRatio - 1) * rampAmount;
                    priority *= ramp;
                }
                return priority;
            }
            else
            {
                return alsoRampDown ? 0 : priority;
            }
        }
    }
}
