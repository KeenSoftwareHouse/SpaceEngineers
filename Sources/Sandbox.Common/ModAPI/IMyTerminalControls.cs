using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Allows you to modify the terminal control list before it is displayed to the user.  Modifying controls will change which controls are displayed.
    /// </summary>
    /// <param name="block">The block that was selected</param>
    /// <param name="controls"></param>
    public delegate void CustomControlGetDelegate(IMyTerminalBlock block, List<IMyTerminalControl> controls);

    /// <summary>
    /// Allows you to modify the actions associated with a block before it's displayed to user. 
    /// </summary>
    /// <param name="block">The block actions are associated with</param>
    /// <param name="actions">The list of actions for this block</param>
    public delegate void CustomActionGetDelegate(IMyTerminalBlock block, List<IMyTerminalAction> actions);

    /// <summary>
    /// This interface allows you to query, add or remove terminal controls for a block.  The terminal controls are the controls that appear
    /// in the terminal screen when you select a block.  You may add new controls, remove existing controls, or modify existing controls.
    /// </summary>
    public interface IMyTerminalControls
    {
        /// <summary>
        /// This event allows you to modify the list of controls that the game displays when a user selects a block.  Each time terminal controls are 
        /// enumerated for a block, this delegate is called, which allows you to modify the control list directly, and remove/add as you see fit before 
        /// the controls are dispalyed.  This is to allow fine grain control of the controls being displayed, so you can display only controls you want to
        /// in specific situations (like blocks with different subtypes, or even on specific blocks by entityId)
        /// </summary>
        event CustomControlGetDelegate CustomControlGetter;
        /// <summary>
        /// This event allows you to modify the list of actions available when a user wants to select an action for a block in the toolbar.  Modifying the list
        /// in this event modifies the list displayed to the user so that you can customize it in specific situations (like blocks with different subtypes, 
        /// or even on specific blocks by entityId)
        /// </summary>
        event CustomActionGetDelegate CustomActionGetter;
        /// <summary>
        /// Gets the controls associated with a block.
        /// </summary>
        /// <typeparam name="TBlock">This is the object builder type of the associated block you want to get terminal controls for</typeparam>
        /// <param name="items">The list that contains the terminal controls for this block</param>
        void GetControls<TBlock>(out List<IMyTerminalControl> items);
        /// <summary>
        /// Adds a terminal control to a block.
        /// </summary>
        /// <typeparam name="TBlock">This is the ModAPI interface of the associated block you want to add a terminal control to</typeparam>
        /// <param name="item">This is the control you're adding, created with CreateControl or CreateProperty</param>
        void AddControl<TBlock>(IMyTerminalControl item);
        /// <summary>
        /// Removes a terminal control from a block.
        /// </summary>
        /// <typeparam name="TBlock">This is the ModAPI interface of the associated block you want to remove a terminal control from</typeparam>
        /// <param name="item">This is the control you're removing.  Use GetControls to get the item itself.</param>
        void RemoveControl<TBlock>(IMyTerminalControl item);
        /// <summary>
        /// This creates a control that can be added to a block.
        /// </summary>
        /// <typeparam name="TControl">The type of control you're creating</typeparam>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="id">A unique identifier for this control</param>
        /// <returns>Returns an interface to the control you've created depending on TControl</returns>
        TControl CreateControl<TControl, TBlock>(string id);
        /// <summary>
        /// This creates a property that can be added to a block.  A property is not visible on the terminal screen but can hold a value that can be used in
        /// programmable blocks.
        /// </summary>
        /// <typeparam name="TValue">The type of property you're creating</typeparam>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="id">A unique identifier for this property</param>
        /// <returns>Returns an IMyTerminalControlProperty that can be added to a block via AddControl</returns>
        IMyTerminalControlProperty<TValue> CreateProperty<TValue, TBlock>(string id);
        /// <summary>
        /// This allows you to get all actions associated with this block.
        /// </summary>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="items">The list that contains the actions associated with this block</param>
        void GetActions<TBlock>(out List<IMyTerminalAction> items);
        /// <summary>
        /// This allows you to add an action to an assocated block
        /// </summary>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="action">An IMyTerminalAction object returned from CreateAction</param>
        void AddAction<TBlock>(IMyTerminalAction action);
        /// <summary>
        /// This allows you to remove an action from a block
        /// </summary>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="action">An IMyTerminalAction object</param>
        void RemoveAction<TBlock>(IMyTerminalAction action);
        /// <summary>
        /// This allows you to create an action to associate with a block
        /// </summary>
        /// <typeparam name="TBlock">The ModAPI interface of the associated block</typeparam>
        /// <param name="id">A unique identifier for this action</param>
        /// <returns>An IMyTerminalAction object</returns>
        IMyTerminalAction CreateAction<TBlock>(string id);
    }
}
