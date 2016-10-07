#if !XB1

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI.DebugInputComponents.HonzaDebugInputComponent;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using SharpDX.Windows;
using VRage.Input;
using VRage.Library.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Color = VRageMath.Color;
using Vector2 = VRageMath.Vector2;

namespace Sandbox.Game.Gui
{
    public static class MemberInfoExtensions
    {
        public static object GetValue(this MemberInfo info, object instance)
        {
            object value = null;
            var field = info as FieldInfo;
            if (field != null)
                value = field.GetValue(instance);
            var prop = info as PropertyInfo;
            if (prop != null && prop.GetIndexParameters().Length == 0)
            {
                value = prop.GetValue(instance, null);
            }
            return value;
        }
    }

    partial class MyHonzaInputComponent
    {
        public class LiveWatchComponent : MyDebugComponent
        {
            int MAX_HISTORY = 10000;
            private object m_currentInstance;
            private Type m_selectedType;
            private Type m_lastType;
            private readonly List<MemberInfo> m_members = new List<MemberInfo>();
            private readonly List<MemberInfo> m_currentPath = new List<MemberInfo>();
            private readonly Dictionary<Type, MyListDictionary<MemberInfo, MemberInfo>> m_watch = new Dictionary<Type, MyListDictionary<MemberInfo, MemberInfo>>();
            private List<List<object>> m_history = new List<List<object>>();
            private bool m_showWatch = false;
            private bool m_showOnScreenWatch = false;
            private LiveWatch m_form = null;
            private float m_scale = 2;
            private HashSet<int> m_toPlot = new HashSet<int>(); 

            public LiveWatchComponent()
            {
                MyHonzaInputComponent.OnSelectedEntityChanged += MyHonzaInputComponent_OnSelectedEntityChanged;
                
                AddSwitch(MyKeys.NumPad9, (key)=>
                 {
                    if (m_form != null)
                    {
                        if (m_form.IsDisposed)
                            m_form = null;
                        else
                        {
                            Action a = () =>
                            {
                                m_form.Close();
                                m_form = null;
                            };
                            m_form.Invoke(a);
                        }
                    }
                    else
                    {
                        System.Threading.Thread t = new Thread(() =>
                        {
                            Debug.Assert(m_form == null);
                            m_form = new LiveWatch();
                            m_form.Show();
                            RenderLoop.Run(m_form, RenderCallback);
                        });
                        t.Start();
                    }
                    return true;
                }, new MyRef<bool>(()=> m_form != null, null), "External viewer");
                AddSwitch(MyKeys.NumPad8, (key) =>
                {
                    m_showOnScreenWatch = !m_showOnScreenWatch;
                    return true;
                }, new MyRef<bool>(() => m_showOnScreenWatch, null), "External viewer");
            }

            private void RenderCallback()
            {
                if (m_form.propertyGrid1.SelectedObject != SelectedEntity)
                    m_form.propertyGrid1.SelectedObject = SelectedEntity;

                if(!m_showWatch)
                    return;

                MyListDictionary<MemberInfo, MemberInfo> watch = null;
                m_watch.TryGetValue(m_selectedType, out watch);
                if(watch == null)
                    return;
                int neededRows = Math.Max(0, watch.Values.Count - m_form.Watch.Rows.Count);
                if (neededRows != 0)
                {
                    m_form.Watch.Rows.Add(neededRows);
                }

                int idx = 0;
                foreach (var list in watch.Values)
                {
                    object currentInstance = SelectedEntity;
                    MemberInfo currentMember = null;
                    foreach (var member in list)
                    {
                        currentInstance = member.GetValue(currentInstance);
                        currentMember = member;
                    }
                    (m_form.Watch.Rows[idx].Cells[0] as DataGridViewTextBoxCell).Value = currentMember.Name;
                    (m_form.Watch.Rows[idx].Cells[1] as DataGridViewTextBoxCell).Value = currentInstance.ToString();
                    idx++;
                }
            }

            void MyHonzaInputComponent_OnSelectedEntityChanged()
            {
                if (SelectedEntity == null)
                    return;
                if (m_selectedType == SelectedEntity.GetType())
                    return;

                m_selectedType = SelectedEntity.GetType();
                m_members.Clear();
                m_currentPath.Clear();
            }

            public override string GetName()
            {
                return "LiveWatch";
            }

            public override bool HandleInput()
            {
                var handled = base.HandleInput();
                if (handled)
                    return true;

                if (MyInput.Static.IsKeyPress(MyKeys.OemTilde) && SelectedMember >= 0)
                {
                    var info = m_members.Count > SelectedMember ? m_members[SelectedMember] : null;
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.OemPeriod))
                    {
                        if (m_currentPath.Count > 0 && m_currentPath[m_currentPath.Count - 1] == info)
                            return true;
                        var obj = info.GetValue(m_currentInstance);
                        if (obj != null && !obj.GetType().IsPrimitive && obj.GetType().GetFields().Length + obj.GetType().GetProperties().Length > 0)
                        {
                            m_currentPath.Add(info);
                        }
                        m_counter = 0;
                        return true;
                    }
                    else if (MyInput.Static.IsNewKeyPressed(MyKeys.OemComma))
                    {
                        if (m_currentPath.Count > 0)
                        {
                            m_currentPath.RemoveAt(m_currentPath.Count - 1);
                            m_counter = 0;
                        }
                        return true;
                    }
                    else if (MyInput.Static.IsNewKeyPressed(MyKeys.OemQuestion))
                    {
                        if (m_showWatch)
                        {
                            if (!m_toPlot.Add(SelectedMember))
                                m_toPlot.Remove(SelectedMember);
                        }
                        else
                        {
                            MyListDictionary<MemberInfo, MemberInfo> watch = null;
                            if (!m_watch.TryGetValue(m_selectedType, out watch))
                                watch = m_watch[m_selectedType] = new MyListDictionary<MemberInfo, MemberInfo>();
                            var lst = watch.GetList(info);
                            if (lst != null)
                            {
                                watch.Remove(info);
                                return true;
                            }
                            lst = watch.GetOrAddList(info);
                            lst.AddList(m_currentPath);
                            lst.Add(info);
                        }
                        return true;
                    }
                    else if (MyInput.Static.IsNewKeyPressed(MyKeys.M))
                    {
                        m_showWatch = !m_showWatch;
                    }
                    else if (MyInput.Static.IsNewKeyPressed(MyKeys.OemPlus))
                        m_scale *= 2;
                    else if (MyInput.Static.IsNewKeyPressed(MyKeys.OemMinus))
                        m_scale /= 2;

                    m_counter += VRage.Input.MyInput.Static.PreviousMouseScrollWheelValue() - VRage.Input.MyInput.Static.MouseScrollWheelValue();
                    m_counter = (long)MyMath.Clamp(m_counter, 0, m_members.Count / 0.002f);
                }

                return handled;
            }

            private int SelectedMember
            {
                get
                {
                    int startMember = (int) (m_counter*0.005f);
                    if(m_showWatch)
                    {
                        if (m_watch.ContainsKey(m_selectedType))
                        {
                            startMember = Math.Min(Math.Max(startMember, 0),
                                m_watch[m_selectedType].Values.Count - 1);
                        }
                        else
                            startMember = 0;
                    }
                    else
                    {
                        startMember = Math.Min(Math.Max(startMember, 0),
                            m_members.Count - 1);
                    }
                    return startMember;
                }
            }

            public override void Draw()
            {
                base.Draw();

                if (SelectedEntity == null || !m_showOnScreenWatch)
                    return;
                MyListDictionary<MemberInfo, MemberInfo> watch = null;
                m_watch.TryGetValue(m_selectedType, out watch);

                if (m_showWatch)
                {
                    DrawWatch(watch);
                    return;
                }

                StringBuilder sb = new StringBuilder(SelectedEntity.GetType().Name);

                Type currentType = m_selectedType;
                m_currentInstance = SelectedEntity;
                foreach (var member in m_currentPath)
                {
                    sb.Append(".");
                    sb.Append(member.Name);
                    m_currentInstance = member.GetValue(m_currentInstance);
                    currentType = m_currentInstance.GetType();
                }
                if (currentType != m_lastType)
                {
                    m_lastType = currentType;

                    m_members.Clear();
                    MemberInfo[] members = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var member in members)
                    {
                        if(member.DeclaringType == currentType)
                            m_members.Add(member);
                    }
                    //m_members.AddArray(members);
                    members = currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var member in members)
                    {
                        if (member.DeclaringType == currentType)
                            m_members.Add(member);
                    }
                    m_members.Sort((x,y)=> string.Compare(x.Name,y.Name));
                    //m_members.AddArray(members);
                }

                Vector2 pos = new Vector2(100, 50);// m_counter * 0.2f);

                MyRenderProxy.DebugDrawText2D(pos, sb.ToString(),
                    Color.White, 0.65f);
                pos.Y += 20;
                for (int i = SelectedMember; i < m_members.Count; i++)
                {
                    var info = m_members[i];
                    object value = info.GetValue(m_currentInstance);
                    var text = value != null ? value.ToString() : "null";
                    text = text.Replace("\n", "");
                    MyRenderProxy.DebugDrawText2D(pos, info.Name + " : " + text,
                        (watch != null && watch.GetList(info) != null) ? Color.Green : Color.White, 0.55f);
                    pos.Y += 12;
                }
            }

            private int m_frame = 0;
            private void DrawWatch(MyListDictionary<MemberInfo, MemberInfo> watch)
            {
                PlotHistory();
                if (watch == null)
                    return;
                List<object> log = new CacheList<object>(watch.Values.Count);

                StringBuilder sb = new StringBuilder();
                Vector2 pos = new Vector2(100, 50);// m_counter * 0.2f);

                int i = -1;
                foreach (var list in watch.Values)
                {
                    i++;
                    if(i < SelectedMember)
                        continue;
                    object currentInstance = SelectedEntity;
                    foreach (var member in list)
                    {
                        sb.Append(".");
                        sb.Append(member.Name);
                        currentInstance = member.GetValue(currentInstance);
                    }
                    sb.Append(":");
                    sb.Append(currentInstance.ToString());
                    MyRenderProxy.DebugDrawText2D(pos, sb.ToString(),
                        m_toPlot.Contains(i) ? m_colors[i] : Color.White, 0.55f);
                    pos.Y += 12;
                    sb.Clear();
                    log.Add(currentInstance);
                }
                pos.X = 90;
                foreach (var toPlot in m_toPlot)
                {
                    int idx = (toPlot - SelectedMember);
                    if (idx < 0)
                        continue;
                    pos.Y = 50 + idx * 12;
                    MyRenderProxy.DebugDrawText2D(pos, "*",
                       m_colors[toPlot], 0.55f);
                }
                m_history.Add(log);
                if(m_history.Count >= MAX_HISTORY)
                    m_history.RemoveAtFast(m_frame);
                m_frame++;
                m_frame %= MAX_HISTORY;
            }

            private void PlotHistory()
            {
                const float zeroLine = 400;
                int idx = 0;
                Vector2 b = new Vector2(100, zeroLine);
                Vector2 b2 = b;
                b2.X += 1;
                MyRenderProxy.DebugDrawLine2D(new Vector2(b.X, b.Y - 200), new Vector2(b.X + 1000, b.Y - 200), Color.Gray,
                    Color.Gray);
                MyRenderProxy.DebugDrawLine2D(new Vector2(b.X, b.Y + 200), new Vector2(b.X + 1000, b.Y + 200), Color.Gray,
                    Color.Gray);
                MyRenderProxy.DebugDrawLine2D(new Vector2(b.X, b.Y), new Vector2(b.X + 1000, b.Y), Color.Gray, Color.Gray);
                MyRenderProxy.DebugDrawText2D(new Vector2(90, 400 - 200), (200/m_scale).ToString(), Color.White, 0.55f,
                    MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                MyRenderProxy.DebugDrawText2D(new Vector2(90, 400 + 200), (-200/m_scale).ToString(), Color.White, 0.55f,
                    MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                for (int i = Math.Min(1000, m_history.Count); i > 0; i--)
                {
                    int thisFrame = (m_frame + m_history.Count - i) % m_history.Count;
                    var frame = m_history[thisFrame];
                    var frame2 = m_history[(thisFrame + 1) % m_history.Count];
                    idx++;
                    foreach (var toPlot in m_toPlot)
                    {
                        if(frame.Count <= toPlot || frame2.Count <= toPlot)
                            continue;
                        var o = frame[toPlot];
                        if (o.GetType().IsPrimitive)
                        {
                            var v = ConvertToFloat(o);
                            var v2 = ConvertToFloat(frame2[toPlot]);
                            b.Y = zeroLine - v * m_scale;
                            b2.Y = zeroLine - v2 * m_scale;
                            if (idx == 1)
                                b.Y = b2.Y;
                            if (i < 3)
                                b2.Y = b.Y;
                            MyRenderProxy.DebugDrawLine2D(b, b2, m_colors[toPlot], m_colors[toPlot]);
                        }
                    }
                    b.X += 1;
                    b2.X += 1;
                }
            }

            private static float ConvertToFloat(object o)
            {
                float v = float.NaN;
                var val = o as int?;
                if (val.HasValue)
                    v = val.Value;
                var val2 = o as float?;
                if (val2.HasValue)
                    v = val2.Value;
                var val3 = o as double?;
                if (val3.HasValue)
                    v = (float) val3.Value;
                return v;
            }

            protected static Color[] m_colors = { new Color(0,192,192), Color.Orange, Color.BlueViolet * 1.5f, Color.BurlyWood, Color.Chartreuse,
                                  Color.CornflowerBlue, Color.Cyan, Color.ForestGreen, Color.Fuchsia,
                                  Color.Gold, Color.GreenYellow, Color.LightBlue, Color.LightGreen, Color.LimeGreen,
                                  Color.Magenta, Color.MintCream, Color.Orchid, Color.PeachPuff, Color.Purple };
        }
    }
}

#endif