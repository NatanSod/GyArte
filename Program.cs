using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Raylib_cs;
using TalkBox;
using Hivemind;
using System.Numerics;
using System.Text.Json;

namespace GyArte
{
    static class Program
    {
        static void Main(string[] args)
        {
            Render.Initialise(352, 270, 2, false);
            Mastermind.Awaken();
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Render.BeginFrame();

                Render.BeginDraw(Render.Layer.DEBUG, 0);
                Raylib.DrawText(Mastermind.cycles.ToString(), 50, 30, 10, Color.BLACK);
                Raylib.DrawFPS(50, 40);
                Render.EndDraw();


                Mastermind.Contemplate();
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_C))
                {
                    Mastermind.ConstructHive("Test2", 0);
                }


                Render.EndFrame();
            }
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