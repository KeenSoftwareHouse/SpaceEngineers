using System.Diagnostics;
using VRage.Game.ObjectBuilders.Campaign;
using VRage.Generics;

namespace VRage.Game.VisualScripting.Campaign
{
    public class MyCampaignStateMachine : MySingleStateMachine
    {
        private MyObjectBuilder_CampaignSM  m_objectBuilder;

        public bool Initialized { get { return m_objectBuilder != null;} }

        public bool Finished { get { return ((MyCampaignStateMachineNode)CurrentNode).Finished;} }

        public void Deserialize(MyObjectBuilder_CampaignSM ob) 
        {
            if (m_objectBuilder != null)
            {
                Debug.Fail("Loading twice.");
                return;
            }

            m_objectBuilder = ob;

            foreach (var nodeData in m_objectBuilder.Nodes)
            {
                var node = new MyCampaignStateMachineNode(nodeData.Name) {SavePath = nodeData.SaveFilePath};
                AddNode(node);
            }

            foreach (var transitionData in m_objectBuilder.Transitions)
            {
                AddTransition(transitionData.From, transitionData.To, name: transitionData.Name);
            }
        }

        public void ResetToStart()
        {
            foreach (var node in m_nodes.Values)
            {
                var campaignNode = node as MyCampaignStateMachineNode;
                if (campaignNode != null && campaignNode.InTransitionCount == 0)
                {
                    SetState(campaignNode.Name);
                    return;
                }
            }
        }
    }
}
