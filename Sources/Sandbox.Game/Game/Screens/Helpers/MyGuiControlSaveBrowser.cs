using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.DirectoryBrowser;
using VRage;
using VRage.FileSystem;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlSaveBrowser : MyGuiControlDirectoryBrowser
    {
        private readonly List<FileInfo>                     m_saveEntriesToCreate       = new List<FileInfo>();        
        private readonly Dictionary<string, MyWorldInfo>    m_loadedWorldsByFilePaths   = new Dictionary<string, MyWorldInfo>();
        private readonly HashSet<string>                    m_loadedDirectories         = new HashSet<string>(); 

        // The top most directory should be saves folder and no usual files should be displayed
        public MyGuiControlSaveBrowser() : base(MyFileSystem.SavesPath, MyFileSystem.SavesPath, filePredicate: info => false)
        {
            SetColumnName(1, MyTexts.Get(MyCommonTexts.Loaded));
            // Date comparer
            SetColumnComparison(1, (cellA, cellB) =>
            {
                var cellAData = cellA.UserData as FileInfo;
                var cellBData = cellB.UserData as FileInfo;

                if(cellAData == cellBData)
                {
                    if(cellAData == null) return 0;
                }
                else
                {
                    if (cellAData == null) return -1;
                    if (cellBData == null) return 1;
                }

                return  m_loadedWorldsByFilePaths[cellAData.DirectoryName].LastLoadTime
                        .CompareTo(
                        m_loadedWorldsByFilePaths[cellBData.DirectoryName].LastLoadTime);
            });
        }

        // Helper for retrieval of directory infos.
        public DirectoryInfo GetDirectory(Row row)
        {
            if (row == null) return null;

            return row.UserData as DirectoryInfo;
        }

        // Helper method for World info access.
        public Tuple<string, MyWorldInfo> GetSave(Row row)
        {
            if(row == null) return null;

            var fileInfo = row.UserData as FileInfo;
            if(fileInfo == null) return null;

            var directory = Path.GetDirectoryName(fileInfo.FullName);
            var worldInfo = m_loadedWorldsByFilePaths[Path.GetDirectoryName(fileInfo.FullName)];

            return new Tuple<string, MyWorldInfo>(directory, worldInfo);
        }

        // Takes a look into selected saves folder and looks for backups folder.
        // Changes the directory afterwards.
        public void AccessBackups()
        {
            if(SelectedRow == null) return;
            var saveData = SelectedRow.UserData as FileInfo;

            var backupDirectory = saveData.Directory.GetDirectories().FirstOrDefault(dir => dir.Name.StartsWith("Backup"));
            if(backupDirectory == null)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageText: MyTexts.Get(MyCommonTexts.SaveBrowserMissingBackup),
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError)));
                return;
            }

            CurrentDirectory = backupDirectory.FullName;
        }

        protected override void AddFolderRow(DirectoryInfo dir)
        {
            var files = dir.GetFiles();

            // Create a file-like entry for folders with saved games
            bool isSaveFolder = false;
            foreach (var fileInfo in files)
            {
                // A little bit hacky approach, but will at least keep people from
                // adding saves into saves.
                if (fileInfo.Name == "Sandbox.sbc")
                {
                    // Safety check for adding corrupted saves that were not loaded
                    // by system properly. They cannot be used.
                    if(m_loadedWorldsByFilePaths.ContainsKey(fileInfo.DirectoryName))
                        m_saveEntriesToCreate.Add(fileInfo);

                    isSaveFolder = true;
                    break;
                }
            }

            // Normal entry for normal save
            if(!isSaveFolder)
                base.AddFolderRow(dir);
        }

        // Override to prevent early calls.
        public override void Refresh()
        {
            RefreshTheWorldInfos();
        }

        public void ForceRefresh()
        {
            // Add loading mini screen and force the refresh of probably already loaded directory.
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, StartLoadingWorldInfos, OnLoadingFinished));  
        }

        // Refresh after the loading was done.
        private void RefreshAfterLoaded()
        {
            base.Refresh();

            // First sort enties by last load time
            m_saveEntriesToCreate.Sort((fileA, fileB) => 
                m_loadedWorldsByFilePaths[fileB.DirectoryName].LastLoadTime
                .CompareTo(
                m_loadedWorldsByFilePaths[fileA.DirectoryName].LastLoadTime));

            // Lazy addition of the saved games
            foreach (var fileInfo in m_saveEntriesToCreate)
            {
                AddSavedGame(fileInfo);
            }
            m_saveEntriesToCreate.Clear();
        }

        private void AddSavedGame(FileInfo fileInfo)
        {           
            // Create row
            var row = new Row(fileInfo);
            // Add single cell
            var newSaveCell = new Cell(
                text: m_loadedWorldsByFilePaths[fileInfo.DirectoryName].SessionName,
                userData: fileInfo,
                icon: FileCellIconTexture,
                iconOriginAlign: FileCellIconAlign
                );
            // Corrupted worlds should be red
            if (m_loadedWorldsByFilePaths[fileInfo.DirectoryName].IsCorrupted) newSaveCell.TextColor = Color.Red;
            row.AddCell(newSaveCell);

            // Add creation time
            row.AddCell(new Cell(
                text: fileInfo.CreationTime.ToString()
                ));

            Add(row);
        }

        // Loads checkpoint data and refreshes table cells.
        // Async.
        private void RefreshTheWorldInfos()
        {
            // Use cached data if the directory was already loaded once.
            if(!m_loadedDirectories.Contains(CurrentDirectory))
            {
                // Add loading mini screen
                MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, StartLoadingWorldInfos, OnLoadingFinished));              
            }
            else
            {
                RefreshAfterLoaded();
            }
        }

        // Starts Async loading.
        private IMyAsyncResult StartLoadingWorldInfos()
        {
            return new MyLoadWorldInfoListResult(CurrentDirectory);
        }

        // Checks for corrupted worlds and refreshes the table cells.
        private void OnLoadingFinished(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;

            m_loadedDirectories.Add(CurrentDirectory);
            foreach (var saveTuple in loadListRes.AvailableSaves)
            {
                m_loadedWorldsByFilePaths[saveTuple.Item1] = saveTuple.Item2;
            }

            if (loadListRes.ContainsCorruptedWorlds)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.SomeWorldFilesCouldNotBeLoaded),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(messageBox);
            }

            RefreshAfterLoaded();

            // Close the loading miniscreen
            screen.CloseScreen();
        }

        // For backup folders we want to get up two levels.
        protected override void OnBackDoubleclicked()
        {
            if(m_currentDir.Name.StartsWith("Backup"))
                CurrentDirectory = m_currentDir.Parent.Parent.FullName;
            else
                base.OnBackDoubleclicked();
        }
    }
}
