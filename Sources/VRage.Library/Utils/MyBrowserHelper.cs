using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Utils
{
    public static class MyBrowserHelper
    {
        public const string IE_PROCESS = "IExplore.exe";
        public static bool OpenInternetBrowser(string url)
        {
#if XB1
			return true;
#else
			try
            {
                try
                {
                    System.Diagnostics.Process.Start(url);
                }
                // System.ComponentModel.Win32Exception is a known exception that occurs when Firefox is default browser.  
                // It actually opens the browser but STILL throws this exception so we can just ignore it.  If not this exception,
                // then attempt to open the URL in IE instead.
                catch (System.ComponentModel.Win32Exception)
                {
                    // sometimes throws exception so we have to just ignore
                    // this is a common .NET bug that no one online really has a great reason for so now we just need to try to open
                    // the URL using IE if we can.
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(IE_PROCESS, url);
                    System.Diagnostics.Process.Start(startInfo);
                    startInfo = null;
                }
            }
            catch (Exception)
            {
                // oper browser failed
                return false;
            }
            return true;
#endif
		}
	}
}
