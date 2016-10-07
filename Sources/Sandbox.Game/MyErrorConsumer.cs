
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
            System.Diagnostics.Debug.Assert(false, header + ": " + message + "\n\nStack:\n" + callstack);
        }
    }
}
