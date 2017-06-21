using System;
using System.Collections.Generic;
using Sandbox.Engine.Networking;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Plugins;

namespace Sandbox.Game.SessionComponents
{
    /*
     * Derive from this class to create a custom achievent.
     * ------------------------------------------------------------
     * How: 
     *  1:  Use steamworks to create a new "Stat"s and "Achievement".
     *  2:  Derive from this class and use the stats you created
     *      To implement the achievement logic inside respective 
     *      game assembly.
     * ------------------------------------------------------------
     * Keep in mind that once the achivement is achived it no longer
     * needs to be active. So set up the init and callbacks that way.
     */
    public abstract class MySteamAchievementBase
    {
        /// <summary>
        /// Invoked when the achievement is achieved.
        /// Invocation list is cleared afterwards.
        /// </summary>
        public event Action<MySteamAchievementBase> Achieved;

        /// <summary>
        /// Tag that identifies the achievement within the Steam system.
        /// </summary>
        public abstract string AchievementTag { get; }

        /// <summary>
        /// Achievement will stop recieving updates when Achieved.
        /// </summary>
        public bool IsAchieved { get; protected set; }

        /// <summary>
        /// Tells if the Achievement needs to recieve updates.
        /// </summary>
        public abstract bool NeedsUpdate { get; }

        /// <summary>
        /// Called when session components are getting loaded.
        /// </summary>
        public virtual void SessionLoad() { }

        /// <summary>
        /// Update called in AfterSimulation.
        /// </summary>
        public virtual void SessionUpdate() { }

        /// <summary>
        /// Called when session components are getting saved.
        /// </summary>
        public virtual void SessionSave() { }

        /// <summary>
        /// Called when session gets unloaded.
        /// </summary>
        public virtual void SessionUnload() { }

        /// <summary>
        /// Called once after the session is loaded and before updates start.
        /// </summary>
        public virtual void SessionBeforeStart() { }

        /// <summary>
        /// Use to notify the achievement state change.
        /// </summary>
        protected void NotifyAchieved()
        {
            // Push the achivement state
            MySteam.API.SetAchievement(AchievementTag);
            
            // DEBUG HELPER
            if (MySteamAchievements.OFFLINE_ACHIEVEMENT_INFO)
            {
                MyAPIGateway.Utilities.ShowNotification("Achievement Unlocked: " + AchievementTag, 10000, MyFontEnum.Red);
            }

            IsAchieved = true;

            // Invoke event and clear the invocation list
            if(Achieved != null)
            {
                Achieved(this);

                foreach (var @delegate in Achieved.GetInvocationList())
                {
                    Achieved -= (Action<MySteamAchievementBase>)@delegate;
                }
            }
        }

        /// <summary>
        /// Called once when the session gets loaded for the first time.
        /// Always call base.Init()!
        /// </summary>
        public virtual void Init()
        {
            IsAchieved = MySteam.API.IsAchieved(AchievementTag);
        }
    }

    /*
     * This Class represents a system for Steam Achievement handling and should
     * ease the separation of per game achievement implementation separation.
     */
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 2000)]
    public class MySteamAchievements : MySessionComponentBase
    {
        // DEBUG: Use this to get rid of your steam user account achievement data
        public static readonly bool CLEAR_ACHIEVEMENTS_AND_STATS_ON_STARTUP = false;
        // DEBUG: Use this to debug the achievements offline
        public static readonly bool OFFLINE_ACHIEVEMENT_INFO = false;


        // Static repository for all active achievemtns of the game
        private static readonly List<MySteamAchievementBase> m_achievements = new List<MySteamAchievementBase>();
        // Simple Init flag
        private static bool m_initialized = false;
           
        private static void Init()
        {
            // Dedicated servers cannot get achievements.
            if(MySandboxGame.IsDedicated) return;
            // Something somewhere went terribly wrong.
            if(MySteam.API == null) return;

            // Load account stats
            MySteam.API.LoadStats();

            // DEBUG HELPER
            if (CLEAR_ACHIEVEMENTS_AND_STATS_ON_STARTUP)
            {
                MySteam.API.ResetAllStats(true);
                MySteam.API.StoreStats();
            }

            foreach (var type in MyPlugins.GameAssembly.GetTypes())
            {
                try
                {
                    // Instantiate all 
                    if (typeof(MySteamAchievementBase).IsAssignableFrom(type))
                    {
                        var achievement = (MySteamAchievementBase)Activator.CreateInstance(type);
                        // Try to init an Achievement
                        achievement.Init();
                        // Store only active achievements
                        if (!achievement.IsAchieved)
                        {
                            // Store the stats when the achievement is achieved
                            achievement.Achieved += x => MySteam.API.StoreStats();
                            m_achievements.Add(achievement);
                        }
                    }
                }
                catch (Exception e)
                {
                    MySandboxGame.Log.WriteLine("Initialization of achivement failed: " + type.Name);
                    MySandboxGame.Log.IncreaseIndent();
                    MySandboxGame.Log.WriteLine(e);
                    MySandboxGame.Log.DecreaseIndent();
                }
            }

            m_initialized = true;
        }

        public override void UpdateAfterSimulation()
        {
            // Prevent uninitialized call propagation.
            if (!m_initialized) return;

            // Propagate the updates
            foreach (var achievement in m_achievements)
            {
                if (achievement.NeedsUpdate && !achievement.IsAchieved)
                {
                    achievement.SessionUpdate();
                }
            }
        }

        public override void LoadData()
        {
            // Init the achievements if they are not.
            if (!m_initialized) Init();

            // Prevent uninitialized call propagation.
            if(!m_initialized) return;

            // Propagate the Load
            foreach (var achievement in m_achievements)
            {
                if(!achievement.IsAchieved)
                {
                    achievement.SessionLoad();
                }
            }
        }

        public override void SaveData()
        {
            // Prevent uninitialized call propagation.
            if (!m_initialized) return;

            // Propagate the save
            foreach (var achievement in m_achievements)
            {
                if(!achievement.IsAchieved)
                {
                    achievement.SessionSave();
                }
            }

            // Store possible changes
            MySteam.API.StoreStats();
        }

        protected override void UnloadData()
        {
            // Prevent uninitialized call propagation.
            if (!m_initialized) return;

            // Propagate the unload
            foreach (var achievement in m_achievements)
            {
                if(!achievement.IsAchieved)
                {
                    achievement.SessionUnload();
                }
            }

            // Store possible changes
            MySteam.API.StoreStats();
        }

        public override void BeforeStart()
        {
            // Prevent uninitialized call propagation.
            if (!m_initialized) return;

            // Propagate the before start
            foreach (var achievement in m_achievements)
            {
                if (!achievement.IsAchieved)
                {
                    achievement.SessionBeforeStart();
                }
            }
        }
    }
}
