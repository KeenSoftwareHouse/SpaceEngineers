#if !XB1
using System.Reflection;
using System.Threading;

//  Single Process Instance Object
//  Enforcing a rule that only one instance of process is running is an interesting task. In order to enhance the detection of other instances of a process, 
//  I decided to use a named mutex synchronization object. 
//  http://www.codeproject.com/KB/cs/cssingprocess.aspx

namespace VRage.Utils
{
    public class MySingleProgramInstance
    {
        //  Win32 API calls necesary to raise an unowned processs main window
        //[DllImport("user32.dll")] 
        //private static extern bool SetForegroundWindow(IntPtr hWnd);
        //[DllImport("user32.dll")] 
        //private static extern bool ShowWindowAsync(IntPtr hWnd,int nCmdShow);
        //[DllImport("user32.dll")] 
        //private static extern bool IsIconic(IntPtr hWnd);

        //private const int SW_RESTORE = 9;

        Mutex m_mutex;
        bool m_weOwn = false;


        public MySingleProgramInstance()
        {   
            //  Initialize a named mutex and attempt to
            //  get ownership immediately 
            m_mutex = new Mutex(
                true, // desire intial ownership
                Assembly.GetExecutingAssembly().GetName().Name, out m_weOwn);
        }

        public MySingleProgramInstance(string identifier)
        {   
            //  Initialize a named mutex and attempt to
            //  get ownership immediately.
            //  se an addtional identifier to lower
            //  our chances of another process creating
            //  a mutex with the same name.
            
            m_mutex = new Mutex(
                true, // desire intial ownership
                identifier, out m_weOwn);

            //m_mutex = new Mutex(
            //    true, // desire intial ownership
            //    Assembly.GetExecutingAssembly().GetName().Name + identifier, out m_weOwn);
        }

        public bool IsSingleInstance
        {
            //  If we don't own the mutex than
            //  we are not the first instance.
            get {return m_weOwn;}
        }

        //public void RaiseOtherProcess()
        //{
        //    Process proc = Process.GetCurrentProcess();

        //    //  Using Process.ProcessName does not function properly when
        //    //  the actual name exceeds 15 characters. Using the assembly 
        //    //  name takes care of this quirk and is more accruate than 
        //    //  other work arounds.

        //    string assemblyName =  Assembly.GetExecutingAssembly().GetName().Name;
        //    foreach (Process otherProc in Process.GetProcessesByName(assemblyName))
        //    {
        //        //  Ignore "this" process
        //        if (proc.Id != otherProc.Id)
        //        {
        //            // Found a "same named process".
        //            // Assume it is the one we want brought to the foreground.
        //            // Use the Win32 API to bring it to the foreground.
        //            IntPtr hWnd = otherProc.MainWindowHandle;
        //            if (IsIconic(hWnd))
        //            {
        //                ShowWindowAsync(hWnd,SW_RESTORE);
        //            }
        //            SetForegroundWindow(hWnd);
        //            break;
        //        }
        //    }
        //}

        public void Close()
        {
            if (m_weOwn)
            {
                //  If we own the mutex than release it so that other "same" processes can now start.
                m_mutex.ReleaseMutex();
                m_mutex.Close();    //  It looks like we MUST call Close, otherwise mutex stays alive and we can't start another process even if original is already gone....
                m_weOwn = false;
            }
        }
    }
}
#endif // !XB1
