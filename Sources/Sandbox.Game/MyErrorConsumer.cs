
namespace Sandbox
{
    public interface IErrorConsumer
    {
        void OnError(string header, string message, string callstack);
    }

    public class MyGameErrorConsumer : IErrorConsumer
    {
        public void OnError(string header, string message, string callstack)
        {
            string error = header + ": " + message + "\n\nStack:\n" + callstack;

#if DEBUG
            System.Diagnostics.Debug.Fail(error);
#else // !DEBUG
#if !XB1
            VRage.Utils.MyMessageBox.Show(header, message);
#endif // XB1
#endif // !DEBUG
        }
    }
}
