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
            Raylib.InitWindow(800, 600, "The title of my window");
            Master gm = new Master();
            gm.Create<Player>("Player");
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();

                Raylib.ClearBackground(Color.WHITE);

                gm.Update();

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