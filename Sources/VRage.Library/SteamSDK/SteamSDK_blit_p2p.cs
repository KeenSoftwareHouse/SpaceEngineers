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
    

    public struct P2PSessionState
    {
        public bool ConnectionActive;

        public bool Connecting;

        public P2PSessionErrorEnum LastSessionError;

        public bool UsingRelay;

        public int BytesQueuedForSend;

        public int PacketsQueuedForSend;

        public uint RemoteIP;

        public ushort RemotePort;
    }

    public class Peer2Peer : IDisposable
    {
        private static bool m_isServer;

        private static Peer2Peer m_instance;

        // internal readonly CallbackHolder<1> m_holder;

        // internal readonly CallbackHolder<0> m_holder2;

        // internal static volatile bool NetworkingEnabled;
        static bool NetworkingEnabled;

        // private static SessionRequest <backing_store>SessionRequest;

        // private static ConnectionFailed <backing_store>ConnectionFailed;

        public static event ConnectionFailed ConnectionFailed;
        //public static event ConnectionFailed ConnectionFailed
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         Peer2Peer.<backing_store>ConnectionFailed = (ConnectionFailed)Delegate.Combine(Peer2Peer.<backing_store>ConnectionFailed, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         Peer2Peer.<backing_store>ConnectionFailed = (ConnectionFailed)Delegate.Remove(Peer2Peer.<backing_store>ConnectionFailed, value);
        //     }
        // }


        public static event SessionRequest SessionRequest;
        //public static event SessionRequest SessionRequest
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         Peer2Peer.<backing_store>SessionRequest = (SessionRequest)Delegate.Combine(Peer2Peer.<backing_store>SessionRequest, value);
        //     }
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         Peer2Peer.<backing_store>SessionRequest = (SessionRequest)Delegate.Remove(Peer2Peer.<backing_store>SessionRequest, value);
        //     }
        // }

        // private unsafe static ISteamNetworking* GetSteamNetworking()
        // {
        //     ISteamNetworking* result;
        //     if (Peer2Peer.m_isServer)
        //     {
        //         ISteamNetworking* ptr;
        //         if (Peer2Peer.NetworkingEnabled && <Module>.SteamGameServer() != null)
        //         {
        //             ptr = <Module>.SteamGameServerNetworking();
        //         }
        //         else
        //         {
        //             ptr = null;
        //         }
        //         result = ptr;
        //     }
        //     else
        //     {
        //         result = <Module>.SteamNetworking();
        //     }
        //     return result;
        // }

        internal Peer2Peer()
        {
            // TODO [vicent] current stub implementation
            Peer2Peer.NetworkingEnabled = false;

            // CallbackHolder<1> holder = new CallbackHolder<1>();
            // try
            // {
            //     this.m_holder = holder;
            //     CallbackHolder<0> this2 = new CallbackHolder<0>();
            //     try
            //     {
            //         this.m_holder2 = this2;
            //         base..ctor();
            //         this.m_holder.AddNative<SteamSDK::Peer2Peer,P2PSessionRequest_t>(this, ldftn(sessionRequest));
            //         this.m_holder.AddNative<SteamSDK::Peer2Peer,P2PSessionConnectFail_t>(this, ldftn(connectionFailed));
            //         this.m_holder2.AddNative<SteamSDK::Peer2Peer,P2PSessionRequest_t>(this, ldftn(sessionRequest));
            //         this.m_holder2.AddNative<SteamSDK::Peer2Peer,P2PSessionConnectFail_t>(this, ldftn(connectionFailed));
            //         Peer2Peer.NetworkingEnabled = false;
            //     }
            //     catch
            //     {
            //         ((IDisposable)this.m_holder2).Dispose();
            //         throw;
            //     }
            // }
            // catch
            // {
            //     ((IDisposable)this.m_holder).Dispose();
            //     throw;
            // }
        }

        internal static void Init(bool server)
        {
            Peer2Peer.m_instance = new Peer2Peer();
            Peer2Peer.m_isServer = server;
        }

        internal static void Destroy()
        {
            IDisposable instance = Peer2Peer.m_instance;
            if (instance != null)
            {
                instance.Dispose();
            }
            Peer2Peer.m_instance = null;
            Peer2Peer.NetworkingEnabled = false;

            // HACK [vicent] we need to avoid warning as error!
            if (Peer2Peer.NetworkingEnabled)
            {
                Peer2Peer.m_instance = null;
            }
        }

        // internal unsafe static void sessionRequest(Peer2Peer owner, P2PSessionRequest_t* data)
        // {
        //     ulong data2 = (ulong)(*(long*)data);
        //     SessionRequest owner2 = Peer2Peer.<backing_store>SessionRequest;
        //     if (owner2 != null)
        //     {
        //         owner2(data2);
        //     }
        // }

        // internal unsafe static void connectionFailed(Peer2Peer owner, P2PSessionConnectFail_t* data)
        // {
        //     ulong data2 = (ulong)(*(long*)data);
        //     P2PSessionErrorEnum error = (P2PSessionErrorEnum)(*(byte*)(data + 8L / (long)sizeof(P2PSessionConnectFail_t)));
        //     ConnectionFailed owner2 = Peer2Peer.<backing_store>ConnectionFailed;
        //     if (owner2 != null)
        //     {
        //         owner2(data2, error);
        //     }
        // }

         protected static void raise_SessionRequest(ulong value0)
         {
             //SessionRequest sessionRequest = Peer2Peer.<backing_store>SessionRequest;
             if (SessionRequest != null)
             {
                 SessionRequest(value0);
             }
         }

         protected static void raise_ConnectionFailed(ulong value0, P2PSessionErrorEnum value1)
         {
             if (ConnectionFailed != null)
             {
                 ConnectionFailed(value0, value1);
             }
         }

        public unsafe static bool AcceptSession(ulong remoteUser)
        {
            // ISteamNetworking* remoteUser2 = Peer2Peer.GetSteamNetworking();
            // CSteamID cSteamID = remoteUser;
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), remoteUser2, cSteamID, *(*(long*)remoteUser2 + 24L));
            return false;
        }

        public unsafe static bool CloseSession(ulong remoteUser)
        {
            // ISteamNetworking* remoteUser2 = Peer2Peer.GetSteamNetworking();
            // CSteamID cSteamID = remoteUser;
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID), remoteUser2, cSteamID, *(*(long*)remoteUser2 + 32L));
            return false;
        }

        public unsafe static bool SendPacket(ulong remoteUser, byte* dataPtr, int byteCount, P2PMessageEnum msgType, int channel)
        {
            // CSteamID byteCount2 = remoteUser;
            // if (<Module>.CSteamID.IsValid(ref byteCount2) != null)
            // {
            //     ISteamNetworking* remoteUser2 = Peer2Peer.GetSteamNetworking();
            //     CSteamID dataPtr2 = remoteUser;
            //     return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,System.Void modopt(System.Runtime.CompilerServices.IsConst)*,System.UInt32,EP2PSend,System.Int32), remoteUser2, dataPtr2, dataPtr, byteCount, msgType, channel, *(*(long*)remoteUser2));
            // }
            return false;
        }

        public unsafe static bool SendPacket(ulong remoteUser, byte[] data, int byteCount, P2PMessageEnum msgType, int channel)
        {
            // int dataPtr_07_cp_1 = 0;
            // ISteamNetworking* remoteUser2 = Peer2Peer.GetSteamNetworking();
            // CSteamID data2 = remoteUser;
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,System.Void modopt(System.Runtime.CompilerServices.IsConst)*,System.UInt32,EP2PSend,System.Int32), remoteUser2, data2, ref data[dataPtr_07_cp_1], byteCount, msgType, channel, *(*(long*)remoteUser2));
            return false;
        }
        

        public unsafe static bool GetSessionState(ulong remoteUser, ref P2PSessionState state)
        {
            // ISteamNetworking* remoteUser2 = Peer2Peer.GetSteamNetworking();
            // CSteamID cSteamID = remoteUser;
            // P2PSessionState_t nativeState;
            // bool result = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,P2PSessionState_t*), remoteUser2, cSteamID, ref nativeState, *(*(long*)remoteUser2 + 48L));
            // state.BytesQueuedForSend = *(ref nativeState + 4);
            // byte connecting = (*(ref nativeState + 1) != 0) ? 1 : 0;
            // state.Connecting = (connecting != 0);
            // byte connectionActive = (nativeState != 0) ? 1 : 0;
            // state.ConnectionActive = (connectionActive != 0);
            // state.LastSessionError = (P2PSessionErrorEnum)(*(ref nativeState + 2));
            // state.PacketsQueuedForSend = *(ref nativeState + 8);
            // state.RemoteIP = (uint)(*(ref nativeState + 12));
            // state.RemotePort = *(ref nativeState + 16);
            // byte state2 = (*(ref nativeState + 3) != 0) ? 1 : 0;
            // state.UsingRelay = (state2 != 0);
            // return result;
            return false;
        }

        public unsafe static bool IsPacketAvailable(out uint msgSize, int channel)
        {
            msgSize = 0;
            // ISteamNetworking* networking = Peer2Peer.GetSteamNetworking();
            // if (networking != null)
            // {
            //     uint size;
            //     bool result = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32*,System.Int32), networking, ref size, channel, *(*(long*)networking + 8L));
            //     msgSize = size;
            //     return result;
            // }
            return false;
        }

        public unsafe static bool ReadPacket(byte[] buffer, out uint dataSize, out ulong sender, int channel)
        {
            sender = 0;
            dataSize = 0;
            // ISteamNetworking* networking = Peer2Peer.GetSteamNetworking();
            // if (networking != null)
            // {
            //     int dataPtr_10_cp_1 = 0;
            //     bool result;
            //     try
            //     {
            //         CSteamID send;
            //         <Module>.CSteamID.{ctor}(ref send);
            //         uint size;
            //         result = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void*,System.UInt32,System.UInt32*,CSteamID*,System.Int32), networking, ref buffer[dataPtr_10_cp_1], buffer.Length, ref size, ref send, channel, *(*(long*)networking + 16L));
            //         sender = send;
            //         dataSize = size;
            //     }
            //     catch
            //     {
            //         throw;
            //     }
            //     return result;
            // }
            return false;
        }

        // [HandleProcessCorruptedStateExceptions]
        protected void Dispose( bool A_0)
        {
        //    if (A_0)
        //    {
        //        try
        //        {
        //            return;
        //        }
        //        finally
        //        {
        //            try
        //            {
        //                ((IDisposable)this.m_holder2).Dispose();
        //            }
        //            finally
        //            {
        //                try
        //                {
        //                    ((IDisposable)this.m_holder).Dispose();
        //                }
        //                finally
        //                {
        //                }
        //            }
        //        }
        //    }
            //base.Finalize();
        }

        public void Dispose()
        {
            this.Dispose(true);
            //GC.SuppressFinalize(this);
        }
    }


    public class RemoteStorage
    {
        public unsafe bool FileWrite(string file, byte[] bytes)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // int pinnedBytes_13_cp_1 = 0;
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.Void modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32), this2, pchFile, ref bytes[pinnedBytes_13_cp_1], bytes.Length, *(*(long*)this2));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return retVal;
            return false;
        }

        public unsafe void FileShare(string file, Action<bool, RemoteStorageFileShareResult> onCallResult)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageFileShareResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageFileShareResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 32L)), ldftn(OnFileShare), onCallResult);
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
        }

        public unsafe ulong FileWriteStreamOpen(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // ulong arg_2B_0 = calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 48L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return arg_2B_0;
            return 0;
        }

        public unsafe bool FileWriteStreamWriteChunk(ulong handle, byte[] bytes, int size)
        {
            // int pinnedBytes_07_cp_1 = 0;
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Void modopt(System.Runtime.CompilerServices.IsConst)*,System.Int32), this2, handle, ref bytes[pinnedBytes_07_cp_1], size, *(*(long*)this2 + 56L));
            return false;
        }

        public unsafe bool FileWriteStreamClose(ulong handle)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, handle, *(*(long*)this2 + 64L));
            return false;
        }

        public unsafe bool FileWriteStreamCancel(ulong handle)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, handle, *(*(long*)this2 + 72L));
            return false;
        }

        public unsafe void PublishWorkshopFile(string file, string previewFile, uint appId, string title, string description, string longDescription, PublishedFileVisibility visibility, string[] tags, Action<bool, RemoteStoragePublishFileResult> onCallResult)
        {
            // sbyte* pchFile = (sbyte*)Marshal.StringToHGlobalAnsi(file).ToPointer();
            // sbyte* pchPreviewFile = (sbyte*)Marshal.StringToHGlobalAnsi(previewFile).ToPointer();
            // sbyte* pchTitle = (sbyte*)Marshal.StringToHGlobalAnsi(title).ToPointer();
            // sbyte* pchDescription = (sbyte*)Marshal.StringToHGlobalAnsi(description).ToPointer();
            // sbyte* pchLongDescription = (sbyte*)Marshal.StringToHGlobalAnsi(longDescription).ToPointer();
            // SteamParamStringArray_t steamTags;
            // *(ref steamTags + 8) = tags.Length;
            // ulong longDescription2 = (ulong)((long)(*(ref steamTags + 8)));
            // ulong description2;
            // if (longDescription2 <= 2305843009213693951uL)
            // {
            //     description2 = longDescription2 * 8uL;
            // }
            // else
            // {
            //     description2 = 18446744073709551615uL;
            // }
            // steamTags = <Module>.new[](description2);
            // int i = 0;
            // if (0 < *(ref steamTags + 8))
            // {
            //     long appId2 = 0L;
            //     do
            //     {
            //         IntPtr intPtr = Marshal.StringToHGlobalAnsi(tags[i]);
            //         *(appId2 + steamTags) = intPtr.ToPointer();
            //         i++;
            //         appId2 += 8L;
            //     }
            //     while (i < *(ref steamTags + 8));
            // }
            // ISteamRemoteStorage* title2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStoragePublishFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStoragePublishFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.UInt32,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*,ERemoteStoragePublishedFileVisibility,SteamParamStringArray_t*,EWorkshopFileType), title2, pchFile, pchPreviewFile, appId, pchTitle, pchDescription, visibility, ref steamTags, 0, *(*(long*)title2 + 216L)), ldftn(OnPublishFile), onCallResult);
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchPreviewFile));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchTitle));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchDescription));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchLongDescription));
            // int j = 0;
            // if (0 < *(ref steamTags + 8))
            // {
            //     long file2 = 0L;
            //     do
            //     {
            //         Marshal.FreeHGlobal((IntPtr)(*(file2 + steamTags)));
            //         j++;
            //         file2 += 8L;
            //     }
            //     while (j < *(ref steamTags + 8));
            // }
            // <Module>.delete[](steamTags);
        }

        public unsafe void UGCDownload(ulong ugcHandle, int priority, Action<bool, RemoteStorageDownloadUGCResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageDownloadUGCResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageDownloadUGCResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32), this2, ugcHandle, priority, *(*(long*)this2 + 168L)), ldftn(OnUGCDownload), onCallResult);
        }

        public unsafe bool GetUGCDownloadProgress(ulong ugcHandle, out int bytesDownloaded, out int bytesExpected)
        {
            bytesExpected = 0;
            bytesDownloaded = 0;
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Int32*,System.Int32*), this2, ugcHandle, ref bytesDownloaded, ref bytesExpected, *(*(long*)this2 + 176L));
            return false;
        }

        public unsafe int UGCRead(ulong ugcHandle, byte[] outputBuffer, int size, uint offset)
        {
            // int pvData_07_cp_1 = 0;
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Void*,System.Int32,System.UInt32,EUGCReadAction), this2, ugcHandle, ref outputBuffer[pvData_07_cp_1], size, offset, 0, *(*(long*)this2 + 192L));
            return 0;
        }

        public unsafe void EnumerateUserSubscribedFiles(uint startIndex, Action<bool, RemoteStorageEnumerateUserSubscribedFilesResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageEnumerateUserSubscribedFilesResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageEnumerateUserSubscribedFilesResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32), this2, startIndex, *(*(long*)this2 + 320L)), ldftn(OnEnumerateUserSubscribedFiles), onCallResult);
        }

        public unsafe bool FileExists(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 80L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return retVal;
            return false;
        }

        public unsafe bool FilePersisted(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 88L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return retVal;
            return false;
        }

        public unsafe int GetFileSize(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // int arg_2B_0 = calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 96L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return arg_2B_0;
            return 0;
        }

        public unsafe int GetFileTimestamp(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // int arg_2C_0 = calli(System.Int64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 104L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return arg_2C_0;
            return 0;
        }

        public unsafe int GetFileCount()
        {
            // ISteamRemoteStorage* expr_05 = <Module>.SteamRemoteStorage();
            // return calli(System.Int32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 120L));
            return 0;
        }

        public unsafe string GetFileNameAndSize(int fileIndex, out int fileSizeInBytes)
        {
            fileSizeInBytes = 0;
            // ISteamRemoteStorage* fileIndex2 = <Module>.SteamRemoteStorage();
            // sbyte* fileName = calli(System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)* modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32,System.Int32*), fileIndex2, fileIndex, ref fileSizeInBytes, *(*(long*)fileIndex2 + 128L));
            // if (fileName != null)
            // {
            //     return new string((sbyte*)fileName);
            // }
            return null;
        }

        public unsafe bool GetQuota(out int totalBytes, out int availableBytes)
        {
            totalBytes = 0;
            availableBytes = 0;
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Int32*,System.Int32*), this2, ref totalBytes, ref availableBytes, *(*(long*)this2 + 136L));
            return false;
        }

        public unsafe bool FileForget(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool ret = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 16L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return ret;
            return false;
        }

        public unsafe void FileDelete(string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // object arg_20_0 = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, pchFile, *(*(long*)this2 + 24L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
        }

        public unsafe void GetPublishedFileDetails(ulong publishedFileId, uint maxSecondsOld, Action<bool, RemoteStorageGetPublishedFileDetailsResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageGetPublishedFileDetailsResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageGetPublishedFileDetailsResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32), this2, publishedFileId, maxSecondsOld, *(*(long*)this2 + 288L)), ldftn(OnGetPublishedFileDetails), onCallResult);
        }

        public unsafe void SubscribePublishedFile(ulong publishedFileId, Action<bool, RemoteStorageSubscribePublishedFileResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageSubscribePublishedFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageSubscribePublishedFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, publishedFileId, *(*(long*)this2 + 312L)), ldftn(OnSubscribePublishedFile), onCallResult);
        }

        public unsafe void UnsubscribePublishedFile(ulong publishedFileId, Action<bool, RemoteStorageUnsubscribePublishedFileResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageUnsubscribePublishedFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageUnsubscribePublishedFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, publishedFileId, *(*(long*)this2 + 328L)), ldftn(OnUnsubscribePublishedFile), onCallResult);
        }

        public unsafe ulong CreatePublishedFileUpdateRequest(ulong publishedFileId)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // return calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, publishedFileId, *(*(long*)this2 + 224L));
            return 0;
        }

        public unsafe bool UpdatePublishedFileFile(ulong updateHandle, string file)
        {
            // sbyte* pchFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(file));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, updateHandle, pchFile, *(*(long*)this2 + 232L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchFile));
            // return retVal;
            return false;
        }

        public unsafe bool UpdatePublishedFilePreviewFile(ulong updateHandle, string previewFile)
        {
            // sbyte* pchPreviewFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(previewFile));
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, updateHandle, pchPreviewFile, *(*(long*)this2 + 240L));
            // Marshal.FreeHGlobal((IntPtr)((void*)pchPreviewFile));
            // return retVal;
            return false;
        }

        public unsafe bool UpdatePublishedFileTags(ulong updateHandle, string[] tags)
        {
            // SteamParamStringArray_t steamTags;
            // *(ref steamTags + 8) = tags.Length;
            // ulong num = (ulong)((long)(*(ref steamTags + 8)));
            // ulong num2;
            // if (num <= 2305843009213693951uL)
            // {
            //     num2 = num * 8uL;
            // }
            // else
            // {
            //     num2 = 18446744073709551615uL;
            // }
            // steamTags = <Module>.new[](num2);
            // int i = 0;
            // if (0 < *(ref steamTags + 8))
            // {
            //     long num3 = 0L;
            //     do
            //     {
            //         IntPtr intPtr = Marshal.StringToHGlobalAnsi(tags[i]);
            //         *(num3 + steamTags) = intPtr.ToPointer();
            //         i++;
            //         num3 += 8L;
            //     }
            //     while (i < *(ref steamTags + 8));
            // }
            // ISteamRemoteStorage* ptr = <Module>.SteamRemoteStorage();
            // bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,SteamParamStringArray_t*), ptr, updateHandle, ref steamTags, *(*(long*)ptr + 272L));
            // int j = 0;
            // if (0 < *(ref steamTags + 8))
            // {
            //     long updateHandle2 = 0L;
            //     do
            //     {
            //         Marshal.FreeHGlobal((IntPtr)(*(updateHandle2 + steamTags)));
            //         j++;
            //         updateHandle2 += 8L;
            //     }
            //     while (j < *(ref steamTags + 8));
            // }
            // <Module>.delete[](steamTags);
            // return retVal;
            return false;
        }

        public unsafe void CommitPublishedFileUpdate(ulong updateHandle, Action<bool, RemoteStorageUpdatePublishedFileResult> onCallResult)
        {
            // ISteamRemoteStorage* this2 = <Module>.SteamRemoteStorage();
            // <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageUpdatePublishedFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageUpdatePublishedFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, updateHandle, *(*(long*)this2 + 280L)), ldftn(OnCommitPublishedFileUpdate), onCallResult);
        }

        // internal unsafe static void OnFileShare(RemoteStorageFileShareResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageFileShareResult> action)
        // {
        //     // action(ioFailure, new RemoteStorageFileShareResult
        //     // {
        //     //     Result = (Result)(*(int*)result),
        //     //     FileHandle = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageFileShareResult_t)))
        //     // });
        // }

        // internal unsafe static void OnPublishFile(RemoteStoragePublishFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStoragePublishFileResult> action)
        // {
        //     // action(ioFailure, new RemoteStoragePublishFileResult
        //     // {
        //     //     Result = (Result)(*(int*)result),
        //     //     PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStoragePublishFileResult_t)))
        //     // });
        // }

        // internal unsafe static void OnSubscribePublishedFile(RemoteStorageSubscribePublishedFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageSubscribePublishedFileResult> action)
        // {
        //     // action(ioFailure, new RemoteStorageSubscribePublishedFileResult
        //     // {
        //     //     Result = (Result)(*(int*)result),
        //     //     PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageSubscribePublishedFileResult_t)))
        //     // });
        // }

        // internal unsafe static void OnUnsubscribePublishedFile(RemoteStorageUnsubscribePublishedFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageUnsubscribePublishedFileResult> action)
        // {
        //     // action(ioFailure, new RemoteStorageUnsubscribePublishedFileResult
        //     // {
        //     //     Result = (Result)(*(int*)result),
        //     //     PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageUnsubscribePublishedFileResult_t)))
        //     // });
        // }

        // internal unsafe static void OnEnumerateUserSubscribedFiles(RemoteStorageEnumerateUserSubscribedFilesResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageEnumerateUserSubscribedFilesResult> action)
        // {
        //     // RemoteStorageEnumerateUserSubscribedFilesResult data = default(RemoteStorageEnumerateUserSubscribedFilesResult);
        //     // data.Result = (Result)(*(int*)result);
        //     // data.ResultsReturned = *(int*)(result + 4L / (long)sizeof(RemoteStorageEnumerateUserSubscribedFilesResult_t));
        //     // data.TotalResultCount = *(int*)(result + 8L / (long)sizeof(RemoteStorageEnumerateUserSubscribedFilesResult_t));
        //     // if (!ioFailure && data.Result == Result.OK)
        //     // {
        //     //     int i = 0;
        //     //     if (0 < data.ResultsReturned)
        //     //     {
        //     //         RemoteStorageEnumerateUserSubscribedFilesResult_t* ioFailure2 = result + 16L / (long)sizeof(RemoteStorageEnumerateUserSubscribedFilesResult_t);
        //     //         do
        //     //         {
        //     //             *data.PublishedFileIds.op_Subscript(i) = (ulong)(*(long*)ioFailure2);
        //     //             i++;
        //     //             ioFailure2 += 8L / (long)sizeof(RemoteStorageEnumerateUserSubscribedFilesResult_t);
        //     //         }
        //     //         while (i < data.ResultsReturned);
        //     //     }
        //     // }
        //     // action(ioFailure, data);
        // }

        // internal unsafe static void OnGetPublishedFileDetails(RemoteStorageGetPublishedFileDetailsResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageGetPublishedFileDetailsResult> action)
        // {
        //     // int num;
        //     // if (!ioFailure && *(int*)result == 1)
        //     // {
        //     //     num = 1;
        //     // }
        //     // else
        //     // {
        //     //     num = 0;
        //     // }
        //     // bool success = (byte)num != 0;
        //     // RemoteStorageGetPublishedFileDetailsResult data = default(RemoteStorageGetPublishedFileDetailsResult);
        //     // data.Result = (Result)(*(int*)result);
        //     // data.PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.CreatorAppID = (uint)(*(int*)(result + 16L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.ConsumerAppID = (uint)(*(int*)(result + 20L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // string title;
        //     // if (success)
        //     // {
        //     //     RemoteStorageGetPublishedFileDetailsResult_t* ptr = result + 24L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t);
        //     //     sbyte* ptr2 = (sbyte*)ptr;
        //     //     ulong num2;
        //     //     if (ptr2 == null)
        //     //     {
        //     //         num2 = 0uL;
        //     //     }
        //     //     else
        //     //     {
        //     //         num2 = <Module>.strnlen(ptr2, 129uL);
        //     //     }
        //     //     title = new string((sbyte*)ptr, 0, (int)num2, Encoding.UTF8);
        //     // }
        //     // else
        //     // {
        //     //     title = null;
        //     // }
        //     // data.Title = title;
        //     // string description;
        //     // if (success)
        //     // {
        //     //     RemoteStorageGetPublishedFileDetailsResult_t* ptr3 = result + 153L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t);
        //     //     sbyte* ptr4 = (sbyte*)ptr3;
        //     //     ulong num3;
        //     //     if (ptr4 == null)
        //     //     {
        //     //         num3 = 0uL;
        //     //     }
        //     //     else
        //     //     {
        //     //         num3 = <Module>.strnlen(ptr4, 8000uL);
        //     //     }
        //     //     description = new string((sbyte*)ptr3, 0, (int)num3, Encoding.UTF8);
        //     // }
        //     // else
        //     // {
        //     //     description = null;
        //     // }
        //     // data.Description = description;
        //     // data.FileHandle = (ulong)(*(long*)(result + 8160L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.PreviewFileHandle = (ulong)(*(long*)(result + 8168L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.SteamIDOwner = (ulong)(*(long*)(result + 8176L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.TimeUpdated = (uint)(*(int*)(result + 8188L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.TimeCreated = (uint)(*(int*)(result + 8184L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.Visibility = (ERemoteStoragePublishedFileVisibility)(*(int*)(result + 8192L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.Banned = (*(byte*)(result + 8196L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)) != 0);
        //     // string tags;
        //     // if (success)
        //     // {
        //     //     RemoteStorageGetPublishedFileDetailsResult_t* ptr5 = result + 8197L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t);
        //     //     sbyte* ptr6 = (sbyte*)ptr5;
        //     //     ulong num4;
        //     //     if (ptr6 == null)
        //     //     {
        //     //         num4 = 0uL;
        //     //     }
        //     //     else
        //     //     {
        //     //         num4 = <Module>.strnlen(ptr6, 1025uL);
        //     //     }
        //     //     tags = new string((sbyte*)ptr5, 0, (int)num4, Encoding.UTF8);
        //     // }
        //     // else
        //     // {
        //     //     tags = null;
        //     // }
        //     // data.Tags = tags;
        //     // data.TagsTruncated = (*(byte*)(result + 9222L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)) != 0);
        //     // string fileName;
        //     // if (success)
        //     // {
        //     //     RemoteStorageGetPublishedFileDetailsResult_t* ptr7 = result + 9223L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t);
        //     //     sbyte* ptr8 = (sbyte*)ptr7;
        //     //     ulong num5;
        //     //     if (ptr8 == null)
        //     //     {
        //     //         num5 = 0uL;
        //     //     }
        //     //     else
        //     //     {
        //     //         num5 = <Module>.strnlen(ptr8, 260uL);
        //     //     }
        //     //     fileName = new string((sbyte*)ptr7, 0, (int)num5, Encoding.UTF8);
        //     // }
        //     // else
        //     // {
        //     //     fileName = null;
        //     // }
        //     // data.FileName = fileName;
        //     // data.FileSize = *(int*)(result + 9484L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t));
        //     // data.PreviewFileSize = *(int*)(result + 9488L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t));
        //     // string ioFailure2;
        //     // if (success)
        //     // {
        //     //     RemoteStorageGetPublishedFileDetailsResult_t* ptr9 = result + 9492L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t);
        //     //     sbyte* ptr10 = (sbyte*)ptr9;
        //     //     ulong action2;
        //     //     if (ptr10 == null)
        //     //     {
        //     //         action2 = 0uL;
        //     //     }
        //     //     else
        //     //     {
        //     //         action2 = <Module>.strnlen(ptr10, 256uL);
        //     //     }
        //     //     ioFailure2 = new string((sbyte*)ptr9, 0, (int)action2, Encoding.UTF8);
        //     // }
        //     // else
        //     // {
        //     //     ioFailure2 = null;
        //     // }
        //     // data.URL = ioFailure2;
        //     // data.FileType = (EWorkshopFileType)(*(int*)(result + 9748L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)));
        //     // data.AcceptedForUse = (*(byte*)(result + 9752L / (long)sizeof(RemoteStorageGetPublishedFileDetailsResult_t)) != 0);
        //     // action(ioFailure, data);
        // }

        // internal unsafe static void OnUGCDownload(RemoteStorageDownloadUGCResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageDownloadUGCResult> action)
        // {
        //     // int ioFailure2;
        //     // if (!ioFailure && *(int*)result == 1)
        //     // {
        //     //     ioFailure2 = 1;
        //     // }
        //     // else
        //     // {
        //     //     ioFailure2 = 0;
        //     // }
        //     // RemoteStorageDownloadUGCResult data = default(RemoteStorageDownloadUGCResult);
        //     // data.Result = (Result)(*(int*)result);
        //     // data.AppID = (uint)(*(int*)(result + 16L / (long)sizeof(RemoteStorageDownloadUGCResult_t)));
        //     // data.FileHandle = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageDownloadUGCResult_t)));
        //     // data.SizeInBytes = *(int*)(result + 20L / (long)sizeof(RemoteStorageDownloadUGCResult_t));
        //     // data.SteamIDOwner = (ulong)(*(long*)(result + 288L / (long)sizeof(RemoteStorageDownloadUGCResult_t)));
        //     // string result2;
        //     // if ((byte)ioFailure2 != 0)
        //     // {
        //     //     result2 = new string((sbyte*)(result + 24L / (long)sizeof(RemoteStorageDownloadUGCResult_t)));
        //     // }
        //     // else
        //     // {
        //     //     data.FileName = null;
        //     //     result2 = null;
        //     // }
        //     // data.FileName = result2;
        //     // action(ioFailure, data);
        // }

        // internal unsafe static void OnCommitPublishedFileUpdate(RemoteStorageUpdatePublishedFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageUpdatePublishedFileResult> action)
        // {
        //     action(ioFailure, new RemoteStorageUpdatePublishedFileResult
        //     {
        //         Result = (Result)(*(int*)result),
        //         PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageUpdatePublishedFileResult_t)))
        //     });
        // }
    }

    public struct RemoteStorageDownloadUGCResult
    {
        public Result Result;

        public ulong FileHandle;

        public uint AppID;

        public int SizeInBytes;

        public string FileName;

        public ulong SteamIDOwner;
    }

    public struct RemoteStorageEnumerateUserSubscribedFilesResult
    {
        // internal inline_array<unsigned __int64,50> PublishedFileIds;

        public Result Result;

        public int ResultsReturned;

        public int TotalResultCount;

        public unsafe ulong this[int i]
        {
            get
            {
                // return *this.PublishedFileIds.op_Subscript(i);
                return 0;
            }
            set
            {
                // *this.PublishedFileIds.op_Subscript(i) = value;
            }
        }
    }

}

#endif