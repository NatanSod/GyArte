using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using GameMaster;

namespace GyArte
{
    class Player : Actor
    {
        enum State
        {
            Stand,
            Walk,
        }

        protected override void Start()
        {
            position = Vector3.Zero;
        }

        protected override void Update()
        {
            velocity = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) { velocity -= Vector3.UnitY * 10; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) { velocity += Vector3.UnitY * 10; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) { velocity -= Vector3.UnitX * 10; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) { velocity += Vector3.UnitX * 10; }
        }

        public override void Draw()
        {
            Raylib.DrawRectangle((int) position.X - 25, (int) position.Y - 25, 50, 50, Color.RED);
        }
    }
}