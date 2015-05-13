using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// This is entry point for entire scripting possibilities in game
    /// </summary>
    public static class MyAPIGateway
    {
        /// <summary>
        /// IMySession represents session object e.g. current world and its settings
        /// </summary>
        public static IMySession Session;
        /// <summary>
        /// IMyEntities represents all objects that currently in world 
        /// </summary>
        public static IMyEntities Entities;
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
    }
}
