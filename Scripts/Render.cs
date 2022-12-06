using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TalkBox;

namespace GyArte
{
    /// <summary>
    /// A little class I use to make the display scalable and pixel perfect.
    /// It basically captures everything drawn through Raylib and then only displays what it captured.
    /// </summary>
    public static class Render
    {
        public enum Layer
        {
            NONE = -1,
            BACKGROUND,
            MID_GROUND,
            FOREGROUND,
            UI,
            DEBUG,
        }

        public static int Width { get; private set; } = 0;
        public static int Height { get; private set; } = 0;
        public static int PixelSize { get; private set; } = 0;

        public static int ScreenWidth { get => Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()); }
        public static int ScreenHeight { get => Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()); }

        public static int WindowWidth { get => Raylib.GetScreenWidth(); }
        public static int WindowHeight { get => Raylib.GetScreenHeight(); }

        private static int _windowWidthSave;
        private static int _windowHeightSave;

        public static int DisplayWidth { get => Width * PixelSize; }
        public static int DisplayHeight { get => Height * PixelSize; }

        static int LeftMargin { get => (WindowWidth - DisplayWidth) >> 1; }
        static int TopMargin { get => (WindowHeight - DisplayHeight) >> 1; }


        private static bool switchFullscreen;

        /// <summary>
        /// Start displaying.
        /// </summary>
        /// <param name="renderWidth">The amount of full pixels that can be displayed on one horizontal line.</param>
        /// <param name="renderHeight">The amount of full pixels that can be displayed on one vertical line.</param>
        /// <param name="startingPixelSize">The amount of pixels one pixel will be displayed as.</param>
        public static void Initialise(int renderWidth, int renderHeight, int startingPixelSize)
        {
            if (Raylib.IsWindowReady()) return; // Don't accidentally make two or something stupid like that. I can't be bothered to put this anywhere else.

            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_NONE); // Don't log all the dumb stuff, it's annoying.
            Width = renderWidth;
            Height = renderHeight;

            PixelSize = startingPixelSize;

            Raylib.InitWindow(DisplayWidth, DisplayHeight, "Game");

            Raylib.SetWindowMinSize(renderWidth, renderHeight);
            
        }

        static void Resize()
        {
            // This is here for testing purposes.
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_F))
            {
                switchFullscreen = true;
            }

            // Switch from or to fullscreen.
            if (switchFullscreen)
            {
                if (Raylib.IsWindowFullscreen())
                {
                    Raylib.ToggleFullscreen();
                    Raylib.SetWindowSize(_windowWidthSave, _windowHeightSave);
                }
                else
                {
                    _windowWidthSave = WindowWidth;
                    _windowHeightSave = WindowHeight;

                    Raylib.SetWindowSize(ScreenWidth, ScreenHeight);
                    Raylib.ToggleFullscreen();
                }

                switchFullscreen = false;
            }
            else if (!Raylib.IsWindowResized())
            {
                return;
            }

            int i = 1;
            while (i * Width <= WindowWidth && i * Height <= WindowHeight)
            {
                i++;
            }

            PixelSize = i - 1; // i stops incrementing after it passes the current window size. So the number right below should be the highest acceptable value.
        }

        static bool isDrawing = false;
        public static void BeginDrawing()
        {
            if (isDrawing) throw new Exception("It's already drawing");
            // Before doing anything, handle the window being resized.
            Resize();
            isDrawing = true;

            // Maybe make a variable that decides if it's able to draw.
            Raylib.BeginDrawing();

            // Raylib.DrawRectangle(LeftMargin, TopMargin, DisplayWidth, DisplayHeight, Color.WHITE);
        }

        public static void EndDrawing()
        {
            if (!isDrawing) throw new Exception("It wasn't drawing");
            isDrawing = false;
            // Make a variable that decides if it's able to draw.

            Raylib.ClearBackground(Color.WHITE); // If something wasn't drawn properly, dispose of it.

            foreach (Drawing drawing in drawings)
            {
                Raylib.DrawTexturePro(drawing.Texture, new Rectangle(0, 0, Width, -Height), new Rectangle(LeftMargin, TopMargin, DisplayWidth, DisplayHeight), Vector2.Zero, 0, Color.WHITE);
            }
            drawings.Clear();
            Raylib.EndDrawing();
        }

        private class Drawing
        {
            public Layer Layer { get; private set; }
            public int Distance { get; private set; }
            public Texture2D Texture { get; private set; }
            public Drawing (Layer layer, int distance, Texture2D texture)
            {
                Layer = layer;
                Distance = distance;
                Texture = texture;
            }
        }

        private static List<Drawing> drawings = new List<Drawing>();

        private static Layer _currentLayer = Layer.NONE;
        private static int _currentDistance = 0;
        private static RenderTexture2D capture;

        /// <summary>
        /// Call this before using raylib to draw anything. It will make sure the correct things are drawn on top. Call <see cref ="DoneDraw"/> as soon as it's done.
        /// </summary>
        /// <param name="layer">The layer it will be drawn to.</param>
        /// <param name="distance">The distance from the camera, so that closer things on the same layer are on top.</param>
        public static void DrawAt(Layer layer, int distance)
        {
            if (layer == Layer.NONE) return;
            capture = Raylib.LoadRenderTexture(Width, Height);
            Raylib.BeginTextureMode(capture);
            Raylib.ClearBackground(Color.BLANK);

            _currentLayer = layer;
            _currentDistance = distance;
        }

        public static void DoneDraw()
        {
            if (_currentLayer == Layer.NONE) return;
            Raylib.EndTextureMode();
            int i = 0;
            foreach(Drawing drawing in drawings)
            {
                // Continue until it reaches one that is larger than itself or ends.
                if (_currentLayer < drawing.Layer || (_currentLayer == drawing.Layer && _currentDistance < drawing.Distance))
                {
                    break;
                }
                i++;
            }
            drawings.Insert(i, new Drawing(_currentLayer, _currentDistance, capture.texture));
            _currentLayer = Layer.NONE;
        }
    }
}