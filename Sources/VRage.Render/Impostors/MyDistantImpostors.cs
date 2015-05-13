using VRage.Utils;
using VRageRender.Effects;

namespace VRageRender
{
    internal class MyDistantImpostors : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.DistantImpostors;
        }

        public static MyDistantImpostors Static;

        static readonly MyDistantObjectImpostors m_objectImpostors;
        static readonly MyVoxelMapImpostors m_voxelImpostors;

        public static MyImpostorProperties[] ImpostorProperties;

        static MyDistantImpostors()
        {
            MyRender.Log.WriteLine("MyDistantImpostors()");

            const float defaultSize = 200000;

            m_objectImpostors = new MyDistantObjectImpostors();
            m_objectImpostors.Scale = MySectorConstants.SECTOR_SIZE / defaultSize;

            m_voxelImpostors = new MyVoxelMapImpostors();

            MyRender.RegisterRenderModule(MyRenderModuleEnum.DistantImpostors, "Distant impostors", PrepareForDraw, MyRenderStage.PrepareForDraw);
            MyRender.RegisterRenderModule(MyRenderModuleEnum.DistantImpostors, "Distant impostors", Draw, MyRenderStage.Background);
        }

        public override void LoadContent()
        {
            Static = this;
            m_objectImpostors.LoadData();
            m_objectImpostors.LoadContent();
            m_voxelImpostors.LoadContent();
        }

        public override void ReloadContent()
        {
            m_voxelImpostors.LoadContent();
        }

        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("MyDistantImpostors.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            m_objectImpostors.UnloadContent();
            m_voxelImpostors.UnloadContent();
            m_objectImpostors.UnloadData();

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyDistantImpostors.UnloadContent - END");
        }

        public static void Update()
        {
            m_objectImpostors.Update();
        }

        public static void PrepareForDraw()
        {
            if (MyRenderConstants.RenderQualityProfile.EnableDistantImpostors)
            {
                m_voxelImpostors.PrepareForDraw(
                    MyRender.GetEffect(MyEffects.DistantImpostors) as MyEffectDistantImpostors);
            }
        }

        public static void Draw()
        {
            if (MyRenderConstants.RenderQualityProfile.EnableDistantImpostors)
            {
                Update();
                m_objectImpostors.Draw(MyRender.GetEffect(MyEffects.DistantImpostors) as MyEffectDistantImpostors);
                m_voxelImpostors.Draw(MyRender.GetEffect(MyEffects.DistantImpostors) as MyEffectDistantImpostors);
            }
        }
    }
}
