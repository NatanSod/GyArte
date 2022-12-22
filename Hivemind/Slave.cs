using System.Numerics;
using GyArte;

namespace Hivemind
{
    // The NPC and prop class.
    class Slave : TalkBox.ISearchable
    {
        public string Name { get; private set; }
        public Vector2 Position { get; private set; }
        public int Facing { get; private set; }

        public int Width { get; private set; }
        public int Length { get; private set; }

        public bool Solid { get; private set; }

        public SpriteSheet SpriteSheet { get; private set; }
        Animation stand;
        public Interaction? Interaction { get; private set; }

        public Slave(MetaSlave meta)
        {
            Name = meta.Name;
            Position = new Vector2(meta.X, meta.Y);
            Facing = meta.Facing;

            Width = meta.Width;
            Length = meta.Length;

            Solid = meta.Solid;

            SpriteSheet = Mastermind.LoadSheet(meta.SpriteSheet);
            stand = SpriteSheet.GetAnimation("stand");

            if (meta.Interaction != null)
            {
                Interaction = new Interaction(meta.Interaction);
            }
        }

        public void Draw()
        {
            Render.DrawAt(Render.Layer.MID_GROUND, (int)Position.Y);
            int height = 36;
            Raylib_cs.Raylib.DrawRectangle((int)(Position.X - Mastermind.Eyes.X) - (Width >> 1), (int)(Position.Y - Mastermind.Eyes.Y) - height + (Length >> 1), Width, height, Raylib_cs.Color.LIGHTGRAY);
            Raylib_cs.Raylib.DrawRectangle((int)(Position.X - Mastermind.Eyes.X) - (Width >> 1), (int)(Position.Y - Mastermind.Eyes.Y) - height - (Length >> 1), Width, Length, Raylib_cs.Color.GRAY);

            stand.Draw(Facing, 0, (int)(Position.X - Mastermind.Eyes.X), (int)(Position.Y - Mastermind.Eyes.Y)); // I will not support an NPC with different animations until I have made one.
            Render.DoneDraw();
        }

        public void Face(Vector2 target)
        {
            Facing = DirectionIndexFromVector(target - Position);
        }

        public static int DirectionIndexFromVector(Vector2 vector)
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
            throw new ArgumentException("That vector does not include a direction.");
        }
    }

    class MetaSlave
    {
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Facing { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public bool Solid { get; set; }
        public string SpriteSheet { get; set; } = "";
        public MetaInteraction? Interaction { get; set; }
    }

    class Interaction
    {
        public enum iType
        {
            TALK,
            DOOR,
        }
        public iType Type { get; private set; }
        public string Extra { get; private set; }

        public Interaction(MetaInteraction meta)
        {
            Extra = meta.Extra;
            Type = meta.Type;
        }
    }

    class MetaInteraction
    {
        public Interaction.iType Type { get; set; }
        public string Extra { get; set; } = "";
    }
}