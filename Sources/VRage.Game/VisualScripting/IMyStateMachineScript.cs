namespace VRage.Game.VisualScripting
{
    public interface IMyStateMachineScript
    {
        string TransitionTo { get; set; }
        long OwnerId { get; set; }

        [VisualScriptingMember(true)]
        void Init();
        [VisualScriptingMember(true)]
        void Update();
        [VisualScriptingMember(true)]
        void Dispose();
        [VisualScriptingMember(true, true)]
        void Complete(string transitionName = "Completed");
        [VisualScriptingMember(false, true)]
        long GetOwnerId();
        [VisualScriptingMember(true)]
        void Deserialize();
    }
}
