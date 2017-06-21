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

    public struct SteamUGCDetails
    {
        public ulong PublishedFileId;

        public Result Result;

        public WorkshopFileType FileType;

        public uint CreatorAppID;

        public uint ConsumerAppID;

        public string Title;

        public string Description;

        public ulong SteamIDOwner;

        public uint TimeCreated;

        public uint TimeUpdated;

        public uint TimeAddedToUserList;

        public PublishedFileVisibility Visibility;

        public bool Banned;

        public bool AcceptedForUse;

        public bool TagsTruncated;

        public string Tags;

        public ulong File;

        public ulong PreviewFile;

        public string FileName;

        public int FileSize;

        public int PreviewFileSize;

        public string URL;

        public uint VotesUp;

        public uint VotesDown;

        public float Score;

        public uint NumChildren;
    }

    public struct SteamUGCQueryCompleted
    {
        public ulong handle;

        public Result Result;

        public uint NumResultsReturned;

        public uint TotalMatchingResults;

        public bool CachedData;
    }

    public struct SteamUGCRequestUGCDetailsResult
    {
        public SteamUGCDetails Details;

        public bool CachedData;
    }

    public struct SubmitItemUpdateResult
    {
        public Result Result;

        public bool UserNeedsToAcceptWorkshopLegalAgreement;
    }

    public class UGC
    {
        public bool IsQueryHandleValid(ulong handle)
        {
            return ((handle != 18446744073709551615uL) ? 1 : 0) != 0;
        }

//         public unsafe ulong CreateQueryUserUGCRequest(uint unAccountID, UserUGCList eListType, UGCMatchingUGCType eMatchingUGCType, UserUGCListSortOrder eSortOrder, uint nCreatorAppID, uint nConsumerAppID, uint unPage)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,EUserUGCList,EUGCMatchingUGCType,EUserUGCListSortOrder,System.UInt32,System.UInt32,System.UInt32), this2, unAccountID, eListType, eMatchingUGCType, eSortOrder, nCreatorAppID, nConsumerAppID, unPage, *(*(long*)this2));
//         }

//         public unsafe ulong CreateQueryAllUGCRequest(UGCQuery eQueryType, UGCMatchingUGCType eMatchingeMatchingUGCTypeFileType, uint nCreatorAppID, uint nConsumerAppID, uint unPage)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,EUGCQuery,EUGCMatchingUGCType,System.UInt32,System.UInt32,System.UInt32), this2, eQueryType, eMatchingeMatchingUGCTypeFileType, nCreatorAppID, nConsumerAppID, unPage, *(*(long*)this2 + 8L));
//         }

//         public unsafe void SendQueryUGCRequest(ulong handle, Action<bool, SteamUGCQueryCompleted> onCallResult)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct SteamUGCQueryCompleted_t,class System::Action<bool,struct SteamSDK::SteamUGCQueryCompleted> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, handle, *(*(long*)this2 + 16L)), ldftn(OnQueryCompleted), onCallResult);
//         }

//         public unsafe bool GetQueryUGCResult(ulong handle, uint index, out SteamUGCDetails details)
//         {
//             details = 0;
//             ISteamUGC* ptr = <Module>.SteamUGC();
//             SteamUGCDetails_t data;
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32,SteamUGCDetails_t*), ptr, handle, index, ref data, *(*(long*)ptr + 24L));
//             details.PublishedFileId = data;
//             details.Result = (Result)(*(ref data + 8));
//             details.FileType = (WorkshopFileType)(*(ref data + 12));
//             details.CreatorAppID = (uint)(*(ref data + 16));
//             details.ConsumerAppID = (uint)(*(ref data + 20));
//             string title;
//             if (retVal)
//             {
//                 ulong num = <Module>.strnlen(ref data + 24, 129uL);
//                 title = new string(ref data + 24, 0, (int)num, Encoding.UTF8);
//             }
//             else
//             {
//                 title = null;
//             }
//             details.Title = title;
//             string description;
//             if (retVal)
//             {
//                 ulong num2 = <Module>.strnlen(ref data + 153, 8000uL);
//                 description = new string(ref data + 153, 0, (int)num2, Encoding.UTF8);
//             }
//             else
//             {
//                 description = null;
//             }
//             details.Description = description;
//             details.SteamIDOwner = (ulong)(*(ref data + 8160));
//             details.TimeCreated = (uint)(*(ref data + 8168));
//             details.TimeUpdated = (uint)(*(ref data + 8172));
//             details.TimeAddedToUserList = (uint)(*(ref data + 8176));
//             details.Visibility = (PublishedFileVisibility)(*(ref data + 8180));
//             details.Banned = (*(ref data + 8184) != 0);
//             details.AcceptedForUse = (*(ref data + 8185) != 0);
//             details.TagsTruncated = (*(ref data + 8186) != 0);
//             string details2;
//             if (retVal)
//             {
//                 ulong num3 = <Module>.strnlen(ref data + 8187, 1025uL);
//                 details2 = new string(ref data + 8187, 0, (int)num3, Encoding.UTF8);
//             }
//             else
//             {
//                 details2 = null;
//             }
//             details.Tags = details2;
//             details.File = (ulong)(*(ref data + 9216));
//             details.PreviewFile = (ulong)(*(ref data + 9224));
//             string index2;
//             if (retVal)
//             {
//                 ulong num4 = <Module>.strnlen(ref data + 9232, 260uL);
//                 index2 = new string(ref data + 9232, 0, (int)num4, Encoding.UTF8);
//             }
//             else
//             {
//                 index2 = null;
//             }
//             details.FileName = index2;
//             details.FileSize = *(ref data + 9492);
//             details.PreviewFileSize = *(ref data + 9496);
//             string handle2;
//             if (retVal)
//             {
//                 ulong num5 = <Module>.strnlen(ref data + 9500, 256uL);
//                 handle2 = new string(ref data + 9500, 0, (int)num5, Encoding.UTF8);
//             }
//             else
//             {
//                 handle2 = null;
//             }
//             details.URL = handle2;
//             details.VotesUp = (uint)(*(ref data + 9756));
//             details.VotesDown = (uint)(*(ref data + 9760));
//             details.Score = *(ref data + 9764);
//             details.NumChildren = (uint)(*(ref data + 9768));
//             return retVal;
//         }

//         public unsafe bool ReleaseQueryUGCRequest(ulong handle)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, handle, *(*(long*)this2 + 32L));
//         }

//         public unsafe bool AddRequiredTag(ulong handle, string tagName)
//         {
//             sbyte* pchTagName = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(tagName));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchTagName, *(*(long*)this2 + 40L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchTagName));
//             return retVal;
//         }

//         public unsafe bool AddExcludedTag(ulong handle, string tagName)
//         {
//             sbyte* pchTagName = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(tagName));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchTagName, *(*(long*)this2 + 48L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchTagName));
//             return retVal;
//         }

//         public unsafe bool SetReturnLongDescription(ulong handle, [MarshalAs(UnmanagedType.U1)] bool bReturnLongDescription)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), this2, handle, bReturnLongDescription, *(*(long*)this2 + 56L));
//         }

//         public unsafe bool SetReturnTotalOnly(ulong handle, [MarshalAs(UnmanagedType.U1)] bool bReturnTotalOnly)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), this2, handle, bReturnTotalOnly, *(*(long*)this2 + 64L));
//         }

//         public unsafe bool SetAllowCachedResponse(ulong handle, uint unMaxAgeSeconds)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32), this2, handle, unMaxAgeSeconds, *(*(long*)this2 + 72L));
//         }

//         public unsafe bool SetCloudFileNameFilter(ulong handle, string matchCloudFileName)
//         {
//             sbyte* pchMatchCloudFileName = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(matchCloudFileName));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchMatchCloudFileName, *(*(long*)this2 + 80L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchMatchCloudFileName));
//             return retVal;
//         }

//         public unsafe bool SetMatchAnyTag(ulong handle, [MarshalAs(UnmanagedType.U1)] bool bMatchAnyTag)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), this2, handle, bMatchAnyTag, *(*(long*)this2 + 88L));
//         }

//         public unsafe bool SetSearchText(ulong handle, string searchText)
//         {
//             sbyte* pchSearchText = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(searchText));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchSearchText, *(*(long*)this2 + 80L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchSearchText));
//             return retVal;
//         }

//         public unsafe bool SetRankedByTrendDays(ulong handle, uint unDays)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32), this2, handle, unDays, *(*(long*)this2 + 104L));
//         }

        public bool IsUpdateHandleValid(ulong handle)
        {
            return ((handle != 18446744073709551615uL) ? 1 : 0) != 0;
        }

//         public unsafe void RequestUGCDetails(ulong publishedFileID, uint maxAgeSeconds, Action<bool, SteamUGCRequestUGCDetailsResult> onCallResult)
//         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct SteamUGCRequestUGCDetailsResult_t,class System::Action<bool,struct SteamSDK::SteamUGCRequestUGCDetailsResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt32), this2, publishedFileID, maxAgeSeconds, *(*(long*)this2 + 112L)), ldftn(OnRequestUGCDetails), onCallResult);
//         }

         public unsafe void CreateItem(uint appId, WorkshopFileType workshopFileType, Action<bool, CreateItemResult> onCallResult)
         {
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct CreateItemResult_t,class System::Action<bool,struct SteamSDK::CreateItemResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,EWorkshopFileType), this2, appId, workshopFileType, *(*(long*)this2 + 120L)), ldftn(OnCreateItem), onCallResult);
         }

         public unsafe ulong StartItemUpdate(uint appId, ulong publishedFileID)
         {
             return 0;
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32,System.UInt64), this2, appId, publishedFileID, *(*(long*)this2 + 128L));
         }

         public unsafe bool SetItemTitle(ulong handle, string title)
         {
             return false;
//             sbyte* pchTitle = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(title));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchTitle, *(*(long*)this2 + 136L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchTitle));
//             return retVal;
         }

         public unsafe bool SetItemDescription(ulong handle, string description)
         {
             return false;
//             sbyte* pchDescription = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(description));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchDescription, *(*(long*)this2 + 144L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchDescription));
//             return retVal;
         }

         public unsafe bool SetItemVisibility(ulong handle, PublishedFileVisibility visibility)
         {
             return false;
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,ERemoteStoragePublishedFileVisibility), this2, handle, visibility, *(*(long*)this2 + 152L));
         }

         public unsafe bool SetItemTags(ulong handle, string[] tags)
         {
             return false;
//             SteamParamStringArray_t steamTags;
//             *(ref steamTags + 8) = tags.Length;
//             ulong num = (ulong)((long)(*(ref steamTags + 8)));
//             ulong num2;
//             if (num <= 2305843009213693951uL)
//             {
//                 num2 = num * 8uL;
//             }
//             else
//             {
//                 num2 = 18446744073709551615uL;
//             }
//             steamTags = <Module>.new[](num2);
//             int i = 0;
//             if (0 < *(ref steamTags + 8))
//             {
//                 long num3 = 0L;
//                 do
//                 {
//                     IntPtr intPtr = Marshal.StringToHGlobalAnsi(tags[i]);
//                     *(num3 + steamTags) = intPtr.ToPointer();
//                     i++;
//                     num3 += 8L;
//                 }
//                 while (i < *(ref steamTags + 8));
//             }
//             ISteamUGC* ptr = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,SteamParamStringArray_t modopt(System.Runtime.CompilerServices.IsConst)*), ptr, handle, ref steamTags, *(*(long*)ptr + 160L));
//             int j = 0;
//             if (0 < *(ref steamTags + 8))
//             {
//                 long handle2 = 0L;
//                 do
//                 {
//                     Marshal.FreeHGlobal((IntPtr)(*(handle2 + steamTags)));
//                     j++;
//                     handle2 += 8L;
//                 }
//                 while (j < *(ref steamTags + 8));
//             }
//             <Module>.delete[](steamTags);
//             return retVal;
         }

         public unsafe bool SetItemContent(ulong handle, string contentFolder)
         {
             return false;
//             sbyte* pchContentFolder = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(contentFolder));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchContentFolder, *(*(long*)this2 + 168L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchContentFolder));
//             return retVal;
         }

         public unsafe bool SetItemPreview(ulong handle, string previewFile)
         {
             return false;
//             sbyte* pchPreviewFile = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(previewFile));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchPreviewFile, *(*(long*)this2 + 176L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchPreviewFile));
//             return retVal;
         }

         public unsafe void SubmitItemUpdate(ulong handle, string changeNote, Action<bool, SubmitItemUpdateResult> onCallResult)
         {
//             sbyte* pchChangeNote = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(changeNote));
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct SubmitItemUpdateResult_t,class System::Action<bool,struct SteamSDK::SubmitItemUpdateResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte) modopt(System.Runtime.CompilerServices.IsConst)*), this2, handle, pchChangeNote, *(*(long*)this2 + 184L)), ldftn(OnSubmitItemUpdate), onCallResult);
//             Marshal.FreeHGlobal((IntPtr)((void*)pchChangeNote));
         }

//         public unsafe ItemUpdateStatus GetItemUpdateProgress(ulong handle, out ulong bytesProcessed, out ulong bytesTotal)
//         {
//             bytesTotal = 0;
//             bytesProcessed = 0;
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(EItemUpdateStatus modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt64*,System.UInt64*), this2, handle, ref bytesProcessed, ref bytesTotal, *(*(long*)this2 + 192L));
//         }

         public unsafe void SubscribeItem(ulong publishedFileID, Action<bool, RemoteStorageSubscribePublishedFileResult> onCallResult)
         {
             // TODO [vicent] current stub implementation
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageSubscribePublishedFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageSubscribePublishedFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, publishedFileID, *(*(long*)this2 + 200L)), ldftn(OnSubscribeItem), onCallResult);
         }

         public unsafe void UnsubscribeItem(ulong publishedFileID, Action<bool, RemoteStorageUnsubscribePublishedFileResult> onCallResult)
         {
             // TODO [vicent] current stub implementation
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             <Module>.SteamSDK.?A0x57ffc328.MakeCall<struct RemoteStorageUnsubscribePublishedFileResult_t,class System::Action<bool,struct SteamSDK::RemoteStorageUnsubscribePublishedFileResult> >(calli(System.UInt64 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64), this2, publishedFileID, *(*(long*)this2 + 208L)), ldftn(OnUnsubscribeItem), onCallResult);
         }

         public unsafe uint GetNumSubscribedItems()
         {
             return 0;
//             ISteamUGC* expr_05 = <Module>.SteamUGC();
//             return calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 216L));
         }

         public unsafe uint GetSubscribedItems(ulong[] publishedFileIDs, uint maxEntries)
         {
             return 0;
//             int pvPublishedFileIDs_07_cp_1 = 0;
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64*,System.UInt32), this2, ref publishedFileIDs[pvPublishedFileIDs_07_cp_1], maxEntries, *(*(long*)this2 + 224L));
         }
// 
//         public unsafe bool GetItemInstallInfo(ulong publishedFileID, out ulong sizeOnDisk, string folder)
//         {
//             sizeOnDisk = 0;
//             sbyte* pchFolder = (sbyte*)((void*)Marshal.StringToHGlobalAnsi(folder));
//             sbyte* publishedFileID2 = pchFolder;
//             if (*(sbyte*)pchFolder != 0)
//             {
//                 do
//                 {
//                     publishedFileID2 += 1L / (long)sizeof(sbyte);
//                 }
//                 while (*(sbyte*)publishedFileID2 != 0);
//             }
//             long num = (long)(publishedFileID2 - pchFolder);
//             ISteamUGC* sizeOnDisk2 = <Module>.SteamUGC();
//             bool retVal = calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.UInt64*,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte)*,System.UInt32), sizeOnDisk2, publishedFileID, ref sizeOnDisk, pchFolder, (uint)num, *(*(long*)sizeOnDisk2 + 232L));
//             Marshal.FreeHGlobal((IntPtr)((void*)pchFolder));
//             return retVal;
//         }

//         public unsafe bool GetItemUpdateInfo(ulong publishedFileID, out bool needsUpdate, out bool isDownloading, out ulong bytesDownloaded, out ulong bytesTotal)
//         {
//             bytesTotal = 0;
//             bytesDownloaded = 0;
//             isDownloading = 0;
//             needsUpdate = 0;
//             ISteamUGC* this2 = <Module>.SteamUGC();
//             return calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt64,System.Boolean*,System.Boolean*,System.UInt64*,System.UInt64*), this2, publishedFileID, ref needsUpdate, ref isDownloading, ref bytesDownloaded, ref bytesTotal, *(*(long*)this2 + 240L));
//         }

//         internal unsafe static void OnQueryCompleted(SteamUGCQueryCompleted_t* result, bool ioFailure, Action<bool, SteamUGCQueryCompleted> action)
//         {
//             action(ioFailure, new SteamUGCQueryCompleted
//             {
//                 Result = (Result)(*(int*)(result + 8L / (long)sizeof(SteamUGCQueryCompleted_t))),
//                 handle = (ulong)(*(long*)result),
//                 NumResultsReturned = (uint)(*(int*)(result + 12L / (long)sizeof(SteamUGCQueryCompleted_t))),
//                 TotalMatchingResults = (uint)(*(int*)(result + 16L / (long)sizeof(SteamUGCQueryCompleted_t))),
//                 CachedData = (*(byte*)(result + 20L / (long)sizeof(SteamUGCQueryCompleted_t)) != 0)
//             });
//         }

//         internal unsafe static void OnRequestUGCDetails(SteamUGCRequestUGCDetailsResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, SteamUGCRequestUGCDetailsResult> action)
//         {
//             int num;
//             if (!ioFailure && *(int*)(result + 8L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)) == 1)
//             {
//                 num = 1;
//             }
//             else
//             {
//                 num = 0;
//             }
//             bool success = (byte)num != 0;
//             SteamUGCRequestUGCDetailsResult data = default(SteamUGCRequestUGCDetailsResult);
//             data.CachedData = (*(byte*)(result + 9776L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)) != 0);
//             data.Details.PublishedFileId = (ulong)(*(long*)result);
//             data.Details.Result = (Result)(*(int*)(result + 8L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.FileType = (WorkshopFileType)(*(int*)(result + 12L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.CreatorAppID = (uint)(*(int*)(result + 16L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.ConsumerAppID = (uint)(*(int*)(result + 20L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             string title;
//             if (success)
//             {
//                 SteamUGCRequestUGCDetailsResult_t* ptr = result + 24L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t);
//                 sbyte* ptr2 = (sbyte*)ptr;
//                 ulong num2;
//                 if (ptr2 == null)
//                 {
//                     num2 = 0uL;
//                 }
//                 else
//                 {
//                     num2 = <Module>.strnlen(ptr2, 129uL);
//                 }
//                 title = new string((sbyte*)ptr, 0, (int)num2, Encoding.UTF8);
//             }
//             else
//             {
//                 title = null;
//             }
//             data.Details.Title = title;
//             string description;
//             if (success)
//             {
//                 SteamUGCRequestUGCDetailsResult_t* ptr3 = result + 153L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t);
//                 sbyte* ptr4 = (sbyte*)ptr3;
//                 ulong num3;
//                 if (ptr4 == null)
//                 {
//                     num3 = 0uL;
//                 }
//                 else
//                 {
//                     num3 = <Module>.strnlen(ptr4, 8000uL);
//                 }
//                 description = new string((sbyte*)ptr3, 0, (int)num3, Encoding.UTF8);
//             }
//             else
//             {
//                 description = null;
//             }
//             data.Details.Description = description;
//             data.Details.SteamIDOwner = (ulong)(*(long*)(result + 8160L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.TimeCreated = (uint)(*(int*)(result + 8168L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.TimeUpdated = (uint)(*(int*)(result + 8172L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.TimeAddedToUserList = (uint)(*(int*)(result + 8176L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.Visibility = (PublishedFileVisibility)(*(int*)(result + 8180L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.Banned = (*(byte*)(result + 8184L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)) != 0);
//             data.Details.AcceptedForUse = (*(byte*)(result + 8185L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)) != 0);
//             data.Details.TagsTruncated = (*(byte*)(result + 8186L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)) != 0);
//             string tags;
//             if (success)
//             {
//                 SteamUGCRequestUGCDetailsResult_t* ptr5 = result + 8187L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t);
//                 sbyte* ptr6 = (sbyte*)ptr5;
//                 ulong num4;
//                 if (ptr6 == null)
//                 {
//                     num4 = 0uL;
//                 }
//                 else
//                 {
//                     num4 = <Module>.strnlen(ptr6, 1025uL);
//                 }
//                 tags = new string((sbyte*)ptr5, 0, (int)num4, Encoding.UTF8);
//             }
//             else
//             {
//                 tags = null;
//             }
//             data.Details.Tags = tags;
//             data.Details.File = (ulong)(*(long*)(result + 9216L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.PreviewFile = (ulong)(*(long*)(result + 9224L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             string fileName;
//             if (success)
//             {
//                 SteamUGCRequestUGCDetailsResult_t* ptr7 = result + 9232L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t);
//                 sbyte* ptr8 = (sbyte*)ptr7;
//                 ulong num5;
//                 if (ptr8 == null)
//                 {
//                     num5 = 0uL;
//                 }
//                 else
//                 {
//                     num5 = <Module>.strnlen(ptr8, 260uL);
//                 }
//                 fileName = new string((sbyte*)ptr7, 0, (int)num5, Encoding.UTF8);
//             }
//             else
//             {
//                 fileName = null;
//             }
//             data.Details.FileName = fileName;
//             data.Details.FileSize = *(int*)(result + 9492L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t));
//             data.Details.PreviewFileSize = *(int*)(result + 9496L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t));
//             string ioFailure2;
//             if (success)
//             {
//                 SteamUGCRequestUGCDetailsResult_t* ptr9 = result + 9500L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t);
//                 sbyte* ptr10 = (sbyte*)ptr9;
//                 ulong action2;
//                 if (ptr10 == null)
//                 {
//                     action2 = 0uL;
//                 }
//                 else
//                 {
//                     action2 = <Module>.strnlen(ptr10, 256uL);
//                 }
//                 ioFailure2 = new string((sbyte*)ptr9, 0, (int)action2, Encoding.UTF8);
//             }
//             else
//             {
//                 ioFailure2 = null;
//             }
//             data.Details.URL = ioFailure2;
//             data.Details.VotesUp = (uint)(*(int*)(result + 9756L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.VotesDown = (uint)(*(int*)(result + 9760L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             data.Details.Score = *(float*)(result + 9764L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t));
//             data.Details.NumChildren = (uint)(*(int*)(result + 9768L / (long)sizeof(SteamUGCRequestUGCDetailsResult_t)));
//             action(ioFailure, data);
//         }

//         internal unsafe static void OnCreateItem(CreateItemResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, CreateItemResult> action)
//         {
//             action(ioFailure, new CreateItemResult
//             {
//                 Result = (Result)(*(int*)result),
//                 PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(CreateItemResult_t))),
//                 UserNeedsToAcceptWorkshopLegalAgreement = (*(byte*)(result + 16L / (long)sizeof(CreateItemResult_t)) != 0)
//             });
//         }

//         internal unsafe static void OnSubmitItemUpdate(SubmitItemUpdateResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, SubmitItemUpdateResult> action)
//         {
//             action(ioFailure, new SubmitItemUpdateResult
//             {
//                 Result = (Result)(*(int*)result),
//                 UserNeedsToAcceptWorkshopLegalAgreement = (*(byte*)(result + 4L / (long)sizeof(SubmitItemUpdateResult_t)) != 0)
//             });
//         }

//         internal unsafe static void OnSubscribeItem(RemoteStorageSubscribePublishedFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageSubscribePublishedFileResult> action)
//         {
//             action(ioFailure, new RemoteStorageSubscribePublishedFileResult
//             {
//                 Result = (Result)(*(int*)result),
//                 PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageSubscribePublishedFileResult_t)))
//             });
//         }

//         internal unsafe static void OnUnsubscribeItem(RemoteStorageUnsubscribePublishedFileResult_t* result, [MarshalAs(UnmanagedType.U1)] bool ioFailure, Action<bool, RemoteStorageUnsubscribePublishedFileResult> action)
//         {
//             action(ioFailure, new RemoteStorageUnsubscribePublishedFileResult
//             {
//                 Result = (Result)(*(int*)result),
//                 PublishedFileId = (ulong)(*(long*)(result + 8L / (long)sizeof(RemoteStorageUnsubscribePublishedFileResult_t)))
//             });
//         }
    }

    public sealed class VoIP
    {
         public unsafe int SampleRate
         {
             get
             {
                 /*ISteamUser* expr_05 = <Module>.SteamUser();
                 return calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_05, *(*expr_05 + 96L));*/
                 return 0;
             }
         }

    //     private unsafe void SetInGameVoiceSpeaking([MarshalAs(UnmanagedType.U1)] bool speaking)
    //     {
    //         ISteamUser* speaking2 = <Module>.SteamUser();
    //         ISteamFriends* this2 = <Module>.SteamFriends();
    //         CSteamID cSteamID;
    //         calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride)), this2, *calli(CSteamID* modreq(System.Runtime.CompilerServices.IsUdtReturn) modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,CSteamID*), speaking2, ref cSteamID, *(*(long*)speaking2 + 16L)), speaking, *(*(long*)this2 + 168L));
    //     }

         public unsafe void StartVoiceRecording()
         {
             System.Diagnostics.Debug.Assert( false, "Not implemented yet!" );
             //this.SetInGameVoiceSpeaking(true);
             //ISteamUser* expr_0C = <Module>.SteamUser();
             //calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_0C, *(*expr_0C + 56L));
         }

         public unsafe VoiceResult GetVoice(byte[] buffer, out uint bytesWritten)
         {
             bytesWritten = 0;
             return VoiceResult.NoData;
    //         uint tmpBytes = 0u;
    //         int dataPtr_09_cp_1 = 0;
    //         ISteamUser* this2 = <Module>.SteamUser();
    //         VoiceResult arg_2D_0 = calli(EVoiceResult modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride),System.Void*,System.UInt32,System.UInt32*,System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride),System.Void*,System.UInt32,System.UInt32*,System.UInt32), this2, 1, ref buffer[dataPtr_09_cp_1], buffer.Length, ref tmpBytes, 0, 0L, 0, 0L, 0, *(*(long*)this2 + 80L));
    //         bytesWritten = tmpBytes;
    //         return arg_2D_0;
         }

         public unsafe VoiceResult GetAvailableVoice(out uint size)
         {
             size = 0;
             return VoiceResult.NoData;
    //         ISteamUser* this2 = <Module>.SteamUser();
    //         uint compressedSize;
    //         VoiceResult arg_1B_0 = calli(EVoiceResult modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.UInt32*,System.UInt32*,System.UInt32), this2, ref compressedSize, 0L, 0, *(*(long*)this2 + 72L));
    //         size = compressedSize;
    //         return arg_1B_0;
         }

         public unsafe void StopVoiceRecording()
         {
             System.Diagnostics.Debug.Assert( false, "Not implemented yet!" );

             //this.SetInGameVoiceSpeaking(false);
             //ISteamUser* expr_0C = <Module>.SteamUser();
             //calli(System.Void modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_0C, *(*expr_0C + 64L));
         }

         public unsafe VoiceResult DecompressVoice(byte[] compressedBuffer, uint size, byte[] uncompressedBuffer, out uint writtenBytes)
         {
             writtenBytes = 0;
             
             return VoiceResult.NoData;
             //int compressedDataPtr_07_cp_1 = 0;
             //int uncompressedDataPtr_10_cp_1 = 0;
    //         uint written = 0u;
    //         ISteamUser* this2 = <Module>.SteamUser();
    //         ISteamUser* expr_1E = <Module>.SteamUser();
    //         int compressedBuffer2 = calli(System.UInt32 modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr), expr_1E, *(*expr_1E + 96L));
    //         VoiceResult arg_45_0 = calli(EVoiceResult modopt(System.Runtime.CompilerServices.CallConvCdecl)(System.IntPtr,System.Void modopt(System.Runtime.CompilerServices.IsConst)*,System.UInt32,System.Void*,System.UInt32,System.UInt32*,System.UInt32), this2, ref compressedBuffer[compressedDataPtr_07_cp_1], size, ref uncompressedBuffer[uncompressedDataPtr_10_cp_1], uncompressedBuffer.Length, ref written, compressedBuffer2, *(*(long*)this2 + 88L));
    //         writtenBytes = written;
    //         return arg_45_0;
         }
    }
}

#endif