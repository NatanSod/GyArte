using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Raylib_cs;
using TalkBox;
using GameMaster;
using System.Numerics;

namespace GyArte
{
    static class Program
    {
        static void Main(string[] args)
        {
            Render.Initialise(352, 270, 2);
            Master gm = new Master();
            gm.Create<Player>("Player");
            // gm.Create<DialogueHandler>("DialogueHandler")()?.BeginDialogue("testDi");

            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Render.BeginDrawing();

                Render.DrawAt(Render.Layer.DEBUG, 0);
                Raylib.DrawRectangle(-25, -25, 50, 50, Color.MAGENTA);
                Raylib.DrawRectangle(Render.Width - 25, -25, 50, 50, Color.GREEN);
                Raylib.DrawRectangle(-25, Render.Height - 25, 50, 50, Color.GREEN);
                Raylib.DrawRectangle(Render.Width - 25, Render.Height - 25, 50, 50, Color.YELLOW);
                Raylib.DrawText(Master.updates.ToString(), 50, 30, 10, Color.BLACK);
                Raylib.DrawFPS(50, 40);
                Render.DoneDraw();


                gm.Update();


                Render.EndDrawing();
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