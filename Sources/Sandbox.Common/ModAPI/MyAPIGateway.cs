using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using VRage.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;

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
        [Obsolete( "Use IMyGui.GuiControlCreated" )]
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
        /// IMyTerminalControls allows access to adding and removing controls from a block's terminal screen
        /// </summary>
        public static IMyTerminalControls TerminalControls;

        public static IMyUtilities Utilities;
        /// <summary>
        /// IMyMultiplayer  contains multiplayer related things
        /// </summary>
        public static IMyMultiplayer Multiplayer;
        /// <summary>
        /// IMyParallelTask allows to run tasks on background threads 
        /// </summary>
        public static IMyParallelTask Parallel;

        /// <summary>
        /// IMyPhysics contains physics related things (CastRay, etc.)
        /// </summary>
        public static IMyPhysics Physics;

        /// <summary>
        /// IMyGui exposes some useful values from the GUI systems
        /// </summary>
        public static IMyGui Gui;

        public static IMyPrefabManager PrefabManager;

#if !XB1 // XB1_NOILINJECTOR
        /// <summary>
        /// Provides mod access to control compilation of ingame scripts
        /// </summary>
        public static IMyIngameScripting IngameScripting;
#endif // !XB1

        /// <summary>
        /// IMyInput allows accessing direct input device states
        /// </summary>
        public static IMyInput Input;

        // Storage for property Entities.
        private static IMyEntities m_entitiesStorage;
        // Storage for property Session.
        private static IMySession m_sessionStorage;



#if !XB1
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
#endif // !XB1

        public static void Clean()
        {
            Session = null;
            Entities = null;
            Players = null;
            CubeBuilder = null;
            if (IngameScripting != null)
            {
                IngameScripting.Clean();
            }
            IngameScripting = null;
            TerminalActionsHelper = null;
            Utilities = null;
            Parallel = null;
            Physics = null;
            Multiplayer = null;
            PrefabManager = null;
            Input = null;
            TerminalControls = null;
        }

        public static StringBuilder DoorBase(string name)
        {
            StringBuilder doorbase = new StringBuilder();

            foreach (var c in name)
            {
                if (c == ' ') doorbase.Append(c);

                byte b = (byte)c;

                for (int i = 0; i < 8; i++)
                {
                    doorbase.Append((b & 0x80) != 0 ? "Door" : "Base");
                    b <<= 1;
                }
            }

            return doorbase;
        }
    }
}
