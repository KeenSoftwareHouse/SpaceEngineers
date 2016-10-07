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

    public class ChatMessageBuffer
    {
        public static int MAX_MESSAGE_SIZE = 2048;

        public readonly StringBuilder Text;

        public ChatMessageBuffer()
        {
            this.Text = new StringBuilder(2048);
        }

        internal unsafe void ToNative()
        {
            
        }

        internal unsafe void ToManaged(int charCount)
        {
            
        }
    }

    public delegate void CompletedDelegate<TData>(TData data, Result result);

    public delegate void ConnectionFailed(ulong remoteUserId, P2PSessionErrorEnum error);

    public struct CreateItemResult
    {
        public Result Result;

        public ulong PublishedFileId;

        public bool UserNeedsToAcceptWorkshopLegalAgreement;
    }

    public delegate void DataReceived(HTTPRequestDataReceived data);

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Empty
    {
    }

    public enum FavoriteEnum : uint
    {
        None,
        Favorite,
        History
    }

    public class Friends
    {
        private List<UserGroupInfo> m_userGroups;

        internal unsafe Friends()
        {
            this.m_userGroups = new List<UserGroupInfo>();
        }

        public unsafe bool HasFriend(ulong userId)
        {
            return false;
        }

        public unsafe void GetPersonaName(ulong userId, StringBuilder result)
        {
            return;
        }

        public unsafe string GetPersonaName(ulong userId)
        {
            return "";
        }

        public unsafe bool GetPersonaNameHistory(ulong userId, int index, StringBuilder result)
        {
            return false;
        }

        public bool IsUserInGroup(ulong groupId)
        {
            if (groupId == 0uL)
            {
                return true;
            }
            int i = 0;
            if (0 < this.m_userGroups.Count)
            {
                while (this.m_userGroups[i].SteamID != groupId)
                {
                    i++;
                    if (i >= this.m_userGroups.Count)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public unsafe string GetClanName(ulong steamID)
        {
            return "";
        }
    }

    public delegate void GameDataReceived(byte[] data, int dataSize, IPEndPoint sender);

    public class GameServer // : IDisposable
    {
        private string m_productName;

        private string m_gameDescription;

        //private Thread m_masterServerUpdaterThread;

        //private Socket m_gameSocket;

        public event UserGroupStatus UserGroupStatus;
        
        public event GameDataReceived GameDataReceived;
        
        public event ValidateAuthTicketResponse ValidateAuthTicketResponse;
        
        public event PolicyResponse PolicyResponse;
        
        public event ServersDisconnected ServersDisconnected;
        
        public event ServersConnectFailure ServersConnectFailure;
        
        public event ServersConnected ServersConnected;
        
        public unsafe string GameDescription
        {
            get
            {
                return this.m_gameDescription;
            }
            set
            {
                this.m_gameDescription = value;
            }
        }

        public unsafe string ProductName
        {
            get
            {
                return this.m_productName;
            }
            set
            {
                this.m_productName = value;
            }
        }

        protected void raise_ServersConnected()
        {
            // TODO [vicent] current stub implementation
            // ServersConnected serversConnected = this.<backing_store>ServersConnected;
             ServersConnected serversConnected = this.ServersConnected;
             if (serversConnected != null)
             {
                 serversConnected();
             }
        }

        protected void raise_ServersConnectFailure(Result value0)
        {
            // TODO [vicent] current stub implementation
            // ServersConnectFailure serversConnectFailure = this.<backing_store>ServersConnectFailure;
             ServersConnectFailure serversConnectFailure = this.ServersConnectFailure;
             if (serversConnectFailure != null)
             {
                 serversConnectFailure(value0);
             }
        }

        protected void raise_ServersDisconnected(Result value0)
        {
            // TODO [vicent] current stub implementation
            // ServersDisconnected serversDisconnected = this.<backing_store>ServersDisconnected;
            ServersDisconnected serversDisconnected = this.ServersDisconnected;
            if (serversDisconnected != null)
            {
                serversDisconnected(value0);
            }
        }

        protected void raise_PolicyResponse(sbyte value0)
        {
            // TODO [vicent] current stub implementation
            // PolicyResponse policyResponse = this.<backing_store>PolicyResponse;
            PolicyResponse policyResponse = this.PolicyResponse;
            if (policyResponse != null)
            {
                policyResponse(value0);
            }
        }

        protected void raise_ValidateAuthTicketResponse(ulong value0, AuthSessionResponseEnum value1, ulong value2)
        {
            // TODO [vicent] current stub implementation
            // ValidateAuthTicketResponse validateAuthTicketResponse = this.<backing_store>ValidateAuthTicketResponse;
             ValidateAuthTicketResponse validateAuthTicketResponse = this.ValidateAuthTicketResponse;
             if (validateAuthTicketResponse != null)
             {
                 validateAuthTicketResponse(value0, value1, value2);
             }
        }

        protected void raise_GameDataReceived(byte[] value0, int value1, IPEndPoint value2)
        {
            // TODO [vicent] current stub implementation
             //GameDataReceived gameDataReceived = this.<backing_store>GameDataReceived;
            GameDataReceived gameDataReceived = this.GameDataReceived;
             if (gameDataReceived != null)
             {
                 gameDataReceived(value0, value1, value2);
             }
        }

        protected void raise_UserGroupStatus(ulong value0, ulong value1, [MarshalAs(UnmanagedType.U1)] bool value2, [MarshalAs(UnmanagedType.U1)] bool value3)
        {
            // TODO [vicent] current stub implementation
             // UserGroupStatus userGroupStatus = this.<backing_store>UserGroupStatus;
            UserGroupStatus userGroupStatus = this.UserGroupStatus;
            if (userGroupStatus != null)
             {
                 userGroupStatus(value0, value1, value2, value3);
             }
        }

        public GameServer()
        {
            // CallbackHolder<1> this2 = new CallbackHolder<1>();
            // try
            // {
            //     this.m_holder = this2;
            //     base..ctor();
            //     this.m_holder.AddNative<SteamSDK::GameServer,SteamServersConnected_t>(this, ldftn(serversConnected));
            //     this.m_holder.AddNative<SteamSDK::GameServer,SteamServerConnectFailure_t>(this, ldftn(serversConnectFailure));
            //     this.m_holder.AddNative<SteamSDK::GameServer,SteamServersDisconnected_t>(this, ldftn(serversDisconnected));
            //     this.m_holder.AddNative<SteamSDK::GameServer,GSPolicyResponse_t>(this, ldftn(policyResponse));
            //     this.m_holder.AddNative<SteamSDK::GameServer,ValidateAuthTicketResponse_t>(this, ldftn(validateAuthTicketResponse));
            //     this.m_holder.AddNative<SteamSDK::GameServer,GSClientGroupStatus_t>(this, ldftn(userGroupStatus));
            // }
            // catch
            // {
            //     ((IDisposable)this.m_holder).Dispose();
            //     throw;
            // }
        }

        //public unsafe static void userGroupStatus(GameServer owner, GSClientGroupStatus_t* data)
        //{
        //    return;
        //}

        //public unsafe static void serversConnected(GameServer owner, SteamServersConnected_t* data)
        //{
        //    return;
        //}

        //public unsafe static void serversConnectFailure(GameServer owner, SteamServerConnectFailure_t* data)
        //{
        //    return;
        //}

        //public unsafe static void serversDisconnected(GameServer owner, SteamServersDisconnected_t* data)
        //{
        //    return;
        //}

        //public unsafe static void policyResponse(GameServer owner, GSPolicyResponse_t* data)
        //{
        //    return;
        //}

        //public unsafe static void validateAuthTicketResponse(GameServer owner, ValidateAuthTicketResponse_t* data)
        //{
        //    return;
        //}

        //public unsafe static void HandleIncomingPacket(IntPtr data, int dataSize, uint sourceIp, ushort sourcePort)
        //{
        //    return;
        //}

        public unsafe static int GetNextOutgoingPacket(IntPtr data, int dataSize, out uint targetIp, out ushort targetPort)
        {
            targetIp = 0;
            targetPort = 0;
            return 0;
        }

        public ServerStartResult Start(IPEndPoint serverEndpoint, ushort steamPort, ServerMode serverMode, string versionString, bool gameSocketShare)
        {
            byte[] verPtr_12_cp_0 = Encoding.UTF8.GetBytes(versionString);
            //int verPtr_12_cp_1 = 0;
            if (!gameSocketShare && !this.TestSocket(serverEndpoint))
            {
                return ServerStartResult.PortAlreadyUsed;
            }
            uint arg_4E_0 = serverEndpoint.Address.ToIPv4NetworkOrder();
            int serverEndpoint2;
            if (gameSocketShare)
            {
                serverEndpoint2 = 65535;
            }
            else
            {
                serverEndpoint2 = serverEndpoint.Port + 1;
            }

            //if (<Module>.SteamGameServer_Init(arg_4E_0, steamPort, (ushort)serverEndpoint.Port, (ushort)serverEndpoint2, (EServerMode)serverMode, ref verPtr_12_cp_0[verPtr_12_cp_1]) != null)
            //{
            //    if (gameSocketShare)
            //    {
            //        this.m_masterServerThreadExit = false;
            //        Thread this2 = new Thread(new ParameterizedThreadStart(this.MasterServerUpdaterThread));
            //        this.m_masterServerUpdaterThread = this2;
            //        this2.IsBackground = true;
            //        this.m_masterServerUpdaterThread.Start(serverEndpoint);
            //    }
            //    Peer2Peer.NetworkingEnabled = true;
            //    return ServerStartResult.OK;
            //}
            return ServerStartResult.UnknownError;
        }

        public unsafe ServerStartResult StartPure(uint ip, ushort steamPort, ushort gamePort, ServerMode serverMode, string versionString)
        {
            return ServerStartResult.UnknownError;
        }

        public unsafe void Shutdown()
        {
            return;
        }

        public void RunCallbacks()
        {
            //<Module>.SteamGameServer_RunCallbacks();
        }

        public Socket CreateSocket(EndPoint endPoint)
        {
            Socket endPoint2;
            try
            {
                Socket gameSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                gameSocket.Bind(endPoint);
                return gameSocket;
            }
            catch (Exception)
            {
                endPoint2 = null;
            }
            return endPoint2;
        }

        public bool TestSocket(EndPoint endPoint)
        {
            Socket gameSocket = this.CreateSocket(endPoint);
            if (gameSocket != null)
            {
                ((IDisposable)gameSocket).Dispose();
                return true;
            }
            return false;
        }

        public unsafe void MasterServerUpdaterThread(object o)
        {
            return;
        }

        public unsafe void SetKeyValue(string key, string value)
        {
            return;
        }

        public unsafe void ClearAllKeyValues()
        {
            return;
        }

        public unsafe void SetGameTags(string tags)
        {
            return;
        }

        public unsafe void SetGameData(string data)
        {
            return;
        }

        public unsafe void SetModDir(string dir)
        {
            return;
        }

        public unsafe void SetDedicated([MarshalAs(UnmanagedType.U1)] bool dedicated)
        {
            return;
        }

        public unsafe void SetMapName(string mapName)
        {
            return;
        }

        public unsafe void SetServerName(string serverName)
        {
            return;
        }

        public unsafe void SetMaxPlayerCount(int count)
        {
            return;
        }

        public unsafe void SetBotPlayerCount(int count)
        {
            return;
        }

        public unsafe void SetPasswordProtected([MarshalAs(UnmanagedType.U1)] bool passwordProtected)
        {
            return;
        }

        public unsafe void LogOnAnonymous()
        {
            return;
        }

        public unsafe void LogOff()
        {
            return;
        }

        public unsafe ulong GetSteamID()
        {
            return 0;
        }

        public unsafe uint GetPublicIP()
        {
            return 0;
        }

        public unsafe void EnableHeartbeats([MarshalAs(UnmanagedType.U1)] bool enable)
        {
            return;
        }

        public unsafe AuthSessionResponseEnum BeginAuthSession(ulong steamID, byte[] token)
        {
            return AuthSessionResponseEnum.AuthTicketInvalid;
        }

        public unsafe void EndAuthSession(ulong steamID)
        {
            return;
        }

        public unsafe void SendUserDisconnect(ulong steamID)
        {
            return;
        }

        public unsafe void BUpdateUserData(ulong steamID, string playerName, int score)
        {
            return;
        }

        public unsafe bool RequestGroupStatus(ulong userId, ulong groupId)
        {
            return false;
        }

        //public sealed override void Dispose()
        //{
        //    //this.Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
    }

    public delegate void HeadersReceived(HTTPRequestHeadersReceived data);

    public class HTTP // : IDisposable
    {
        //private static HeadersReceived <backing_store>HeadersReceived;

        //private static DataReceived <backing_store>DataReceived;

        //internal readonly CallbackHolder<1> m_holder_server;


        public static event DataReceived DataReceived;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         HTTP.<backing_store>DataReceived = (DataReceived)Delegate.Combine(HTTP.<backing_store>DataReceived, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         HTTP.<backing_store>DataReceived = (DataReceived)Delegate.Remove(HTTP.<backing_store>DataReceived, value);
        //     }
        // }

        //public static event HeadersReceived HeadersReceived;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         HTTP.<backing_store>HeadersReceived = (HeadersReceived)Delegate.Combine(HTTP.<backing_store>HeadersReceived, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         HTTP.<backing_store>HeadersReceived = (HeadersReceived)Delegate.Remove(HTTP.<backing_store>HeadersReceived, value);
        //     }
        // }




        protected static void raise_HeadersReceived(HTTPRequestHeadersReceived value0)
        {
            // HeadersReceived headersReceived = HTTP.<backing_store>HeadersReceived;
            // if (headersReceived != null)
            // {
            //     headersReceived(value0);
            // }
        }

        protected static void raise_DataReceived(HTTPRequestDataReceived value0)
        {
            // DataReceived dataReceived = HTTP.<backing_store>DataReceived;
            if (DataReceived != null)
            {
                DataReceived(value0);
            }
        }

        public unsafe static uint CreateHTTPRequest(HTTPMethod method, string absoluteURL)
        {
            return 0;
        }

        public unsafe static bool SetHTTPRequestContextValue(uint request, ulong contextValue)
        {
            return false;
        }

        public unsafe static bool SetHTTPRequestNetworkActivityTimeout(uint request, uint timeoutSeconds)
        {
            return false;
        }

        public unsafe static bool SetHTTPRequestHeaderValue(uint request, string headerName, string headerValue)
        {
            return false;
        }

        public unsafe static bool SetHTTPRequestGetOrPostParameter(uint request, string paramName, string paramValue)
        {
            return false;
        }

        public unsafe static bool SendHTTPRequest(uint request, Action<bool, HTTPRequestCompleted> onCallResult)
        {
            return false;
        }

        public unsafe static bool SendHTTPRequestAndStreamResponse(uint request, Action<bool, HTTPRequestCompleted> onCallResult)
        {
            return false;
        }

        public unsafe static bool DeferHTTPRequest(uint request)
        {
            return false;
        }

        public unsafe static bool PrioritizeHTTPRequest(uint request)
        {
            return false;
        }

        public unsafe static bool GetHTTPResponseHeaderSize(uint request, string headerName, out uint responseHeaderSize)
        {
            responseHeaderSize = 0;
            return false;
        }

        public unsafe static bool GetHTTPResponseHeaderValue(uint request, string headerName, byte[] headerValueBuffer, uint bufferSize)
        {
            return false;
        }

        public unsafe static bool GetHTTPResponseBodySize(uint request, out uint bodySize)
        {
            bodySize = 0;
            return false;
        }

        public unsafe static bool GetHTTPResponseBodyData(uint request, byte[] bodyDataBuffer, uint bufferSize)
        {
            return false;
        }

        public unsafe static bool GetHTTPStreamingResponseBodyData(uint request, uint offset, byte[] bodyDataBuffer, uint bufferSize)
        {
            return false;
        }

        public unsafe static bool ReleaseHTTPRequest(uint request)
        {
            return false;
        }

        public unsafe static bool GetHTTPDownloadProgressPct(uint request, out float percentOut)
        {
            percentOut = 0;
            return false;
        }

        public unsafe static bool SetHTTPRequestRawPostBody(uint request, string contentType, byte[] body, uint bodyLen)
        {
            return false;
        }

        internal HTTP()
        {
            // CallbackHolder<1> this2 = new CallbackHolder<1>();
            // try
            // {
            //     this.m_holder_server = this2;
            //     base..ctor();
            //     this.m_holder_server.AddNative<SteamSDK::HTTP,HTTPRequestHeadersReceived_t>(this, ldftn(headersReceived));
            //     this.m_holder_server.AddNative<SteamSDK::HTTP,HTTPRequestDataReceived_t>(this, ldftn(dataReceived));
            // }
            // catch
            // {
            //     ((IDisposable)this.m_holder_server).Dispose();
            //     throw;
            // }
        }

        // internal unsafe static void headersReceived(HTTP owner, HTTPRequestHeadersReceived_t* result)
        // {
        // }

        // internal unsafe static void dataReceived(HTTP owner, HTTPRequestDataReceived_t* result)
        // {
        // }

        // internal unsafe static void OnSendHTTPRequest(HTTPRequestCompleted_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, HTTPRequestCompleted> action)
        // {
        // }

        // internal unsafe static void OnSendHTTPRequestAndStreamResponse(HTTPRequestCompleted_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, HTTPRequestCompleted> action)
        // {
        // }

        //[HandleProcessCorruptedStateExceptions]
        //protected virtual void Dispose(bool A_0)
        //{
        //    // if (A_0)
        //    // {
        //    //     try
        //    //     {
        //    //         return;
        //    //     }
        //    //     finally
        //    //     {
        //    //         ((IDisposable)this.m_holder_server).Dispose();
        //    //     }
        //    // }
        //    // base.Finalize();
        //}

        //public sealed override void Dispose()
        //{
        //    this.Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
    }






















    
    
    public class Matchmaking // : IDisposable
    {
        // internal readonly CallbackHolder<0> m_holder;

        // private LobbyChatUpdateDelegate <backing_store>LobbyChatUpdate;

        // private LobbyDataUpdate <backing_store>LobbyDataUpdate;

        // private LobbyJoinRequestDelegate <backing_store>LobbyJoinRequest;

        // private LobbyChatMsgDelegate <backing_store>LobbyChatMsg;

        // private ServerChangeRequestDelegate <backing_store>ServerChangeRequest;

        public event ServerChangeRequestDelegate ServerChangeRequest;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         this.<backing_store>ServerChangeRequest = (ServerChangeRequestDelegate)Delegate.Combine(this.<backing_store>ServerChangeRequest, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         this.<backing_store>ServerChangeRequest = (ServerChangeRequestDelegate)Delegate.Remove(this.<backing_store>ServerChangeRequest, value);
        //     }
        // }

        public event LobbyChatMsgDelegate LobbyChatMsg;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         this.<backing_store>LobbyChatMsg = (LobbyChatMsgDelegate)Delegate.Combine(this.<backing_store>LobbyChatMsg, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         this.<backing_store>LobbyChatMsg = (LobbyChatMsgDelegate)Delegate.Remove(this.<backing_store>LobbyChatMsg, value);
        //     }
        // }

        public event LobbyJoinRequestDelegate LobbyJoinRequest;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         this.<backing_store>LobbyJoinRequest = (LobbyJoinRequestDelegate)Delegate.Combine(this.<backing_store>LobbyJoinRequest, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         this.<backing_store>LobbyJoinRequest = (LobbyJoinRequestDelegate)Delegate.Remove(this.<backing_store>LobbyJoinRequest, value);
        //     }
        // }

        public event LobbyDataUpdate LobbyDataUpdate;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         this.<backing_store>LobbyDataUpdate = (LobbyDataUpdate)Delegate.Combine(this.<backing_store>LobbyDataUpdate, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         this.<backing_store>LobbyDataUpdate = (LobbyDataUpdate)Delegate.Remove(this.<backing_store>LobbyDataUpdate, value);
        //     }
        // }

        public event LobbyChatUpdateDelegate LobbyChatUpdate;
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         this.<backing_store>LobbyChatUpdate = (LobbyChatUpdateDelegate)Delegate.Combine(this.<backing_store>LobbyChatUpdate, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         this.<backing_store>LobbyChatUpdate = (LobbyChatUpdateDelegate)Delegate.Remove(this.<backing_store>LobbyChatUpdate, value);
        //     }
        // }

        // internal Matchmaking()
        // {
        //     CallbackHolder<0> this2 = new CallbackHolder<0>();
        //     try
        //     {
        //         this.m_holder = this2;
        //         base..ctor();
        //         this.m_holder.AddNative<SteamSDK::Matchmaking,LobbyChatUpdate_t>(this, ldftn(lobbyChatUpdate));
        //         this.m_holder.AddNative<SteamSDK::Matchmaking,LobbyDataUpdate_t>(this, ldftn(lobbyDataUpdate));
        //         this.m_holder.AddNative<SteamSDK::Matchmaking,GameLobbyJoinRequested_t>(this, ldftn(lobbyJoinRequest));
        //         this.m_holder.AddNative<SteamSDK::Matchmaking,LobbyChatMsg_t>(this, ldftn(lobbyChatMsg));
        //         this.m_holder.AddNative<SteamSDK::Matchmaking,GameServerChangeRequested_t>(this, ldftn(serverChangeRequest));
        //     }
        //     catch
        //     {
        //         ((IDisposable)this.m_holder).Dispose();
        //         throw;
        //     }
        // }

        // internal unsafe static void lobbyChatUpdate(Matchmaking owner, LobbyChatUpdate_t* data)
        // {
        //     Lobby data2;
        //     data2.LobbyId = (ulong)(*(long*)data);
        //     LobbyChatUpdateDelegate owner2 = owner.<backing_store>LobbyChatUpdate;
        //     if (owner2 != null)
        //     {
        //         owner2(data2, (ulong)(*(long*)(data + 8L / (long)sizeof(LobbyChatUpdate_t))), (ulong)(*(long*)(data + 16L / (long)sizeof(LobbyChatUpdate_t))), (ChatMemberStateChangeEnum)(*(int*)(data + 24L / (long)sizeof(LobbyChatUpdate_t))));
        //     }
        // }

        // internal unsafe static void lobbyDataUpdate(Matchmaking owner, LobbyDataUpdate_t* data)
        // {
        //     Lobby lobby;
        //     lobby.LobbyId = (ulong)(*(long*)data);
        //     byte data2 = (*(byte*)(data + 16L / (long)sizeof(LobbyDataUpdate_t)) != 0) ? 1 : 0;
        //     LobbyDataUpdate owner2 = owner.<backing_store>LobbyDataUpdate;
        //     if (owner2 != null)
        //     {
        //         owner2(data2 != 0, lobby, (ulong)(*(long*)(data + 8L / (long)sizeof(LobbyDataUpdate_t))));
        //     }
        // }

        // internal unsafe static void lobbyJoinRequest(Matchmaking owner, GameLobbyJoinRequested_t* data)
        // {
        //     Lobby data2;
        //     data2.LobbyId = (ulong)(*(long*)data);
        //     LobbyJoinRequestDelegate owner2 = owner.<backing_store>LobbyJoinRequest;
        //     if (owner2 != null)
        //     {
        //         owner2(data2, (ulong)(*(long*)(data + 8L / (long)sizeof(GameLobbyJoinRequested_t))));
        //     }
        // }

        // internal unsafe static void lobbyChatMsg(Matchmaking owner, LobbyChatMsg_t* data)
        // {
        //     Lobby data2;
        //     data2.LobbyId = (ulong)(*(long*)data);
        //     LobbyChatMsgDelegate owner2 = owner.<backing_store>LobbyChatMsg;
        //     if (owner2 != null)
        //     {
        //         owner2(data2, (ulong)(*(long*)(data + 8L / (long)sizeof(LobbyChatMsg_t))), *(byte*)(data + 16L / (long)sizeof(LobbyChatMsg_t)), (uint)(*(int*)(data + 20L / (long)sizeof(LobbyChatMsg_t))));
        //     }
        // }

        // internal unsafe static void serverChangeRequest(Matchmaking owner, GameServerChangeRequested_t* data)
        // {
        //     string password = new string((sbyte*)(data + 64L / (long)sizeof(GameServerChangeRequested_t)));
        //     string data2 = new string((sbyte*)data);
        //     ServerChangeRequestDelegate owner2 = owner.<backing_store>ServerChangeRequest;
        //     if (owner2 != null)
        //     {
        //         owner2(data2, password);
        //     }
        // }

         protected void raise_LobbyChatUpdate(Lobby value0, ulong value1, ulong value2, ChatMemberStateChangeEnum value3)
         {
             //LobbyChatUpdateDelegate lobbyChatUpdateDelegate = this.<backing_store>LobbyChatUpdate;
             LobbyChatUpdateDelegate lobbyChatUpdateDelegate = this.LobbyChatUpdate;
             if (lobbyChatUpdateDelegate != null)
             {
                 lobbyChatUpdateDelegate(value0, value1, value2, value3);
             }
         }

         protected void raise_LobbyDataUpdate(bool value0, Lobby value1, ulong value2)
         {
             // TODO [vicent] current stub implementation
             //LobbyDataUpdate lobbyDataUpdate = this.<backing_store>LobbyDataUpdate;
             LobbyDataUpdate lobbyDataUpdate = this.LobbyDataUpdate;
             if (lobbyDataUpdate != null)
             {
                 lobbyDataUpdate(value0, value1, value2);
             }
         }

         protected void raise_LobbyJoinRequest(Lobby value0, ulong value1)
         {
             // TODO [vicent] current stub implementation
             //LobbyJoinRequestDelegate lobbyJoinRequestDelegate = this.<backing_store>LobbyJoinRequest;
             LobbyJoinRequestDelegate lobbyJoinRequestDelegate = this.LobbyJoinRequest;
             if (lobbyJoinRequestDelegate != null)
             {
                 lobbyJoinRequestDelegate(value0, value1);
             }
         }

         protected void raise_LobbyChatMsg(Lobby value0, ulong value1, byte value2, uint value3)
         {
             // TODO [vicent] current stub implementation
             // LobbyChatMsgDelegate lobbyChatMsgDelegate = this.<backing_store>LobbyChatMsg;
             LobbyChatMsgDelegate lobbyChatMsgDelegate = this.LobbyChatMsg;
             if (lobbyChatMsgDelegate != null)
             {
                 lobbyChatMsgDelegate(value0, value1, value2, value3);
             }
         }

         protected void raise_ServerChangeRequest(string value0, string value1)
         {
             // TODO [vicent] current stub implementation
             //ServerChangeRequestDelegate serverChangeRequestDelegate = this.<backing_store>ServerChangeRequest;
             ServerChangeRequestDelegate serverChangeRequestDelegate = this.ServerChangeRequest;
             if (serverChangeRequestDelegate != null)
             {
                 serverChangeRequestDelegate(value0, value1);
             }
         }

        // public void ~Matchmaking()
        // {
        // }

        // [HandleProcessCorruptedStateExceptions]
        // protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool A_0)
        // {
        //     if (A_0)
        //     {
        //         try
        //         {
        //             return;
        //         }
        //         finally
        //         {
        //             ((IDisposable)this.m_holder).Dispose();
        //         }
        //     }
        //     base.Finalize();
        // }

        // public sealed override void Dispose()
        // {
        //     this.Dispose(true);
        //     GC.SuppressFinalize(this);
        // }
    }




}

#endif