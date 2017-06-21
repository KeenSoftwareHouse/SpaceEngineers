namespace VRage.Game.VisualScripting
{
    public interface IMyLevelScript
    {
        [VisualScriptingMember(true)]
        void Dispose();
        [VisualScriptingMember(true)]
        void Update();
        [VisualScriptingMember(true)]
        void GameStarted();
        [VisualScriptingMember(true)]
        void GameFinished();
    }
}
