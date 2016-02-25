namespace VRage.Game.Components
{
    public abstract class MySyncComponentBase : MyEntityComponentBase
    {
        public abstract void SendCloseRequest();
        public abstract void MarkPhysicsDirty();

        public override string ComponentTypeDebugString
        {
            get { return "Sync"; }
        }
    }
}
