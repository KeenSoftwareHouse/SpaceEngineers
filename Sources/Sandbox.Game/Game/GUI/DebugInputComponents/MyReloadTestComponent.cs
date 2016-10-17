#if !XB1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Sandbox.Common;
using VRage.Game.Components;

namespace Sandbox.Game.Gui.DebugInputComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class MyReloadTestComponent : MySessionComponentBase
    {
        public static bool Enabled = false;

        public override void UpdateAfterSimulation()
        {
            if (Enabled && MySandboxGame.IsGameReady && MySession.Static != null && MySession.Static.ElapsedPlayTime.TotalSeconds > 5)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                MySandboxGame.Log.WriteLine(String.Format("RELOAD TEST, Game GC: {0} B", GC.GetTotalMemory(false).ToString("##,#")));
                MySandboxGame.Log.WriteLine(String.Format("RELOAD TEST, Game WS: {0} B", Process.GetCurrentProcess().PrivateMemorySize64.ToString("##,#")));
                MySessionLoader.UnloadAndExitToMenu();
            }
        }

        public static void DoReload()
        {
            GC.Collect(2, GCCollectionMode.Forced);
            MySandboxGame.Log.WriteLine(String.Format("RELOAD TEST, Menu GC: {0} B", GC.GetTotalMemory(false).ToString("##,#")));
            MySandboxGame.Log.WriteLine(String.Format("RELOAD TEST, Menu WS: {0} B", Process.GetCurrentProcess().PrivateMemorySize64.ToString("##,#")));
            var session = MyLocalCache.GetAvailableWorldInfos().OrderByDescending(s => s.Item2.LastLoadTime).FirstOrDefault();
            if (session != null)
                MySessionLoader.LoadSingleplayerSession(session.Item1);
        }
    }
}
#endif // !XB1
