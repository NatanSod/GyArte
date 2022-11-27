using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Raylib_cs;
using TalkBox;
using GameMaster;

namespace GyArte
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_NONE);
            Raylib.InitWindow(800, 600, "Game");
            Master gm = new Master();
            // gm.Create<Player>("Player");
            gm.Create<DialogueHandler>("DialogueHandler")()?.BeginDialogue("testDi");

            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();

                Raylib.ClearBackground(Color.WHITE);

                gm.Update();

                Raylib.DrawRectangle(-25, -25, 50, 50, Color.RED);
                Raylib.DrawRectangle(800 -25, -25, 50, 50, Color.RED);
                Raylib.DrawRectangle(-25, 600 -25, 50, 50, Color.RED);
                Raylib.DrawRectangle(800 -25, 600 -25, 50, 50, Color.BLUE);
                Raylib.DrawText(Master.updates.ToString(), 50, 30, 20, Color.BLACK);

                Raylib.EndDrawing();
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