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
        // TODO: Make a function that ends dialogue in a more elegant and official fashion.
        // TODO: If I've gone insane, make it possible to select options with the mouse.

        CommandManager cm = new CommandManager();
        public string dialogue = String.Empty;
        DialogueRunner? dr;
        IEnumerator<TLineCollection?>? LineGetter;
        DialogueDisplay dd;
        bool waiting = false;
        bool auto = false;
        List<int> choices = new List<int>();
        int choice = 0;

        public DialogueHandler()
        {
            Font font = Raylib.GetFontDefault();
            Scale textSize = new Scale(15, 10, 5);
            Vector2 border = new Vector2(2, 2);
            int spaceWidth = 2; 
            int lineSpacing = 3;
            int layoutSpacing = 4;
            
            int dLines = 4;
            Vector2 dArea = new Vector2(320, textSize.Medium * dLines + lineSpacing * (dLines - 1));
            Vector2 dMargin = new Vector2(14, 7);
            Vector2 dOrigin = new Vector2(dMargin.X + border.X, Render.Height - dArea.Y - dMargin.Y - border.Y);
            DebugTextBox dBox = new DebugTextBox(dOrigin, new(0, 0), dArea, dMargin, border, font, textSize, spaceWidth, lineSpacing, layoutSpacing);
            Vector2 nArea = new Vector2(0, textSize.Medium);
            Vector2 nMargin = new Vector2(5, 3);
            Vector2 nOrigin = new Vector2(44 + nMargin.X + border.X, Render.Height - textSize.Medium - dArea.Y - (dMargin.Y + border.Y) * 2 - nMargin.Y); 
            DebugTextBox nBox = new DebugTextBox(nOrigin, new(0, 0), nArea, nMargin, border, font, textSize, spaceWidth, lineSpacing, layoutSpacing); 
            Vector2 oArea = new Vector2(0, 0);
            Vector2 oMargin = new Vector2(4, 3);
            Vector2 oOrigin = new Vector2(Render.Width - 11 - oMargin.X - border.X, dOrigin.Y - dMargin.Y - oMargin.Y - border.Y * 2 - 20);
            DebugTextBox oBox = new DebugTextBox(oOrigin, new(1, 1), oArea, border, oMargin, font, textSize, spaceWidth, lineSpacing, layoutSpacing);
            dd = new DialogueDisplay(dBox, nBox, oBox, new Scale(80, 30, 10));
        }

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
                    if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) && choices[choice] >= 0)
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
                            choice--;
                        }
                        if (Raylib.IsKeyPressed(KeyboardKey.KEY_S) || Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
                        {
                            choice++;
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
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
                {
                    dd.SkipLine();
                }
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
                        auto = currentLine.line.lineTags.GetTag(Tag.LAST);
                    }
                    else
                    {
                        // Option line.
                        int i = 0;
                        foreach (TLine line in ((TLine[])currentLine.lines))
                        {
                            if (line.statement == String.Empty || Variables.Evaluate(line.statement))
                            {
                                // This line is to be displayed.
                                dd.AddOption(line, true);
                                choices.Add(i);
                            }
                            else if (line.lineTags.GetTag(Tag.LOCK))
                            {
                                // This line is to be displayed, but not selectable.
                                dd.AddOption(line, false);
                                choices.Add(-i); // In order to still be able to hover over them but not select them, I did this.
                            }
                            // Else, this line is not to be displayed.
                            i++;
                        }

                        if (choices.Count == 0)
                        {
                            // There is no option that can be selected, end the dialogue.
                            LineGetter = null;
                            dr = null;
                            return;
                        }

                        auto = false;
                        choice = 0;
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
    }
}