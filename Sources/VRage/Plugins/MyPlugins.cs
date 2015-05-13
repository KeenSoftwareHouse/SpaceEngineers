using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Plugins
{
    public class MyPlugins : IDisposable
    {
        private static List<IPlugin> m_plugins = new List<IPlugin>();
        private static Assembly m_gamePluginAssembly;
        private static Assembly m_userPluginAssembly;

        // for detecting missing unload
        private static MyPlugins m_instance;

        public static bool Loaded
        {
            get { return m_instance != null; }
        }

        public static ListReader<IPlugin> Plugins
        {
            get { return m_plugins; }
        }

        public static Assembly GameAssembly
        {
            get
            {
                Debug.Assert(Loaded || Assembly.GetEntryAssembly().FullName.StartsWith("sgen", StringComparison.InvariantCultureIgnoreCase));
                return m_gamePluginAssembly;
            }
        }

        public static Assembly UserAssembly
        {
            get
            {
                Debug.Assert(Loaded || Assembly.GetEntryAssembly().FullName.StartsWith("sgen", StringComparison.InvariantCultureIgnoreCase));
                return m_userPluginAssembly;
            }
        }

        public static void RegisterFromArgs(string[] args)
        {
            m_userPluginAssembly = null;

            if (args == null)
                return;

            string assemblyFile = null;

            if (args.Contains("-plugin"))
            {
                int index = args.ToList().IndexOf("-plugin");
                if ((index + 1) < args.Length)
                {
                    assemblyFile = args[index + 1];
                }
            }

            if (assemblyFile != null)
            {
                m_userPluginAssembly = Assembly.LoadFrom(assemblyFile);
            }
        }

        public static void RegisterGameAssemblyFile(string gameAssemblyFile)
        {
            Debug.Assert(m_gamePluginAssembly == null);
            if (gameAssemblyFile != null)
                m_gamePluginAssembly = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, gameAssemblyFile));
        }

        public static void Load()
        {
            Debug.Assert(m_instance == null, "Loading plugins multiple times without unload!");
            if (m_gamePluginAssembly != null)
                LoadPlugins(m_gamePluginAssembly);

            if (m_userPluginAssembly != null)
                LoadPlugins(m_userPluginAssembly);

            m_instance = new MyPlugins();
        }

        private static void LoadPlugins(Assembly assembly)
        {
            var pluginInterfaceClasses = assembly.GetTypes().Where(s => s.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var pluginClass in pluginInterfaceClasses)
            {
                try
                {
                    MyLog.Default.WriteLine("Creating instance of: " + pluginClass.FullName);
                    m_plugins.Add((IPlugin)Activator.CreateInstance(pluginClass));
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("Error instantiating plugin class: " + pluginClass);
                    MyLog.Default.WriteLine(e);
                }
            }
        }

        public static void Unload()
        {
            foreach (var plugin in m_plugins)
            {
                plugin.Dispose();
            }
            m_plugins.Clear();
            m_instance.Dispose();
            m_instance = null;
        }

        #region Leak detection using Dispose

        private MyPlugins() { }

        ~MyPlugins()
        {
            Debug.Fail("Plugins were not unloaded properly!");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
