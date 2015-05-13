using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using VRageMath;

// Recording keys through a sequence of input snapshots, which are then pumped
// to the engine, so we can use for playback

namespace Sandbox.Common.Input
{
    [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
    public enum MyInputRecordingSession
    {
        Specific,
        NewGame,
        MainMenu,
    }

    [Serializable, Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
    public class MyInputRecording
    {
        // MUST be public, otherwise the serializer won't work...
        public string Name;
        public string Description;
        public List<MyInputSnapshot> SnapshotSequence;
        public MyInputRecordingSession Session;
        // Record also the resolution used when recording
        public int OriginalWidth;
        public int OriginalHeight;

        private int m_currentSnapshotNumber;
        private int m_startScreenWidth;
        private int m_startScreenHeight;

        public MyInputRecording()
        {
            m_currentSnapshotNumber = 0;
            SnapshotSequence = new List<MyInputSnapshot>();
        }

        public bool IsDone()
        {
            return m_currentSnapshotNumber == SnapshotSequence.Count;
        }
        
        public void Save(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MyInputRecording));
            using (TextWriter writer = new StreamWriter(filename))
            {
                serializer.Serialize(writer, this);
            } 
        }

        public void SetStartingScreenDimensions(int width, int height)
        {
            m_startScreenWidth = width;
            m_startScreenHeight = height;
        }

        public int GetStartingScreenWidth()
        {
            return m_startScreenWidth;
        }

        public int GetStartingScreenHeight()
        {
            return m_startScreenHeight;
        }

        public Vector2 GetMouseNormalizationFactor()
        {
            return new Vector2((float)m_startScreenWidth / OriginalWidth, (float)m_startScreenHeight / OriginalHeight);
        }

        public MyInputSnapshot GetNextSnapshot()
        {
            return SnapshotSequence[m_currentSnapshotNumber++];
        }

        public static MyInputRecording FromFile(string filename)
        {
            using (StreamReader reader = new StreamReader(filename))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MyInputRecording));
                return (MyInputRecording)serializer.Deserialize(reader);
            }
        }

        public void AddSnapshot(MyInputSnapshot snapshot)
        {
            SnapshotSequence.Add(snapshot);
        }

    }
}
