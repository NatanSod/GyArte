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
    /// It is also used to handle layers so that I won't have to think about the order in which everything is drawn.
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

        /// <summary>
        /// The amount of pixels on the x axis.
        /// </summary>
        /// <value>The amount of pixels on the x axis.</value>
        public static int Width { get; private set; } = 0;
        /// <summary>
        /// The amount of pixels on the y axis.
        /// </summary>
        /// <value>The amount of pixels on the y axis.</value>
        public static int Height { get; private set; } = 0;
        /// <summary>
        /// The width and height of one rendered pixel in screen pixels.
        /// </summary>
        /// <value>The width and height of one rendered pixel in screen pixels.</value>
        public static int PixelSize { get; private set; } = 0;

        /// <summary>
        /// Gets the horizontal resolution of the screen.
        /// </summary>
        /// <returns>The horizontal resolution of the screen.</returns>
        public static int ScreenWidth { get => Raylib.GetScreenWidth(); }
        /// <summary>
        /// Gets the vertical resolution of the screen.
        /// </summary>
        /// <returns>The vertical resolution of the screen.</returns>
        public static int ScreenHeight { get => Raylib.GetScreenHeight(); }

        /// <summary>
        /// Holds the width the window had in screen pixels before it was fullscreen.
        /// </summary>
        private static int _windowWidthSave;
        /// <summary>
        /// Holds the width the window had in screen pixels before it was fullscreen.
        /// </summary>
        private static int _windowHeightSave;

        /// <summary>
        /// Get the width of the render in screen pixels.
        /// </summary>
        /// <returns>The width of the render in screen pixels.</returns>
        public static int DisplayWidth { get => Width * PixelSize; }
        /// <summary>
        /// Get the height of the render in screen pixels.
        /// </summary>
        /// <returns>The height of the render in screen pixels.</returns>
        public static int DisplayHeight { get => Height * PixelSize; }

        static int LeftMargin { get => (ScreenWidth - DisplayWidth) >> 1; }
        static int TopMargin { get => (ScreenHeight - DisplayHeight) >> 1; }

        static bool debug = false;

        private static bool switchFullscreen;

        /// <summary>
        /// Start displaying.
        /// </summary>
        /// <param name="renderWidth">The amount of full pixels that can be displayed on one horizontal line.</param>
        /// <param name="renderHeight">The amount of full pixels that can be displayed on one vertical line.</param>
        /// <param name="startingPixelSize">The amount of pixels one pixel will be displayed as.</param>
        public static void Initialise(int renderWidth, int renderHeight, int startingPixelSize, bool displayDebug)
        {
            debug = displayDebug;
            if (Raylib.IsWindowReady()) return; // Don't accidentally make two or something stupid like that. I can't be bothered to put this anywhere else.

            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_NONE); // Don't log all the dumb stuff, it's annoying.
            Width = renderWidth;
            Height = renderHeight;

            PixelSize = startingPixelSize;

            Raylib.InitWindow(DisplayWidth, DisplayHeight, "Game");

            Raylib.SetWindowMinSize(renderWidth, renderHeight);
        }

        /// <summary>
        /// Checks if the window should be resized and then changes. 
        /// If it should be changed then it also resizes the window.
        /// </summary>
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
                    _windowWidthSave = ScreenWidth;
                    _windowHeightSave = ScreenHeight;

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
            while (i * Width <= ScreenWidth && i * Height <= ScreenHeight)
            {
                i++;
            }

            PixelSize = i - 1; // i stops incrementing after it passes the current window size. So the number right below should be the highest acceptable value.
        }

        static bool isDrawing = false;
        /// <summary>
        /// Begin drawing a new frame.
        /// </summary>
        public static void BeginFrame()
        {
            if (isDrawing) throw new Exception("It's already drawing");
            // Before doing anything, handle the window being resized.
            Resize();
            isDrawing = true;

            // Maybe make a variable that decides if it's able to draw.
            Raylib.BeginDrawing();
        }

        /// <summary>
        /// End drawing the current frame, merge all layers, and display the resulting image on the screen.
        /// </summary>
        public static void EndFrame()
        {
            if (!isDrawing) throw new Exception("It wasn't drawing");
            isDrawing = false;
            // Make a variable that decides if it's able to draw.

            Raylib.ClearBackground(Color.BLACK); // If something wasn't drawn properly, dispose of it.
            // Raylib.DrawRectangle(LeftMargin, TopMargin, DisplayWidth, DisplayHeight, Color.WHITE);


            foreach (Drawing drawing in drawings)
            {
                if (drawing.Layer == Layer.DEBUG && !debug) continue;
                Raylib.DrawTexturePro(drawing.Texture, new Rectangle(0, 0, Width, -Height), new Rectangle(LeftMargin, TopMargin, DisplayWidth, DisplayHeight), Vector2.Zero, 0, Color.WHITE);
            }
            Raylib.EndDrawing();
            // After having having been drawn to the screen and drawing has ended, "Kill" every single drawing.
            // Letting them live was found to cause major framerate issues.
            foreach (Drawing drawing in drawings)
            {
                drawing.Kill();
            }
            drawings.Clear();
        }

        /// <summary>
        /// An image which should be displayed on the specified layer at the specified distance.
        /// </summary>
        private class Drawing
        {
            public Layer Layer { get; private set; }
            public int Distance { get; private set; }
            public Texture2D Texture { get => renderTexture.texture; }
            private RenderTexture2D renderTexture;
            public Drawing(Layer layer, int distance, RenderTexture2D texture)
            {
                Layer = layer;
                Distance = distance;
                renderTexture = texture;
            }
            public void Kill()
            {
                Raylib.UnloadRenderTexture(renderTexture);
            }
        }

        private static List<Drawing> drawings = new List<Drawing>();

        private static Layer _currentLayer = Layer.NONE;
        private static int _currentDistance = 0;
        private static RenderTexture2D capture;

        /// <summary>
        /// Call this before using raylib to draw anything. It will make sure the correct things are drawn on top. Call <see cref ="EndDraw"/> as soon as it's done.
        /// </summary>
        /// <param name="layer">The layer it will be drawn to.</param>
        /// <param name="distance">The distance from the camera, so that closer things on the same layer are on top.</param>
        public static void BeginDraw(Layer layer, int distance)
        {
            if (layer == Layer.NONE) return;
            capture = Raylib.LoadRenderTexture(Width, Height);
            Raylib.BeginTextureMode(capture);
            Raylib.ClearBackground(Color.BLANK);

            _currentLayer = layer;
            _currentDistance = distance;
        }

        /// <summary>
        /// End the current drawing and add it to the list of layers to be drawn.
        /// </summary>
        public static void EndDraw()
        {
            if (_currentLayer == Layer.NONE) return;
            Raylib.EndTextureMode();
            int i = 0;
            foreach (Drawing drawing in drawings)
            {
                // Continue until it reaches one that is larger than itself or ends.
                if (_currentLayer < drawing.Layer || (_currentLayer == drawing.Layer && _currentDistance < drawing.Distance))
                {
                    break;
                }
                i++;
            }
            drawings.Insert(i, new Drawing(_currentLayer, _currentDistance, capture));
            _currentLayer = Layer.NONE;
        }
    }
}