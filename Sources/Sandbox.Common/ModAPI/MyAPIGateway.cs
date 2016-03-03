using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// This is entry point for entire scripting possibilities in game
    /// </summary>
    public static class MyAPIGateway
    {

        /// <summary>
        /// Event triggered on gui control created.
        /// </summary>
        public static Action<object> GuiControlCreated;

        /// <summary>
        /// IMySession represents session object e.g. current world and its settings
        /// </summary>
        public static IMySession Session
        {
            get
            {
                return m_sessionStorage;
            }
            set
            {
                m_sessionStorage = value;
            }
        }
        /// <summary>
        /// IMyEntities represents all objects that currently in world 
        /// </summary>
        public static IMyEntities Entities
        {
            get
            {
                return m_entitiesStorage;
            }
            set
            {
                m_entitiesStorage = value;
                if (Entities != null)
                {
                    MyAPIGatewayShortcuts.RegisterEntityUpdate = Entities.RegisterForUpdate;
                    MyAPIGatewayShortcuts.UnregisterEntityUpdate = Entities.UnregisterForUpdate;
                }
                else
                {
                    MyAPIGatewayShortcuts.RegisterEntityUpdate = null;
                    MyAPIGatewayShortcuts.UnregisterEntityUpdate = null;
                }
            }
        }
        /// <summary>
        /// IMyPlayerCollection contains all players that are in world 
        /// </summary>
        public static IMyPlayerCollection Players;
        /// <summary>
        /// IMyCubeBuilder represents building hand 
        /// </summary>
        public static IMyCubeBuilder CubeBuilder;
        /// <summary>
        /// IMyTerminalActionsHelper is helper for terminal actions and allows to access terminal 
        /// </summary>
        public static IMyTerminalActionsHelper TerminalActionsHelper;
        /// <summary>
        /// IMyUtilities is helper for loading/saving files , showing messages to players
        /// </summary>
        public static IMyUtilities Utilities;
        /// <summary>
        /// IMyMultiplayer  contains multiplayer related things
        /// </summary>
        public static IMyMultiplayer Multiplayer;
        /// <summary>
        /// IMyParallelTask allows to run tasks on baground threads 
        /// </summary>
        public static IMyParallelTask Parallel;

        public static IMyPrefabManager PrefabManager;

        /// <summary>
        /// IMyInput allows accessing direct input device states
        /// </summary>
        public static VRage.ModAPI.IMyInput Input;

        // Storage for property Entities.
        private static IMyEntities m_entitiesStorage;
        // Storage for property Session.
        private static IMySession m_sessionStorage;

        [Conditional("DEBUG")] 
        public static void GetMessageBoxPointer(ref IntPtr pointer)
        {

            IntPtr user32 = LoadLibrary("user32.dll");
            pointer = GetProcAddress(user32, "MessageBoxW");
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(String dllname);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, String procname);

        public static void Clean()
        {
            Session = null;
            Entities = null;
            Players = null;
            CubeBuilder = null;
            TerminalActionsHelper = null;
            Utilities = null;
            Parallel = null;
            Multiplayer = null;
            PrefabManager = null;
            Input = null;
        }
    }
}
