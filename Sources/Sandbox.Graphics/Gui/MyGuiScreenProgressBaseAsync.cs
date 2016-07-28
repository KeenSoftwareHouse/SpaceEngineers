using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Graphics.GUI
{
    delegate void ActionDoneHandler<T>(IAsyncResult asyncResult, T asyncState);
    delegate void ErrorHandler<T>(Exception exception, T asyncState);

    abstract class MyGuiScreenProgressBaseAsync<T> : MyGuiScreenProgressBase
    {
        struct ProgressAction
        {
            public IAsyncResult AsyncResult;
            public ActionDoneHandler<T> ActionDoneHandler;
            public ErrorHandler<T> ErrorHandler;
        }

        private LinkedList<ProgressAction> m_actions = new LinkedList<ProgressAction>();
        private string m_constructorStackTrace;

        protected MyGuiScreenProgressBaseAsync(MyStringId progressText, MyStringId? cancelText = null)
            : base(progressText, cancelText)
        {
#if !XB1
            if (Debugger.IsAttached)
            {
                m_constructorStackTrace = Environment.StackTrace;
            }
#endif // !XB1
        }

        // Sets IAsyncResult which is checked for completed on each update
        protected void AddAction(IAsyncResult asyncResult, ErrorHandler<T> errorHandler = null)
        {
            AddAction(asyncResult, OnActionCompleted, errorHandler);
        }

        protected void AddAction(IAsyncResult asyncResult, ActionDoneHandler<T> doneHandler, ErrorHandler<T> errorHandler = null)
        {
            Debug.Assert(asyncResult.AsyncState is T, "AsyncState must be of type T");
            m_actions.AddFirst(new ProgressAction() { AsyncResult = asyncResult, ActionDoneHandler = doneHandler, ErrorHandler = errorHandler ?? OnError });
        }

        protected void CancelAll()
        {
            m_actions.Clear();
        }

        protected override void OnCancelClick(MyGuiControlButton sender)
        {
            CancelAll();
            base.OnCancelClick(sender);
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            var current = m_actions.First;
            while (current != null)
            {
                if (current.Value.AsyncResult.IsCompleted)
                {
                    // Call handler
                    try
                    {
                        current.Value.ActionDoneHandler(current.Value.AsyncResult, (T)current.Value.AsyncResult.AsyncState);
                    }
                    catch (Exception e)
                    {
                        current.Value.ErrorHandler(e, (T)current.Value.AsyncResult.AsyncState);
                    }
                    var toRemove = current;
                    current = current.Next;
                    m_actions.Remove(toRemove);
                }
                else
                {
                    current = current.Next;
                }
            }

            return State == MyGuiScreenState.OPENED;
        }

        // Called when action completed
        protected virtual void OnActionCompleted(IAsyncResult asyncResult, T asyncState)
        {
            // Do nothing, childs will override
        }

        protected virtual void OnError(Exception exception, T asyncState)
        {
            // When error occured, log and rethrow
            MyLog.Default.WriteLine(exception);
            throw exception;
        }
        
        protected void Retry()
        {
            m_actions.Clear();
            ProgressStart();
        }
    }
}
