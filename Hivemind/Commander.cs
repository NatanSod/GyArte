using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;
using GyArte;
using TalkBox;

namespace Hivemind
{
    class Commander : CommandManager
    {
        override public void RunFunction(DialogueRunner runner, string function, Variable[] args)
        {
            switch (function)
            {
                case "change_room":
                    string room = args[0].GetValue<string>();
                    int entrance = (int)args[1].GetValue<float>();
                    Mastermind.ConstructHive(room, entrance);
                    break;
                case "set_solid":
                    string name = args[0].GetValue<string>();
                    bool value = args[1].GetValue<bool>();
                    Slave? slave = Mastermind.currentHive.HuntSlave(name);
                    if (slave == null) throw new NullReferenceException($"The slave called {name} was not found");
                    slave.SetSolid(value);
                    break;
                default:
                    throw new NotImplementedException($"{function} is not a real function.");
            }
            runner.FinnishCommand(); 
        }
    }
}