#if XB1

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SteamSDK
{

    public delegate void ServerResponse(GameServerItem serverItem);
    public delegate void ServerRulesResponse(Dictionary<string, string> rules);
    public delegate void ServerListResponse(int server);
    public delegate void ServerListRefreshCompleteResponse(MatchMakingServerResponseEnum response);
    public delegate void ServerListFailResponse(int server);



    public class SteamAPIBase : IDisposable
    {
        protected SteamAPIBase(bool server)
        {
            // Peer2Peer.Init(server);
        }

        // private void ~SteamAPIBase()
        // {
        //     Peer2Peer.Destroy();
        // }

        public uint GetAccountId(ulong steamId)
        {
            return (uint)steamId;
        }

        public unsafe AccountType GetAccountType(ulong steamId)
        {
            // return (AccountType)((uint)(*(ref steamId + 4)) >> 20 & 15u);
            return 0;
        }

        protected virtual void Dispose(bool A_0)
        {
            if (A_0)
            {
                Peer2Peer.Destroy();
            }
            else
            {
                //base.Finalize();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            //GC.SuppressFinalize(this);
        }
    }

    public class SteamAPI : SteamAPIBase
    {
        // private unsafe delegate void SerialKeyDelegate(AppProofOfPurchaseKeyResponse_t* A_0);

        private RemoteStorage m_remoteStorage;

        private UGC m_UGC;

        private Matchmaking m_matchmaking;

        private Friends m_friends;

        private static bool m_initializationFailed;

        private static SteamAPI m_instance;

        //private int m_pingServerQuery;

        // private unsafe MatchmakingPingResponse* m_pingServerResponse;

        // private unsafe MatchmakingServerListResponse* m_dedicatedServerListResponse;

        // private unsafe void* m_dedicatedServerListRequest;

        // private unsafe MatchmakingServerListResponse* m_favoritesServerListResponse;

        // private unsafe void* m_favoritesServerListRequest;

        // private unsafe MatchmakingServerListResponse* m_historyServerListResponse;

        // private unsafe void* m_historyServerListRequest;

        // private unsafe MatchmakingServerListResponse* m_LANServerListResponse;

        // private unsafe void* m_LANServerListRequest;

        // private unsafe MatchmakingServerListResponse* m_friendsServerListResponse;

        // private unsafe void* m_friendsServerListRequest;

        public ServerResponse OnPingServerResponded;

        public Action OnPingServerFailedToRespond;

        public ServerListResponse OnDedicatedServerListResponded;

        public ServerListFailResponse OnDedicatedServerListFailResponse;

        public ServerListRefreshCompleteResponse OnDedicatedServersCompleteResponse;

        public ServerListResponse OnFavoritesServerListResponded;

        public ServerListFailResponse OnFavoritesServerListFailResponse;

        public ServerListRefreshCompleteResponse OnFavoritesServersCompleteResponse;

        public ServerListResponse OnHistoryServerListResponded;

        public ServerListFailResponse OnHistoryServerListFailResponse;

        public ServerListRefreshCompleteResponse OnHistoryServersCompleteResponse;

        public ServerListResponse OnLANServerListResponded;

        public ServerListFailResponse OnLANServerListFailResponse;

        public ServerListRefreshCompleteResponse OnLANServersCompleteResponse;

        public ServerListResponse OnFriendsServerListResponded;

        public ServerListFailResponse OnFriendsServerListFailResponse;

        public ServerListRefreshCompleteResponse OnFriendsServersCompleteResponse;

        public Friends Friends
        {
            get
            {
                return this.m_friends;
            }
        }

        public UGC UGC
        {
            get
            {
                return this.m_UGC;
            }
        }

        public RemoteStorage RemoteStorage
        {
            get
            {
                return this.m_remoteStorage;
            }
        }

        public Matchmaking Matchmaking
        {
            get
            {
                return this.m_matchmaking;
            }
        }

        public static SteamAPI Instance
        {
            get
            {
                if (SteamAPI.m_instance == null && !SteamAPI.m_initializationFailed)
                {
                    // SteamAPI.m_initializationFailed = (((<Module>.SteamAPI_Init() == 0) ? 1 : 0) != 0);
                    SteamAPI.m_initializationFailed = true;
                    if (!SteamAPI.m_initializationFailed)
                    {
                        SteamAPI.m_instance = new SteamAPI();
                    }
                }
                return SteamAPI.m_instance;
            }
        }

        private SteamAPI()
            : base(false)
        {
            // Peer2Peer.Init(false);
             try
             {
                 this.m_remoteStorage = new RemoteStorage();
                 this.m_UGC = new UGC();
                 this.m_matchmaking = new Matchmaking();
                 this.m_friends = new Friends();
                 //MatchmakingPingResponse* ptr = <Module>.@new(24uL);
                 //MatchmakingPingResponse* pingServerResponse;
                 //try
                 //{
                 //    if (ptr != null)
                 //    {
                 //        pingServerResponse = <Module>.MatchmakingPingResponse.{ctor}(ptr, new ServerResponse(this.PingServerResponded), new Action(this.PingServerFailedToRespond));
                 //    }
                 //    else
                 //    {
                 //        pingServerResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)ptr);
                 //    throw;
                 //}
                 //this.m_pingServerResponse = pingServerResponse;
                 //MatchmakingServerListResponse* ptr2 = <Module>.@new(32uL);
                 //MatchmakingServerListResponse* dedicatedServerListResponse;
                 //try
                 //{
                 //    if (ptr2 != null)
                 //    {
                 //        dedicatedServerListResponse = <Module>.MatchmakingServerListResponse.{ctor}(ptr2, new ServerListRequestResponse(this.ServerListResponded), new ServerListRequestFailResponse(this.ServerListFailedToRespond), new ServerListRequestRefreshCompleteResponse(this.RefreshCompleteResponse));
                 //    }
                 //    else
                 //    {
                 //        dedicatedServerListResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)ptr2);
                 //    throw;
                 //}
                 //this.m_dedicatedServerListResponse = dedicatedServerListResponse;
                 //MatchmakingServerListResponse* ptr3 = <Module>.@new(32uL);
                 //MatchmakingServerListResponse* favoritesServerListResponse;
                 //try
                 //{
                 //    if (ptr3 != null)
                 //    {
                 //        favoritesServerListResponse = <Module>.MatchmakingServerListResponse.{ctor}(ptr3, new ServerListRequestResponse(this.ServerListResponded), new ServerListRequestFailResponse(this.ServerListFailedToRespond), new ServerListRequestRefreshCompleteResponse(this.RefreshCompleteResponse));
                 //    }
                 //    else
                 //    {
                 //        favoritesServerListResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)ptr3);
                 //    throw;
                 //}
                 //this.m_favoritesServerListResponse = favoritesServerListResponse;
                 //MatchmakingServerListResponse* ptr4 = <Module>.@new(32uL);
                 //MatchmakingServerListResponse* historyServerListResponse;
                 //try
                 //{
                 //    if (ptr4 != null)
                 //    {
                 //        historyServerListResponse = <Module>.MatchmakingServerListResponse.{ctor}(ptr4, new ServerListRequestResponse(this.ServerListResponded), new ServerListRequestFailResponse(this.ServerListFailedToRespond), new ServerListRequestRefreshCompleteResponse(this.RefreshCompleteResponse));
                 //    }
                 //    else
                 //    {
                 //        historyServerListResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)ptr4);
                 //    throw;
                 //}
                 //this.m_historyServerListResponse = historyServerListResponse;
                 //MatchmakingServerListResponse* ptr5 = <Module>.@new(32uL);
                 //MatchmakingServerListResponse* lANServerListResponse;
                 //try
                 //{
                 //    if (ptr5 != null)
                 //    {
                 //        lANServerListResponse = <Module>.MatchmakingServerListResponse.{ctor}(ptr5, new ServerListRequestResponse(this.ServerListResponded), new ServerListRequestFailResponse(this.ServerListFailedToRespond), new ServerListRequestRefreshCompleteResponse(this.RefreshCompleteResponse));
                 //    }
                 //    else
                 //    {
                 //        lANServerListResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)ptr5);
                 //    throw;
                 //}
                 //this.m_LANServerListResponse = lANServerListResponse;
                 //MatchmakingServerListResponse* this2 = <Module>.@new(32uL);
                 //MatchmakingServerListResponse* friendsServerListResponse;
                 //try
                 //{
                 //    if (this2 != null)
                 //    {
                 //        friendsServerListResponse = <Module>.MatchmakingServerListResponse.{ctor}(this2, new ServerListRequestResponse(this.ServerListResponded), new ServerListRequestFailResponse(this.ServerListFailedToRespond), new ServerListRequestRefreshCompleteResponse(this.RefreshCompleteResponse));
                 //    }
                 //    else
                 //    {
                 //        friendsServerListResponse = 0L;
                 //    }
                 //}
                 //catch
                 //{
                 //    <Module>.delete((void*)this2);
                 //    throw;
                 //}
                 //this.m_friendsServerListResponse = friendsServerListResponse;
             }
             catch
             {
                 //base.Dispose(true);
                 throw;
             }
        }

        // private void ~SteamAPI()
        // {
        //     if (SteamAPI.m_instance == null)
        //     {
        //         throw new InvalidOperationException("API is not initialized!");
        //     }
        //     <Module>.SteamAPI_Shutdown();
        //     SteamAPI.m_instance = null;
        //     IDisposable disposable = this.m_remoteStorage as IDisposable;
        //     if (disposable != null)
        //     {
        //         disposable.Dispose();
        //     }
        //     IDisposable disposable2 = this.m_UGC as IDisposable;
        //     if (disposable2 != null)
        //     {
        //         disposable2.Dispose();
        //     }
        //     IDisposable matchmaking = this.m_matchmaking;
        //     if (matchmaking != null)
        //     {
        //         matchmaking.Dispose();
        //     }
        //     IDisposable this2 = this.m_friends as IDisposable;
        //     if (this2 != null)
        //     {
        //         this2.Dispose();
        //     }
        // }

        // private unsafe GameServerItem GetServerDetails(void* request, int serverIndex)
        // {
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, request, serverIndex, *(*(long*)this2 + 56L)));
        // }

         public static bool RestartIfNecessary(uint appId)
         {
             // TODO [vicent] current stub implementation
             return false;
        //     return <Module>.SteamAPI_RestartAppIfNecessary(appId) != null;
         }

         public unsafe bool IsOverlayEnabled()
         {
             // TODO [vicent] current stub implementation
             return false;
        //     ISteamUtils* expr_05 = <Module>.SteamUtils();
        //     return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 136L));
         }

         public unsafe bool IsOnline()
         {
             return false;
             //ISteamUser* expr_05 = <Module>.SteamUser();
             //return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 8L));
         }

         public unsafe ulong GetSteamUserId()
         {
             // TODO [vicent] current stub implementation
             return 0;
        //     ISteamUser* this2 = <Module>.SteamUser();
        //     CSteamID cSteamID;
        //     return (ulong)(*calli(CSteamID* modreq(System.Runtime.CompilerServices.IsUdtReturn) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID*), this2, ref cSteamID, *(*(long*)this2 + 16L)));
         }

         public unsafe Universe GetSteamUserUniverse()
         {
             // TODO [vicent] current stub implementation
             return Universe.Invalid;
        //     ISteamUser* this2 = <Module>.SteamUser();
        //     CSteamID cSteamID;
        //     return (Universe)(*(calli(CSteamID* modreq(System.Runtime.CompilerServices.IsUdtReturn) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID*), this2, ref cSteamID, *(*(long*)this2 + 16L)) + 7L));
         }

         public unsafe string GetBranchName()
         {
             // TODO [vicent] current stub implementation
             return "None";
        //     ISteamApps* this2 = <Module>.SteamApps();
        //     $ArrayType$$$BY0BJ@D name;
        //     if (calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte)*,System.Int32), this2, ref name, 25, *(*(long*)this2 + 120L)))
        //     {
        //         return new string((sbyte*)(&name));
        //     }
        //     return null;
         }

         public unsafe byte[] GetAuthSessionTicket(out uint handle)
         {
             // TODO [vicent] current stub implementation
             handle = 0;

        //     ISteamUser* ptr = <Module>.SteamUser();
        //     $ArrayType$$$BY0EAA@E buf;
        //     uint len;
        //     uint handle2 = calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32,System.UInt32*), ptr, ref buf, 1024, ref len, *(*(long*)ptr + 104L));
        //     handle = handle2;
        //     if (handle2 != 0u)
        //     {
        //         byte[] result = new byte[len];
        //         Marshal.Copy((IntPtr)((void*)(&buf)), result, 0, (int)len);
        //         return result;
        //     }
             return null;
         }

         public unsafe bool GetAuthSessionTicket(out uint handle, byte[] buffer, out uint length)
         {
             // TODO [vicent] current stub implementation
             length = 0;
             handle = 0;
             return false;

        //     int rgchToken_07_cp_1 = 0;
        //     uint unTokenLen = 0u;
        //     ISteamUser* this2 = <Module>.SteamUser();
        //     handle = calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32,System.UInt32*), this2, ref buffer[rgchToken_07_cp_1], buffer.Length, ref unTokenLen, *(*(long*)this2 + 104L));
        //     length = unTokenLen;
        //     return ((handle != 0u) ? 1 : 0) != 0;
         }

        // public unsafe void CancelAuthTicket(uint handle)
        // {
        //     ISteamUser* this2 = <Module>.SteamUser();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32), this2, handle, *(*(long*)this2 + 128L));
        // }

         public unsafe string GetSteamName()
         {
             // TODO [vicent] current stub implementation
             return "Invalid!";
        //     ISteamFriends* expr_05 = <Module>.SteamFriends();
        //     sbyte* personaNameUtf8 = calli(System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05));
        //     sbyte* this2 = personaNameUtf8;
        //     if (*(sbyte*)personaNameUtf8 != 0)
        //     {
        //         do
        //         {
        //             this2 += 1L / (long)sizeof(sbyte);
        //         }
        //         while (*(sbyte*)this2 != 0);
        //     }
        //     long num = (long)(this2 - personaNameUtf8);
        //     return new string((sbyte*)personaNameUtf8, 0, (int)num, Encoding.UTF8);
         }

         public unsafe bool HasGame()
         {
             // TODO [vicent] current stub implementation
             return false;
        //     ISteamApps* expr_05 = <Module>.SteamApps();
        //     return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05));
         }

         public unsafe void SetNotificationPosition(NotificationPosition position)
         {
             // TODO [vicent] current stub implementation
             return;
        //     ISteamUtils* this2 = <Module>.SteamUtils();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,ENotificationPosition), this2, position, *(*(long*)this2 + 80L));
         }

         public void RunCallbacks()
         {
             // TODO [vicent] current stub implementation
             System.Diagnostics.Debug.Assert(false, "Not implemented yet!");
             //<Module>.SteamAPI_RunCallbacks();
         }

        // public unsafe void IndicateAchievementProgress(string name, uint val, uint maxVal)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     object arg_22_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.UInt32,System.UInt32), this2, nameChars, val, maxVal, *(*(long*)this2 + 104L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        // }

         public unsafe void LoadStats()
         {
             // TODO [vicent] current stub implementation
             return;
        //     ISteamUserStats* expr_05 = <Module>.SteamUserStats();
        //     object arg_0D_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05));
         }

        // public unsafe void StoreStats()
        // {
        //     ISteamUserStats* expr_05 = <Module>.SteamUserStats();
        //     object arg_11_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 80L));
        // }

        // public unsafe int GetStatInt(string name)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     int result;
        //     object arg_22_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32*), this2, nameChars, ref result, *(*(long*)this2 + 16L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        //     return result;
        // }

        // public unsafe float GetStatFloat(string name)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     float result;
        //     object arg_21_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Single*), this2, nameChars, ref result, *(*(long*)this2 + 8L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        //     return result;
        // }

        // public unsafe void SetStat(string name, float val)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     object arg_21_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Single), this2, nameChars, val, *(*(long*)this2 + 24L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        // }

        // public unsafe void SetStat(string name, int val)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     object arg_21_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32), this2, nameChars, val, *(*(long*)this2 + 32L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        // }

        // public unsafe void SetAchievement(string name)
        // {
        //     sbyte* nameChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(name));
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     object arg_20_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, nameChars, *(*(long*)this2 + 56L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)nameChars));
        // }

        // public unsafe void ResetAllStats([MarshalAs(UnmanagedType.U1)] bool achievementsToo)
        // {
        //     ISteamUserStats* this2 = <Module>.SteamUserStats();
        //     object arg_17_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), this2, achievementsToo, *(*(long*)this2 + 168L));
        // }

         public unsafe void OpenOverlayUrl(string url)
         {
             // TODO [vicent] current stub implementation
             System.Diagnostics.Debug.Assert(false, "Not implemented yet!");
             return;

        //     sbyte* urlChars = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(url));
        //     ISteamFriends* this2 = <Module>.SteamFriends();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, urlChars, *(*(long*)this2 + 192L));
        //     Marshal.FreeHGlobal((IntPtr)((void*)urlChars));
         }

         public unsafe void OpenOverlayUser(ulong steamId)
         {
             // TODO [vicent] current stub implementation
             System.Diagnostics.Debug.Assert(false, "Not implemented yet!");
             return;
             //ISteamFriends* this2 = <Module>.SteamFriends();
             //CSteamID steamId2 = steamId;
             //calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,CSteamID), this2, ref <Module>.??_C@_07NDJFPDBL@steamid?$AA@, steamId2, *(*(long*)this2 + 184L));
         }

         public unsafe void PingServer(uint unIP, ushort unPort)
         {
             // TODO [vicent] current stub implementation
             System.Diagnostics.Debug.Assert(false, "Not implemented yet!");
             return;
             
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     this.m_pingServerQuery = calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,System.UInt16,ISteamMatchmakingPingResponse*), this2, unIP, unPort, this.m_pingServerResponse, *(*(long*)this2 + 104L));
         }

        // public void PingServerResponded(GameServerItem serverItem)
        // {
        //     this.OnPingServerResponded(serverItem);
        // }

        // public void PingServerFailedToRespond()
        // {
        //     this.OnPingServerFailedToRespond();
        // }

         public unsafe void GetServerRules(uint unIP, ushort unPort, ServerRulesResponse completedAction, Action failedAction)
         {
             // TODO [vicent] current stub implementation
             return;

             //MatchmakingRulesResponse* this2 = <Module>.@new(32uL);
             //MatchmakingRulesResponse* unPort2;
             //try
             //{
             //    if (this2 != null)
             //    {
             //        unPort2 = <Module>.MatchmakingRulesResponse.{ctor}(this2, completedAction, failedAction);
             //    }
             //    else
             //    {
             //        unPort2 = 0L;
             //    }
             //}
             //catch
             //{
             //    <Module>.delete((void*)this2);
             //    throw;
             //}
             //ISteamMatchmakingServers* unIP2 = <Module>.SteamMatchmakingServers();
             //object arg_3A_0 = calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,System.UInt16,ISteamMatchmakingRulesResponse*), unIP2, unIP, unPort, unPort2, *(*(long*)unIP2 + 120L));
         }

         public void RequestInternetServerList(string filterOps)
         {
             // TODO [vicent] current stub implementation
             // this.RequestServerList(filterOps, this.m_dedicatedServerListResponse);
         }

         public void RequestFavoritesServerList(string filterOps)
         {
             // TODO [vicent] current stub implementation
             //this.RequestServerList(filterOps, this.m_favoritesServerListResponse);
         }

         public void RequestHistoryServerList(string filterOps)
         {
            // TODO [vicent] current stub implementation
             //this.RequestServerList(filterOps, this.m_historyServerListResponse);
         }

         public unsafe void RequestLANServerList()
         {
             // TODO [vicent] current stub implementation
             return;
             //ISteamUtils* expr_05 = <Module>.SteamUtils();
             //uint appId = calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 72L));
             //ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //this.m_LANServerListRequest = calli(System.Void* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,ISteamMatchmakingServerListResponse*), this2, appId, this.m_LANServerListResponse, *(*(long*)this2 + 8L));
         }

         public void RequestFriendsServerList(string filterOps)
         {
             // TODO [vicent] current stub implementation
             return;
             //this.RequestServerList(filterOps, this.m_friendsServerListResponse);
         }

         public unsafe void CancelInternetServersRequest()
         {
             // TODO [vicent] current stub implementation
             return;
             //if (this.m_dedicatedServerListRequest != null)
             //{
             //    ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //    calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*), this2, this.m_dedicatedServerListRequest, *(*(long*)this2 + 64L));
             //}
             //this.m_dedicatedServerListRequest = null;
         }

         public unsafe void CancelFavoritesServersRequest()
         {
             // TODO [vicent] current stub implementation
             return;
             //if (this.m_favoritesServerListRequest != null)
             //{
             //    ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //    calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*), this2, this.m_favoritesServerListRequest, *(*(long*)this2 + 64L));
             //}
             //this.m_favoritesServerListRequest = null;
         }

         public unsafe void CancelHistoryServersRequest()
         {
             // TODO [vicent] current stub implementation
             return;
             //if (this.m_historyServerListRequest != null)
             //{
             //    ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //    calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*), this2, this.m_historyServerListRequest, *(*(long*)this2 + 64L));
             //}
             //this.m_historyServerListRequest = null;
         }

         public unsafe void CancelLANServersRequest()
         {
             // TODO [vicent] current stub implementation
             return;
             //if (this.m_LANServerListRequest != null)
             //{
             //    ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //    calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*), this2, this.m_LANServerListRequest, *(*(long*)this2 + 64L));
             //}
             //this.m_LANServerListRequest = null;
         }

         public unsafe void CancelFriendsServersRequest()
         {
             // TODO [vicent] current stub implementation
             return;
             //if (this.m_friendsServerListRequest != null)
             //{
             //    ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
             //    calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*), this2, this.m_friendsServerListRequest, *(*(long*)this2 + 64L));
             //}
             //this.m_friendsServerListRequest = null;
         }

         public unsafe GameServerItem GetDedicatedServerDetails(int serverIndex)
         {
             // TODO [vicent] current stub implementation
             return new GameServerItem();

        //     void* serverIndex2 = this.m_dedicatedServerListRequest;
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, serverIndex2, serverIndex, *(*(long*)this2 + 56L)));
         }

         public unsafe GameServerItem GetFavoritesServerDetails(int serverIndex)
         {
             // TODO [vicent] current stub implementation
             return new GameServerItem();
        //     void* serverIndex2 = this.m_favoritesServerListRequest;
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, serverIndex2, serverIndex, *(*(long*)this2 + 56L)));
         }

         public unsafe GameServerItem GetHistoryServerDetails(int serverIndex)
         {
             // TODO [vicent] current stub implementation
             return new GameServerItem();
        //     void* serverIndex2 = this.m_historyServerListRequest;
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, serverIndex2, serverIndex, *(*(long*)this2 + 56L)));
         }

         public unsafe GameServerItem GetLANServerDetails(int serverIndex)
         {
             // TODO [vicent] current stub implementation
             return new GameServerItem();
        //     void* serverIndex2 = this.m_LANServerListRequest;
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, serverIndex2, serverIndex, *(*(long*)this2 + 56L)));
         }

         public unsafe GameServerItem GetFriendsServerDetails(int serverIndex)
         {
             // TODO [vicent] current stub implementation
             return new GameServerItem();
        //     void* serverIndex2 = this.m_friendsServerListRequest;
        //     ISteamMatchmakingServers* this2 = <Module>.SteamMatchmakingServers();
        //     return <Module>.MatchmakingPingResponse.GetServerDetails(calli(gameserveritem_t* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.Int32), this2, serverIndex2, serverIndex, *(*(long*)this2 + 56L)));
         }

        // public unsafe void ServerListResponded(void* serverListRequest, int server)
        // {
        //     if (serverListRequest == this.m_dedicatedServerListRequest && this.OnDedicatedServerListResponded != null)
        //     {
        //         this.OnDedicatedServerListResponded(server);
        //     }
        //     if (serverListRequest == this.m_favoritesServerListRequest && this.OnFavoritesServerListResponded != null)
        //     {
        //         this.OnFavoritesServerListResponded(server);
        //     }
        //     if (serverListRequest == this.m_historyServerListRequest && this.OnHistoryServerListResponded != null)
        //     {
        //         this.OnHistoryServerListResponded(server);
        //     }
        //     if (serverListRequest == this.m_LANServerListRequest && this.OnLANServerListResponded != null)
        //     {
        //         this.OnLANServerListResponded(server);
        //     }
        //     if (serverListRequest == this.m_friendsServerListRequest && this.OnFriendsServerListResponded != null)
        //     {
        //         this.OnFriendsServerListResponded(server);
        //     }
        // }

        // public unsafe void ServerListFailedToRespond(void* serverListRequest, int server)
        // {
        //     if (serverListRequest == this.m_dedicatedServerListRequest && this.OnDedicatedServerListFailResponse != null)
        //     {
        //         this.OnDedicatedServerListFailResponse(server);
        //     }
        //     if (serverListRequest == this.m_favoritesServerListRequest && this.OnFavoritesServerListFailResponse != null)
        //     {
        //         this.OnFavoritesServerListFailResponse(server);
        //     }
        //     if (serverListRequest == this.m_historyServerListRequest && this.OnHistoryServerListFailResponse != null)
        //     {
        //         this.OnHistoryServerListFailResponse(server);
        //     }
        //     if (serverListRequest == this.m_LANServerListRequest && this.OnLANServerListFailResponse != null)
        //     {
        //         this.OnLANServerListFailResponse(server);
        //     }
        //     if (serverListRequest == this.m_friendsServerListRequest && this.OnFriendsServerListFailResponse != null)
        //     {
        //         this.OnFriendsServerListFailResponse(server);
        //     }
        // }

        // public unsafe void RefreshCompleteResponse(void* serverListRequest, MatchMakingServerResponseEnum response)
        // {
        //     if (serverListRequest == this.m_dedicatedServerListRequest)
        //     {
        //         this.m_dedicatedServerListRequest = null;
        //         if (this.OnDedicatedServersCompleteResponse != null)
        //         {
        //             this.OnDedicatedServersCompleteResponse(response);
        //         }
        //     }
        //     if (serverListRequest == this.m_favoritesServerListRequest)
        //     {
        //         this.m_favoritesServerListRequest = null;
        //         if (this.OnFavoritesServersCompleteResponse != null)
        //         {
        //             this.OnFavoritesServersCompleteResponse(response);
        //         }
        //     }
        //     if (serverListRequest == this.m_historyServerListRequest)
        //     {
        //         this.m_historyServerListRequest = null;
        //         if (this.OnHistoryServersCompleteResponse != null)
        //         {
        //             this.OnHistoryServersCompleteResponse(response);
        //         }
        //     }
        //     if (serverListRequest == this.m_LANServerListRequest)
        //     {
        //         this.m_LANServerListRequest = null;
        //         if (this.OnLANServersCompleteResponse != null)
        //         {
        //             this.OnLANServersCompleteResponse(response);
        //         }
        //     }
        //     if (serverListRequest == this.m_friendsServerListRequest)
        //     {
        //         this.m_friendsServerListRequest = null;
        //         if (this.OnFriendsServersCompleteResponse != null)
        //         {
        //             this.OnFriendsServersCompleteResponse(response);
        //         }
        //     }
        // }

        // public unsafe void RequestServerList(string filterOps, MatchmakingServerListResponse* response)
        // {
        //     char[] delimiters = ":;".ToCharArray();
        //     string[] ops = filterOps.Split(delimiters);
        //     int num = ops.Length;
        //     if (num % 2 != 0)
        //     {
        //         throw new ArgumentException("Filter operations and their arguments must be delimited by colons or semicolons. Odd tokens are operations, even ones are their arguments");
        //     }
        //     ulong num2 = (ulong)((long)(num / 2));
        //     MatchMakingKeyValuePair_t* ptr = <Module>.new[]((num2 > 36028797018963967uL) ? 18446744073709551615uL : (num2 * 512uL));
        //     MatchMakingKeyValuePair_t* ptr3;
        //     try
        //     {
        //         if (ptr != null)
        //         {
        //             void* ptr2 = (void*)ptr;
        //             int num3 = (int)num2 - 1;
        //             if (num3 >= 0)
        //             {
        //                 do
        //                 {
        //                     ((byte*)ptr2)[256L] = 0;
        //                     *(byte*)ptr2 = 0;
        //                     ptr2 = (void*)((byte*)ptr2 + 512L);
        //                     num3 += -1;
        //                 }
        //                 while (num3 >= 0);
        //             }
        //             ptr3 = ptr;
        //         }
        //         else
        //         {
        //             ptr3 = null;
        //         }
        //     }
        //     catch
        //     {
        //         <Module>.delete[]((void*)ptr);
        //         throw;
        //     }
        //     MatchMakingKeyValuePair_t* filters = ptr3;
        //     int i = 0;
        //     if (0 < ops.Length / 2)
        //     {
        //         long num4 = 0L;
        //         int response2 = 0;
        //         do
        //         {
        //             sbyte* filterOpStr = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(ops[response2]));
        //             sbyte* filterArgStr = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(ops[response2 + 1]));
        //             MatchMakingKeyValuePair_t matchMakingKeyValuePair_t;
        //             <Module>.strncpy((sbyte*)(&matchMakingKeyValuePair_t), (sbyte*)filterOpStr, 256uL);
        //             *(ref matchMakingKeyValuePair_t + 255) = 0;
        //             <Module>.strncpy(ref matchMakingKeyValuePair_t + 256, (sbyte*)filterArgStr, 256uL);
        //             *(ref matchMakingKeyValuePair_t + 511) = 0;
        //             cpblk(num4 / (long)sizeof(MatchMakingKeyValuePair_t) + filters, ref matchMakingKeyValuePair_t, 512);
        //             Marshal.FreeHGlobal((IntPtr)((void*)filterOpStr));
        //             Marshal.FreeHGlobal((IntPtr)((void*)filterArgStr));
        //             i++;
        //             response2 += 2;
        //             num4 += 512L;
        //         }
        //         while (i < ops.Length / 2);
        //     }
        //     ISteamUtils* expr_160 = <Module>.SteamUtils();
        //     uint appId = calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_160, *(*expr_160 + 72L));
        //     if (response == this.m_dedicatedServerListResponse)
        //     {
        //         ISteamMatchmakingServers* ptr4 = <Module>.SteamMatchmakingServers();
        //         this.m_dedicatedServerListRequest = calli(System.Void* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,MatchMakingKeyValuePair_t**,System.UInt32,ISteamMatchmakingServerListResponse*), ptr4, appId, ref filters, ops.Length / 2, response, *(*(long*)ptr4));
        //     }
        //     if (response == this.m_favoritesServerListResponse)
        //     {
        //         ISteamMatchmakingServers* ptr5 = <Module>.SteamMatchmakingServers();
        //         this.m_favoritesServerListRequest = calli(System.Void* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,MatchMakingKeyValuePair_t**,System.UInt32,ISteamMatchmakingServerListResponse*), ptr5, appId, ref filters, ops.Length / 2, response, *(*(long*)ptr5 + 24L));
        //     }
        //     if (response == this.m_historyServerListResponse)
        //     {
        //         ISteamMatchmakingServers* ptr6 = <Module>.SteamMatchmakingServers();
        //         this.m_historyServerListRequest = calli(System.Void* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,MatchMakingKeyValuePair_t**,System.UInt32,ISteamMatchmakingServerListResponse*), ptr6, appId, ref filters, ops.Length / 2, response, *(*(long*)ptr6 + 32L));
        //     }
        //     if (response == this.m_friendsServerListResponse)
        //     {
        //         ISteamMatchmakingServers* ptr7 = <Module>.SteamMatchmakingServers();
        //         this.m_friendsServerListRequest = calli(System.Void* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,MatchMakingKeyValuePair_t**,System.UInt32,ISteamMatchmakingServerListResponse*), ptr7, appId, ref filters, ops.Length / 2, response, *(*(long*)ptr7 + 16L));
        //     }
        //     <Module>.delete[]((void*)filters);
        // }

        // public unsafe int GetFavoriteGameCount()
        // {
        //     ISteamMatchmaking* expr_05 = <Module>.SteamMatchmaking();
        //     return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05));
        // }

        // public unsafe bool GetFavoriteGame(uint index, out uint appID, out uint IP, out ushort connPort, out ushort queryPort, out FavoriteEnum flags, out uint timestamp)
        // {
        //     timestamp = 0;
        //     flags = 0;
        //     queryPort = 0;
        //     IP = 0;
        //     appID = 0;
        //     uint nFlags = 0u;
        //     ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
        //     bool ret = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32,System.UInt32*,System.UInt32*,System.UInt16*,System.UInt16*,System.UInt32*,System.UInt32*), this2, index, ref appID, ref IP, ref connPort, ref queryPort, ref nFlags, ref timestamp, *(*(long*)this2 + 8L));
        //     flags = (FavoriteEnum)nFlags;
        //     return ret;
        // }

         public unsafe int AddFavoriteGame(uint appID, uint IP, ushort connPort, ushort queryPort, FavoriteEnum flags, uint timestamp)
         {
             // TODO [vicent] current stub implementation
             return 0;
        //     ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
        //     return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,System.UInt32,System.UInt16,System.UInt16,System.UInt32,System.UInt32), this2, appID, IP, connPort, queryPort, flags, timestamp, *(*(long*)this2 + 16L));
         }

         public unsafe bool RemoveFavoriteGame(uint appID, uint IP, ushort connPort, ushort queryPort, FavoriteEnum flags)
         {
             // TODO [vicent] current stub implementation
             return false;
             //ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
             //return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,System.UInt32,System.UInt16,System.UInt16,System.UInt32), this2, appID, IP, connPort, queryPort, flags, *(*(long*)this2 + 24L));
         }

        // [HandleProcessCorruptedStateExceptions]
        //protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool A_0)
        protected override void Dispose(bool A_0)
        {
            // TODO [vicent] current stub implementation
            return;
        //     if (A_0)
        //     {
        //         try
        //         {
        //             this.~SteamAPI();
        //             return;
        //         }
        //         finally
        //         {
        //             base.Dispose(true);
        //         }
        //     }
        //     base.Dispose(false);
        }
    }

   
    public class SteamServerAPI : SteamAPIBase
    {
        private GameServer m_gameserver = null;

        //private HTTP m_http;

        private static SteamServerAPI m_instance;

        public GameServer GameServer
        {
            get
            {
                return this.m_gameserver;
            }
        }

        public static SteamServerAPI Instance
        {
            get
            {
                if (SteamServerAPI.m_instance == null)
                {
                    SteamServerAPI.m_instance = new SteamServerAPI();
                }
                return SteamServerAPI.m_instance;
            }
        }

        private SteamServerAPI()
            : base(true)
        {
        //     Peer2Peer.Init(true);
        //     try
        //     {
        //         this.m_gameserver = new GameServer();
        //         this.m_http = new HTTP();
        //     }
        //     catch
        //     {
        //         base.Dispose(true);
        //         throw;
        //     }
        }

        // private unsafe void ~SteamServerAPI()
        // {
        //     if (<Module>.SteamGameServer() != null)
        //     {
        //         ISteamGameServer* ptr = <Module>.SteamGameServer();
        //         calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), ptr, 0, *(*(long*)ptr + 312L));
        //     }
        //     if (<Module>.SteamGameServer() != null)
        //     {
        //         ISteamGameServer* expr_2A = <Module>.SteamGameServer();
        //         calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_2A, *(*expr_2A + 56L));
        //     }
        //     this.m_gameserver.Shutdown();
        //     if (SteamServerAPI.m_instance == null)
        //     {
        //         throw new InvalidOperationException("Server is not initialized!");
        //     }
        //     SteamServerAPI.m_instance = null;
        //     IDisposable http = this.m_http;
        //     if (http != null)
        //     {
        //         http.Dispose();
        //     }
        //     IDisposable this2 = this.m_gameserver;
        //     if (this2 != null)
        //     {
        //         this2.Dispose();
        //     }
        //     this.m_gameserver = null;
        // }

        // [HandleProcessCorruptedStateExceptions]
        // protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool A_0)
        // {
        //     if (A_0)
        //     {
        //         try
        //         {
        //             this.~SteamServerAPI();
        //             return;
        //         }
        //         finally
        //         {
        //             base.Dispose(true);
        //         }
        //     }
        //     base.Dispose(false);
        // }
    }
}

#endif