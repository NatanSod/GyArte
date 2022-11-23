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
        DialogueDisplay dd = new DialogueDisplay(new Vector2(50, 400), new Vector2(700, 90), 10, 5, new FontSize(30, 20, 10), 30);
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
                        auto = currentLine.line.lineTags.GetTag(Tag.LAST);
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
                            else if (line.lineTags.GetTag(Tag.LOCK))
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

        struct FontSize
        {
            public int big { get; private set; }
            public int medium { get; private set; }
            public int small { get; private set; }

            public FontSize(int big, int medium, int small)
            {
                this.big = big;
                this.medium = medium;
                this.small = small;
            }

            public int Get(Span.MarkValue size)
            {
                if (size == Span.MarkValue.BIG)
                    return big;
                else if (size == Span.MarkValue.SMALL)
                    return small;
                else
                    return medium;
            }
        }

        class DialogueDisplay
        {
            TLine? currentLine;
            SpanCollection? currentSpans;
            TextLayout layout = new TextLayout(20);
            Vector2 textOrigin;
            Vector2 textArea;
            FontSize fontSize;
            int lineHeight;
            int margin;
            int border;
            FontSize symbolBase;
            float progress = 0;
            TextBox text = new TextBox();
            TextBox name = new TextBox();
            List<(TextBox, bool)> options = new List<(TextBox, bool)>();

            /// <summary>
            /// These are a lot of parameters, ay?
            /// </summary>
            /// <param name="textOrigin">The top left coordinate of where the first letter will be drawn.</param>
            /// <param name="textArea">The size of the area that is allowed to include text.</param>
            /// <param name="margin">The "radius" of extra background colour outside the text area.</param>
            /// <param name="border">The "radius" of border colour outside the margin.</param>
            /// <param name="fontSize">Self explanatory.</param>
            /// <param name="lineHeight">Self explanatory.</param>
            /// <param name="monospace">Should the font be displayed as if monospaced?</param>
            /// <param name="symbolMargin">The amount of additional pixels between each symbol</param>
            public DialogueDisplay(Vector2 textOrigin, Vector2 textArea, int margin, int border, FontSize fontSize, int lineHeight)
            {
                this.fontSize = fontSize;
                this.lineHeight = lineHeight;
                this.textArea = textArea;
                this.margin = margin;
                this.border = border;
                this.textOrigin = textOrigin;

                int aaWidth = Raylib.ImageText("aa", fontSize.medium, Color.BLACK).width;
                int aWidth = Raylib.ImageText("a", fontSize.medium, Color.BLACK).width * 2;
                int symbolMargin = (aaWidth - aWidth) >> 1;
                layout = new TextLayout((int)textArea.X, Raylib.ImageText(" ", fontSize.medium, Color.BLACK).width, symbolMargin);

                // This is all for the purpose of making the bottom of big, medium, and small symbols line up.
                unsafe
                {
                    // returns the 0 base distance from the top of the bottom most part of the symbol 'I'.
                    int GetBase(int size)
                    {
                        Image iImage = Raylib.ImageText("I", size, Color.BLACK);
                        Color* colors = Raylib.LoadImageColors(iImage);

                        int center = iImage.width >> 1;
                        for (int i = iImage.height - 1; i >= 0; i--)
                        {
                            if (colors[i * iImage.width + center].a != 0)
                            {
                                return i + 1;
                            }
                        }
                        return -1; // To make it apparent that things went wrong.
                    }
                    symbolBase = new FontSize(GetBase(this.fontSize.big), GetBase(this.fontSize.medium), GetBase(this.fontSize.small));
                }
            }

            public bool done { get; private set; } = false;

            public void SetLine(TLine line)
            {
                currentLine = line;
                currentSpans = currentLine.GetLine();
                progress = 0;
                done = false;

                layout.Clear();
                foreach (Span span in currentSpans)
                {
                    foreach (char symbol in span.contents)
                    {
                        if (symbol == ' ')
                        {
                            layout.Add(null);
                        }
                        else if (symbol == '\n')
                        {
                            layout.Add(-1);
                        }
                        else
                        {
                            int width = Raylib.ImageText(symbol.ToString(), fontSize.Get(span.GetValue(Span.Markup.SIZE)), Color.BLACK).width;

                            layout.Add(width);
                        }
                    }
                }
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
                if (currentLine == null) return;

                if (!done)
                {
                    if (progress < currentLine.t.Length)
                    {
                        progress++;
                    }
                    else
                    {
                        done = true;
                    }
                }

                int i = 0;

                // This is, for the time being, the backdrop of the dialogue.
                Raylib.DrawRectangle((int)textOrigin.X - border - margin,
                                     (int)textOrigin.Y - border - margin,
                                     (int)textArea.X + (border + margin) * 2,
                                     (int)textArea.Y + (border + margin) * 2,
                                     Color.SKYBLUE);
                Raylib.DrawRectangle((int)textOrigin.X - margin,
                                     (int)textOrigin.Y - margin,
                                     (int)textArea.X + margin * 2,
                                     (int)textArea.Y + margin * 2,
                                     Color.BLUE);

                SpanCollection spans = currentLine.GetLine();
                foreach (Span span in spans)
                {
                    Span.MarkValue mSize = span.GetValue(Span.Markup.SIZE);
                    Color currentColor = new Color(span.red, span.green, span.blue, span.alpha);

                    int currentFontSize = fontSize.Get(mSize);
                    int currentBaseOffset = symbolBase.medium - symbolBase.Get(mSize);

                    if (mSize == Span.MarkValue.BIG)
                    {
                        currentFontSize = fontSize.big;
                    }
                    else if (mSize == Span.MarkValue.SMALL)
                    {
                        currentFontSize = fontSize.small;
                    }

                    if (i > progress) break;
                    foreach (char symbol in span.contents)
                    {
                        if (i > progress) break;
                        if (symbol == ' ' || symbol == '\n')
                        {
                            i++;
                            continue;
                        }

                        Vector2 position = layout[i] * (new Vector2(1, lineHeight)) + textOrigin;

                        // This is to help check that the symbols are spaced properly.
                        // int width = Raylib.ImageText(symbol.ToString(), currentFontSize, Color.BLACK).width;
                        // Raylib.DrawRectangle((int)position.X, (int)position.Y + currentBaseOffset, width, currentFontSize, Color.RED);

                        Raylib.DrawText(symbol.ToString(),
                                        (int)position.X,
                                        (int)position.Y + currentBaseOffset,
                                        currentFontSize,
                                        currentColor);


                        i++;
                    }
                }
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

            public TextBox(TLine text)
            {
                // Unfinished.
            }
        }
    }

}