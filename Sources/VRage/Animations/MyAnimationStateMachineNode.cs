using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

namespace VRage.Animations
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
            Debug.Assert(animationClip != null, "Creating single animation node in machine " + this.Name + ", node name " 
                + name + ": Animation clip must not be null!");
            if (animationClip != null)
            {
                var nodeTrack = new AnimationNodes.MyAnimationTreeNodeTrack();
                nodeTrack.SetClip(animationClip);
                m_rootAnimationNode = nodeTrack;
            }
        }

        public override void OnUpdate(MyStateMachine stateMachine)
        {
            Debug.Assert(stateMachine is MyAnimationStateMachine, "Animation machine nodes must be inside animation state machine.");
            MyAnimationStateMachine animStateMachine = stateMachine as MyAnimationStateMachine;
            if (m_rootAnimationNode != null)
            {
                m_rootAnimationNode.Update(ref animStateMachine.CurrentUpdateData);
            }
        }
    }
}
