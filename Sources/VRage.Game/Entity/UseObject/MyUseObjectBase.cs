using VRage.Import;
using VRage.ModAPI;
using VRageMath;
using VRageRender.Import;

namespace VRage.Game.Entity.UseObject
{
    public abstract class MyUseObjectBase : IMyUseObject
    {
        public MyUseObjectBase(IMyEntity owner, MyModelDummy dummy)
        {
            Owner = owner;
            Dummy = dummy;
        }

        public IMyEntity Owner
        {
            get;
            private set;
        }

        public MyModelDummy Dummy
        {
            get;
            private set;
        }

        public abstract float InteractiveDistance
        {
            get;
        }

        public abstract MatrixD ActivationMatrix
        {
            get;
        }

        public abstract MatrixD WorldMatrix
        {
            get;
        }

        public abstract int RenderObjectID
        {
            get;
        }

        public virtual int InstanceID
        {
            get { return -1; }
        }

        public abstract bool ShowOverlay
        {
            get;
        }

        public abstract UseActionEnum SupportedActions
        {
            get;
        }

        public abstract bool ContinuousUsage
        {
            get;
        }

        public abstract void Use(UseActionEnum actionEnum, IMyEntity user);

        public abstract MyActionDescription GetActionInfo(UseActionEnum actionEnum);

        public abstract bool HandleInput();

        public abstract void OnSelectionLost();

        public virtual void SetRenderID(uint id)
        {
        }

        public virtual void SetInstanceID(int id)
        {
        }

        public abstract bool PlayIndicatorSound
        {
            get;
        }
    }
}
