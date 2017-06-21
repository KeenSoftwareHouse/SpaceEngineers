using System;
using System.IO;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Gui.DirectoryBrowser
{
    public class MyCancelEventArgs
    {
        public bool Cancel { get; set; }
        public MyCancelEventArgs()
        {
            Cancel = false;
        }
    }

    public sealed class MyDirectoryChangeCancelEventArgs : MyCancelEventArgs
    {
        // DO NOT USE TO SET OUTSIDE OF DIRECTORY BROWSER Class!
        public string From { get; set; }
        public string To { get; set; }
        public MyGuiControlDirectoryBrowser Browser { get; private set; }
        public MyDirectoryChangeCancelEventArgs(string from, string to, MyGuiControlDirectoryBrowser browser) { From = from; To = to; Browser = browser; }
    }

    public class MyGuiControlDirectoryBrowser : MyGuiControlTable
    {
        protected readonly MyDirectoryChangeCancelEventArgs m_cancelEvent;
        protected DirectoryInfo m_topMostDir;
        protected DirectoryInfo m_currentDir;
        protected Row m_backRow;

        #region File and Folder style accessors

        public Color? FolderCellColor { get; set; }
        public MyGuiHighlightTexture FolderCellIconTexture { get; set; }
        public MyGuiDrawAlignEnum FolderCellIconAlign { get; set; }

        public Color? FileCellColor { get; set; }
        public MyGuiHighlightTexture FileCellIconTexture { get; set; }
        public MyGuiDrawAlignEnum FileCellIconAlign { get; set; }

        #endregion

        public Predicate<DirectoryInfo> DirPredicate { get; private set; }
        public Predicate<FileInfo> FilePredicate { get; private set; }

        #region Public Events

        public event Action<MyDirectoryChangeCancelEventArgs> DirectoryChanging;
        public event Action<MyGuiControlDirectoryBrowser, string> DirectoryChanged;
        public event Action<MyGuiControlDirectoryBrowser, string> FileDoubleClick;
        public event Action<MyGuiControlDirectoryBrowser, string> DirectoryDoubleclick;
        public event Action<MyDirectoryChangeCancelEventArgs> DirectoryDoubleclicking;

        #endregion


        public string CurrentDirectory
        {
            get { return m_currentDir.FullName; }
            set { TraverseToDirectory(value); }
        }

        public MyGuiControlDirectoryBrowser(string topMostDirectory = null, 
                                            string initialDirectory = null, 
                                            Predicate<DirectoryInfo> dirPredicate = null, 
                                            Predicate<FileInfo> filePredicate = null)
        {
            // Empty strings are also considered invalid
            if(!string.IsNullOrEmpty(topMostDirectory))
            {
                m_topMostDir = new DirectoryInfo(topMostDirectory);
            }

            if(!string.IsNullOrEmpty(initialDirectory))
            {
                m_currentDir = new DirectoryInfo(initialDirectory);
            }
            else
            {
                m_currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            DirPredicate = dirPredicate;
            FilePredicate = filePredicate;

            FolderCellIconTexture = MyGuiConstants.TEXTURE_ICON_MODS_LOCAL;
            FolderCellIconAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;

            ItemDoubleClicked += OnItemDoubleClicked;
            // Init basic look for less usage confusion
            ColumnsCount = 2;
            SetCustomColumnWidths(new float[] { 0.65f, 0.35f });
            SetColumnName(0, MyTexts.Get(MyCommonTexts.Name));
            SetColumnName(1, MyTexts.Get(MyCommonTexts.Created));
            SetColumnComparison(0, (cellA, cellB) => cellA.Text.CompareToIgnoreCase(cellB.Text));
            SetColumnComparison(1, (cellA, cellB) => cellB.Text.CompareToIgnoreCase(cellA.Text));

            m_cancelEvent = new MyDirectoryChangeCancelEventArgs(null, null, this);
            Refresh();
        }

        public virtual void Refresh()
        {
            Clear();

            var directories = m_currentDir.GetDirectories();
            var files = m_currentDir.GetFiles();

            // User can go back only when not in top most dir
            if (!m_topMostDir.FullName.TrimEnd(Path.DirectorySeparatorChar).Equals(m_currentDir.FullName, StringComparison.OrdinalIgnoreCase))
                AddBackRow();

            foreach (var directoryInfo in directories)
            {
                // Discard all thet do not pass the directory predicate.
                if (DirPredicate != null && !DirPredicate(directoryInfo))
                    continue;

                AddFolderRow(directoryInfo);
            }

            foreach (var fileInfo in files)
            {
                // Discard all thet do not pass the file predicate.
                if(FilePredicate != null && !FilePredicate(fileInfo))
                    continue;

                AddFileRow(fileInfo);
            }

            ScrollToSelection();
        }

        // Adds file entry
        protected virtual void AddFileRow(FileInfo file)
        {
            // Create row
            var row = new Row(file);
            // Add single cell
            row.AddCell(new Cell(
                text: file.Name,
                userData: file,
                icon: FileCellIconTexture,
                iconOriginAlign: FileCellIconAlign
                ));

            // Add creation time
            row.AddCell(new Cell(
                text: file.CreationTime.ToString()
                ));

            Add(row);
        }

        // Adds folder entry
        protected virtual void AddFolderRow(DirectoryInfo dir)
        {
            // Create row
            var row = new Row(dir);
            // Add single cell
            row.AddCell(new Cell(
                text: dir.Name,
                userData: dir,
                icon: FolderCellIconTexture,
                iconOriginAlign: FolderCellIconAlign
                ));
            
            // add filler - needed for sorting otherwise crashes
            row.AddCell(new Cell(String.Empty));

            Add(row);
        }
        
        // Adds back entry that allows user to go up a level
        protected virtual void AddBackRow()
        {
            // Init new row
            if(m_backRow == null)
            {
                m_backRow = new Row();
                m_backRow.AddCell(new Cell("..", icon: MyGuiConstants.TEXTURE_BLUEPRINTS_ARROW, iconOriginAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            }

            Add(m_backRow);
        }

        // Change the directory
        private void TraverseToDirectory(string path)
        {
            // Path needs to be child of top most directory
            if(path == m_currentDir.FullName ||
              (m_topMostDir != null && !m_topMostDir.IsParentOf(path)))
                return;

            // Try prompting user fro cancel.
            if (NotifyDirectoryChanging(m_currentDir.FullName, path))
            {
                return;
            }

            // Move
            m_currentDir = new DirectoryInfo(path);
            Refresh();

            // Call after everything is up and ready.
            if(DirectoryChanged != null)
                DirectoryChanged(this, m_currentDir.FullName);
        }

        // Handles doubleclicks on per item basis
        private void OnItemDoubleClicked(MyGuiControlTable myGuiControlTable, EventArgs eventArgs)
        {
            if (eventArgs.RowIndex >= RowsCount)
                return;

            var row = GetRow(eventArgs.RowIndex);
            
            if(row == null) return;

            // Back entry double clicked
            if(row == m_backRow)
            {
                OnBackDoubleclicked();
                return;
            }

            // Traverse to directory?
            var directoryInfo = row.UserData as DirectoryInfo;
            if (directoryInfo != null)
            {
                OnDirectoryDoubleclicked(directoryInfo);
                return;
            }

            // File 
            var fileInfo = row.UserData as FileInfo;
            if (fileInfo != null)
            {
                OnFileDoubleclicked(fileInfo);
            }
        }

        // Default behavior is to traverse to directory
        protected virtual void OnDirectoryDoubleclicked(DirectoryInfo info)
        {
            // Ask user for cancelation
            if (NotifyDirectoryChanging(m_currentDir.FullName, info.FullName))
            {
                return;
            }

            TraverseToDirectory(info.FullName);
        }

        // Just handle the callbacks
        protected virtual void OnFileDoubleclicked(FileInfo info)
        {
            if(FileDoubleClick != null)
                FileDoubleClick(this, info.FullName);
        }

        // Default behavior is go up a level
        protected virtual void OnBackDoubleclicked()
        {
            // Already to most -- drive root
            if (m_currentDir.Parent == null)
                return;

            var path = m_currentDir.Parent.FullName;

            // Ask user for cancelation
            if (NotifyDirectoryChanging(m_currentDir.FullName, path))
            {
                return;
            }

            TraverseToDirectory(path);
        }

        // Fires the Directorty changing event.
        protected bool NotifyDirectoryChanging(string from, string to)
        {
            // Ask user for cancelation
            if (DirectoryChanging != null)
            {
                m_cancelEvent.From = from;
                m_cancelEvent.To = to;
                m_cancelEvent.Cancel = false;
                DirectoryChanging(m_cancelEvent);
                return m_cancelEvent.Cancel;
            }

            return false;
        }

        // Handle back requests from mouse and backspace
        public override MyGuiControlBase HandleInput()
        {
            if(MyInput.Static.IsNewXButton1MousePressed() || MyInput.Static.IsNewKeyPressed(MyKeys.Back))
                OnBackDoubleclicked();

            return base.HandleInput();
        }

        // Sets the top most and current directory
        public bool SetTopMostAndCurrentDir(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);
            if (dirInfo.Exists)
            {
                m_topMostDir = dirInfo;
                m_currentDir = dirInfo;

                return true;
            }

            return false;
        }
    }
}
