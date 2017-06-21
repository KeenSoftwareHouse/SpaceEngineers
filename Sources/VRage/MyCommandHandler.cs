using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    internal class MyCommandHandler
    {
        private Dictionary<string, MyCommand> m_commands;

        public MyCommandHandler()
        {
            m_commands = new Dictionary<string, MyCommand>();
        }

        public StringBuilder Handle(string input)
        {

            List<string> args = SplitArgs(input);
            MyCommand command;

            if (args.Count <= 0)
                return new StringBuilder("Error: Empty string");

            string CommandString = args[0];
            var CommandKey = GetCommandKey(CommandString);
            if (CommandKey == null)
                return new StringBuilder().AppendFormat("Error: Invalid method syntax \'{0}\'", input);

            //We don't need the CommandString as args
            args.RemoveAt(0);

            if (m_commands.TryGetValue(CommandKey, out command))
            {
                var CommandMethod = GetCommandMethod(CommandString);
                if (CommandMethod == null)
                    return new StringBuilder().AppendFormat("Error: Invalid method syntax \'{0}\'", input);
                else if (CommandMethod == "")
                    return new StringBuilder("Error: Empty Method");

                try
                {
                    return new StringBuilder().Append(CommandKey).Append(".").Append(CommandMethod).Append(": ").Append(command.Execute(CommandMethod, args));
                }
                catch (MyConsoleInvalidArgumentsException)
                {
                    return new StringBuilder().AppendFormat("Error: Invalid Argument for method {0}.{1}",CommandKey, CommandMethod);
                }
                catch (MyConsoleMethodNotFoundException)
                {
                    return new StringBuilder().AppendFormat("Error: Command {0} does not contain method {1}",CommandKey, CommandMethod);
                }
            }

            return new StringBuilder().AppendFormat("Error: Unknown command {0}\n", CommandKey);
        }

        #region string manipulation

#if XB1
        private IEnumerable<String> SplitArgsSelector2(String[] element)
        {
            return element;
        }
#endif
        
        //Splits by spaces unless in quotes, in which case we remove the quotes
        public List<string> SplitArgs(string input)
        {
#if XB1
            string[] splitted = input.Split('"');
            IEnumerable<String[]> selected = splitted.Select((element, index) => index % 2 == 0
                                                            ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                            : new string[] { element });
            IEnumerable<String> simplified = selected.SelectMany(SplitArgsSelector2);
            List<string> asList = simplified.ToList();
            return asList;
#else
            return input.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToList();

#endif
        }

        //gets A from string A.B
        public string GetCommandKey(string input)
        {
            if (!input.Contains("."))
                return null;

            return input.Substring(0, input.IndexOf("."));
        }

        //gets B from string A.B
        public string GetCommandMethod(string input)
        {
            try
            {
                return input.Substring(input.IndexOf(".") + 1);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        public void AddCommand(MyCommand command)
        {
            if (m_commands.ContainsKey(command.Prefix()))
                m_commands.Remove(command.Prefix());
            m_commands.Add(command.Prefix(), command);
        }

        public void RemoveAllCommands()
        {
            m_commands.Clear();
        }

        public bool ContainsCommand(string command)
        {
            return m_commands.ContainsKey(command);
        }

        public bool TryGetCommand(string commandName, out MyCommand command)
        {
            if (!m_commands.ContainsKey(commandName))
            {
                command = null;
                return false;
            }
            command = m_commands[commandName];
            return true;
        }
    }
}
