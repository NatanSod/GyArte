using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TalkBox;

namespace GyArte
{
    /// <summary>
    /// Used by <see cref="DialogueHandler"/>.
    /// </summary>
    class DialogueDisplay
    {
        // TODO: Make options display the subject (TLine.s) of options as well.
        // TODO: Make it display images of the speaker.
        // TODO: Make it display one of those "The line is done, press something to continue" symbols when you can continue. (ITextBox should handle displaying it.)
        // TODO: If I've gone insane, make it able to split lines that are too long for the box into multiple parts.

        TLine? currentLine;
        SpanCollection? currentSpans;
        List<(TLine, bool)> options = new List<(TLine, bool)>();
        List<SpanCollection> optionsSpans = new List<SpanCollection>();

        ITextBox dialogueBox;
        TextLayout dialogueLayout = new TextLayout(20);
        ITextBox nameBox;
        TextLayout nameLayout = new TextLayout(20);
        ITextBox optionBox;
        List<TextLayout> optionLayouts = new List<TextLayout>();

        Scale textSpeed;
        float progress = 0;

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
        public DialogueDisplay(ITextBox dialogueBox, ITextBox nameBox, ITextBox optionBox, Scale textSpeed)
        {
            this.dialogueBox = dialogueBox;
            this.nameBox = nameBox;
            this.optionBox = optionBox;
            this.textSpeed = textSpeed;
        }

        public bool Done { get; private set; } = false;

        public void SetLine(TLine line)
        {
            currentLine = line;
            currentSpans = currentLine.GetLine();
            progress = 0;
            Done = false;

            dialogueLayout = MakeLayout(currentSpans, dialogueBox);
            if (line.s != null)
            {
                nameLayout = MakeLayout(line.s, nameBox);
            }
        }

        public void SkipLine()
        {
            if (currentSpans != null)
            {
                progress = currentSpans.txt.Length;
                Done = true;
            }
        }

        public void EndOptions()
        {
            options.Clear();
            optionsSpans.Clear();
            optionLayouts.Clear();
        }

        public void AddOption(TLine option, bool selectable)
        {
            options.Add((option, selectable));
            optionsSpans.Add(option.GetLine());
            TextLayout layout = MakeLayout(option.GetLine(), dialogueBox);
            if (!selectable)
            {
                // It's locked, make it display the text as if $"- {option.t} -" for added effect.
                layout.Add(null);
                layout.AddToStart(null);
                layout.Add(Raylib.ImageText("-", optionBox.FontSize.Medium, Color.BLACK).width);
                layout.AddToStart(Raylib.ImageText("-", optionBox.FontSize.Medium, Color.BLACK).width);
            }
            optionLayouts.Add(layout);
        }

        public void Display()
        {
            if (currentLine == null || currentSpans == null) return;

            // Raylib.DrawText(currentLine.t.ToString(), 50, 90, 20, Color.BLACK);
            if (!Done)
            {
                if (progress < currentSpans.txt.Length)
                {
                    progress += textSpeed.Get(currentLine.lineTags);
                }
                else
                {
                    Done = true;
                }
            }
            // Raylib.DrawText(progress.ToString(), 50, 60, 20, Color.BLACK);

            int i = 0;

            if (currentLine.s != null)
            {
                Render.BeginDraw(Render.Layer.UI, 1);
                nameBox.Draw(nameLayout);

                if (currentLine.s != null)
                {
                    foreach (char symbol in currentLine.s)
                    {
                        if (symbol == ' ' || symbol == '\n')
                        {
                            i++;
                            continue;
                        }

                        Vector2 position = nameLayout[i] + nameBox.Origin;

                        // This is to help check that the symbols are spaced properly.
                        // int width = Raylib.ImageText(symbol.ToString(), currentFontSize, Color.BLACK).width;
                        // Raylib.DrawRectangle((int)position.X, (int)position.Y + currentBaseOffset, width, currentFontSize, Color.RED);

                        Raylib.DrawText(symbol.ToString(),
                                        (int)position.X,
                                        (int)position.Y,
                                        nameBox.FontSize.Medium,
                                        Color.BLACK);
                        i++;
                    }
                }
                Render.EndDraw();
            }
            
            Render.BeginDraw(Render.Layer.UI, 0);
            dialogueBox.Draw();
            i = 0;

            foreach (Span span in currentSpans)
            {
                Span.MarkValue mSize = span.GetValue(Span.Markup.SIZE);
                Color currentColor = new Color(span.red, span.green, span.blue, span.alpha);

                int currentFontSize = dialogueBox.FontSize.Get(mSize);
                int currentBaseOffset = dialogueBox.SymbolBase.Medium - dialogueBox.SymbolBase.Get(mSize);

                if (mSize == Span.MarkValue.BIG)
                {
                    currentFontSize = dialogueBox.FontSize.Big;
                }
                else if (mSize == Span.MarkValue.SMALL)
                {
                    currentFontSize = dialogueBox.FontSize.Small;
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

                    Vector2 position = dialogueLayout[i] + dialogueBox.Origin;

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
                Render.EndDraw();
            }
        }

        public void Display(int option)
        {
            Display();

            Render.BeginDraw(Render.Layer.UI, 0);
            int[] origins = optionBox.Draw(optionLayouts, option);
            for (int i = 0; i < origins.Length; i++)
            {
                foreach (Span span in optionsSpans[i])
                {
                    Span.MarkValue mSize = span.GetValue(Span.Markup.SIZE);
                    Color currentColor = new Color(span.red, span.green, span.blue, span.alpha);

                    int currentFontSize = optionBox.FontSize.Get(mSize);
                    int currentBaseOffset = optionBox.SymbolBase.Medium - optionBox.SymbolBase.Get(mSize);

                    if (mSize == Span.MarkValue.BIG)
                    {
                        currentFontSize = optionBox.FontSize.Big;
                    }
                    else if (mSize == Span.MarkValue.SMALL)
                    {
                        currentFontSize = optionBox.FontSize.Small;
                    }

                    int index = 0;
                    if (!options[i].Item2)
                    {
                        currentColor = Color.LIGHTGRAY;
                        Raylib.DrawText("-",
                                        (int)(optionLayouts[i][0] + optionBox.Origin).X,
                                        (int)(optionLayouts[i][0] + optionBox.Origin).Y + origins[i] + currentBaseOffset,
                                        currentFontSize,
                                        currentColor);
                        Raylib.DrawText("-",
                                        (int)(optionLayouts[i][optionLayouts[i].Length - 1] + optionBox.Origin).X,
                                        (int)(optionLayouts[i][optionLayouts[i].Length - 1] + optionBox.Origin).Y + origins[i] + currentBaseOffset,
                                        currentFontSize,
                                        currentColor);
                        index = 2;
                    }

                    foreach (char symbol in span.contents)
                    {
                        if (symbol == ' ' || symbol == '\n')
                        {
                            index++;
                            continue;
                        }

                        Vector2 position = optionLayouts[i][index] + optionBox.Origin;

                        // This is to help check that the symbols are spaced properly.
                        // int width = Raylib.ImageText(symbol.ToString(), currentFontSize, Color.BLACK).width;
                        // Raylib.DrawRectangle((int)position.X, (int)position.Y + currentBaseOffset, width, currentFontSize, Color.RED);

                        Raylib.DrawText(symbol.ToString(),
                                        (int)position.X,
                                        (int)position.Y + origins[i] + currentBaseOffset,
                                        currentFontSize,
                                        currentColor);
                        index++;
                    }
                }
            }
            Render.EndDraw();
        }

        public static TextLayout MakeLayout(SpanCollection spanCollection, ITextBox textBox)
        {
            TextLayout layout = new TextLayout((int)textBox.Area.X, textBox.FontSize.Medium + textBox.LineSpacing, textBox.SpaceWidth, textBox.LetterSpacing);
            foreach (Span span in spanCollection)
            {
                int currentSize = textBox.FontSize.Get(span.GetValue(Span.Markup.SIZE));
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
                        int width = Raylib.ImageText(symbol.ToString(), currentSize, Color.BLACK).width;

                        layout.Add(width);
                    }
                }
            }
            return layout;
        }

        public static TextLayout MakeLayout(string text, ITextBox textBox)
        {
            TextLayout layout = new TextLayout((int)textBox.Area.X, textBox.FontSize.Medium + textBox.LineSpacing, textBox.SpaceWidth, textBox.LetterSpacing);
            foreach (char symbol in text)
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
                    int width = Raylib.ImageText(symbol.ToString(), textBox.FontSize.Medium, Color.BLACK).width;

                    layout.Add(width);
                }
            }
            return layout;
        }
    }
}