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
    public enum AuthSessionResponseEnum
    {
        OK,
        UserNotConnectedToSteam,
        NoLicenseOrExpired,
        VACBanned,
        LoggedInElseWhere,
        VACCheckTimedOut,
        AuthTicketCanceled,
        AuthTicketInvalidAlreadyUsed,
        AuthTicketInvalid
    }

    public enum AccountType
    {
        Invalid,
        Individual,
        Multiseat,
        GameServer,
        AnonGameServer,
        Pending,
        ContentServer,
        Clan,
        Chat,
        ConsoleUser,
        AnonUser
    }

    public enum Result
    {
        OK = 1,
        Fail,
        NoConnection,
        InvalidPassword = 5,
        LoggedInElsewhere,
        InvalidProtocolVer,
        InvalidParam,
        FileNotFound,
        Busy,
        InvalidState,
        InvalidName,
        InvalidEmail,
        DuplicateName,
        AccessDenied,
        Timeout,
        Banned,
        AccountNotFound,
        InvalidSteamID,
        ServiceUnavailable,
        NotLoggedOn,
        Pending,
        EncryptionFailure,
        InsufficientPrivilege,
        LimitExceeded,
        Revoked,
        Expired,
        AlreadyRedeemed,
        DuplicateRequest,
        AlreadyOwned,
        IPNotFound,
        PersistFailed,
        LockingFailed,
        LogonSessionReplaced,
        ConnectFailed,
        HandshakeFailed,
        IOFailure,
        RemoteDisconnect,
        ShoppingCartNotFound,
        Blocked,
        Ignored,
        NoMatch,
        AccountDisabled,
        ServiceReadOnly,
        AccountNotFeatured,
        AdministratorOK,
        ContentVersion,
        TryAnotherCM,
        PasswordRequiredToKickSession,
        AlreadyLoggedInElsewhere,
        Suspended,
        Cancelled,
        DataCorruption,
        DiskFull,
        RemoteCallFailed,
        PasswordUnset,
        ExternalAccountUnlinked,
        PSNTicketInvalid,
        ExternalAccountAlreadyLinked,
        RemoteFileConflict,
        IllegalPassword,
        SameAsPreviousValue,
        AccountLogonDenied,
        CannotUseOldPassword,
        InvalidLoginAuthCode,
        AccountLogonDeniedNoMail,
        HardwareNotCapableOfIPT,
        IPTInitError,
        ParentalControlRestricted,
        FacebookQueryError,
        ExpiredLoginAuthCode,
        IPLoginRestrictionFailed,
        AccountLockedDown,
        AccountLogonDeniedVerifiedEmailRequired,
        NoMatchingURL,
        BadResponse,
        RequirePasswordReEntry,
        ValueOutOfRange
    }

        public enum HTTPMethod
    {
        Invalid,
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
        OPTIONS
    }

    public struct HTTPRequestCompleted
    {
        public uint Request;

        public ulong ContextValue;

        public bool RequestSuccessful;

        public HTTPStatusCode StatusCode;
    }

    public struct HTTPRequestDataReceived
    {
        public uint Request;

        public ulong ContextValue;

        public uint Offset;

        public uint BytesReceived;
    }

    public struct HTTPRequestHeadersReceived
    {
        public uint Request;

        public ulong ContextValue;
    }

    public enum HTTPStatusCode
    {
        Invalid,
        Continue = 100,
        SwitchingProtocols,
        OK = 200,
        Created,
        Accepted,
        NonAuthoritative,
        NoContent,
        ResetContent,
        PartialContent,
        MultipleChoices = 300,
        MovedPermanently,
        Found,
        SeeOther,
        NotModified,
        UseProxy,
        Unused,
        TemporaryRedirect,
        BadRequest = 400,
        Unauthorized,
        PaymentRequired,
        Forbidden,
        NotFound,
        MethodNotAllowed,
        NotAcceptable,
        ProxyAuthRequired,
        RequestTimeout,
        Conflict,
        Gone,
        LengthRequired,
        PreconditionFailed,
        RequestEntityTooLarge,
        RequestURITooLong,
        UnsupportedMediaType,
        RequestedRangeNotSatisfiable,
        ExpectationFailed,
        IAmATeapot,
        InternalServerError = 500,
        NotImplemented,
        BadGateway,
        ServiceUnavailable,
        GatewayTimeout,
        HTTPVersionNotSupported
    }

    public enum ItemUpdateStatus
    {
        Invalid,
        PreparingConfig,
        PreparingContent,
        UploadingContent,
        UploadingPreviewFile,
        CommittingChanges
    }

    public enum NotificationPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum P2PMessageEnum
    {
        Unreliable,
        UnreliableNoDelay,
        Reliable,
        ReliableWithBuffering
    }

    public enum P2PSessionErrorEnum
    {
        None,
        NotRunningApp,
        NoRightsToApp,
        DestinationNotLoggedIn,
        Timeout,
        Max
    }

    public enum WorkshopFileType
    {
        Community,
        Microtransaction,
        Collection,
        Art,
        Video,
        Screenshot,
        Game,
        Software,
        Concept,
        WebGuide,
        IntegratedGuide,
        Merch,
        ControllerBinding,
        SteamworksAccessInvite
    }

    public enum ChatEntryTypeEnum
    {
        Invalid,
        ChatMsg,
        Typing,
        InviteGame,
        Emote,
        LeftConversation = 6,
        Entered,
        WasKicked,
        WasBanned,
        Disconnected,
        HistoricalChat
    }

    public class UserGroupInfo
    {
        public ulong SteamID;

        public string Name;

        public string Tag;
    }


    [Flags]
    public enum ChatMemberStateChangeEnum
    {
        Entered = 1,
        Left = 2,
        Disconnected = 4,
        Kicked = 8,
        Banned = 16
    }

        public enum UGCMatchingUGCType
    {
        Items,
        Items_Mtx,
        Items_ReadyToUse,
        Collections,
        Artwork,
        Videos,
        Screenshots,
        AllGuides,
        WebGuides,
        IntegratedGuides,
        UsableInGame,
        ControllerBindings
    }

    public enum UGCQuery
    {
        RankedByVote,
        RankedByPublicationDate,
        AcceptedForGameRankedByAcceptanceDate,
        RankedByTrend,
        FavoritedByFriendsRankedByPublicationDate,
        CreatedByFriendsRankedByPublicationDate,
        RankedByNumTimesReported,
        CreatedByFollowedUsersRankedByPublicationDate,
        NotYetRated,
        RankedByTotalVotesAsc,
        RankedByVotesUp,
        RankedByTextSearch
    }

    public enum Universe
    {
        Invalid,
        Public,
        Beta,
        Internal,
        Dev,
        Max
    }

    public delegate void UserGroupStatus(ulong userId, ulong groupId, [MarshalAs(UnmanagedType.U1)] bool member, [MarshalAs(UnmanagedType.U1)] bool officier);

    public enum UserUGCList
    {
        Published,
        VotedOn,
        VotedUp,
        VotedDown,
        WillVoteLater,
        Favorited,
        Subscribed,
        UsedOrPlayed,
        Followed
    }

    public enum UserUGCListSortOrder
    {
        CreationOrderDesc,
        CreationOrderAsc,
        TitleAsc,
        LastUpdatedDesc,
        SubscriptionDateDesc,
        VoteScoreDesc,
        ForModeration
    }

    [NativeCppClass]
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    internal struct Utils
    {
    }

    public delegate void ValidateAuthTicketResponse(ulong steamID, AuthSessionResponseEnum response, ulong ownerSteamID);

    public enum VoiceResult
    {
        OK,
        NotInitialized,
        NotRecording,
        NoData,
        BufferTooSmall,
        DataCorrupted,
        Restricted,
        UnsupportedCodec
    }

    public delegate void PolicyResponse(sbyte result);

    public enum PublishedFileVisibility
    {
        Public,
        FriendsOnly,
        Private
    }







        public struct RemoteStorageFileShareResult
    {
        public Result Result;

        public ulong FileHandle;
    }

    public struct RemoteStorageGetPublishedFileDetailsResult
    {
        public Result Result;

        public ulong PublishedFileId;

        public uint CreatorAppID;

        public uint ConsumerAppID;

        public string Title;

        public string Description;

        public ulong FileHandle;

        public ulong PreviewFileHandle;

        public ulong SteamIDOwner;

        public uint TimeCreated;

        public uint TimeUpdated;

        //public ERemoteStoragePublishedFileVisibility Visibility;

        public bool Banned;

        public string Tags;

        public bool TagsTruncated;

        public string FileName;

        public int FileSize;

        public int PreviewFileSize;

        public string URL;

        //public EWorkshopFileType FileType;

        public bool AcceptedForUse;
    }

    public struct RemoteStoragePublishFileResult
    {
        public Result Result;

        public ulong PublishedFileId;
    }

    public struct RemoteStorageSubscribePublishedFileResult
    {
        public Result Result;

        public ulong PublishedFileId;
    }

    public struct RemoteStorageUnsubscribePublishedFileResult
    {
        public Result Result;

        public ulong PublishedFileId;
    }

    public struct RemoteStorageUpdatePublishedFileResult
    {
        public Result Result;

        public ulong PublishedFileId;
    }

    public delegate void ServerChangeRequestDelegate(string server, string password);

    public enum ServerMode
    {
        eServerModeInvalid,
        eServerModeNoAuthentication,
        eServerModeAuthentication,
        eServerModeAuthenticationAndSecure
    }

    public delegate void ServersConnected();

    public delegate void ServersConnectFailure(Result result);

    public delegate void ServersDisconnected(Result result);

    public enum ServerStartResult
    {
        OK,
        UnknownError,
        PortAlreadyUsed
    }

    public delegate void SessionRequest(ulong remoteUserId);


    public delegate void MessageDelegate(ulong userId, int channelId, byte[] buffer, int dataSize);







    public struct MemberCollection : IEnumerable<ulong>
    {
        public struct Enumerator : IEnumerator<ulong>
        {
            public readonly ulong m_lobby;

            public readonly List<ulong> m_memberList;

            public int m_index;

            public object CurrentBase
            {
                get
                {
                    return this.Current;
                }
            }

            public object Current { get { return this.Get_Current(); } }
            ulong IEnumerator<ulong>.Current { get { return this.Get_Current(); } }
            
            private ulong Get_Current()
            {
                List<ulong> this2 = this.m_memberList;
                if (this2 == null)
                {
                    Lobby lobby = new Lobby( this.m_lobby );
                    return lobby.GetLobbyMemberByIndex(this.m_index);
                }
                return this2[this.m_index];
            }

            public Enumerator(List<ulong> memberList)
            {
                this.m_memberList = memberList;
                this.m_index = -1;
                m_lobby = 0;
            }

            public Enumerator(ulong lobby)
            {
                m_memberList = new List<ulong>();
                this.m_lobby = lobby;
                this.m_index = -1;
            }

            public bool MoveNext()
            {
                this.m_index++;
                List<ulong> this2 = this.m_memberList;
                if (this2 == null)
                {
                    Lobby lobby = new Lobby( this.m_lobby );
                    return ((this.m_index < lobby.MemberCount) ? 1 : 0) != 0;
                }
                return ((this.m_index < this2.Count) ? 1 : 0) != 0;
            }

            public void Reset()
            {
                this.m_index = -1;
            }

            public void Dispose()
            {
            }
        }

        public readonly ulong m_lobby;

        public readonly List<ulong> m_memberList;

        public MemberCollection(List<ulong> memberList)
        {
            this.m_memberList = memberList;
            this.m_lobby = 0;
        }

        public MemberCollection(ulong lobbyId)
        {
            this.m_memberList = null;
            this.m_lobby = lobbyId;
        }



        MemberCollection.Enumerator private_get_enumerator()
        {
            List<ulong> this2 = this.m_memberList;
            if (this2 == null)
            {
                MemberCollection.Enumerator result = new MemberCollection.Enumerator(this.m_lobby);
                result.m_index = -1;
                return result;
            }
            MemberCollection.Enumerator result2 = new MemberCollection.Enumerator(this2);
            result2.m_index = -1;
            return result2;
        }

        public IEnumerator<ulong> GetEnumerator()
        {
            return this.private_get_enumerator();
        }

        IEnumerator<ulong> IEnumerable<ulong>.GetEnumerator()
        {
            return this.private_get_enumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.private_get_enumerator();
        }

        public IEnumerator<ulong> GetEnumeratorBaseGeneric()
        {

            return (IEnumerator<ulong>)this.private_get_enumerator();
        }

        public IEnumerator<ulong> GetEnumeratorBase()
        {
            return (IEnumerator<ulong>)this.private_get_enumerator();
        }
    }



    public class GameServerItem
    {
        public string Name;

        public IPEndPoint NetAdr;

        public int Ping;

        public bool HadSuccessfulResponse;

        public bool DoNotRefresh;

        public string GameDir;

        public string Map;

        public string GameDescription;

        public uint AppID;

        public int Players;

        public int MaxPlayers;

        public int BotPlayers;

        public bool Password;

        public bool Secure;

        public uint TimeLastPlayed;

        public int ServerVersion;

        public string GameTags;

        public List<string> GameTagList;

        public ulong SteamID;

        public string GetGameTagByPrefix(string prefix)
        {
            List<string>.Enumerator enumerator = this.GameTagList.GetEnumerator();
            if (enumerator.MoveNext())
            {
                string gameTag;
                do
                {
                    gameTag = enumerator.Current;
                    if (gameTag.StartsWith(prefix))
                    {
                        goto IL_31;
                    }
                }
                while (enumerator.MoveNext());
                goto IL_4B;
            IL_31:
                return gameTag.Substring(prefix.Length, gameTag.Length - prefix.Length);
            }
        IL_4B:
            return "";
        }

        public ulong GetGameTagByPrefixUlong(string prefix)
        {
            string tagValue = this.GetGameTagByPrefix(prefix);
            if (string.IsNullOrEmpty(tagValue))
            {
                return 0uL;
            }
            ulong prefix2;
            try
            {
                return Convert.ToUInt64(tagValue);
            }
            catch (Exception)
            {
                prefix2 = 0uL;
            }
            return prefix2;
        }
    }

    public enum MatchMakingServerResponseEnum
    {
        eServerResponded,
        eServerFailedToRespond,
        eNoServersListedOnMasterServer
    }


}

#endif