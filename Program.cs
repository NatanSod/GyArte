using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Raylib_cs;
using TalkBox;

namespace GyArte
{
    internal class Program
    {
        public static void TypeWriteLine(string text, int sleep)
        {
            foreach (char c in text)
            {
                Console.Write(c);
                System.Threading.Thread.Sleep(sleep);
            }
            Console.WriteLine();
        }
        public static void TypeWrite(string text, int sleep)
        {
            foreach (char c in text)
            {
                Console.Write(c);
                System.Threading.Thread.Sleep(sleep);
            }
        }

        static void Main(string[] args)
        {
            // This is here for testing

            // bool aa = true;
            // if (aa) return;
            CommandManager cm = new CommandManager();
            DialogueRunner dr = new DialogueRunner(cm, "testDi");
            IEnumerator<TLineCollection?> LineGetter = dr.GetLineE();

            while (LineGetter.MoveNext())
            {
                TLineCollection? tlc = LineGetter.Current;
                if (tlc == null)
                {
                    // I don't know, just do something with it.
                    continue;
                }

                if (tlc.lineType == Line.LineType.Dialogue)
                {
                    // Console.Clear();
                    TLine line = tlc.line ?? throw new Exception();
                    Tag tags = line.lineTags;
                    int speed = (int)(80 - (tags.GetTagValue(Tag.Speed) - 1) * 30);

                    if (line.s != null)
                    {
                        TypeWrite(line.s + ": ", speed);
                    }
                    
                    SpanCollection sc = line.GetLine();
                    foreach (Span span in sc)
                    {
                        if (span.GetValue(Span.Markup.Color) == Span.MarkValue.Red)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                        }
                        TypeWrite(span.contents, speed);
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    Console.WriteLine();

                    if (!tags.GetTag(Tag.Last))
                    {
                        Console.ReadKey(true);
                    }
                }
                else
                {
                    int amount = 0;
                    if (tlc.line != null)
                    {
                        TLine line = tlc.line ?? throw new Exception();
                        if (line.s == null)
                        {
                            Console.WriteLine(line.GetLine());
                        }
                        else
                        {
                            Console.WriteLine("{0}: {1}", line.s, line.GetLine());
                        }
                    }

                    foreach (TLine option in tlc)
                    {
                        if (option.s == null)
                        {
                            Console.WriteLine("{0} | {1}", amount, option.GetLine());
                        }
                        else
                        {
                            Console.WriteLine("{0} | {1}: {2}", amount, option.s, option.GetLine());
                        }
                        amount++;
                    }
                    int answer;
                    while (!int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out answer) && answer >= 0 && answer < amount) ;
                    dr.PickOption(answer);
                }
            }

            return;

            // Raylib.InitWindow(800, 600, "The title of my window");
            // Raylib.SetTargetFPS(60);

            // while (!Raylib.WindowShouldClose())
            // {
            //     Raylib.BeginDrawing();

            //     Raylib.ClearBackground(Color.WHITE);

            //     Raylib.DrawCircle(100, 100, 100, Color.MAGENTA);

            //     Raylib.EndDrawing();
            // }
        }
    }
}
// How to IEnumerator
// IEnumerator a()
// {
//     yield return 1;
//     yield return 2;
//     yield return 3;
// }
// var b = a();
// while (b.MoveNext())
// {

// }

// How to IEnumerable
// IEnumerable a()
// {
//     yield return 1;
//     yield return 2;
//     yield return 3;
// }
// var b = a().GetEnumerator();
// while (b.MoveNext())
// {

// }