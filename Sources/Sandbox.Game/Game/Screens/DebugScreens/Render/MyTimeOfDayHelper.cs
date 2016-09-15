using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;

namespace Sandbox.Game.Gui
{
    internal static class MyTimeOfDayHelper
    {
        static float timeOfDay = 0f;
        static TimeSpan? OriginalTime = null;

        internal static float TimeOfDay { get { return timeOfDay; } }

        internal static void UpdateTimeOfDay(float time)
        {
            if (MySession.Static != null)
            {
                if (!OriginalTime.HasValue)
                    OriginalTime = World.MySession.Static.ElapsedGameTime;

                // time is in range from 0 to MySession.Static.Settings.SunRotationIntervalMinutes! Do not multiply it again!
                MySession.Static.ElapsedGameTime = OriginalTime.Value.Add(new TimeSpan(0, (int)time, 0));
            }
        }
    }
}
