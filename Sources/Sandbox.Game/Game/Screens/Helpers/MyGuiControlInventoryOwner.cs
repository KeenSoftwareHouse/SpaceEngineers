using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using VRageMath;
using System.Diagnostics;
using Sandbox.Game.Gui;
using VRage;
using Sandbox.Graphics.GUI;
using Sandbox.Game.World;
using VRage.Utils;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    /// <summary>
    /// Composite control for inventory. Not a general-use control so don't use 
    /// it for anything but inventories. Also not meant for editor or serialization.
    /// </summary>
    public class MyGuiControlInventoryOwner : MyGuiControlBase
    {
        private static readonly StringBuilder m_textCache = new StringBuilder();
        private static readonly Vector2 m_internalPadding = 15f / MyGuiConstants.GUI_OPTIMAL_SIZE;

        private MyGuiControlLabel m_nameLabel;
        private List<MyGuiControlLabel> m_massLabels;
        private List<MyGuiControlLabel> m_volumeLabels;
        private List<MyGuiControlGrid> m_inventoryGrids;

        private IMyInventoryOwner m_inventoryOwner;
        public IMyInventoryOwner InventoryOwner
        {
            get { return m_inventoryOwner; }
            set
            {
                if (m_inventoryOwner != value)
                    ReplaceCurrentInventoryOwner(value);
            }
        }

        public List<MyGuiControlGrid> ContentGrids
        {
            get { return m_inventoryGrids; }
        }

        public event Action<MyGuiControlInventoryOwner> InventoryContentsChanged;

        public MyGuiControlInventoryOwner(IMyInventoryOwner owner, Vector4 labelColorMask)
            : base(backgroundTexture: new MyGuiCompositeTexture() { Center = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\item_highlight_dark.dds" } },
                    canHaveFocus: true,
                    allowFocusingElements: true,
                    isActiveControl: false)
        {
            Debug.Assert(owner != null);

            m_nameLabel = MakeLabel();
            m_nameLabel.ColorMask = labelColorMask;
            m_massLabels = new List<MyGuiControlLabel>();
            m_volumeLabels = new List<MyGuiControlLabel>();
            m_inventoryGrids = new List<MyGuiControlGrid>();
            ShowTooltipWhenDisabled = true;

            m_nameLabel.Name = "NameLabel";

            Elements.Add(m_nameLabel);

            InventoryOwner = owner;
        }

        private MyGuiControlGrid MakeInventoryGrid(MyInventory inventory)
        {
            var grid = new MyGuiControlGrid();
            grid.Name = "InventoryGrid";
            grid.VisualStyle = MyGuiControlGridStyleEnum.Inventory;
            grid.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            grid.ColumnsCount = 7;
            grid.RowsCount = 1;
            grid.ShowTooltipWhenDisabled = true;
            grid.UserData = inventory;
            return grid;
        }

        private MyGuiControlLabel MakeMassLabel(MyInventory inventory)
        {
            var label = MakeLabel(MySpaceTexts.ScreenTerminalInventory_Mass);
            label.Name = "MassLabel";
            return label;
        }

        private MyGuiControlLabel MakeVolumeLabel(MyInventory inventory)
        {
            var label = MakeLabel(MySpaceTexts.ScreenTerminalInventory_Volume);
            label.Name = "VolumeLabel";
            return label;
        }

        public override void OnRemoving()
        {
            if (m_inventoryOwner != null)
                DetachOwner();

            m_inventoryGrids.Clear();
            InventoryContentsChanged = null;

            base.OnRemoving();
        }

        protected override void OnSizeChanged()
        {
            RefreshInternals();
            Size = ComputeControlSize();
            base.OnSizeChanged();
        }

        protected override void OnEnabledChanged()
        {
            RefreshInternals();
            base.OnEnabledChanged();
        }

        public override MyGuiControlBase HandleInput()
        {
            base.HandleInput();
            var captureElement = base.HandleInputElements();

            if (captureElement != null)
                return this;
            else
                return null;
        }

        public override void Update()
        {
            m_nameLabel.Text = m_inventoryOwner.DisplayNameText.ToString();
            m_nameLabel.Size = new Vector2(Size.X - m_internalPadding.X * 2, m_nameLabel.Size.Y);
            base.Update();
        }

        private void RefreshInternals()
        {
            if (m_nameLabel == null)
                return;

            var internalSize = Size - m_internalPadding * 2;
            m_nameLabel.Position = ComputeControlPositionFromTopLeft(Vector2.Zero);
            m_nameLabel.Size = new Vector2(internalSize.X, m_nameLabel.Size.Y);

            var position = ComputeControlPositionFromTopLeft(new Vector2(0, 0.03f));
            RefreshInventoryGridSizes();
            Debug.Assert(m_inventoryGrids.Count == m_massLabels.Count);
            Debug.Assert(m_inventoryGrids.Count == m_volumeLabels.Count);
            for (int i = 0; i < m_inventoryGrids.Count; ++i)
            {
                var massLabel = m_massLabels[i];
                var volumeLabel = m_volumeLabels[i];
                var grid = m_inventoryGrids[i];
                massLabel.Position = position;
                volumeLabel.Position = new Vector2(-0.04f, massLabel.Position.Y);
                massLabel.Size = new Vector2(volumeLabel.Position.X - massLabel.Position.X, massLabel.Size.Y);
                volumeLabel.Size = new Vector2(internalSize.X - massLabel.Size.X, volumeLabel.Size.Y);
                position.Y += massLabel.Size.Y + m_internalPadding.Y * 0.5f;
                grid.Position = position;
                position.Y += grid.Size.Y + m_internalPadding.Y;
            }
        }

        private void RefreshInventoryContents()
        {
            Debug.Assert(m_inventoryOwner.InventoryCount == m_inventoryGrids.Count);
            Debug.Assert(m_inventoryOwner.InventoryCount == m_massLabels.Count);
            Debug.Assert(m_inventoryOwner.InventoryCount == m_volumeLabels.Count);
            for (int i = 0; i < m_inventoryOwner.InventoryCount; ++i)
            {
                var inventory = m_inventoryOwner.GetInventory(i);
                var inventoryGrid = m_inventoryGrids[i];
                var massLabel = m_massLabels[i];
                var volumeLabel = m_volumeLabels[i];
                int? selectedIndex = inventoryGrid.SelectedIndex;
                inventoryGrid.Clear();

                massLabel.UpdateFormatParams(((double)inventory.CurrentMass).ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture));

                string volume = ((double)(inventory.CurrentVolume * 1000)).ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture);
                if (MySession.Static.SurvivalMode)
                {
                    volume += " / " + ((double)(inventory.MaxVolume * 1000)).ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture);
                }
                volumeLabel.UpdateFormatParams(volume);

                //RefreshInventoryGridSize(inventory, inventoryGrid);
                if (inventory.Constraint != null)
                {
                    inventoryGrid.EmptyItemIcon = inventory.Constraint.Icon;
                    inventoryGrid.SetEmptyItemToolTip(inventory.Constraint.Description);
                }
                else
                {
                    inventoryGrid.EmptyItemIcon = null;
                    inventoryGrid.SetEmptyItemToolTip(null);
                }

                foreach (var item in inventory.GetItems())
                {
                    inventoryGrid.Add(CreateInventoryGridItem(item));
                }

                if (selectedIndex.HasValue)
                {
                    if (inventoryGrid.IsValidIndex(selectedIndex.Value))
                        inventoryGrid.SelectedIndex = selectedIndex;
                    else
                        inventoryGrid.SelectLastItem();
                }
                else
                    inventoryGrid.SelectedIndex = null;

            }

            RefreshInventoryGridSizes();
            Size = ComputeControlSize();
            RefreshInternals();
        }

        private void RefreshInventoryGridSizes()
        {
            foreach (var inventoryGrid in m_inventoryGrids)
            {
                var inventory = (MyInventory)inventoryGrid.UserData;
                int itemsCount = inventory.GetItems().Count;
                inventoryGrid.ColumnsCount = Math.Max(1, (int)((Size.X - m_internalPadding.X * 2f) / (inventoryGrid.ItemSize.X * 1.01f)));
                inventoryGrid.RowsCount = Math.Max(1, (int)Math.Ceiling((itemsCount + 1) / (float)inventoryGrid.ColumnsCount));
                inventoryGrid.TrimEmptyItems();
            }
        }

        private Vector2 ComputeControlPositionFromTopLeft(Vector2 offset)
        {
            return m_internalPadding + (Size * -0.5f) + offset;
        }

        private Vector2 ComputeControlPositionFromTopCenter(Vector2 offset)
        {
            return new Vector2(0f, m_internalPadding.Y + Size.Y * -0.5f) + offset;
        }

        private MyGuiControlLabel MakeLabel(MyStringId? text = null,
            MyGuiDrawAlignEnum labelAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            float labelTextScale = 0.85f;
            var res = new MyGuiControlLabel(
                text: (text.HasValue) ? MyTexts.GetString(text.Value) : null,
                textScale: labelTextScale,
                originAlign: labelAlign);
            res.AutoEllipsis = true;
            return res;
        }

        private void ReplaceCurrentInventoryOwner(IMyInventoryOwner owner)
        {
            DetachOwner();
            AttachOwner(owner);
        }

        private void inventory_OnContentsChanged(MyInventory obj)
        {
            RefreshInventoryContents();
            if (InventoryContentsChanged != null)
                InventoryContentsChanged(this);
        }

        private Vector2 ComputeControlSize()
        {
            var sizeY = m_nameLabel.Size.Y + m_internalPadding.Y * 2;
            for (int i = 0; i < m_inventoryGrids.Count; ++i)
            {
                var grid = m_inventoryGrids[i];
                var massLabel = m_massLabels[i];
                sizeY += massLabel.Size.Y + m_internalPadding.Y * 0.5f;
                sizeY += grid.Size.Y + m_internalPadding.Y;
            }

            return new Vector2(Size.X, sizeY);
        }

        private void AttachOwner(IMyInventoryOwner owner)
        {
            if (owner == null)
                return;

            m_nameLabel.Text = owner.DisplayNameText.ToString();

            for (int i = 0; i < owner.InventoryCount; ++i)
            {
                var inventory = owner.GetInventory(i);
                inventory.UserData = this;
                inventory.ContentsChanged += inventory_OnContentsChanged;

                var massLabel = MakeMassLabel(inventory);
                Elements.Add(massLabel);
                m_massLabels.Add(massLabel);

                var volumeLabel = MakeVolumeLabel(inventory);
                Elements.Add(volumeLabel);
                m_volumeLabels.Add(volumeLabel);

                var inventoryGrid = MakeInventoryGrid(inventory);
                Elements.Add(inventoryGrid);
                m_inventoryGrids.Add(inventoryGrid);
            }

            m_inventoryOwner = owner;

            RefreshInventoryContents();
        }

        private void DetachOwner()
        {
            if (m_inventoryOwner == null)
                return;

            for (int i = 0; i < m_inventoryOwner.InventoryCount; ++i)
            {
                var inventory = m_inventoryOwner.GetInventory(i);
                inventory.UserData = null;
                inventory.ContentsChanged -= inventory_OnContentsChanged;
            }

            for (int i = 0; i < m_inventoryGrids.Count; ++i)
            {
                Elements.Remove(m_massLabels[i]);
                Elements.Remove(m_volumeLabels[i]);
                Elements.Remove(m_inventoryGrids[i]);
            }
            m_inventoryGrids.Clear();
            m_massLabels.Clear();
            m_volumeLabels.Clear();

            m_inventoryOwner = null;
        }

        public static void FormatItemAmount(MyPhysicalInventoryItem item, StringBuilder text)
        {
            try
            {
                var typeId = item.Content.GetType();
                if (typeId == typeof(MyObjectBuilder_Ore) ||
                    typeId == typeof(MyObjectBuilder_Ingot))
                {
                    double amount = (double)item.Amount;

                    if (amount < 0.01)
                        text.Append(amount.ToString("<0.01", CultureInfo.InvariantCulture));
                    else if (item.Amount < 10)
                        text.Append(amount.ToString("0.##", CultureInfo.InvariantCulture));
                    else if (item.Amount < 100)
                        text.Append(amount.ToString("0.#", CultureInfo.InvariantCulture));
                    else if (item.Amount < 1000)
                        text.Append(amount.ToString("0.", CultureInfo.InvariantCulture));
                    else if (item.Amount < 10000)
                        text.Append((amount / 1000.0).ToString("0.##k", CultureInfo.InvariantCulture));
                    else if (item.Amount < 100000)
                        text.Append((amount / 1000.0).ToString("0.#k", CultureInfo.InvariantCulture));
                    else
                        text.Append((amount / 1000.0).ToString("#,##0.k", CultureInfo.InvariantCulture));
                }
                else if (typeId == typeof(MyObjectBuilder_PhysicalGunObject))
                {
                    Debug.Assert(item.Amount == 1, "There should only be one gun in a single slot. This is safe to ignore.");
                }
                else if (typeId == typeof(MyObjectBuilder_OxygenContainerObject))
                {
                    Debug.Assert(item.Amount == 1, "There should only be one oxygen bottle in a single slot. This is safe to ignore.");

                    var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                    text.Append((oxygenContainer.OxygenLevel * 100f).ToString("F0") + "%");
                }
                else
                {
                    int integerPart = (int)item.Amount;
                    var decimalPart = item.Amount - integerPart;
                    if (decimalPart > 0)
                        text.Append('~'); // used for half empty magazines and such
                    text.Append(integerPart.ToString("#,##0.x", CultureInfo.InvariantCulture));
                }
            }
            catch (System.OverflowException)
            {
                text.Append("ERROR");
            }
        }

        public static MyGuiControlGrid.Item CreateInventoryGridItem(MyPhysicalInventoryItem item)
        {
            var definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Content);

            var itemMass = definition.Mass * (double)item.Amount;
            var itemVolume = definition.Volume * 1000 * (double)item.Amount;

            var gridItem = new MyGuiControlGrid.Item(
                icon: definition.Icon,
                userData: item,
                toolTip: new StringBuilder().AppendFormat(MyTexts.GetString(MySpaceTexts.ToolTipTerminalInventory_ItemInfo),
                    definition.DisplayNameText,
                    (itemMass < 0.01) ? "<0.01" : itemMass.ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture),
                    (itemVolume < 0.01) ? "<0.01" : itemVolume.ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture),
                    (item.Content.Flags == MyItemFlags.Damaged ? MyTexts.Get(MySpaceTexts.ItemDamagedDescription) : MyTexts.Get(MySpaceTexts.Blank))).ToString());
            if (MyFakes.SHOW_INVENTORY_ITEM_IDS)
            {
                gridItem.ToolTip.AddToolTip(new StringBuilder().AppendFormat("ItemID: {0}", item.ItemId).ToString());
            }
            FormatItemAmount(item, m_textCache);
            gridItem.AddText(m_textCache, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            m_textCache.Clear();

            if (definition.IconSymbol.HasValue)
                gridItem.AddText(MyTexts.Get(definition.IconSymbol.Value), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            return gridItem;
        }
    }
}
