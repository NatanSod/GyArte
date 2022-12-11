using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TalkBox;
using System.Text.Json;

namespace GyArte
{
    class SpriteSheet
    {
        static JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        string name;
        Texture2D sheet;
        Animation[] animations;

        public SpriteSheet(string sheetName)
        {
            name = sheetName;
            sheet = Raylib.LoadTexture($"{name}.png");

            string json = File.ReadAllText($"{name}.png.json");
            AnimationMeta[] metaData = JsonSerializer.Deserialize<AnimationMeta[]>(json, jsonOptions) ?? throw new Exception($"{name}.png.json broke.");
            animations = new Animation[metaData.Length];

            for (int i = 0; i < metaData.Length; i++)
            {
                animations[i] = new Animation(metaData[i], sheet);
            }
        }

        public Animation GetAnimation(string animationName)
        {
            foreach (Animation animation in animations)
            {
                if (animation.Name == animationName)
                {
                    return animation;
                }
            }
            throw new Exception($"That is not the name of an animation in the {name} sprite-sheet");
        }

        // Kill all the animations.
        public void Kill()
        {
            foreach(Animation animation in animations)
            {
                animation.Kill();
            }
        }
    }

    class Animation
    {
        public string Name { get; private set; } = ""; // The name of the animation.

        public int Width { get; private set; } // Width and height of each sprite in pixels.
        public int Height { get; private set; }

        public int OriginX { get; private set; } // X and Y origin
        public int OriginY { get; private set; }

        private RenderTexture2D[,] frames;

        public Animation(AnimationMeta meta, Texture2D sheet)
        {
            Height = meta.Height;
            Width = meta.Width;
            OriginX = meta.OriginX;
            OriginY = meta.OriginY;

            frames = new RenderTexture2D[meta.Facings, meta.Frames];
            for (int dir = 0; dir < meta.Facings; dir++)
            {
                for (int frm = 0; frm < meta.Frames; frm++)
                {
                    frames[dir, frm] = Raylib.LoadRenderTexture(meta.Width, meta.Height);
                    Raylib.BeginTextureMode(frames[dir, frm]);
                    Raylib.ClearBackground(Color.BLANK);
                    Raylib.DrawTexturePro(sheet, new Rectangle(OriginX, OriginY, Width, -Height), new Rectangle(0, 0, Width, Height), Vector2.Zero, 0, Color.WHITE);
                    Raylib.EndTextureMode();
                }
            }
        }

        public void Draw(int direction, int frame, int x, int y)
        {
            Raylib.DrawTexture(frames[direction, frame].texture, x - OriginX, y - OriginY, Color.WHITE);
        }

        public Texture2D GetTexture(int direction, int frame)
        {
            return frames[direction, frame].texture;
        }

        // Unload everything.
        public void Kill()
        {
            int frmNr = frames.GetLength(0);
            for (int dir = 0; dir < frames.Length; dir++)
            {
                for (int frm = 0; frm < frmNr; frm++)
                {
                    Raylib.UnloadRenderTexture(frames[dir, frm]);
                }
            }
        }
    }
    class AnimationMeta
    {
        // In the sprite sheet, it assumes the sprites for animations are organised like this.
        //         frames ->
        //   +--+--+--+--+--+--+
        // f |  |  |  |  |  |  |
        // a |  |  |  |  |  |  |
        // c +--+--+--+--+--+--+
        // i |  |  |  |  |  |  |
        // n |  |  |  |  |  |  |
        // g +--+--+--+--+--+--+
        //   |  |  |  |  |  |  |
        // | |  |  |  |  |  |  |
        // V +--+--+--+--+--+--+

        public string Name { get; set; } = ""; // The name of the animation.
        public int Frames { get; set; } // The number of frames in one animation.
        public int Facings { get; set; } // The amount of different directions it's made to face
        public int Width { get; set; } // Width and height of each sprite in pixels.
        public int Height { get; set; }
        public int OriginX { get; set; } // X and Y origin
        public int OriginY { get; set; }
        public int StartX { get; set; } // X and Y start
        public int StartY { get; set; }
    }
}