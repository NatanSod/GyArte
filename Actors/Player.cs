using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using GameMaster;

namespace GyArte
{
    // TODO: Make the player character unresponsive when dialogue is happening.
    class Player : Actor
    {
        enum State
        {
            STAND,
            WALK,
        }

        float speed = 5;
        Trail trail = new Trail(() => Vector3.Zero, (() => 0), 0);

        protected override void Start()
        {

        }
        public Player()
        {
            position = Vector3.Zero;
            velocity = Vector3.Zero;
            trail = new Trail(() => position, (() => currentTime), 25 * 4 / speed);
        }

        float currentTime = 0; // It starts at -1 so that the first time it's checked, right after it's been incremented, it's equal to 0.
        Vector3 previousDirection = Vector3.Zero; // The direction the character moved in last frame.

        protected override void Update()
        {
            // Figure out where to move to.
            velocity = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) { velocity -= Vector3.UnitY * speed; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) { velocity += Vector3.UnitY * speed; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) { velocity -= Vector3.UnitX * speed; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) { velocity += Vector3.UnitX * speed; }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
            {
                position = new Vector3(Render.Width / 2, Render.Height / 2, 0);
            }

            if (velocity.Length() != 0)
            {
                velocity = Vector3.Normalize(velocity);
                velocity *= speed;
                // If they changed directions, add it to the list of changed direction positions.
                if (velocity != previousDirection)
                {
                    trail.MakeKey();
                }

                currentTime++;
                previousDirection = velocity;
            }
        }

        public override void Draw()
        {
            Render.DrawAt(Render.Layer.DEBUG, 0);

            Vector3 previous = trail[0].Pos;
            for (int i = 1; i < trail.Count; i++)
            {
                Vector3 pos = trail[i].Pos;
                Raylib.DrawLine((int)previous.X, (int)previous.Y, (int)pos.X, (int)pos.Y, Color.LIME);
                Raylib.DrawRectangle((int)previous.X - 2, (int)previous.Y - 2, 4, 4, Color.LIME);
                previous = pos;
            }
            Raylib.DrawRectangle((int)previous.X - 2, (int)previous.Y - 2, 4, 4, Color.LIME);
            Render.DoneDraw();

            float thirdLength = trail.Lifespan / 3;
            // Draw them from the last one to the first one so that the first one is drawn on top.
            for (int i = 3; i >= 0; i--)
            {
                // Draw each character in a different colour.
                Color color;
                switch (i)
                {
                    case 0:
                        color = Color.RED;
                        break;
                    case 1:
                        color = Color.GREEN;
                        break;
                    case 2:
                        color = Color.BLUE;
                        break;
                    default:
                        color = Color.YELLOW;
                        break;
                }
                Vector3 pos = trail.GetPositionAt(i * thirdLength);
                Render.DrawAt(Render.Layer.MID_GROUND, (int)pos.Y);
                Raylib.DrawRectangle((int)pos.X - 15, (int)pos.Y - 15, 30, 30, color);
                Raylib.DrawText(DirectionIndexFromVector(trail.GetDirectionAt(i * thirdLength)).ToString(), (int)pos.X - 15, (int)pos.Y - 15, 30, Color.BLACK);
                Render.DoneDraw();
            }
        }

        // Facing direction charts.
        // Vector:
        //         y=-1
        //    x=-1  0  x=1
        //         y=1
        // Int:
        //    +2   +3   +4
        //      7   8   9
        //        \ | /
        //   -1 4 - 5 - 6 +1
        //        / | \
        //      1   2   3
        //   -4    -3   -2
        // (These are 2d only.)
        int DirectionIndexFromVector(Vector3 vector)
        {
            int direction = 5;
            float tolerance = - MathF.Sin(60);

            vector = Vector3.Normalize(new Vector3(vector.X, vector.Y, 0));

            if (MathF.Abs(vector.X) >= tolerance)
            {
                direction += vector.X > 0 ? 1 : -1;
            }
            if (MathF.Abs(vector.Y) >= tolerance)
            {
                direction += vector.Y > 0 ? -3 : 3;
            }

            return direction;
        }
    }
}