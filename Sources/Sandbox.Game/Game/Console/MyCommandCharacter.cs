using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game.GUI
{
    [PreloadRequired]
    public class MyCommandCharacter : MyCommand
    {
        private class MyCommandArgsValuesList : MyCommandArgs
        {
            public List<int> values;
        }

        public override string Prefix()
        {
            return "Character";
        }

        
        static MyCommandCharacter()
        {
            MyConsole.AddCommand(new MyCommandCharacter());
        }

        
        private MyCommandCharacter()
        {
            m_methods.Add
            (
                "AddSomeValues",
                new MyCommandAction()
                {
                    AutocompleteHint = new StringBuilder("int_val1 int_val2 ..."),
                    Parser = (x) => ParseValues(x),
                    CallAction = (x) => PassValuesToCharacter(x)
                }
           );

        }

        private MyCommandArgs ParseValues(List<string> args)
        {
            MyCommandArgsValuesList output = new MyCommandArgsValuesList();
            output.values = new List<int>();
            foreach (var arg in args)
                output.values.Add(Int32.Parse(arg));

            return output;
        }

        private StringBuilder PassValuesToCharacter(MyCommandArgs args)
        {
            var argsvl = args as MyCommandArgsValuesList;
            if (argsvl.values.Count == 0)
                return new StringBuilder("No values passed onto character");

            foreach (var value in argsvl.values)
            {
            }

            StringBuilder output = new StringBuilder().Append("Added values ");
            foreach (var value in argsvl.values)
                output.Append(value).Append(" ");
            output.Append("to character");
            return output;

        }
    }
}
