using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public static class MyConsole
    {
        private static StringBuilder m_displayScreen = new StringBuilder();
        private static MyCommandHandler m_handler = new MyCommandHandler();
        private static LinkedList<string> m_commandHistory = new LinkedList<String>();
        private static LinkedListNode<string> m_position = null;

        public static StringBuilder DisplayScreen
        {
            get
            {
                return m_displayScreen;
            }
        }

        public static void ParseCommand(string command)
        {
            if (m_position == null)
            {
                m_commandHistory.AddLast(command);
            }
            else
            {
                m_commandHistory.AddAfter(m_position, command);
                m_position = m_position.Next;
            }
            m_displayScreen.Append(m_handler.Handle(command)).AppendLine();
        }

        public static void PreviousLine()
        {
            if (m_position == null)
                m_position = m_commandHistory.Last;

            else if(m_position != m_commandHistory.First)
                m_position = m_position.Previous;
        }

        public static void NextLine()
        {
            if (m_position != null)
                m_position = m_position.Next;
        }

        public static string GetLine()
        {
            if (m_position == null)
                return "";

            return m_position.Value;
        }

        public static void Clear()
        {
            m_displayScreen.Clear();
        }

        public static void AddCommand(MyCommand command)
        {
            m_handler.AddCommand(command);
        }

        public static bool TryGetCommand(string commandName, out MyCommand command)
        {
            return m_handler.TryGetCommand(commandName, out command);
        }
    }
}
