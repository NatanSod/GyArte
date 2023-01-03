using System.Numerics;
using Raylib_cs;
using System.Text.Json;

namespace GyArte
{
    class SpriteSheet
    {
        public string Name { get; private set; }
        Texture2D sheet;
        Animation[] animations;

        public SpriteSheet(string sheetName)
        {
            Name = sheetName;
            sheet = Raylib.LoadTexture($"Assets/Sprites/S_{Name}.png");

            string json = File.ReadAllText($"Assets/Sprites/S_{Name}.png.json");
            MetaAnimation[] metaData = JsonSerializer.Deserialize<MetaAnimation[]>(json, Hivemind.Mastermind.jsonOptions) ?? throw new Exception($"S_{Name}.png.json broke.");
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
            throw new Exception($"That is not the name of an animation in the {Name} sprite-sheet");
        }

        // Kill all the animations.
        public void Kill()
        {
            foreach (Animation animation in animations)
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

        public int Speed { get; private set; } // How many frames it takes to move to the next frame of the animation.

        public int Facings { get => frames.Length; }
        public int Frames { get => frames[0].Length; }

        private RenderTexture2D[][] frames;

        public Animation(MetaAnimation meta, Texture2D sheet)
        {
            Name = meta.Name;
            Height = meta.Height;
            Width = meta.Width;
            OriginX = meta.OriginX;
            OriginY = meta.OriginY;
            Speed = meta.Speed;

            frames = new RenderTexture2D[meta.Facings][];
            for (int dir = 0; dir < meta.Facings; dir++)
            {
                frames[dir] = new RenderTexture2D[meta.Frames];
                for (int frm = 0; frm < meta.Frames; frm++)
                {
                    frames[dir][frm] = Raylib.LoadRenderTexture(meta.Width, meta.Height);
                    Raylib.BeginTextureMode(frames[dir][frm]);
                    Raylib.ClearBackground(Color.BLANK);
                    Raylib.DrawTexturePro(sheet, new Rectangle(meta.StartX + frm * Width, meta.StartY + dir * Height, Width, -Height), 
                                          new Rectangle(0, 0, Width, Height), Vector2.Zero, 0, Color.WHITE);
                    Raylib.EndTextureMode();
                }
            }
        }

        public void Draw(int direction, int frame, int x, int y)
        {
            Raylib.DrawTexture(frames[direction][(frame / Speed)].texture, x - OriginX, y - OriginY, Color.WHITE);
        }

        public Texture2D GetTexture(int direction, int frame)
        {
            return frames[direction][frame].texture;
        }

        // Unload everything.
        public void Kill()
        {
            for (int dir = 0; dir < frames.Length; dir++)
            {
                for (int frm = 0; frm < frames[dir].Length; frm++)
                {
                    Raylib.UnloadRenderTexture(frames[dir][frm]);
                }
            }
        }
    }
    class MetaAnimation
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
        public int Speed { get; set; } // How many frames it takes to move to the next frame of the animation.
        public int StartX { get; set; } // X and Y start
        public int StartY { get; set; }
    }
}