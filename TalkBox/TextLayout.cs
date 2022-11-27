using System.Numerics;

namespace TalkBox
{
    /// <summary>
    /// A zero base index of the position a symbol should have in order to minimise the amount of line breaks mid word.
    /// Add the width of each symbol before accessing the positions as adding a symbol may change the result.
    /// To represent a space or line break, use null and -1 respectively.
    /// </summary>
    class TextLayout
    {
        public int maxWidth { get; private set; }
        int spaceWidth = 1; // The width of a single space.
        int symbolMargin = 0; // The distance between two symbols.
        int lineHeight = 1; // The y coordinate added for each line.


        List<int?> widths = new List<int?>(); // The width of each letter in this layout.
        List<Vector2> positions = new List<Vector2>(); // The positions of the words.
        List<int> lineWidths = new List<int>(); // The width of each line.
        bool compiled = true;

        public TextLayout(int maxWidth)
        {
            this.maxWidth = maxWidth;
        }
        public TextLayout(int maxWidth, int lineHeight, int spaceWidth)
        {
            this.maxWidth = maxWidth;
            this.lineHeight = lineHeight;
            this.spaceWidth = spaceWidth;
        }
        public TextLayout(int maxWidth, int lineHeight, int spaceWidth, int letterSpacing)
        {
            this.maxWidth = maxWidth;
            this.lineHeight = lineHeight;
            this.spaceWidth = spaceWidth;
            this.symbolMargin = letterSpacing;
        }

        public void Clear()
        {
            widths.Clear();
            positions.Clear();
            lineWidths.Clear();
            compiled = true;
        }

        /// <summary>
        /// Add the width of a letter to the list of widths.
        /// </summary>
        public void Add(int? width)
        {
            widths.Add(width);
            compiled = false;
        }

        public void AddToStart(int? width)
        {
            widths.Insert(0, width);
            compiled = false;
        }

        public Vector2 this[int i]
        {
            get
            {
                Compile();
                if (positions.Count <= i)
                {
                    return -Vector2.One;
                }
                return positions[i];
            }
        }

        public int Length
        {
            get
            {
                Compile();
                return positions.Count;
            }
        }

        /// <summary>
        /// Get the width of the symbol at base zero index <paramref name="i"/>.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int? SymbolWidth(int i)
        {
            if (i < widths.Count)
            {
                return widths[i];
            }
            return -2;
        }

        /// <summary>
        /// Get the width of the line at base zero index <paramref name="i"/>.
        /// </summary>
        public int LineWidth(int i)
        {
            Compile();
            if (i < lineWidths.Count)
            {
                return lineWidths[i];
            }
            return -1;
        }

        /// <summary>
        /// The amount of lines (including empty ones).
        /// </summary>
        public int Lines
        {
            get
            {
                Compile();
                return lineWidths.Count;
            }
        }

        /// <summary>
        /// The width of the whole text box.
        /// </summary>
        public int Width
        {
            get
            {
                Compile();
                int result = 0;
                foreach (int lineWidth in lineWidths)
                {
                    if (result < lineWidth)
                    {
                        result = lineWidth;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// The number of lines (ignoring the trailing empty lines) times the height of a line.
        /// </summary>
        public int Height
        {
            get
            {
                Compile();
                int i = widths.Count;
                while (widths[i - 1] == 0) { i--; }

                return lineWidths.Count * lineHeight;
            }
        }

        // First, add each symbol's width as a multiplier of the width of a space, spaces represented as null and line breaks as -1.
        // Then, call a method that organises all of them into a position.
        private void Compile()
        {
            if (compiled) return; // Don't compile if it already is compiled.
            positions.Clear();
            lineWidths.Clear();

            int column = 0; // The horizontal position of the first letter in the current word.
            int line = 0; // The line that the word is to be added to.
            int width = 0; // The width of the current word.
            int spaces = 0; // Keeps track of the distance that would be created by spaces since the last symbol.
            List<int> currentWord = new List<int>(); // widths of the current letters.

            void NewLine()
            {
                lineWidths.Add(column - (column == 0 ? 0 : symbolMargin));
                spaces = 0; // Discard all potential spaces at the beginning of a new line.
                column = 0;
                line++;
            }

            void CompileCurrentWord()
            {
                if (currentWord.Count == 0) return; // There are no symbols to compile, so don't

                if (maxWidth >= 0 && column != 0 && width + column - symbolMargin > maxWidth)
                {
                    // Begin a new line if the current word is too wide for this line.
                    NewLine();
                }

                // A word with symbols is going to be written. The space of potential spaces will become real.
                column += spaces;
                spaces = 0;

                // Add the values in currentWord as correct relative positions.
                foreach (int currentLetter in currentWord)
                {
                    if (maxWidth != 0 && column + currentLetter - symbolMargin > maxWidth)
                    {
                        // The word is longer than a single line, it needs to continue on the next line.
                        NewLine();
                    }
                    positions.Add(new Vector2(column, line * lineHeight));
                    column += currentLetter;
                }
                currentWord.Clear();
                width = 0;
            }

            foreach (int? w in widths)
            {
                // If w is null, it's a space.
                // If w is -1, it's a line break.
                // If it's neither of those, it's a letter.
                if (w != null && w != -1)
                {
                    width += symbolMargin + w ?? throw new Exception("This CAN'T ever happen, but it makes the IDE happy.");
                    currentWord.Add(symbolMargin + (int)w);
                }
                else
                {
                    // It's a space or a line break.
                    CompileCurrentWord();
                    if (w == -1)
                    {
                        // Line breaks begin a new line.
                        if (column != 0) NewLine();
                    }
                    else if (column != 0)
                    {
                        // Don't add a potential space if this line is empty.
                        spaces += symbolMargin + spaceWidth;
                    }
                    positions.Add(-Vector2.One); // the position of "empty" symbols are always (-1, -1).
                }
            }
            // There are probably some widths left in currentWord, so take care of them.
            CompileCurrentWord();
            if (column != 0)
            {
                lineWidths.Add(column - (column == 0 ? 0 : symbolMargin));
            }

            compiled = true;
        }
    }
}