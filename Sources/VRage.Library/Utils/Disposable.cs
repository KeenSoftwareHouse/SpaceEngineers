using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Utils
{
    public class Disposable : IDisposable
    {
#if DEBUG && !XB1
        StackTrace CreationStackTrace;
#endif

        public Disposable(bool collectStack = false)
		{
#if DEBUG && !XB1
            if (collectStack)
            {
                CreationStackTrace = new StackTrace(1, true);
            }
#endif
		}

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        ~Disposable()
        {
            string caption = "Dispose not called!";
            string text = string.Format("Dispose was not called for '{0}'", GetType().FullName);
#if DEBUG && !XB1
            if (CreationStackTrace != null)
            {
                text += Environment.NewLine;
                text += CreationStackTrace.ToString();
            }
#endif

			System.Diagnostics.Trace.Fail(caption, text);
        }
    }
}
