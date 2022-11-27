using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TalkBox;

namespace GyArte
{
    struct Scale
    {
        public int Big { get; private set; }
        public int Medium { get; private set; }
        public int Small { get; private set; }

        public Scale(int big, int medium, int small)
        {
            this.Big = big;
            this.Medium = medium;
            this.Small = small;
        }

        /// <summary>
        /// Get text size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public int Get(Span.MarkValue size)
        {
            if (size == Span.MarkValue.BIG)
                return Big;
            else if (size == Span.MarkValue.SMALL)
                return Small;
            else
                return Medium;
        }

        /// <summary>
        /// Get how many symbols will be displayed in 60 frames.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public float Get(Tag tag)
        {
            switch ((tag & Tag.Tags.SPEED | Tag.Tags.METAMASK) ^ Tag.Tags.METAMASK)
            {
                case Tag.Tags.FAST:
                    return Big / 60f;
                default:
                case Tag.Tags.NORM:
                    return Medium / 60f;
                case Tag.Tags.SLOW:
                    return Small / 60f;
            }
        }
    }

    /// <summary>
    /// Used by <see cref="DialogueDisplay"/> to store information about text areas and display the background.
    /// </summary>
    interface ITextBox
    {
        /// <summary>
        /// The origin of the text box. Used by <see cref="DialogueDisplay"/> as the top left point of the first letter in the text box.
        /// </summary>
        public Vector2 Origin { get; }
        /// <summary>
        /// The area of the text box. Used by <see cref="DialogueDisplay"/>
        /// </summary>
        public Vector2 Area { get; }
        /// <summary>
        /// The distance from the "inside" of the text box to the border.
        /// </summary>
        public int Margin { get; }
        /// <summary>
        /// The radius of the border.
        /// </summary>
        public int Border { get; }
        /// <summary>
        /// The font.
        /// </summary>
        public Font TextFont { get; }
        /// <summary>
        /// The width of a space, can be defined by <see cref="GetSpaceWidth(Font, int)"/>
        /// </summary>
        public int SpaceWidth { get; }
        /// <summary>
        /// The distance from the bottom of one line to the top of the next.
        /// </summary>
        public int LineSpacing { get; }
        /// <summary>
        /// The distance from the bottom of one layout to the top of hte next.
        /// </summary>
        public int LayoutSpacing { get; }
        /// <summary>
        /// The distance between two symbols.
        /// </summary>
        public int LetterSpacing { get; }
        /// <summary>
        /// The 3 different font sizes.
        /// </summary>
        public Scale FontSize { get; }
        /// <summary>
        /// The y coordinate of the letter that would rest
        /// </summary>
        public Scale SymbolBase { get; }

        /// <summary>
        /// Display the box.
        /// </summary>
        public void Draw();
        /// <summary>
        /// Display the box while fitting as tightly as possible around the layout.
        /// </summary>
        public void Draw(TextLayout layout);
        /// <summary>
        /// Display the box while fitting as tightly as possible around the layouts.
        /// </summary>
        public int[] Draw(List<TextLayout> layouts, int? highlight = null);

        static public Scale GetSymbolBase(Font font, Scale fontSize)
        {
            unsafe
            {
                // returns the 0 base distance from the top of the bottom most part of the symbol 'I'.
                int GetBase(int size)
                {
                    Image iImage = Raylib.ImageTextEx(font, "I", size, 0, Color.BLACK);
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
                return new Scale(GetBase(fontSize.Big), GetBase(fontSize.Medium), GetBase(fontSize.Small));
            }
        }

        static public int GetSpaceWidth(Font font, int fontSize)
        {
            return Raylib.ImageTextEx(font, " ", fontSize, 0, Color.BLACK).width;
        }
    }

    class DebugTextBox : ITextBox
    {
        public Vector2 Origin { get; private set; }
        public Vector2 Area { get; private set; }
        public int Margin { get; private set; }
        public int Border { get; private set; }
        public Font TextFont { get; private set; }
        public int SpaceWidth { get; private set; }
        public int LineSpacing { get; private set; }
        public int LayoutSpacing { get; private set; }
        public int LetterSpacing { get; private set; }
        public Scale FontSize { get; private set; }
        public Scale SymbolBase { get; private set; }

        public DebugTextBox(Vector2 origin, Vector2 area, int margin, int border, Font textFont, Scale fontSize, int letterSpacing, int lineSpacing, int layoutSpacing)
        {
            Origin = origin;
            Area = area;
            Margin = margin;
            Border = border;
            TextFont = textFont;
            FontSize = fontSize;
            LineSpacing = lineSpacing;
            LayoutSpacing = layoutSpacing;
            LetterSpacing = letterSpacing;

            SpaceWidth = ITextBox.GetSpaceWidth(TextFont, FontSize.Medium);
            SymbolBase = ITextBox.GetSymbolBase(TextFont, FontSize);
        }

        public void Draw()
        {
            Raylib.DrawRectangle((int)Origin.X - Border - Margin,
                                 (int)Origin.Y - Border - Margin,
                                 (int)Area.X + (Border + Margin) * 2,
                                 (int)Area.Y + (Border + Margin) * 2,
                                 Color.SKYBLUE);
            Raylib.DrawRectangle((int)Origin.X - Margin,
                                 (int)Origin.Y - Margin,
                                 (int)Area.X + Margin * 2,
                                 (int)Area.Y + Margin * 2,
                                 Color.BLUE);
        }

        public void Draw(TextLayout layout)
        {
            Raylib.DrawRectangle((int)Origin.X - Border - Margin,
                                 (int)Origin.Y - Border - Margin,
                                 (int)layout.Width + (Border + Margin) * 2,
                                 (int)layout.Height - LineSpacing + (Border + Margin) * 2,
                                 Color.SKYBLUE);
            Raylib.DrawRectangle((int)Origin.X - Margin,
                                 (int)Origin.Y - Margin,
                                 (int)layout.Width + Margin * 2,
                                 (int)layout.Height - LineSpacing + Margin * 2,
                                 Color.BLUE);
        }

        public int[] Draw(List<TextLayout> layouts, int? highlight = null)
        {
            int[] origins = new int[layouts.Count];
            int width = 0;
            int height = 0;

            for (int i = 0; i < origins.Length; i++)
            {
                origins[i] = height;
                int layoutWidth = layouts[i].Width;
                width = width < layoutWidth ? layoutWidth : width;
                height += layouts[i].Height - LineSpacing + LayoutSpacing;
            }
            height -= LayoutSpacing;

            Raylib.DrawRectangle((int)Origin.X - Border - Margin,
                                 (int)Origin.Y - Border - Margin,
                                 (int)width + (Border + Margin) * 2,
                                 (int)height + (Border + Margin) * 2,
                                 Color.SKYBLUE);
            Raylib.DrawRectangle((int)Origin.X - Margin,
                                 (int)Origin.Y - Margin,
                                 (int)width + Margin * 2,
                                 (int)height + Margin * 2,
                                 Color.BLUE);

            if (highlight != null)
            {
                Raylib.DrawRectangle((int)Origin.X,
                                     (int)Origin.Y + origins[(int)highlight],
                                     (int)width,
                                     (int)layouts[(int)highlight].Height - LineSpacing,
                                     Color.WHITE);
            }

            for (int i = 1; i < origins.Length; i++)
            {
                int linePos = (int)Origin.Y + origins[i] - (LayoutSpacing >> 1);
                Raylib.DrawLine((int)Origin.X, linePos, (int)Origin.X + width, linePos, Color.DARKBLUE);
            }
            return origins;
        }
    }
}