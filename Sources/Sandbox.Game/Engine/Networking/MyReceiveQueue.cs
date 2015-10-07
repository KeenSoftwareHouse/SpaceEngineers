﻿using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;

namespace Sandbox.Engine.Networking
{
    /// <summary>
    /// Receives messages from Steam immediatelly on separate thread
    /// Precise receive time is required for proper interpolation
    /// </summary>
    sealed class MyReceiveQueue : IDisposable
    {
        public enum Mode
        {
            /// <summary>
            /// Read messages synchronized when calling Process, timestamps won't be accurate
            /// </summary>
            Synchronized,

            /// <summary>
            /// High precision timer will read messages (1ms intervals)
            /// </summary>
            Timer,

            /// <summary>
            /// Messages will be read in spin cycle, HIGH CPU USAGE!
            /// </summary>
            Spin,
        }

        class Message
        {
            public TimeSpan Timestamp;
            public byte[] Data;
            public int Length;
            public ulong UserId;

            public long ReceiveTime;
        }

        MyTimer m_timer;
        Action m_timerAction;
        Thread m_readThread;

        [ThreadStatic]
        bool m_knownThread;

        MyConcurrentPool<Message> m_messagePool;
        MyConcurrentQueue<Message> m_receiveQueue;
        Func<TimeSpan> m_timestampProvider;

        public bool Disposed { get; private set; }
        public readonly int Channel;
        public readonly Mode ReadMode;

        public bool Started = false;

        public MyReceiveQueue(int channel, Mode readMode = Mode.Synchronized, int defaultMessageCount = 1, Func<TimeSpan> timestampProvider = null)
        {
            Trace.Assert(readMode != Mode.Spin, "Spin mode should be used only for testing purposes, it keeps CPU under heavy load!");

            Disposed = false;
            Channel = channel;
            ReadMode = readMode;

            m_messagePool = new MyConcurrentPool<Message>(defaultMessageCount, true);
            m_receiveQueue = new MyConcurrentQueue<Message>(defaultMessageCount);
            m_timestampProvider = timestampProvider;

            if (readMode == Mode.Spin)
            {
                m_readThread = new Thread(ReceiveThread);
                m_readThread.CurrentCulture = CultureInfo.InvariantCulture;
                m_readThread.CurrentUICulture = CultureInfo.InvariantCulture;
                m_readThread.Start();
            }
            else if (readMode == Mode.Timer)
            {
                m_timerAction = new Action(ReceiveTimer);
                m_timer = new MyTimer(1, m_timerAction);
                m_timer.Start();
            }
        }

        Message GetMessage(int size)
        {
            var msg = m_messagePool.Get();
            if (msg.Data == null)
            {
                msg.Data = new byte[Math.Max(256, size)];
            }
            else if (msg.Data.Length < size)
            {
                Array.Resize(ref msg.Data, size);
            }
            return msg;
        }

        void ReceiveThread()
        {
            if (!Started)
            {
                MyLog.Default.WriteLine("Network receive thread started");
                Started = true;
            }

            while (!Disposed)
            {
                ReceiveOne();
            }
        }

        void ReceiveTimer()
        {
            if (!m_knownThread)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                m_knownThread = true;
            }

            lock (m_timer)
            {
                // Read all available messages
                while (!Disposed && ReceiveOne())
                {
                }
            }
        }

        private bool ReceiveOne()
        {
            uint length;

            if ((MySteam.IsActive || MySteam.Server != null) && Peer2Peer.IsPacketAvailable(out length, Channel))
            {
                ulong sender;
                var msg = GetMessage((int)length);
                if (Peer2Peer.ReadPacket(msg.Data, out length, out sender, Channel))
                {
                    if (m_timestampProvider != null)
                    {
                        msg.Timestamp = m_timestampProvider();
                    }

                    msg.ReceiveTime = Stopwatch.GetTimestamp();
                    msg.Length = (int)length;
                    msg.UserId = sender;

                    m_receiveQueue.Enqueue(msg);
                    return true;
                }
                else
                {
                    m_messagePool.Return(msg);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            m_receiveQueue.Clear();
        }

        /// <summary>
        /// Enqueues message for processing, useful for loopback
        /// </summary>
        public void AddMessage(byte[] data, int length, ulong sender)
        {
            var msg = GetMessage(length);
            Array.Copy(data, msg.Data, length);
            msg.Length = length;
            msg.ReceiveTime = Stopwatch.GetTimestamp();
            if (m_timestampProvider != null)
            {
                msg.Timestamp = m_timestampProvider();
            }
            msg.UserId = sender;
            m_receiveQueue.Enqueue(msg);
        }

        /// <summary>
        /// Processes messages
        /// Delay indicates how long must be message in queue before processing (lag emulation)
        /// </summary>
        public void Process(NetworkMessageDelegate handler, TimeSpan delay = default(TimeSpan))
        {
            if (ReadMode == Mode.Synchronized)
            {
                // Process old messages (stored because of delay)
                if (m_receiveQueue.Count > 0)
                {
                    ProcessMessages(handler, delay);
                }

                // Read and process all of them cuz it might ping-pong msgs between server-client all the time
                while (ReceiveOne());

                ProcessMessages(handler, delay);
            }
            else
            {
                ProcessMessages(handler, delay);
            }
        }

        private void ProcessMessages(NetworkMessageDelegate handler, TimeSpan delay)
        {
            long delayTicks = (long)Math.Round(delay.TotalSeconds * Stopwatch.Frequency);
            long processTime = Stopwatch.GetTimestamp() - delayTicks;

            Message msg;
            int count = m_receiveQueue.Count;
            while (m_receiveQueue.TryPeek(out msg) && (processTime > msg.ReceiveTime) && count != 0)
            {
                count--;
                if (m_receiveQueue.TryDequeue(out msg)) // Can fail when queue was cleared
                {
                    if (msg.Timestamp != TimeSpan.Zero)
                    {
                        msg.Timestamp += delay;
                    }

                    handler(msg.Data, msg.Length, msg.UserId, msg.Timestamp);
                    m_messagePool.Return(msg);
                }
            }
        }

        public void Dispose()
        {
            Disposed = true;

            if (ReadMode == Mode.Spin)
            {
                m_readThread.Join();
            }
            else if (ReadMode == Mode.Timer)
            {
                lock (m_timer)
                {
                    m_timer.Dispose();
                    m_timerAction = null;
                }
            }

            GC.SuppressFinalize(this);
        }

        ~MyReceiveQueue()
        {
            Debug.Fail("MyReceiveQueue was not disposed, MyMultiplayer.Dispose not called?");
        }
    }
}
