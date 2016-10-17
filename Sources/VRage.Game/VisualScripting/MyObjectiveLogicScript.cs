namespace VRage.Game.VisualScripting
{
    public class MyObjectiveLogicScript : IMyStateMachineScript
    {
        public string TransitionTo { get; set; }
        public long OwnerId { get; set; }
        public virtual void Init()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Deserialize()
        {
        }

        public virtual void Dispose()
        {
        }

        public void Complete(string transitionName = "Completed")
        {
            TransitionTo = transitionName;
        }

        public long GetOwnerId()
        {
            return OwnerId;
        }
    }
}
