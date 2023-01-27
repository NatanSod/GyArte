using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TalkBox;
using GyArte;

namespace Hivemind
{
    class DialogueHandler : ILineOutput
    {
        // TODO: Make a function that ends dialogue in a more elegant and official fashion.
        // TODO: If I've gone insane, make it possible to select options with the mouse.
        // TODO: Make it get the command manager from Mastermind.
        // CommandManager cm;
        DialogueRunner dr;
        DialogueDisplay dd;
        bool auto = false;
        List<int> choices = new List<int>();
        int choice = 0;
        public bool Running { get; private set; }

        void ILineOutput.Start()
        {
            Running = true;
            NextLine();
        }

        void ILineOutput.DisplayLine(TLine line)
        {
            if (choices.Count != 0)
            {
                choice = 0;
                choices.Clear();
                dd.EndOptions();
            }

            dd.SetLine(line);
            auto = line.lineTags.GetTag(Tag.LAST);
        }

        void ILineOutput.DisplayOptions(TLine[] options)
        {
            if (choices.Count != 0)
            {
                choice = 0;
                choices.Clear();
                dd.EndOptions();
            }

            int i = 0;
            foreach (TLine line in options)
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

            auto = false;

            if (choices.Count == 0)
            {
                // There is no option that can be selected, end the dialogue. Maybe throw an exception.
                Running = false;
                return;
            }
        }

        void ILineOutput.OptionSelected()
        {
            NextLine();
        }

        void ILineOutput.End()
        {
            Running = false;
            // Maybe I should tell the player to start moving here.
        }

        public DialogueHandler()
        {
            dr = Mastermind.lore;
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
            DebugTextBox oBox = new DebugTextBox(oOrigin, new(1, 1), oArea, oMargin, border, font, textSize, spaceWidth, lineSpacing, layoutSpacing);
            dd = new DialogueDisplay(dBox, nBox, oBox, new Scale(80, 30, 10));
        }

        public void Update(bool next, int scroll)
        {
            // There is no dialogue running. Therefore you should stop.
            if (!Running) return;

            if (dd.Done)
            {
                // The line has been fully written to the screen.
                if (choices.Count != 0)
                {
                    // Pick an option.
                    if (next && choices[choice] >= 0)
                    {
                        // Select the option.

                        dr.PickOption(choices[choice]);
                    }
                    else if (scroll != 0)
                    {
                        choice += scroll;
                        choice = (choice + choices.Count) % choices.Count;
                    }
                }
                else
                {
                    // Continue when told to or immediately, depending on the tags.
                    if (auto || next)
                    {
                        NextLine();
                    }
                }
            }
            else
            {
                // The line is being written to the screen and it isn't waiting for an async command.
                // If next line input was given, then tell the displayer to finnish.
                if (next)
                {
                    dd.SkipLine();
                }
            }
        }

        public void Draw()
        {
            // There is no dialogue running. Therefore you should stop.
            if (dr == null) return;

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

        public void BeginDialogue(DialogueRunner dialogueRunner)
        {
            // Get the dialogue and display the first line.
            dr = dialogueRunner;
            dr.Start();
            
        }

        private void NextLine() => dr.NextLine();
    }
}