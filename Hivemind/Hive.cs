using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;
using GyArte;

namespace Hivemind
{
    // The class for areas.
    class Hive
    {
        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Length { get; private set; }
        public int ActualWidth { get; private set; }
        public int ActualLength { get; private set; }

        // These are arrays and not lists because I prefer them when the items contained in them never change.
        public Tile[] Layout { get; private set; }
        public Wall[] Walls { get; private set; }
        public Slave[] Slaves { get; private set; }
        public Vector2[][] Entrances { get; private set; }

        RenderTexture2D background;
        TileSet tileSet;
        Player Victim => Mastermind.victim;

        public Hive(string name)
        {
            Name = name;

            string json = File.ReadAllText($"Assets/Hives/H_{Name}.json");
            MetaHive meta = JsonSerializer.Deserialize<MetaHive>(json, Mastermind.jsonOptions) ?? throw new Exception($"{Name}.json broke.");

            Width = meta.Width;
            Length = meta.Length;

            tileSet = new TileSet(meta.TileSet);

            ActualWidth = Width * tileSet.TileWidth;
            ActualLength = Length * tileSet.TileHeight;

            // Get the background and the layout.
            background = Raylib.LoadRenderTexture(Width * tileSet.TileWidth, Length * tileSet.TileHeight);
            Raylib.BeginTextureMode(background);
            Raylib.ClearBackground(Color.BLACK);
            Layout = tileSet.Print(meta.Layout, Width, Length);
            Raylib.EndTextureMode();

            // Then get all the walls.
            Walls = new Wall[meta.Walls.Length];
            for (int i = 0; i < Walls.Length; i++)
            {
                Walls[i] = new Wall(meta.Walls[i], tileSet);
            }

            Slaves = new Slave[meta.Slaves.Length];
            for (int i = 0; i < Slaves.Length; i++)
            {
                // Might remove this once everything is "finished".
                if (meta.Slaves[i].Name != null)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (Slaves[j].Name == meta.Slaves[i].Name)
                        {
                            throw new Exception("If you're going to give the NPC a name, make sure it's a unique one.");
                        }
                    }
                }

                Slaves[i] = new Slave(meta.Slaves[i]);
            }

            Entrances = new Vector2[2][];
            Entrances[0] = new Vector2[meta.Entrances.Length];
            Entrances[1] = new Vector2[meta.Entrances.Length];
            for (int i = 0; i < meta.Entrances.Length; i ++)
            {
                Entrances[0][i] = new Vector2(meta.Entrances[i].X + .5f, meta.Entrances[i].Y + .5f) * new Vector2(tileSet.TileWidth, tileSet.TileHeight);
                Entrances[1][i] = new Vector2(meta.Entrances[i].FaceX, meta.Entrances[i].FaceY);
            }
        }

        public void Deconstruct()
        {
            tileSet.Kill();
            Raylib.UnloadRenderTexture(background);
            foreach (Wall wall in Walls)
            {
                wall.Kill();
            }
        }

        public void Update()
        {
            // Remember to draw the background, foreground, and walls as well.
            Render.BeginDraw(Render.Layer.BACKGROUND, 0);
            Raylib.DrawTexture(background.texture, -(int)Mastermind.Eyes.X, -(int)Mastermind.Eyes.Y, Color.WHITE);
            Render.EndDraw();

            foreach (Wall wall in Walls)
            {
                wall.Draw();
            }

            foreach (Slave slave in Slaves)
            {
                slave.Draw();
            }
        }

        public Slave? HuntSlave(string name)
        {
            foreach (Slave slave in Slaves)
            {
                if (slave.Name == name)
                {
                    return slave;
                }
            }
            return null;
        }

        public Tile GetTile(Vector2 worldPosition)
        {
            int x = (int)MathF.Floor(worldPosition.X / tileSet.TileWidth);
            int y = (int)MathF.Floor(worldPosition.Y / tileSet.TileHeight);
            if (x < 0 || y < 0 || x >= Width || y >= Length)
            {
                return TileSet.Empty;
            }
            return Layout[y * Width + x];
        }

        public Vector2? CheckCollision(Vector2 goal)
        {
            bool collided = false;
            Vector2 start = Victim.Position;
            Vector2 velocity = goal - start;
            Vector2 xGoal = new Vector2(goal.X, start.Y);
            Vector2 yGoal = new Vector2(start.X, goal.Y);

            // First, collide with the tile walls and figure out where you should go from there.
            if (velocity.Y != 0)
            {
                if
                (
                    GetTile(yGoal + new Vector2((Victim.Width / 2 - 1), Victim.Length / 2 * MathF.Sign(velocity.Y))).Solid
                    || GetTile(yGoal + new Vector2(-(Victim.Width / 2 - 1), Victim.Length / 2 * MathF.Sign(velocity.Y))).Solid
                )
                {
                    collided = true;
                    if (velocity.Y > 0)
                    {
                        goal += Vector2.UnitY * -((goal.Y + Victim.Length / 2) % tileSet.TileHeight);
                    }
                    else
                    {
                        goal += Vector2.UnitY * -((goal.Y % tileSet.TileHeight) - Victim.Length / 2);
                    }
                }
            }
            if (velocity.X != 0)
            {
                if
                (
                    GetTile(xGoal + new Vector2(Victim.Width / 2 * MathF.Sign(velocity.X), (Victim.Length / 2 - 1))).Solid
                    || GetTile(xGoal + new Vector2(Victim.Width / 2 * MathF.Sign(velocity.X), -(Victim.Length / 2 - 1))).Solid
                )
                {
                    collided = true;
                    if (velocity.X > 0)
                    {
                        goal += Vector2.UnitX * -((goal.X + Victim.Width / 2) % tileSet.TileWidth);
                    }
                    else
                    {
                        goal += Vector2.UnitX * -((goal.X % tileSet.TileWidth) - Victim.Width / 2);
                    }
                }
            }

            // Then, get each slave that falls within the danger zone.
            Rectangle dangerZone = DefineDanger(goal);
            foreach (Slave slave in Slaves)
            {
                if (!slave.Solid) continue;

                float sXtl, sYtl;
                sXtl = slave.Position.X - slave.Width / 2;
                sYtl = slave.Position.Y - slave.Length / 2;
                Rectangle slaveZone = new Rectangle(sXtl, sYtl, slave.Width, slave.Length);
                Rectangle overlap = Raylib.GetCollisionRec(dangerZone, slaveZone);
                if (overlap.width != 0 && overlap.height != 0)
                {
                    collided = true;
                    if (overlap.height <= overlap.width && velocity.Y != 0
                    || ((MathF.Sign(velocity.X) < 0 && overlap.x != dangerZone.x) || (MathF.Sign(velocity.X) > 0 && overlap.x != slaveZone.x)))
                    {
                        if (overlap.height > MathF.Abs(velocity.Y))
                        {
                            goal = start + new Vector2(velocity.X, 0);
                        }
                        else
                        {
                            goal -= Vector2.UnitY * (overlap.height * MathF.Sign(velocity.Y));
                        }
                    }
                    else
                    {
                        goal += Vector2.UnitX * (overlap.width * -MathF.Sign(velocity.X));
                    }
                    // Update velocity and reshape the dangerZone.
                    velocity = goal - start;
                    dangerZone = DefineDanger(goal);
                }
            }
            return collided ? goal : null;

            Rectangle DefineDanger(Vector2 goal) => new Rectangle(goal.X - Victim.Width / 2, goal.Y - Victim.Length / 2, Victim.Width, Victim.Length);
        }

        public Slave? Interact(int x, int y)
        {
            Rectangle rect1 = new Rectangle(x - Mastermind.victim.Width / 2, y - Mastermind.victim.Length / 2, Mastermind.victim.Width, Mastermind.victim.Length);
            foreach (Slave slave in Slaves)
            {
                if (slave.Interaction == null || !slave.Solid) continue;

                Rectangle rect2 = new Rectangle(slave.Position.X - slave.Width / 2, slave.Position.Y - slave.Length / 2, slave.Width, slave.Length);
                Rectangle collision = Raylib.GetCollisionRec(rect1, rect2);
                if (collision.width != 0 && collision.height != 0)
                {
                    return slave;
                }
            }
            return null;
        }

        public List<Slave> Trigger(int x, int y)
        {
            List<Slave> triggers = new List<Slave>();
            foreach (Slave slave in Slaves)
            {
                if (slave.Interaction == null || slave.Solid) continue;
                float right = slave.Position.X + slave.Width / 2;
                float left = slave.Position.X - slave.Width / 2;
                float up = slave.Position.Y - slave.Length / 2;
                float down = slave.Position.Y + slave.Length / 2;

                if (x < right && x > left && y < down && y > up)
                {
                    triggers.Add(slave);
                }
            }
            return triggers;
        }
    }

    class MetaHive
    {
        public int Width { get; set; }
        public int Length { get; set; }

        public string TileSet { get; set; } = "";
        public int?[] Layout { get; set; } = new int?[0];

        public MetaWall[] Walls { get; set; } = new MetaWall[0];

        public MetaSlave[] Slaves { get; set; } = new MetaSlave[0];
        public Position[] Entrances { get; set; } = new Position[0];
    }

    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int FaceX { get; set; }
        public int FaceY { get; set; }
    }

    class Wall
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        RenderTexture2D render;
        public Wall(MetaWall meta, TileSet tileSet)
        {
            X = meta.X * tileSet.TileWidth;
            Y = meta.Y * tileSet.TileHeight;
            Height = meta.Height * tileSet.TileWidth;
            Width = meta.Width * tileSet.TileHeight;
            // Remember to do a RenderTexture2D to save what it looks like.
            render = Raylib.LoadRenderTexture(meta.Width * tileSet.TileWidth, meta.Height * tileSet.TileHeight);
            Raylib.BeginTextureMode(render);
            tileSet.Print(meta.Layout, meta.Width, meta.Height);
            Raylib.EndTextureMode();
        }

        public void Draw()
        {
            Render.BeginDraw(Render.Layer.MID_GROUND, Y);
            Raylib.DrawTexture(render.texture, X - (int)Mastermind.Eyes.X, Y - Height - (int)Mastermind.Eyes.Y, Color.WHITE);
            // Raylib.DrawPixel(X - (int)Mastermind.Eyes.X, Y - (int)Mastermind.Eyes.Y, Color.BLUE);
            Render.EndDraw();
        }

        public void Kill()
        {
            Raylib.UnloadRenderTexture(render);
        }
    }

    class MetaWall
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int?[] Layout { get; set; } = new int?[0];
    }
}