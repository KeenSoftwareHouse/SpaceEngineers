using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;

namespace Sandbox.Game.GameSystems.Conveyors
{
    public interface IMyConveyorEndpointBlock
    {
        IMyConveyorEndpoint ConveyorEndpoint { get; }
        void InitializeConveyorEndpoint();

        PullInformation GetPullInformation();
        PullInformation GetPushInformation();
    }

    public class PullInformation
    {
        /// <summary>
        /// Inventory of the block
        /// </summary>
        public MyInventory Inventory { get; set; }

        /// <summary>
        /// Owner of the block
        /// </summary>
        public long OwnerID { get; set; }

        /// <summary>
        /// Inventory constraint in case this block pulls/pushes multiple items
        /// </summary>
        public MyInventoryConstraint Constraint { get; set; }

        /// <summary>
        /// Item definition in case this block only pulls/pushes 1 specific item
        /// </summary>
        public MyDefinitionId ItemDefinition { get; set; }
    }
}
