using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Raylib_cs;
using TalkBox;
using OldBadGameMaster;

namespace GyArte
{
    /// <summary>
    /// A class for storing things used for testing. Most of it is old and unused.
    /// </summary>
    static class Test
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

        public static void ConsoleDialogue(string dialogue = "testDi")
        {
            /* I made a change that breaks this and I'm not fixing it, but I'm also not removing it.
            CommandManager cm = new CommandManager();
            DialogueRunner dr = new DialogueRunner(cm, dialogue);
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
                    int speed = (int)(80 - (tags.GetTagValue(Tag.SPEED) - 1) * 30);

                    if (line.s != null)
                    {
                        TypeWrite(line.s + ": ", speed);
                    }

                    SpanCollection sc = line.GetLine();
                    foreach (Span span in sc)
                    {
                        if (span.GetValue(Span.Markup.COLOR) == Span.MarkValue.RED)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                        }
                        TypeWrite(span.contents, speed);
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    Console.WriteLine();

                    if (!tags.GetTag(Tag.LAST))
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
            */
        }
    }
}