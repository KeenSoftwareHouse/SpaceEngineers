using System.Diagnostics;
using VRage.Generics;
using VRage.Utils;

namespace VRageRender.Animations
{
    /// <summary>
    /// Animation state machine node is a representation of one state inside MyAnimationStateMachine.
    /// </summary>
    public class MyAnimationStateMachineNode : MyStateMachineNode
    {
        // root animation node of animation tree
        MyAnimationTreeNode m_rootAnimationNode = null;
        // get/set root animation node of the animation tree
        public MyAnimationTreeNode RootAnimationNode
        {
            get { return m_rootAnimationNode; }
            set { m_rootAnimationNode = value; }
        }

        // Constructor of empty node, pass node name.
        public MyAnimationStateMachineNode(string name)
            : base(name)
        {
        }

        // Constructor of node having single animation.
        // Parameter animationClip must not be null.
        public MyAnimationStateMachineNode(string name, MyAnimationClip animationClip)
            : base(name)
        {
            if (animationClip != null)
            {
                var nodeTrack = new MyAnimationTreeNodeTrack();
                nodeTrack.SetClip(animationClip);
                m_rootAnimationNode = nodeTrack;
            }
            else
            {
                Debug.Fail("Creating single animation node in machine " + this.Name + ", node name "
                + name + ": Animation clip must not be null!");            
            }
        }

        protected override MyStateMachineTransition QueryNextTransition()
        {
            for (int i = 0; i < OutTransitions.Count; i++)
            {
                if (OutTransitions[i].Name == MyStringId.NullOrEmpty && OutTransitions[i].Evaluate()) // first transition that is valid is used
                {
                    // Needs to be override because of the empty name condition
                    return OutTransitions[i];
                }
            }

            return null; // stay in current state
        }

        public override void OnUpdate(MyStateMachine stateMachine)
        {
            MyAnimationStateMachine animStateMachine = stateMachine as MyAnimationStateMachine;
            if (animStateMachine == null)
            {
                Debug.Fail("Animation machine nodes must be inside animation state machine.");
                return;
            }
            if (m_rootAnimationNode != null)
            {
                animStateMachine.CurrentUpdateData.AddVisitedTreeNodesPathPoint(1);
                m_rootAnimationNode.Update(ref animStateMachine.CurrentUpdateData);
            }
            else
            {
                animStateMachine.CurrentUpdateData.BonesResult =
                    animStateMachine.CurrentUpdateData.Controller.ResultBonesPool.Alloc();
            }
            animStateMachine.CurrentUpdateData.AddVisitedTreeNodesPathPoint(0); 
        }
    }
}
