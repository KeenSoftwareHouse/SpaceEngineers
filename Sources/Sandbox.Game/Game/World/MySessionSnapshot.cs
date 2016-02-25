using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage;
using VRage.Game;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using VRage.Library.Utils;
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
