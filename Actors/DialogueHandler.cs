using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using GameMaster;
using TalkBox;

namespace GyArte
{
    class DialogueHandler : Actor
    {
        CommandManager cm = new CommandManager();
        public string dialogue = String.Empty;
        DialogueRunner? dr;
        IEnumerator<TLineCollection?>? LineGetter;
        DialogueDisplay dd = new DialogueDisplay();
        bool waiting = false;
        bool auto = false;
        List<int> choices = new List<int>();
        int choice = 0;

        protected override void Start()
        {
            position = Vector3.Zero;
            velocity = Vector3.Zero;
        }

        protected override void Update()
        {
            waiting = cm.ExecuteAll();
        }

        public override void Draw()
        {
            // There is no dialogue running. Therefore you should stop.
            if (dr == null) return;

            if (waiting)
            {
                // Currently waiting for a async command to finish.
                // Making it impossible to continue to the next line (even if it's an auto line) while waiting is currently the safest option.
            }
            else if (dd.done)
            {
                // The line has been fully written to the screen.
                if (choices.Count != 0)
                {
                    // Pick an option.
                    if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
                    {
                        // Select the option and get the next line.
                        dr.PickOption(choices[choice]);
                        choice = 0;
                        choices.Clear();
                        dd.EndOptions();
                        NextLine();
                    }
                    else
                    {
                        // Scroll through which options to select.
                        if (Raylib.IsKeyPressed(KeyboardKey.KEY_W) || Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
                        {
                            choice++;
                        }
                        if (Raylib.IsKeyPressed(KeyboardKey.KEY_S) || Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
                        {
                            choice--;
                        }
                        choice = (choice + choices.Count) % choices.Count;
                    }
                }
                else
                {
                    // Continue when told to or immediately, depending on the tags.
                    if (auto || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
                    {
                        NextLine();
                    }
                }
            }
            else
            {
                // The line is being written to the screen and it isn't waiting for an async command.
                // If next line input was given, then tell the displayer to finnish.
            }

            // Display the dialogue.
            if (choices.Count == 0)
            {
                dd.Display();
            }
            else
            {
                dd.Display(choice);
            }
        }

        public void BeginDialogue(string dialogueName)
        {
            // Would prefer if two dialogues wouldn't start at the same time.
            if (dr == null)
            {
                // Get the dialogue and display the first line.
                dr = new DialogueRunner(cm, dialogueName);
                LineGetter = dr.GetLineE();
                NextLine();
            }
        }

        private void NextLine()
        {
            // Get the next line from LineGetter and give it to display line
            if (LineGetter?.MoveNext() == true)
            {
                // The dialogue continues. Display it.
                if (LineGetter.Current != null)
                {
                    TLineCollection currentLine = LineGetter.Current;
                    if (currentLine.lineType == Line.LineType.Dialogue)
                    {
                        // Dialogue line.
                        dd.SetLine(currentLine.line ?? throw new Exception("This should never happen, but it makes the IDE happy."));
                        auto = currentLine.line.lineTags.GetTag(Tag.Last);
                    }
                    else
                    {
                        // Option line.
                        int i = 0;
                        foreach (TLine line in (currentLine.lines ?? throw new Exception("This should never happen, but it makes the IDE happy.")))
                        {
                            if (line.statement == String.Empty || Variables.Evaluate(line.statement))
                            {
                                // This line is to be displayed.
                                dd.AddOption(line, true);
                                choices.Add(i);
                            }
                            else if (line.lineTags.GetTag(Tag.Lock))
                            {
                                // This line is to be displayed, but not selectable.
                                dd.AddOption(line, false);
                            }
                            // Else, this line is not to be displayed.
                            i++;
                        }

                        auto = false;
                        choice = currentLine.lines.Length;
                    }
                }
                else
                {
                    // I have made spaghetti code. I need to clean up the async function stuff.
                    // Until then, if this happens, no action should be taken.
                }
            }
            else 
            {
                // The dialogue has ended (It might also not exist, but that should never happen).
                LineGetter = null;
                dr = null;
            }
        }

        class DialogueDisplay
        {
            TLine? currentLine;
            TextBox text = new TextBox(); 
            TextBox name = new TextBox();
            List<(TextBox, bool)> options = new List<(TextBox, bool)>();
            
            public bool done { get; private set; } = false;

            public void SetLine(TLine line)
            {
                currentLine = line;
            }
            
            public void EndOptions()
            {
                options.Clear();
            }

            public void AddOption(TLine option, bool selectable)
            {
                options.Add((new TextBox(option), selectable));
            }

            public void Display()
            {

            }

            public void Display(int option)
            {

            }
        }

        class TextBox
        {
            public TextBox()
            {
                // Unfinished.
            }

            public TextBox (TLine text)
            {
                // Unfinished.
            }
        }
    }

}