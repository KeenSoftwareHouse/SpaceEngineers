using Sandbox.Definitions;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Diagnostics;
using VRage.Game;
using VRageMath;
using VRage.Game.ObjectBuilders.AI.Bot;
using VRageRender;

namespace Sandbox.Game.AI
{
    [MyBotType(typeof(MyObjectBuilder_HumanoidBot))]
    public class MyHumanoidBot : MyAgentBot
    {
        public MyCharacter HumanoidEntity { get { return AgentEntity; } }
        public MyHumanoidBotActions HumanoidActions { get { return m_actions as MyHumanoidBotActions; } }
        public MyHumanoidBotDefinition HumanoidDefinition { get { return m_botDefinition as MyHumanoidBotDefinition; } }
        public MyHumanoidBotLogic HumanoidLogic { get { return AgentLogic as MyHumanoidBotLogic; } }

        public override bool IsValidForUpdate
        {
            get
            {
                return base.IsValidForUpdate;
            }
        }

        protected MyDefinitionId StartingWeaponId
        {
            get 
            {
                if (HumanoidDefinition == null)
                    return new MyDefinitionId();
                else
                    return HumanoidDefinition.StartingWeaponDefinitionId; 
            }
        }

        public MyHumanoidBot(MyPlayer player, MyBotDefinition botDefinition)
            : base(player, botDefinition)
        {
            Debug.Assert(botDefinition is MyHumanoidBotDefinition, "Provided bot definition is not of humanoid type");
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            if (HumanoidEntity == null) return;

            HumanoidActions.AiTargetBase.DebugDraw();

            var headMatrix = HumanoidEntity.GetHeadMatrix(true, true, false, true);
        //    VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 1.0f, false);
            //VRageRender.MyRenderProxy.DebugDrawLine3D(headMatrix.Translation, headMatrix.Translation + headMatrix.Forward * 30, Color.HotPink, Color.HotPink, false);
            if (HumanoidActions.AiTargetBase.HasTarget())
            {
                HumanoidActions.AiTargetBase.DrawLineToTarget(headMatrix.Translation);

                Vector3D targetPos;
                float radius;
                HumanoidActions.AiTargetBase.GetTargetPosition(headMatrix.Translation, out targetPos, out radius);
                if (targetPos != Vector3D.Zero)
                {
                    MyRenderProxy.DebugDrawSphere(targetPos, 0.3f, Color.Red, 0.4f, false);
                    VRageRender.MyRenderProxy.DebugDrawText3D(targetPos, "GetTargetPosition", Color.Red, 1, false);
                }
            }

            VRageRender.MyRenderProxy.DebugDrawAxis(HumanoidEntity.PositionComp.WorldMatrix, 1.0f, false);
            var invHeadMatrix = headMatrix;
            invHeadMatrix.Translation = Vector3.Zero;
            invHeadMatrix = Matrix.Transpose(invHeadMatrix);
            invHeadMatrix.Translation = headMatrix.Translation;
        }
    }
}
