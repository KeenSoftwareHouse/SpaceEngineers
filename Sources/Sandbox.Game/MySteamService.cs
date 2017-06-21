using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;


namespace Sandbox
{
    /// <summary>
    /// Steam service, may be replaced by interface later.
    /// Don't use it directly in GameLib
    /// </summary>
    public class MySteamService: IDisposable
    {
        public enum NotificationPosition
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
        }

        public readonly SteamSDK.SteamAPI SteamAPI;
        public readonly SteamSDK.SteamServerAPI SteamServerAPI;
        
        public bool IsActive { get; private set; }
        public bool IsOnline { get { return IsActive && SteamAPI.IsOnline(); } }
        public bool IsOverlayEnabled { get { return IsActive && SteamAPI.IsOverlayEnabled(); } }

        public bool HasGameServer { get { return SteamServerAPI.GameServer != null; } }

        public uint AppId { get; private set; }
        public ulong UserId { get; set; }
        public string UserName { get; private set; }
        public string SerialKey { get; private set; }
        public SteamSDK.Universe UserUniverse { get; private set; }
        public string BranchName { get; private set; }

        public bool OwnsGame { get; private set; }

        public MySteamService(bool isDedicated, uint appId)
        {
            AppId = appId;
            if (isDedicated)
            {
                SteamServerAPI = SteamSDK.SteamServerAPI.Instance;
            }
            else
            {
                SteamAPI = SteamSDK.SteamAPI.Instance;
                IsActive = SteamAPI != null;
                if (SteamSDK.SteamAPI.RestartIfNecessary(AppId))
                {
#if !XB1
                    Environment.Exit(0);
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                }

                if (IsActive)
                {
                    UserId = SteamAPI.GetSteamUserId();
                    UserName = SteamAPI.GetSteamName();
                    OwnsGame = SteamAPI.HasGame();
                    UserUniverse = SteamAPI.GetSteamUserUniverse();
                    BranchName = SteamAPI.GetBranchName();

                    SteamAPI.LoadStats();
                }
            }
        }

        public void SetNotificationPosition(NotificationPosition position)
        {
            if(IsActive)
            {
                SteamAPI.SetNotificationPosition((SteamSDK.NotificationPosition)(int)position);
            }
        }
        
        public void Dispose()
        {
            if (SteamAPI != null)
            {
                MyLog.Default.WriteLine("Steam closed");

                SteamAPI.Dispose();
                IsActive = false;

                UserId = 0;
                UserName = String.Empty;
                OwnsGame = false;
                UserUniverse = SteamSDK.Universe.Invalid;
                BranchName = null;
            }

            if (SteamServerAPI != null)
                SteamServerAPI.Dispose();
        }
    }
}
