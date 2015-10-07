﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Library.Utils;
using VRage.Profiler;
using VRageRender;
using VRageRender.Profiler;

namespace VRage
{
    /// <summary>
    /// Shortcut class for network profiler.
    /// </summary>
    public static class NetProfiler
    {
        static MyProfiler m_profiler;
        static Stack<float> m_stack = new Stack<float>(32);

        static NetProfiler()
        {
            m_profiler = MyRenderProfiler.CreateProfiler("Network", "B");
            m_profiler.AutoCommit = false;
        }

        static MyTimeSpan? ToTime(this float customTime)
        {
            return MyTimeSpan.FromMiliseconds(customTime);
        }

        static void TestUsage()
        {
            //NetProfiler.Begin("One");
            //var one = VRage.Library.Utils.MyRandom.Instance.NextFloat() * 10;
            //NetProfiler.End(one);

            //NetProfiler.Begin("Two");
            //NetProfiler.Begin("In Two");
            //one = VRage.Library.Utils.MyRandom.Instance.NextFloat() * 10;
            //NetProfiler.End(one);
            //one = 1 + VRage.Library.Utils.MyRandom.Instance.NextFloat() * 10;
            //NetProfiler.End(one);

            //NetProfiler.Commit();
        }

        /// <summary>
        /// Starts net profiling block.
        /// </summary>
        public static void Begin(string blockName = null, int forceOrder = int.MaxValue, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            m_profiler.StartBlock(blockName, member, line, file, forceOrder);
            m_stack.Push(0);
        }
        
        /// <summary>
        /// End net profiling block.
        /// </summary>
        /// <param name="bytesTransfered">Specify number of bytes transferred or null to automatically calculate number of bytes from inner blocks.</param>
        /// <param name="customValue">You can put any number here.</param>
        /// <param name="customValueFormat">This is formatting string how the number will be written on screen, use something like: 'MyNumber: {0} foos/s'</param>
        public static void End(float? bytesTransfered = null, float customValue = 0, string customValueFormat = "", string byteFormat = "{0} B", string callFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            float val = m_stack.Pop();
            float bytes = bytesTransfered ?? val;
            m_profiler.EndBlock(member, line, file, bytes.ToTime(), customValue, byteFormat, customValueFormat, callFormat);
            if(m_stack.Count > 0)
            {
                m_stack.Push(m_stack.Pop() + bytes);
            }
        }

        //public static void End(float customValue = 0, float? customTime = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        //{
        //    m_profiler.EndBlock(member, line, file, customTime.ToTime(), customValue, "{0} KB", "Something: {0}", "{0} msg");
        //}

        //public static void CustomValue(string name, float customValue, float? customTime, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        //{
        //    m_profiler.StartBlock(name, member, line, file);
        //    m_profiler.EndBlock(member, line, file, customTime.ToTime(), customValue, "{0} KB", valueFormat);
        //}

        public static void Commit()
        {
            if (MyRenderProfiler.Paused)
            {
                m_profiler.ClearFrame();
            }
            else
            {
                m_profiler.CommitFrame();
            }
        }
    }
}
