using System.Numerics;
using Raylib_cs;
using System.Text.Json;

namespace GyArte
{
    class TileSet
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        Texture2D sheet;
        Tile[] tiles;

        static Tile empty;
        /// <summary>
        /// A solid tile with no texture for when the tile id is null.
        /// Used in walls to not block the background, used in backgrounds to be the void.
        /// </summary>
        public static Tile Empty { get => empty; }

        static TileSet()
        {
            empty = new Tile(Raylib.LoadRenderTexture(4, 4), new MetaTile() { Solid = true });
            Raylib.BeginTextureMode(empty.Render);
            Raylib.ClearBackground(Color.BLACK);
            Raylib.EndTextureMode();
        }

        public TileSet(string name)
        {
            sheet = Raylib.LoadTexture($"Assets/TileSets/T_{name}.png");

            string json = File.ReadAllText($"Assets/TileSets/T_{name}.png.json");
            MetaTileSet metaData = JsonSerializer.Deserialize<MetaTileSet>(json, Hivemind.Mastermind.jsonOptions) ?? throw new Exception($"T_{name}.png.json broke.");

            Width = metaData.Width;
            Height = metaData.Height;
            TileWidth = metaData.TileWidth;
            TileHeight = metaData.TileHeight;

            tiles = new Tile[metaData.TileData.Length];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    if (index >= tiles.Length) break;

                    Tile tile = new Tile(Raylib.LoadRenderTexture(TileWidth, TileHeight), metaData.TileData[index]);
                    tiles[index] = tile;

                    Raylib.BeginTextureMode(tile.Render);
                    Raylib.DrawTexturePro(sheet, new Rectangle(x * TileWidth, y * TileHeight, TileWidth, -TileHeight),
                                          new Rectangle(0, 0, TileWidth, TileHeight), Vector2.Zero, 0, Color.WHITE);
                    Raylib.EndTextureMode();
                }
            }
        }

        public Tile GetTile(int id)
        {
            if (id < 0 || id >= tiles.Length)
            {
                return Empty;
            }
            return tiles[id];
        }

        /// <summary>
        /// Draws the entire layout for the purpose of being captured with TextureMode. 
        /// Also returns an array of the layout in tile form.
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Tile[,] Print(int[][] layout, int width, int height)
        {
            // For all arrays here, it's y then x.
            Tile[,] result = new Tile[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // If there isn't enough layout data then the remaining tiles are empty.
                    // If it's less than 0 then it's the empty tile.
                    if (layout.Length <= y || layout[y].Length <= x || layout[y][x] < 0 || layout[y][x] >= tiles.Length)
                    {
                        result[y, x] = Empty;
                        continue;
                    }

                    Tile tile = tiles[layout[y][x]];
                    Raylib.DrawTexturePro(tile.Render.texture, new Rectangle(0, 0, TileWidth, -TileHeight),
                                          new Rectangle(x * TileWidth, (height - y - 1) * TileHeight, TileWidth, TileHeight), Vector2.Zero, 0, Color.WHITE);
                    result[y, x] = tile;
                }
            }
            return result;
        }

        public void Kill()
        {
            Raylib.UnloadTexture(sheet);
            foreach (Tile tile in tiles)
            {
                Raylib.UnloadRenderTexture(tile.Render);
            }
        }
    }

    class MetaTileSet
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public MetaTile[] TileData { get; set; } = new MetaTile[0];
    }

    class Tile
    {
        public RenderTexture2D Render { get; private set; }
        public bool Solid { get; private set; }

        public Tile(RenderTexture2D render, MetaTile meta)
        {
            Render = render;
            Solid = meta.Solid;
        }
    }

    class MetaTile
    {
        public bool Solid { get; set; }
    }
}