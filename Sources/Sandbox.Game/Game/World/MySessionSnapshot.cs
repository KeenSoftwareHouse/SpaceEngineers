using ParallelTasks;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage;
using VRage.Game;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.World
{
    public class MySessionSnapshot
    {
        private static FastResourceLock m_savingLock = new FastResourceLock();

        public string TargetDir;
        public string SavingDir;
        public MyObjectBuilder_Checkpoint CheckpointSnapshot;
        public MyObjectBuilder_Sector SectorSnapshot;
        public Dictionary<string, byte[]> CompressedVoxelSnapshots;

        public const int MAX_WINDOWS_PATH = 260;

        public ulong SavedSizeInBytes // Set after snapshots has been saved.
        {
            get;
            private set;
        }

        public bool SavingSuccess
        {
            get;
            private set;
        }

        public bool Save()
        {
            bool success = true;
            using (m_savingLock.AcquireExclusiveUsing())
            {
                MySandboxGame.Log.WriteLine("Session snapshot save - START");
                using (var indent = MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
                {
                    Debug.Assert(!string.IsNullOrWhiteSpace(TargetDir), "TargetDir should always be correctly set!");

                    Directory.CreateDirectory(TargetDir);

                    MySandboxGame.Log.WriteLine("Checking file access for files in target dir.");
                    if (!CheckAccessToFiles())
                        return false;

                    var saveAbsPath = SavingDir;
                    if (Directory.Exists(saveAbsPath))
                        Directory.Delete(saveAbsPath, true);
                    Directory.CreateDirectory(saveAbsPath);

                    try
                    {
                        ulong sectorSizeInBytes = 0;
                        ulong checkpointSizeInBytes = 0;
                        ulong voxelSizeInBytes = 0;
                        success = MyLocalCache.SaveSector(SectorSnapshot, SavingDir, Vector3I.Zero, out sectorSizeInBytes) &&
                                  MyLocalCache.SaveCheckpoint(CheckpointSnapshot, SavingDir, out checkpointSizeInBytes) &&
                                  MyLocalCache.SaveLastLoadedTime(TargetDir, DateTime.Now);
                        if (success)
                        {
                            foreach (var entry in CompressedVoxelSnapshots)
                            {
                                voxelSizeInBytes += (ulong)entry.Value.Length;
                                success = success && SaveVoxelSnapshot(entry.Key, entry.Value);
                            }
                        }
                        if (success && Sync.IsServer)
                            success = MyLocalCache.SaveLastSessionInfo(TargetDir);

                        if (success)
                            SavedSizeInBytes = sectorSizeInBytes + checkpointSizeInBytes + voxelSizeInBytes;
                    }
                    catch (Exception ex)
                    {
                        MySandboxGame.Log.WriteLine("There was an error while saving snapshot.");
                        MySandboxGame.Log.WriteLine(ex);
                        ReportFileError(ex);
                        success = false;
                    }

                    if (success)
                    {
                        HashSet<string> saveFiles = new HashSet<string>();
                        foreach (var filepath in Directory.GetFiles(saveAbsPath))
                        {
                            string filename = Path.GetFileName(filepath);

                            var targetFile = Path.Combine(TargetDir, filename);
                            if (File.Exists(targetFile))
                                File.Delete(targetFile);

                            File.Move(filepath, targetFile);
                            saveFiles.Add(filename);
                        }

                        // Clean leftovers from previous saves
                        foreach (var filepath in Directory.GetFiles(TargetDir))
                        {
                            string filename = Path.GetFileName(filepath);
                            if (saveFiles.Contains(filename) || filename == MyTextConstants.SESSION_THUMB_NAME_AND_EXTENSION)
                                continue;

                            File.Delete(filepath);
                        }

                        Directory.Delete(saveAbsPath);

                        //Backup Saves Functionality
                        Backup();
                    }
                    else
                    {
                        // We don't delete previous save, just the new one.
                        if (Directory.Exists(saveAbsPath))
                            Directory.Delete(saveAbsPath, true);
                    }

                }
                MySandboxGame.Log.WriteLine("Session snapshot save - END");
            }
            return success;
        }

        /// <summary>
        /// Backup Saves to Backup folder of save game according to MySession.Static.MaxBackupSaves on advanced settings.
        /// If this is zero the backup functionality is disabled.
        /// </summary>
        private void Backup()
        {
            //Backup Section Start
            //----------------------------------------------
            if (MySession.Static.MaxBackupSaves > 0)
            {
                //GR: Used to create unique backup folder name
                var backupName = DateTime.Now.ToString("yyyy-MM-dd HHmmss");

                //GR: The backup folder for the current save
                var resultFolder = Path.Combine(TargetDir, MyTextConstants.SESSION_SAVE_BACKUP_FOLDER, backupName);

                //GR: Create Backup directory if doesn't exist
                Directory.CreateDirectory(resultFolder);

                //GR: First copy files to backup folder
                foreach (var filepath in Directory.GetFiles(TargetDir))
                {
                    //An issue may arise here when reaching maximum path length at least for Windows 7
                    var resultPath = Path.Combine(resultFolder, Path.GetFileName(filepath));
                    if (resultPath.Length <= MAX_WINDOWS_PATH)
                    {
                        File.Copy(filepath, resultPath, true);
                    }
                    else
                    {
                        Debug.Assert(false, "File "+Path.GetFileName(filepath) + " results in file path longer than 260 characters and cannot be copied to backup! Please report the save to the developers");
                    }
                }

                //GR: Removed oldest backup directories. How many to keep is indicated by MaxBackupSaves variable.
                var backdirs = Directory.GetDirectories(Path.Combine(TargetDir, MyTextConstants.SESSION_SAVE_BACKUP_FOLDER));
                if (!IsSorted(backdirs))
                {
                    Array.Sort(backdirs);
                }
                if (backdirs.Length > MySession.Static.MaxBackupSaves)
                {
                    var savesToDeleteNum = backdirs.Length - MySession.Static.MaxBackupSaves;
                    for (int i = 0; i < savesToDeleteNum; i++)
                    {
                        Directory.Delete(backdirs[i], true);
                    }
                }
            }
            else if (MySession.Static.MaxBackupSaves == 0)
            {
                //GR: Delete the backup folder if feature is disabled (MaxBackupSaves == 0) and remove backup folder if exists
                if (Directory.Exists(Path.Combine(TargetDir, MyTextConstants.SESSION_SAVE_BACKUP_FOLDER)))
                {
                    Directory.Delete(Path.Combine(TargetDir, MyTextConstants.SESSION_SAVE_BACKUP_FOLDER), true);
                }
            }
            //----------------------------------------------
            //Backup Section End
        }


        /// <summary>
        /// Determines if string array is sorted from A -> Z
        /// </summary>
        public static bool IsSorted(string[] arr)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i - 1].CompareTo(arr[i]) > 0) // If previous is bigger, return false
                {
                    return false;
                }
            }
            return true;
        }

        private bool SaveVoxelSnapshot(string storageName, byte[] snapshotData)
        {
            var path = Path.Combine(SavingDir, storageName + MyVoxelConstants.FILE_EXTENSION);
            try { File.WriteAllBytes(path, snapshotData); }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLine(string.Format("Failed to write voxel file '{0}'", path));
                MySandboxGame.Log.WriteLine(ex);
                ReportFileError(ex);
                return false;
            }

            return true;
        }

        private bool CheckAccessToFiles()
        {
            foreach (var filePath in Directory.GetFiles(TargetDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (filePath == MySession.Static.ThumbPath)
                    continue;

                if (!MyFileSystem.CheckFileWriteAccess(filePath))
                {
                    MySandboxGame.Log.WriteLine(string.Format("Couldn't access file '{0}'.", Path.GetFileName(filePath)));
                    return false;
                }
            }
            return true;
        }

        private static void ReportFileError(Exception ex)
        {
            var exceptionText = ex.ToString();
            if (exceptionText.Contains("System.IO.IOException: The process cannot access the file"))
            {
                int start, end;
                start = exceptionText.IndexOf('\'');
                end = exceptionText.IndexOf('\'', start+1);
                exceptionText = exceptionText.Remove(start, end - start + 1);
            }
            MyAnalyticsTracker.ReportError(MyAnalyticsTracker.SeverityEnum.Error, exceptionText, async: false);
        }

        public void SaveParallel(Action completionCallback = null)
        {
            var copy = this;
            Action savingAction = () => { SavingSuccess = copy.Save(); };
            if (completionCallback != null)
                Parallel.Start(savingAction, completionCallback);
            else
                Parallel.Start(savingAction);
        }

        public static void WaitForSaving()
        {
            int waiters = 0;
            do
            {
                using (m_savingLock.AcquireExclusiveUsing())
                {
                    waiters = m_savingLock.ExclusiveWaiters;
                }
            }
            while (waiters > 0);
        }
    }
}
