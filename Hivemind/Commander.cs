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
        List<IEnumerator> asyncCommands = new List<IEnumerator>();

        override public void RunFunction(DialogueRunner runner, string function, Variable[] args)
        {
            switch (function)
            {
                case "change_room":
                    string room = args[0].GetValue<string>();
                    int entrance = (int)args[1].GetValue<float>();
                    Mastermind.ConstructHive(room, entrance);
                    runner.FinnishCommand();
                    break;

                case "set_solid":
                    string name = args[0].GetValue<string>();
                    bool value = args[1].GetValue<bool>();
                    Slave? slave = Mastermind.currentHive.HuntSlave(name);
                    if (slave == null) throw new NullReferenceException($"The slave called {name} was not found");
                    slave.SetSolid(value);
                    runner.FinnishCommand();
                    break;

                case "fade_in":
                    Mastermind.mouthpiece.StopDisplaying();
                    IEnumerator fadeIn = FadeIn(runner);
                    asyncCommands.Add(fadeIn);
                    break;

                case "fade_out":
                    Mastermind.mouthpiece.StopDisplaying();
                    IEnumerator fadeOut = FadeOut(runner);
                    asyncCommands.Add(fadeOut);
                    break;

                case "face":
                    string facerName = args[0].GetValue<string>();
                    Slave? facer = Mastermind.currentHive.HuntSlave(facerName);
                    if (facer == null) throw new NullReferenceException($"The slave called {facerName} was not found");
                    if (args[1].Type == Variable.vType.Number)
                    {
                        facer.Face((int)args[1].GetValue<float>());
                    }
                    else 
                    {
                        string targetName = args[1].GetValue<string>();
                        if (targetName == "Player")
                        {
                            facer.Face(facer.Position - Mastermind.victim.Facing);
                        }
                        else
                        {
                            Slave? target = Mastermind.currentHive.HuntSlave(targetName);
                            if (facer == null) throw new NullReferenceException($"The slave called {targetName} was not found");
                        }
                    }
                    runner.FinnishCommand();
                    break;
                default:
                    throw new NotImplementedException($"{function} is not a real function.");
            }
        }

        public void RunAsync()
        {
            for (int i = 0; i < asyncCommands.Count; i++)
            {
                IEnumerator command = asyncCommands[i];

                if (!command.MoveNext())
                {
                    asyncCommands.Remove(command);
                    i--;
                }
            }
        }

        public IEnumerator FadeIn(DialogueRunner runner)
        {
            for (float i = 10; i > 0; i -= i * .01f + .1f)
            {
                Render.BeginDraw(Render.Layer.UI, -1);
                Raylib.DrawRectangle(0, 0, Render.Width, Render.Height, new Color(0, 0, 0, (int)(i * 255 / 10)));
                Render.EndDraw();
                yield return null;
            }
            runner.FinnishCommand();
        }

        public IEnumerator FadeOut(DialogueRunner runner)
        {
            for (float i = 0; i < 10; i += i * .01f + .1f)
            {
                Render.BeginDraw(Render.Layer.UI, -1);
                Raylib.DrawRectangle(0, 0, Render.Width, Render.Height, new Color(0, 0, 0, (int)(i * 255 / 10)));
                Render.EndDraw();
                yield return null;
            }
            runner.FinnishCommand();
        }
    }
}