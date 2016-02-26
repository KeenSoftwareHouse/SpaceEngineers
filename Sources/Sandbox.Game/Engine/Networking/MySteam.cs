using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using Microsoft.Win32;

using Sandbox;
using SteamSDK;
using System.Diagnostics;
using VRage.Utils;

namespace Sandbox.Engine.Networking
{
    /// <summary>
    /// Ingame shortcut for various things
    /// </summary>
    public static class MySteam
    {
        public static SteamAPI API { get { return MySandboxGame.Services.SteamService.SteamAPI; } }
        public static GameServer Server
        {
            get
            {
                var services = MySandboxGame.Services;
                if (services != null)
                {
                    var steamService = services.SteamService;
                    if (steamService != null)
                    {
                        var serverAPI = steamService.SteamServerAPI;
                        if (serverAPI != null)
                        {
                            return serverAPI.GameServer;
                        }
                    }             
                }
                return null;
            }
        }

        public static uint AppId { get { return MySandboxGame.Services.SteamService.AppId; } }
        public static bool IsActive
        {
            get
            {
                var services = MySandboxGame.Services;
                if (services != null)
                {
                    var steamService = services.SteamService;
                    if (steamService != null)
                    {
                        return steamService.IsActive;
                    }
                }
                return false;
            }
        }

        public static bool IsOnline { get { return MySandboxGame.Services.SteamService != null ? MySandboxGame.Services.SteamService.IsOnline : false;  } }
        public static bool IsOverlayEnabled { get { return MySandboxGame.Services.SteamService.IsOverlayEnabled; } }
        public static bool OwnsGame { get { return MySandboxGame.Services.SteamService.OwnsGame; } }

        public static ulong UserId
        {
            get
            {
                return MySandboxGame.Services != null && MySandboxGame.Services.SteamService != null ? MySandboxGame.Services.SteamService.UserId : ulong.MaxValue;
            }
        }

        public static string UserName { get { return MySandboxGame.Services.SteamService.UserName; } }
        public static Universe UserUniverse { get { return MySandboxGame.Services.SteamService.UserUniverse; } }
        public static string BranchName {
            get
            {
                if (MySandboxGame.Services == null || MySandboxGame.Services.SteamService == null)
                {
                    Debug.Fail("MySandboxGame.Services.SteamService not running?");
                    MyLog.Default.WriteLine("ERROR: branch name cannot be resolved, services=" + MySandboxGame.Services);
                    return "ERROR";
                }
                return MySandboxGame.Services.SteamService.BranchName; 
            } 
        }

        public static void OpenOverlayUrl(string url)
        {
            if (API != null)
            {
                API.OpenOverlayUrl(url);
            }
        }
    }
}
