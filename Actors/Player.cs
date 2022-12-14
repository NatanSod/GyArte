using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using GyArte;

namespace Hivemind
{
    // TODO: Make the player character unresponsive when dialogue is happening.
    class Player : ITrailblazer
    {
        public enum State
        {
            STAND,
            WALK,
            TALK,
        }

        State state = State.STAND;

        public int Width { get; private set; } = 30;
        public int Length { get; private set; } = 22;
        int height = 36;

        public Vector2 Position { get; private set; } = new Vector2(Render.Width / 2, Render.Height / 2);
        public float Time { get; private set; } = 0;
        Vector2 velocity = Vector2.Zero;
        Vector2 facing = Vector2.UnitY;
        float speed = 4;
        Trail trail;
        DialogueHandler dh { get => Mastermind.mouthpiece; }

        public Player()
        {
            spriteSheet = Mastermind.LoadSheet("Test");
            stand = spriteSheet.GetAnimation("stand");
            walk = spriteSheet.GetAnimation("walk");

            trail = new Trail(this, 25 * 4 / speed);
        }

        SpriteSheet spriteSheet;
        Animation stand;
        Animation walk;

        public void Update()
        {
            while (true)
            {
                switch (state)
                {
                    case State.STAND:
                        if (Interact()) continue;
                        Walk();
                        return;
                    case State.WALK:
                        if (Trigger()) continue;
                        if (Interact()) continue;
                        Walk();
                        return;
                    case State.TALK:
                        Talk();
                        return;
                }
                break;
            }
        }

        void Walk()
        {
            // Figure out where to move to.
            if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) { velocity -= Vector2.UnitY; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) { velocity += Vector2.UnitY; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) { velocity -= Vector2.UnitX; }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) { velocity += Vector2.UnitX; }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
            {
                Position = new Vector2(Render.Width / 2, Render.Height / 2);
            }

            if (velocity.Length() != 0)
            {
                // If they changed directions, add it to the list of changed direction positions.
                if (velocity != facing)
                {
                    trail.MakeKey();
                }

                facing = velocity;

                velocity = Vector2.Normalize(velocity) * speed;

                // Put the collision logic here somewhere.
                Position += velocity;

                Time++;
                velocity = Vector2.Zero;
                state = State.WALK;
            }
            else
            {
                state = State.STAND;
            }
        }

        bool Interact()
        {
            // It interacted without me pressing space.
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
            {
                State? s = Mastermind.Interact((int)(Position.X + facing.X * Width), (int)(Position.Y + facing.Y * Length));

                if (s != null)
                {
                    state = s ?? state;
                    return true;
                }
            }
            return false;
        }

        bool Trigger()
        {
            State? s = Mastermind.Trigger((int)(Position.X), (int)(Position.Y));

            if (s != null)
            {
                state = s ?? state;
                return true;
            }
            return false;
        }

        void Talk()
        {
            if (dh.Done)
            {
                state = State.WALK;
            }
        }

        public void Draw()
        {
            Render.DrawAt(Render.Layer.DEBUG, 0);

            Vector2 previous = trail[0].Pos;
            for (int i = 1; i < trail.Count; i++)
            {
                Vector2 pos = trail[i].Pos;
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
                Color dark;
                switch (i)
                {
                    case 0:
                        color = Color.RED;
                        dark = Color.MAROON;
                        break;
                    case 1:
                        color = Color.GREEN;
                        dark = Color.DARKGREEN;
                        break;
                    case 2:
                        color = Color.BLUE;
                        dark = Color.DARKBLUE;
                        break;
                    default:
                        color = Color.YELLOW;
                        dark = Color.ORANGE;
                        break;
                }

                Vector2 pos = trail.GetPositionAt(i * thirdLength);
                Render.DrawAt(Render.Layer.MID_GROUND, (int)pos.Y);

                Raylib.DrawRectangle((int)pos.X - (Width >> 1), (int)pos.Y - height + (Length >> 1), Width, height, color);

                Raylib.DrawRectangle((int)pos.X - (Width >> 1), (int)pos.Y - height - (Length >> 1), Width, Length, dark);

                int direction = DirectionIndexFromVector(trail.GetDirectionAt(i * thirdLength));

                switch (state)
                {
                    case State.WALK:
                        walk.Draw(direction, 0, (int)pos.X, (int)pos.Y);
                        break;
                    case State.STAND:
                        stand.Draw(direction, 0, (int)pos.X, (int)pos.Y);
                        break;
                    case State.TALK:
                        direction = DirectionIndexFromVector(facing);
                        stand.Draw(direction, 0, (int)pos.X, (int)pos.Y);
                        break;
                    default:
                        throw new Exception("Not ready to display that state yet.");
                }
                Render.DoneDraw();
            }
        }

        // Facing direction charts.
        // Vector:
        //         y=-1
        //    x=-1  0  x=1
        //         y=1
        // Int (old):
        //    +2   +3   +4
        //      7   8   9
        //        \ | /
        //   -1 4 - 5 - 6 +1
        //        / | \
        //      1   2   3
        //   -4    -3   -2
        // Int (new):
        //      2   3   0
        //        \ | /
        //      2 - X - 0  (Right now, it prioritises the horizontal moving sprites. I might change that.)
        //        / | \
        //      2   1   0
        int DirectionIndexFromVector(Vector2 vector)
        {
            vector = Vector2.Normalize(new Vector2(vector.X, vector.Y));
            float tolerance = -MathF.Sin(60);
            // int direction = 5;


            if (MathF.Abs(vector.X) >= tolerance)
            {
                return vector.X > 0 ? 0 : 2;
                // direction += vector.X > 0 ? 1 : -1;
            }
            if (MathF.Abs(vector.Y) >= tolerance)
            {
                return vector.Y > 0 ? 1 : 3;
                // direction += vector.Y > 0 ? -3 : 3;
            }

            return 1; // I will make this break properly when I can make a starting position for the trail or similar.
            // throw new ArgumentException("That vector does not include a direction.");
            // return direction;
        }
    }
}