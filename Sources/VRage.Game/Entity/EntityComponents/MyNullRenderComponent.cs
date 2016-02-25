namespace VRage.Game.Components
{
    public class MyNullRenderComponent : MyRenderComponentBase
    {
        public override object ModelStorage
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public override void SetRenderObjectID(int index, uint ID)
        {
        }

        public override void ReleaseRenderObjectID(int index)
        {
        }

        public override void AddRenderObjects()
        {
        }

        public override void Draw()
        {
        }

        public override bool IsVisible()
        {
            return false;
        }

        protected override bool CanBeAddedToRender()
        {
            return false;
        }

        public override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
        }

        public override void RemoveRenderObjects()
        {
        }

        public override void UpdateRenderEntity(VRageMath.Vector3 colorMaskHSV)
        {
        }

        protected override void UpdateRenderObjectVisibility(bool visible)
        {
        }
    }
}
