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

    public struct Lobby
    {
        public const string LOBBY_TYPE_KEY = "LobbyType";

        public const int MAX_KEY_LENGTH = 64;

        public const int MAX_VALUE_LENGTH = 1024;

        public const int MAX_CHAT_MSG_LEN = 4096;

        public readonly ulong LobbyId;

        //public unsafe int LobbyDataCount;
        // {
        //     get
        //     {
        //         ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
        //         CSteamID lobbyId = this.LobbyId;
        //         return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), this2, lobbyId, *(*(long*)this2 + 168L));
        //     }
        // }

        public MemberCollection Members
        {
            get
            {
                MemberCollection this2 = new MemberCollection(this.LobbyId);
                return this2;
            }
        }

        public unsafe int MemberLimit
         {
             get
             {
                 //ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
                 //CSteamID lobbyId = this.LobbyId;
                 //return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), this2, lobbyId, *(*(long*)this2 + 256L));
                 return 0;
             }
             set
             {
                 this.SetMemberLimit(value);
             }
         }

        public unsafe int MemberCount
        {
            get
            {
                // ISteamMatchmaking* this2 = <Module>.SteamMatchmaking();
                // CSteamID lobbyId = this.LobbyId;
                // return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), this2, lobbyId, *(*(long*)this2 + 136L));
                return 0;
            }
        }

        public bool IsValid
        {
            get
            {
                // CSteamID this2 = this.LobbyId;
                // return <Module>.CSteamID.IsValid(ref this2) != null;
		return false;
            }
        }

        //public unsafe CSteamID SteamId;
        // {
        //     get
        //     {
        //         *(long*)ptr = (long)this.LobbyId;
        //         return ptr;
        //     }
        // }

        public Lobby(ulong lobbyId)
        {
            this.LobbyId = lobbyId;
        }

        // public unsafe static Result ConvertLobby(LobbyCreated_t* lobby, ref Lobby result)
        // {
        //     return false;
        // }

        // public unsafe static void ConvertJoin(LobbyEnter_t* info, ref LobbyEnterInfo result)
        // {
        // }

        public unsafe static void Create(LobbyTypeEnum lobbyType, int maxMembers, CompletedDelegate<Lobby> callback)
        {
        }

        public unsafe static void OpenInviteOverlay()
        {
        }

        public unsafe void Join(CompletedDelegate<LobbyEnterInfo> callback)
        {
        }

        public static void Join(ulong lobbyId, CompletedDelegate<LobbyEnterInfo> callback)
        {
            Lobby lobbyId2 = new Lobby( lobbyId );
            lobbyId2.Join(callback);
        }

        public unsafe void Leave()
        {
        }

        public unsafe ulong GetOwner()
        {
            return 0;
        }

        public unsafe bool SetOwner(ulong newOwner)
        {
            return false;
        }

        public unsafe bool SetMemberLimit(int maxMembers)
        {
            return false;
        }

        public LobbyTypeEnum GetLobbyType()
        {
            string lobbyTypeName = this.GetLobbyData("LobbyType");
            if (string.IsNullOrEmpty(lobbyTypeName))
            {
                return LobbyTypeEnum.Public;
            }
            return (LobbyTypeEnum)Enum.Parse(typeof(LobbyTypeEnum), lobbyTypeName);
        }

        public unsafe bool SetLobbyType(LobbyTypeEnum type)
         {
             //IsUdtReturn false;
             //if (this.GetOwner() != num)
             //{
             //    return false;
             //}
             //ISteamMatchmaking* type2 = <Module>.SteamMatchmaking();
             //CSteamID lobbyId = this.LobbyId;
             //bool success = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,ELobbyType), type2, lobbyId, type, *(*(long*)type2 + 264L));

             bool success = false;
             if (success)
             {
                 this.SetLobbyData("LobbyType", ((LobbyTypeEnum)type).ToString());
             }
             return success;
         }

        public unsafe bool SetJoinable([MarshalAs(UnmanagedType.U1)] bool joinable)
        {
            return false;
        }

        public unsafe ulong GetLobbyMemberByIndex(int memberIndex)
        {
            return 0;
        }

        public unsafe bool SetLobbyData(string key, string value)
        {
            return false;
        }

        public unsafe bool DeleteLobbyData(string key)
        {
            return false;
        }

        public unsafe string GetLobbyData(string key)
        {
            return null;
        }

        public unsafe bool RequestLobbyData()
        {
            return false;
        }

        public unsafe bool GetLobbyDataByIndex(int index, out string key, out string value)
        {
            value = "";
            key = "";
            return false;
        }

        public unsafe bool SendChatMessage(ChatMessageBuffer msg)
        {
            return false;
        }

        public unsafe void GetLobbyChatEntry(int chatMsgId, ChatMessageBuffer result, out ulong senderId, out ChatEntryTypeEnum chatEntryType)
        {
            chatEntryType = 0;
            senderId = 0;
        }
    }

    public delegate void LobbyChatMsgDelegate(Lobby lobby, ulong steamIDUser, byte chatEntryType, uint chatID);

    public delegate void LobbyChatUpdateDelegate(Lobby lobby, ulong changedUser, ulong makingChangeUser, ChatMemberStateChangeEnum stateChange);

    public enum LobbyComparison
    {
        LobbyComparisonEqualToOrLessThan = -2,
        LobbyComparisonLessThan,
        LobbyComparisonEqual,
        LobbyComparisonGreaterThan,
        LobbyComparisonEqualToOrGreaterThan,
        LobbyComparisonNotEqual
    }

    public delegate void LobbyDataUpdate([MarshalAs(UnmanagedType.U1)] bool success, Lobby lobby, ulong memberOrLobby);

    public enum LobbyDistanceFilter
    {
        LobbyDistanceFilterClose,
        LobbyDistanceFilterDefault,
        LobbyDistanceFilterFar,
        LobbyDistanceFilterWorldwide
    }

    public struct LobbyEnterInfo
    {
        public Lobby Lobby;

        public LobbyEnterResponseEnum EnterState;

        public bool IsLocked;

        public uint Permissions;
    }

    public enum LobbyEnterResponseEnum
    {
        Success = 1,
        DoesntExist,
        NotAllowed,
        Full,
        Error,
        Banned,
        Limited,
        ClanDisabled,
        CommunityBan,
        MemberBlockedYou,
        YouBlockedMember
    }

    public delegate void LobbyJoinRequestDelegate(Lobby lobby, ulong invitedBy);

    public static class LobbySearch
    {
        private static uint m_lobbyCount = 0;

        public static uint LobbyCount
        {
            get
            {
                return LobbySearch.m_lobbyCount;
            }
        }

        // private unsafe static void ConvertListRequest(LobbyMatchList_t* list, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<Result> callback)
        // {
        //     LobbySearch.m_lobbyCount = (uint)(*(int*)list);
        //     Result list2 = ioFailure ? Result.IOFailure : Result.OK;
        //     callback(list2);
        // }

        private static bool ListContainsLobby(List<Lobby> lobbies, Lobby lobby)
        {
            List<Lobby>.Enumerator enumerator = lobbies.GetEnumerator();
            if (enumerator.MoveNext())
            {
                do
                {
                    Lobby i = enumerator.Current;
                    if (i.LobbyId == lobby.LobbyId)
                    {
                        return true;
                    }
                }
                while (enumerator.MoveNext());
                return false;
            }
            return false;
        }

        public unsafe static void RequestLobbyList(Action<Result> callback)
        {
            // ISteamMatchmaking* expr_05 = <Module>.SteamMatchmaking();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct LobbyMatchList_t,class System::Action<enum SteamSDK::Result> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 32L)), ldftn(ConvertListRequest), callback);
        }

        public unsafe static Lobby GetLobbyByIndex(uint index)
        {
            // ISteamMatchmaking* index2 = <Module>.SteamMatchmaking();
            // CSteamID cSteamID;
            // CSteamID* ptr = calli(CSteamID* modreq(System.Runtime.CompilerServices.IsUdtReturn) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID*,System.Int32), index2, ref cSteamID, index, *(*(long*)index2 + 96L));
            Lobby result = new Lobby(0);
            // result.LobbyId = (ulong)(*ptr);
            return result;
        }

        public static void AddPublicLobbies(List<Lobby> result)
        {
            int i = 0;
            if (0u < LobbySearch.m_lobbyCount)
            {
                do
                {
                    Lobby lobbyByIndex = LobbySearch.GetLobbyByIndex((uint)i);
                    if (!LobbySearch.ListContainsLobby(result, lobbyByIndex))
                    {
                        result.Add(lobbyByIndex);
                    }
                    i++;
                }
                while (i < (int)LobbySearch.m_lobbyCount);
            }
        }

        // public unsafe static bool CanJoinLobby(Lobby lobby)
        // {
        //     CSteamID lobbyId = lobby.LobbyId;
        //     if (<Module>.CSteamID.IsValid(ref lobbyId) == null)
        //     {
        //         return false;
        //     }
        //     if (lobby.GetLobbyType() == LobbyTypeEnum.Public)
        //     {
        //         return true;
        //     }
        //     if (lobby.GetLobbyType() == LobbyTypeEnum.FriendsOnly)
        //     {
        //         ISteamFriends* lobby2 = <Module>.SteamFriends();
        //         CSteamID owner = lobby.GetOwner();
        //         return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,System.Int32), lobby2, owner, 4, *(*(long*)lobby2 + 88L));
        //     }
        //     return false;
        // }

         public unsafe static void AddFriendLobbies(List<Lobby> result)
         {
             // TODO [vicent] current stub implementation
             return;
             //ISteamFriends* ptr = <Module>.SteamFriends();
             //int cFriends = calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32), ptr, 4, *(*(long*)ptr + 24L));
             //int i = 0;
             //if (0 < cFriends)
             //{
             //    do
             //    {
             //        FriendGameInfo_t friendGameInfo;
             //        <Module>.FriendGameInfo_t.{ctor}(ref friendGameInfo);
             //        ISteamFriends* ptr2 = <Module>.SteamFriends();
             //        CSteamID cSteamID;
             //        long num = calli(CSteamID* modreq(System.Runtime.CompilerServices.IsUdtReturn) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID*,System.Int32,System.Int32), ptr2, ref cSteamID, i, 4, *(*(long*)ptr2 + 32L));
             //        CSteamID steamIDFriend;
             //        cpblk(ref steamIDFriend, num, 8);
             //        ISteamFriends* ptr3 = <Module>.SteamFriends();
             //        if (calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,FriendGameInfo_t*), ptr3, steamIDFriend, ref friendGameInfo, *(*(long*)ptr3 + 64L)) && <Module>.CSteamID.IsValid(ref friendGameInfo + 16) != null)
             //        {
             //            Lobby lobby2;
             //            lobby2.LobbyId = (ulong)(*(ref friendGameInfo + 16));
             //            Lobby lobby = lobby2;
             //            if (LobbySearch.CanJoinLobby(lobby2) && !LobbySearch.ListContainsLobby(result, lobby))
             //            {
             //                lobby.RequestLobbyData();
             //                result.Add(lobby);
             //            }
             //        }
             //        i++;
             //    }
             //    while (i < cFriends);
             //}
         }

        // public unsafe static void AddRequestLobbyListStringFilter(string keyToMatch, string valueToMatch, LobbyComparison comparisonType)
        // {
        //     byte[] keyPtr_11_cp_0 = Encoding.UTF8.GetBytes(keyToMatch);
        //     int keyPtr_11_cp_1 = 0;
        //     byte[] valuePtr_23_cp_0 = Encoding.UTF8.GetBytes(valueToMatch);
        //     int valuePtr_23_cp_1 = 0;
        //     ISteamMatchmaking* keyToMatch2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,ELobbyComparison), keyToMatch2, ref keyPtr_11_cp_0[keyPtr_11_cp_1], ref valuePtr_23_cp_0[valuePtr_23_cp_1], comparisonType, *(*(long*)keyToMatch2 + 40L));
        // }

         public unsafe static void AddRequestLobbyListNumericalFilter(string keyToMatch, int valueToMatch, LobbyComparison comparisonType)
         {
             // TODO [vicent] current stub implementation
             return;
        //     byte[] keyPtr_11_cp_0 = Encoding.UTF8.GetBytes(keyToMatch);
        //     int keyPtr_11_cp_1 = 0;
        //     ISteamMatchmaking* keyToMatch2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32,ELobbyComparison), keyToMatch2, ref keyPtr_11_cp_0[keyPtr_11_cp_1], valueToMatch, comparisonType, *(*(long*)keyToMatch2 + 48L));
         }

        // public unsafe static void AddRequestLobbyListNearValueFilter(string keyToMatch, int valueToBeCloseTo)
        // {
        //     byte[] keyPtr_11_cp_0 = Encoding.UTF8.GetBytes(keyToMatch);
        //     int keyPtr_11_cp_1 = 0;
        //     ISteamMatchmaking* keyToMatch2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32), keyToMatch2, ref keyPtr_11_cp_0[keyPtr_11_cp_1], valueToBeCloseTo, *(*(long*)keyToMatch2 + 56L));
        // }

        // public unsafe static void AddRequestLobbyListFilterSlotsAvailable(int slotsAvailable)
        // {
        //     ISteamMatchmaking* slotsAvailable2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32), slotsAvailable2, slotsAvailable, *(*(long*)slotsAvailable2 + 64L));
        // }

        // public unsafe static void AddRequestLobbyListDistanceFilter(LobbyDistanceFilter lobbyDistanceFilter)
        // {
        //     ISteamMatchmaking* lobbyDistanceFilter2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32), lobbyDistanceFilter2, lobbyDistanceFilter, *(*(long*)lobbyDistanceFilter2 + 64L));
        // }

        // public unsafe static void AddRequestLobbyListResultCountFilter(int maxResults)
        // {
        //     ISteamMatchmaking* maxResults2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32), maxResults2, maxResults, *(*(long*)maxResults2 + 80L));
        // }

        // public unsafe static void AddRequestLobbyListCompatibleMembersFilter(CSteamID steamIDLobby)
        // {
        //     ISteamMatchmaking* steamIDLobby2 = <Module>.SteamMatchmaking();
        //     calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), steamIDLobby2, steamIDLobby, *(*(long*)steamIDLobby2 + 88L));
        // }
    }

    public enum LobbyType
    {
        LobbyTypePrivate,
        LobbyTypeFriendsOnly,
        LobbyTypePublic,
        LobbyTypeInvisible
    }

    public enum LobbyTypeEnum
    {
        Private,
        FriendsOnly,
        Public,
        Invisible
    }

}

#endif