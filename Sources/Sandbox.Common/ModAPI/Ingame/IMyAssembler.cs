using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyAssembler : IMyProductionBlock
    {
        bool DisassembleEnabled { get; }
        ///<remarks>
        ///Lists all components assembleable through the 3rd tab in the production menu
        ///</remarks>
        List<string> ComponentList { get; }
        ///<remarks>
        ///Lists all tools assembleable through the 4th tab in the production menu.  This is the hand tools tab!
        ///</remarks>
        List<string> ToolList { get; }
        ///<remarks>
        ///Lists all blueprints assembleable through the 1st or 2nd tabs in the production menu- first is big ships, second is not.  This includes things like armor blocks.
        ///</remarks>
        List<string> GetBlueprintList(bool isBigShip);
        ///<remarks>
        ///Lists all the components that make up the specified blueprint.  Also lists how many it requires.  This is what is added to the queue if the blueprint is clicked in the production menu.
        ///</remarks>
        bool GetBlueprintComponents(bool isBigShip, string blueprint, List<string> components, List<long> count);
        ///<remarks>
        ///Lists all the resources that make up the specified blueprint.  Also lists how many it requires.  This is the same list as is displayed on mouseover in the production menu.
        ///NOTE:  Quantities are in the MILLIONS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool GetBlueprintResources(bool isBigShip, string blueprint, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Lists all the resources that make up the specified component.  Also lists how many it requires.  This is the same list as is displayed on mouseover in the production menu.
        ///NOTE:  Quantities are in the MILLIONS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool GetComponentResources(string component, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Lists all the resources that make up the specified tool.  Also lists how many it requires.  This is the same list as is displayed on mouseover in the production menu.
        ///NOTE:  Quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool GetToolResources(string component, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Lists all resources, components, AND tools available to this assembler.
        ///NOTE:  RESOURCE (ingot) quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        void GetResources(List<string> resources, List<long> quantities);
        ///<remarks>
        ///Lists all available resources, components, AND tools available to this assembler, INCLUDING future production and EXCLUDING future usage.  Assembler inputs are ignored, so this may reflect less than is actually available.
        ///NOTE:  RESOURCE (ingot) quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        void GetAvailableResources(List<string> resources, List<long> quantities);
        ///<remarks>
        ///Determines if there are enough resources available to make the specified component.
        ///IF SO, the last two parameters are populated with the REMAINING AVAILABLE RESOURCES (ingot, tool, and component) and 'true' is returned.
        ///IF NOT, the last two parameters are populated with THE MISSING INGOTS and 'false' is returned.
        ///NOTE:  RESOURCE (ingot) quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool CanMakeComponent(string component, long count, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Determines if there are enough resources available to make the specified tool.
        ///IF SO, the last two parameters are populated with the REMAINING AVAILABLE RESOURCES (ingot, tool, and component) and 'true' is returned.
        ///IF NOT, the last two parameters are populated with THE MISSING INGOTS and 'false' is returned.
        ///NOTE:  RESOURCE (ingot) quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool CanMakeTool(string component, long count, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Determines if there are enough resources available to make the specified blueprint.
        ///IF SO, the last two parameters are populated with the REMAINING AVAILABLE RESOURCES (ingot, tool, and component) and 'true' is returned.
        ///IF NOT, the last two parameters are populated with THE MISSING INGOTS and 'false' is returned.
        ///NOTE:  RESOURCE (ingot) quantities are in MILLIONTHS!  Divide by 1,000,000 to get the actual value.  They are provided here as longs for precision and accuracy.
        ///</remarks>
        bool CanMakeBlueprint(bool isBigShip, string blueprint, long count, List<string> resources, List<long> quantities);
        ///<remarks>
        ///Returns the amount of time, in milliseconds, that the assembler has spent processing the current item.
        ///</remarks>
        int CurrentProductionTime { get; }
        ///<remarks>
        ///Returns the amount of time, in milliseconds, that the assembler will take to produce 1 of the specified COMPONENT OR TOOL.
        ///</remarks>
        int GetProductionTime(string component);
        ///<remarks>
        ///Determines whether or not the specified assembly/disassembly repeat mode is on.
        ///</remarks>
        bool IsRepeating(bool assemblyMode);
        ///<remarks>
        ///Toggles the assembler into the specified assembly/disassembly mode and switches the according repeat mode into the specified state.
        ///</remarks>
        void ToggleRepeat(bool assemblyMode, bool repeatMode);
        ///<remarks>
        ///Toggles the assembler into the specified assembly/disassembly mode
        ///</remarks>
        void ToggleAssembly(bool assemblyMode);
        ///<remarks>
        ///Clears the specified assembly or disassembly queue
        ///</remarks>
        void ClearQueue(bool assemblyMode);
        ///<remarks>
        ///Removes up to count items from the specified slot of the specified assembly/disassembly queue.  If count is -1, all items are removed, as if the slot had been right-clicked.
        ///</remarks>
        bool RemoveQueueItem(int slot, long count = -1, bool assemblyMode = true);
        ///<remarks>
        ///Removes up to count of the specified item from the specified assembly/disassembly queue, STARTING AT THE FIRST (CURRENT) INDEX.
        ///If count is -1, all items are removed, as if all slots containing the item were right-clicked.
        ///</remarks>
        bool RemoveQueueItem(string component, long count = -1, bool assemblyMode = true);
        ///<remarks>
        ///Adds the specified count of the specified blueprint to the end of the specified queue.
        ///</remarks>
        bool EnqueueBlueprint(bool isBigShip, string blueprint, long count = 1, bool assemblyMode = true);
        ///<remarks>
        ///Adds the specified count of the specified component to the end of the specified queue.
        ///</remarks>
        bool EnqueueComponent(string component, long count = 1, bool assemblyMode = true);
        ///<remarks>
        ///Adds the specified count of the specified tool to the end of the specified queue.
        ///</remarks>
        bool EnqueueTool(string component, long count = 1, bool assemblyMode = true);
        ///<remarks>
        ///Gets the specified queue.
        ///</remarks>
        bool GetQueue(bool assemblyMode, List<string> components, List<long> counts);
    }
}
