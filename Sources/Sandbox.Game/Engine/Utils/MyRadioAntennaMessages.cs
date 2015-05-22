using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Engine.Utils
{
    static class MyRadioAntennaMessages
    {
        //All messages will decay after this amount of time.
        private const float MAX_MESSAGE_DURATION = 600f;
        private const float MIN_TIME_BETWEEN_UPDATES = 2f;
        public const int MAX_MESSAGE_LENGTH = 100;
        private const int MAX_TEXT_LENGTH = 4200; //Same as the Display Block

        private static readonly float STOPWATCH_FREQUENCY = 1.0f / Stopwatch.Frequency;
        private static readonly int MAX_MESSAGES = MAX_TEXT_LENGTH / MAX_MESSAGE_LENGTH;

        private static MyRadioAntennaMsgStorage m_oldestStorage = null; //For quick and easy decaying of the oldest message.
        private static MyRadioAntennaMsgStorage m_newestStorage = null; //For quick and easy adding of new messages.
        private static Dictionary<int, MyRadioAntennaMsgStorage> m_messageDictionary = null;

        private static long m_timeSinceLastUpdate = 0;
        private static int m_messageCount = 0;

        private class MyRadioAntennaMsgStorage
        {
            public StringBuilder m_text;
            public int m_hash;
            public int m_nextHash;
            public long m_timeStamp;

            public MyRadioAntennaMsgStorage(StringBuilder text, int hash, long timestamp)
            {
                m_text = text;
                m_hash = hash;
                m_timeStamp = timestamp;

                m_messageCount++;
                if (m_messageCount > MAX_MESSAGES)
                {
                    if (m_oldestStorage != null && m_messageDictionary.ContainsKey(m_oldestStorage.m_nextHash))
                    {
                        Remove(m_oldestStorage, m_messageDictionary[m_oldestStorage.m_nextHash]);
                    }
                    else
                    {
                        //Shit happened. Count says that there are too many messages, but they can't be found. Restart!
                        Clear();
                    }
                }

                //Add to dict
                m_messageDictionary.Add(hash, this);

                //Is this the first object to be added?
                if (m_oldestStorage == null)
                {
                    m_oldestStorage = this;
                }
                else
                {
                    //There is an older entry.
                    m_newestStorage.m_nextHash = hash;
                }

                m_newestStorage = this;
                m_nextHash = 0;
            }

        }

        private static void Update()
        {
            var now = Stopwatch.GetTimestamp();
            var elapsedTime = (now - m_timeSinceLastUpdate) * Sync.RelativeSimulationRatio;
            elapsedTime *= STOPWATCH_FREQUENCY;

            //No need to update this to often.
            if (elapsedTime >= MIN_TIME_BETWEEN_UPDATES && m_newestStorage != null)
            {
                m_timeSinceLastUpdate = now;
                MyRadioAntennaMsgStorage storage = m_newestStorage;

                while (storage != null)
                {
                    elapsedTime = (now - storage.m_timeStamp) * Sync.RelativeSimulationRatio;
                    elapsedTime *= STOPWATCH_FREQUENCY;

                    if (elapsedTime >= MAX_MESSAGE_DURATION)
                    {
                        //Message to old. Decay it.
                        //Is ther an other message that we might need to decay?
                        if (m_messageDictionary.ContainsKey(storage.m_nextHash))
                        {
                            var lastStorage = storage;
                            storage = m_messageDictionary[storage.m_nextHash];

                            //Remove the decaying message.
                            Remove(lastStorage, storage);
                        }
                        else
                        {
                            Remove(storage, null);
                            storage = null; //End of the line
                            break;
                        }
                    }
                    else
                    {
                        break; //No sense in going through even new ones.
                    }
                }
            }
        }

        //To do: call this when the game loads another savegame.
        private static void Clear()
        {
            m_messageCount          = 0;
            m_messageDictionary     = null;
            m_newestStorage         = null;
            m_oldestStorage         = null;
            m_timeSinceLastUpdate   = 0;
        }

        private static void Remove(MyRadioAntennaMsgStorage storageToDecay, MyRadioAntennaMsgStorage storageNext)
        {
            //Obviously there isn't any older message then this, as that message should already be removed.
            storageToDecay.m_text = null;
            if (storageNext != null)
            {
                m_oldestStorage = storageNext;
            }
            else
            {
                //The very last message has decayed.
                m_oldestStorage = null;
                m_newestStorage = null;
            }

            //Remove from dict.
            m_messageDictionary.Remove(storageToDecay.m_hash);
            m_messageCount--;
        }

        /// <summary>
        /// Add a new message to the message storage.
        /// </summary>
        /// <param name="text">The stringbuilder containing the new message.</param>
        /// <returns>The hash pointing to this message.</returns>
        public static int Add(StringBuilder text) 
        {
            //Create only when needed.
            if (m_messageDictionary == null)
                m_messageDictionary = new Dictionary<int, MyRadioAntennaMsgStorage>();

            if (text == null || text.Length == 0)
                return -1;

            Update();

            int hash = text.GetHashCode();
            MyRadioAntennaMsgStorage storage = new MyRadioAntennaMsgStorage(text, hash, Stopwatch.GetTimestamp());

            return hash;
        }

        /// <summary>
        /// Builds the text containing all valid messages the calling antenna has received.
        /// </summary>
        /// <param name="antenna">The calling antenna</param>
        /// <returns></returns>
        public static StringBuilder GetMessages(MyRadioAntenna antenna)
        {
            if (m_messageDictionary == null)
                return null;

            //First remove all the old entries.
            Update();

            if (m_oldestStorage == null)
                return null; //There are no messages.

            StringBuilder text = new StringBuilder();
            List<int> newHashs = new List<int>();

            if (antenna.m_messageHashs == null)
                return null;

            foreach (int hash in antenna.m_messageHashs)
            {
                if (m_messageDictionary.ContainsKey(hash))
                {
                    text.Append(m_messageDictionary[hash].m_text);
                    newHashs.Add(hash);
                }
            }

            antenna.m_messageHashs = newHashs;
            return text;
        }

    }
}
