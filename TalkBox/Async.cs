using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    class Command
    {
        /// <summary>
        /// Is this command done?
        /// </summary>
        public bool done { get; private set; } = false;
        /// <summary>
        /// Should the runner wait before continuing?
        /// </summary>
        public bool wait { get; private set; } = true;

        private IEnumerator<bool?> command;

        public Command (IEnumerator<bool?> enumerator)
        {
            command = enumerator;
        }

        public void Continue()
        {
            if (done)
            {
                Console.WriteLine("Hey, this command is done, please stop calling it");
                return;
            }

            if (command.MoveNext())
            {
                if (command.Current == true)
                {
                    wait = false;
                }
            }
            else
            {
                done = true;
            }
        }
    }

    class CommandManager
    {
        private List<Command> commands = new List<Command>();

        public CommandManager ()
        {

        }

        /// <summary>
        /// Add a command to the list of commands to execute over time.
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns>Should it wait?</returns>
        public bool Add(IEnumerator<bool?> enumerator)
        {
            Command command = new Command (enumerator);
            command.Continue();
            if (command.done)
            {
                return false;
            }
            else 
            {
                commands.Add(command);
                return command.wait;
            }
        }

        public bool ExecuteAll()
        {
            bool wait = false;
            for (int i = 0; i < commands.Count; i++)
            {
                Command current = commands[i];
                current.Continue();
                if (current.done)
                {
                    // Remove commands that are done.
                    commands.Remove(current);
                    i--;
                }
                else 
                {
                    // Change the value of wait to true if current command should be waited for
                    wait = current.wait ? current.wait : wait;
                }
            }
            return wait;
        }
    }
}