using System.Numerics;
using Raylib_cs;
using System.Text.Json;

namespace GyArte
{
    class TileSet
    {
        public Tile this[int i]
        {
            get => new Tile();
        }

        public TileSet(string name)
        {

        }

        public void Kill()
        {
            
        }
    }

    class MetaTileSet
    {

    }

    class Tile
    {
        public int XIndex { get; set; }
        public int YIndex { get; set; }
    }
}