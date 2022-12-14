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

        public Tile[] Layout { get; private set; }
        public Wall[] Walls { get; private set; }
        public Slave[] Slaves { get; private set; }

        public Hive(string name)
        {
            Name = name;

            string json = File.ReadAllText($"Assets/Hives/H_{Name}.json");
            MetaHive meta = JsonSerializer.Deserialize<MetaHive>(json, Mastermind.jsonOptions) ?? throw new Exception($"{Name}.json broke.");

            Width = meta.Width;
            Length = meta.Length;

            TileSet tileSet = new TileSet(meta.TileSet);

            Layout = new Tile[meta.Layout.Length];
            for (int i = 0; i < Layout.Length; i++)
            {
                Layout[i] = tileSet[i];
            }
            // Then figure out the background and foreground from that.

            Walls = new Wall[meta.Walls.Length];
            for (int i = 0; i < Walls.Length; i++)
            {
                Walls[i] = new Wall(meta.Walls[i], tileSet);
            }

            tileSet.Kill();

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
        }

        public void Update()
        {
            // Remember to draw the background, foreground, and walls as well.
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

        public byte CheckCollision(int x, int y, Player player)
        {
            byte result = 0;
            // Remember to check the collisions with walls.
            Rectangle rect1 = new Rectangle(x - player.Width / 2, y - player.Length / 2, player.Width, player.Length);
            foreach (Slave slave in Slaves)
            {
                if (!slave.Solid) continue;

                Rectangle rect2 = new Rectangle(slave.Position.X - slave.Width / 2, slave.Position.Y - slave.Length / 2, slave.Width, slave.Length);
                Rectangle collision = Raylib.GetCollisionRec(rect1, rect2);
                if (collision.width != 0 && collision.height != 0)
                {
                    // Check which sides are overlapping with something. I don't know how, just do it.
                }
            }
            return result;
        }

        public Slave? Interact(int x, int y, Player player)
        {
            Rectangle rect1 = new Rectangle(x - player.Width / 2, y - player.Length / 2, player.Width, player.Length);
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

        public Slave? Trigger(int x, int y, Player player)
        {
            foreach (Slave slave in Slaves)
            {
                if (slave.Interaction == null || slave.Solid) continue;
                float right = slave.Position.X + slave.Width / 2;
                float left = slave.Position.X - slave.Width / 2;
                float up = slave.Position.Y - slave.Length / 2;
                float down = slave.Position.Y + slave.Length / 2;

                if (x < right && x > left && y < down && y > up)
                {
                    return slave;
                }
            }
            return null;
        }
    }

    class MetaHive
    {
        public int Width { get; set; }
        public int Length { get; set; }

        public string TileSet { get; set; } = "";
        public int[] Layout { get; set; } = new int[0];

        public MetaWall[] Walls { get; set; } = new MetaWall[0];

        public MetaSlave[] Slaves { get; set; } = new MetaSlave[0];
    }

    class Wall
    {
        public Wall(MetaWall meta, TileSet tileSet)
        {
            // Remember to do a RenderTexture2D to save what it looks like.
        }
    }

    class MetaWall
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int[] Layout { get; set; } = new int[0];
    }
}