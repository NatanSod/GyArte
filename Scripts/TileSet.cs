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

        public TileSet(string name)
        {
            sheet = Raylib.LoadTexture($"Assets/TileSets/T_{name}.png");

            string json = File.ReadAllText($"Assets/TileSets/T_{name}.png.json");
            MetaTileSet metaData = JsonSerializer.Deserialize<MetaTileSet>(json, Hivemind.Mastermind.jsonOptions) ?? throw new Exception($"T_{name}.png.json broke.");

            Width = metaData.Width;
            Height = metaData.Height;
            TileWidth = metaData.TileWidth;
            TileHeight = metaData.TileHeight;

            tiles = new Tile[Height * Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    Tile tile = new Tile(Raylib.LoadRenderTexture(TileWidth, TileHeight));
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
            if (id >= tiles.Length)
            {
                
            }
            return tiles[id];
        }

        public void Print(int[] layout, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int id = layout[y * width + x];
                    Tile tile = tiles[id];
                    Raylib.DrawTexturePro(tile.Render.texture, new Rectangle(0, 0, TileWidth, -TileHeight), 
                                          new Rectangle(x * TileWidth, (height - y - 1) * TileHeight, TileWidth, TileHeight), Vector2.Zero, 0, Color.WHITE);
                }
            }
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
    }

    class Tile
    {
        public RenderTexture2D Render { get; private set; }

        public Tile(RenderTexture2D render)
        {
            Render = render;
        }
    }
}