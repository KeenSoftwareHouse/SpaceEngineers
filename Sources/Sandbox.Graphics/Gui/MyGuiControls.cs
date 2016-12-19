using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControls : MyGuiControlBase.Friend, IEnumerable<MyGuiControlBase>
    {
        private IMyGuiControlsOwner m_owner;
        private ObservableCollection<MyGuiControlBase> m_controls;
        private Dictionary<string, MyGuiControlBase> m_controlsByName;
        private List<MyGuiControlBase> m_visibleControls;

        private bool m_refreshVisibleControls;

        public event Action<MyGuiControls> CollectionChanged;
        public event Action<MyGuiControls> CollectionMembersVisibleChanged;

        #region Construction
        public MyGuiControls(IMyGuiControlsOwner owner)
        {
            m_owner           = owner;
            m_controls        = new ObservableCollection<MyGuiControlBase>();
            m_controlsByName  = new Dictionary<string, MyGuiControlBase>();
            m_visibleControls = new List<MyGuiControlBase>();

            m_controls.CollectionChanged += OnPrivateCollectionChanged;
            m_refreshVisibleControls = true;
        }

        public void Init(MyObjectBuilder_GuiControls objectBuilder)
        {
            Clear();

            if (objectBuilder.Controls == null)
                return;

            foreach (var controlObjectBuilder in objectBuilder.Controls)
            {
                var control = MyGuiControlsFactory.CreateGuiControl(controlObjectBuilder);
                control.Init(controlObjectBuilder);
                Add(control);
            }
        }

        public MyObjectBuilder_GuiControls GetObjectBuilder()
        {
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_GuiControls>();
            objectBuilder.Controls = new List<MyObjectBuilder_GuiControlBase>();

            foreach (var control in m_controlsByName)
            {
                var controlObjectBuilder = control.Value.GetObjectBuilder();
                objectBuilder.Controls.Add(controlObjectBuilder);
            }

            return objectBuilder;
        }
        #endregion

        private void OnPrivateCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(sender == m_controls);
            if (CollectionChanged != null)
                CollectionChanged(this);
        }

        private void control_VisibleChanged(object control, bool isVisible)
        {
            m_refreshVisibleControls = true;

            if (CollectionMembersVisibleChanged != null)
                CollectionMembersVisibleChanged(this);
        }

        private void RefreshVisibleControls()
        {
            if (!m_refreshVisibleControls)
                return;

            m_visibleControls.Clear();
            foreach (MyGuiControlBase control in m_controls)
            {
                if (control.Visible)
                    m_visibleControls.Add(control);
            }
            m_refreshVisibleControls = false;
        }

        public List<MyGuiControlBase> GetVisibleControls()
        {
            RefreshVisibleControls();
            return m_visibleControls;
        }

        public void Add(MyGuiControlBase control)
        {
            Debug.Assert(!m_controls.Contains(control), "You must not add the same control multiple times.");
            Debug.Assert(control != this.m_owner, "Can not insert itself!");

            MyGuiControlBase.Friend.SetOwner(control, m_owner);
            control.Name = ChangeToNonCollidingName(control.Name);
            m_controlsByName.Add(control.Name, control);

            if (control.Visible)
                m_visibleControls.Add(control);

            m_controls.Add(control);

            control.VisibleChanged += control_VisibleChanged;
            control.NameChanged    += control_NameChanged;
        }

        public void AddWeak(MyGuiControlBase control)
        {
            Debug.Assert(!m_controls.Contains(control), "You must not add the same control multiple times.");
            Debug.Assert(control != this.m_owner, "Can not insert itself!");

//            m_controlsByName.Add(control.Name, control);

            if (control.Visible)
                m_visibleControls.Add(control);

            m_controls.Add(control);

            control.VisibleChanged += control_VisibleChanged;
            control.NameChanged += control_NameChanged;
        }

        private void control_NameChanged(MyGuiControlBase control, MyGuiControlBase.NameChangedArgs args)
        {
            Debug.Assert(m_controls.Contains(control));
            m_controlsByName.Remove(args.OldName);
            control.NameChanged -= control_NameChanged;
            control.Name         = ChangeToNonCollidingName(control.Name);
            control.NameChanged += control_NameChanged;
            m_controlsByName.Add(control.Name, control);
        }

        public void ClearWeaks()
        {
            m_controls.Clear();
            m_controlsByName.Clear();
            m_visibleControls.Clear();
        }

        public void Clear()
        {
            foreach (MyGuiControlBase control in m_controls)
            {
                control.OnRemoving();
            }

            m_controls.Clear();
            m_controlsByName.Clear();
            m_visibleControls.Clear();
        }

        public bool Remove(MyGuiControlBase control)
        {
            m_controlsByName.Remove(control.Name);

            bool itemRemoved = m_controls.Remove(control);

            if (itemRemoved)
            {
                m_visibleControls.Remove(control);
                control.OnRemoving();
            }

            return itemRemoved;
        }

        public bool RemoveControlByName(string name)
        {
            var control = GetControlByName(name);
            if (control == null)
                return false;
            return Remove(control);
        }

        public int Count
        {
            get { return m_controls.Count; }
        }

        public int IndexOf(MyGuiControlBase item)
        {
            return m_controls.IndexOf(item);
        }

        public MyGuiControlBase this[int index]
        {
            get
            {
                return m_controls[index];
            }
            set
            {
                MyGuiControlBase oldItem = m_controls[index];
                if (oldItem != null)
                {
                    oldItem.VisibleChanged -= control_VisibleChanged;
                    m_visibleControls.Remove(oldItem);
                }

                if (value != null)
                {
                    MyGuiControlBase newItem = value;
                    newItem.VisibleChanged -= control_VisibleChanged;
                    newItem.VisibleChanged += control_VisibleChanged;
                    m_controls[index] = newItem;

                    m_refreshVisibleControls = true;
                }
            }
        }

        public MyGuiControlBase GetControlByName(string name)
        {
            MyGuiControlBase control = null;
            m_controlsByName.TryGetValue(name, out control);
            return control;
        }

        private string ChangeToNonCollidingName(string originalName)
        {
            string currentName = originalName;
            int k = 1;
            while (m_controlsByName.ContainsKey(currentName))
            {
                currentName = originalName + k;
                ++k;
            }
            return currentName;
        }

        public bool Contains(MyGuiControlBase control)
        {
            return m_controls.Contains(control);
        }

        public ObservableCollection<MyGuiControlBase>.Enumerator GetEnumerator()
        {
            return m_controls.GetEnumerator();
        }

        IEnumerator<MyGuiControlBase> IEnumerable<MyGuiControlBase>.GetEnumerator()
        {
            Debug.Fail("Allocation");
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Fail("Allocation");
            return GetEnumerator();
        }
    }
}
