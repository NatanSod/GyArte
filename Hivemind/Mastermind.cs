using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using GyArte;
using System.Text.Json;
using Raylib_cs;
using TalkBox;

namespace Hivemind
{
    static class Mastermind
    {
        static public uint cycles { get; private set; } = 0;
        // This needs to happen first, because the player and the hive is initialized they try to access it.
        // I should initialize them in Awaken, but then the IDE complains about possible null reference.
        static private List<SpriteSheet> spriteSheets = new List<SpriteSheet>();

        static public DialogueHandler mouthpiece { get; private set; }
        static public DialogueRunner lore { get; private set; }
        static public Player victim { get; private set; }
        static public Hive currentHive { get; private set; }
        static public Slave? subject { get; private set; }
        static public Vector2 Eyes { get; private set; }

        static List<string> hasRun = new List<string>();

        static Mastermind()
        {
            mouthpiece = new DialogueHandler();
            lore = new DialogueRunner(mouthpiece, "testDi");
            victim = new Player();
            currentHive = new Hive("Test");
            
            float x = victim.Position.X - Render.Width;
            float y = victim.Position.Y - Render.Height;

            UpdateEyes();
        }

        static void UpdateEyes()
        {
            float x = victim.Position.X - (Render.Width >> 1);
            float y = victim.Position.Y - (Render.Height >> 1);

            if (x <= 0)
            {
                x = 0;
            }
            else if (x + Render.Width > currentHive.ActualWidth)
            {
                x = currentHive.ActualWidth - Render.Width;
            }

            if (y <= 0)
            {
                y = 0;
            }
            else if (y + Render.Height > currentHive.ActualLength)
            {
                y = currentHive.ActualLength - Render.Height;
            }

            Eyes = new Vector2(x, y);
        }

        public static JsonSerializerOptions jsonOptions
        {
            get => new JsonSerializerOptions()
            {
                WriteIndented = true
            };
        }

        /// <summary>
        /// It's basically Start, but this just fit better with the hivemind theme.
        /// </summary>
        static public void Awaken()
        {
            Console.WriteLine("It has awoken.");
        }

        static public void ConstructHive(string name, int position)
        {
            // Add a bit more fanfare maybe.
            currentHive.Deconstruct();
            currentHive = new Hive(name);
            Vector2 start = currentHive.Entrances[position];
            victim = new Player(start);
        }

        /// <summary>
        /// It's basically Update, but this just fit better with the hivemind theme.
        /// </summary>
        static public void Contemplate()
        {
            if (mouthpiece.Running)
            {
                Converse();
            }

            victim.Update();
            UpdateEyes();
            victim.Draw();
            currentHive.Update();
            cycles++;
        }

        static void Converse()
        {
            // Handle the inputs so that they can be moderated.
            bool next = Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE);

            // Scroll through which options to select.
            int scroll = 0;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_W) || Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
            {
                scroll--;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_S) || Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
            {
                scroll++;
            }

            mouthpiece.Update(next, scroll);
            mouthpiece.Draw();
        }

        static public Vector2? CheckCollision(Vector2 goal) => currentHive.CheckCollision(goal);

        static public Player.State? Interact(int x, int y)
        {
            Slave? interacting = currentHive.Interact(x, y);

            if (interacting?.Interaction == null) return null;

            switch (interacting.Interaction.Type)
            {
                case Interaction.iType.TALK:
                    subject = interacting;
                    mouthpiece.BeginDialogue(subject.Interaction.Extra);
                    mouthpiece.Update(false, 0);
                    mouthpiece.Draw();
                    return Player.State.TALK;
                case Interaction.iType.DOOR:
                    string[] split = interacting.Interaction.Extra.Split(' ');
                    string name = split[0];
                    int entrance = int.Parse(split[1]);
                    ConstructHive(name, entrance);
                    return null;
                default:
                    throw new Exception("Not ready for that kind of interaction yet.");
            }
        }

        static public Player.State? Trigger(int x, int y)
        {
            Slave? trigger = currentHive.Trigger(x, y);

            if (trigger?.Interaction == null) return null;

            switch (trigger.Interaction.Type)
            {
                case Interaction.iType.TALK:
                    // Do not do run trigger dialogue if it has already been run.
                    if (hasRun.Contains(trigger.Interaction.Extra)) return null;

                    hasRun.Add(trigger.Interaction.Extra);
                    subject = trigger;
                    mouthpiece.BeginDialogue(subject.Interaction.Extra);
                    return Player.State.TALK;
                case Interaction.iType.DOOR:
                    string[] split = trigger.Interaction.Extra.Split(' ');
                    string name = split[0];
                    int entrance = int.Parse(split[1]);
                    ConstructHive(name, entrance);
                    return null;
                default:
                    throw new Exception("Not ready for that kind of interaction yet.");
            }
        }

        static public SpriteSheet LoadSheet(string name)
        {
            foreach (SpriteSheet spriteSheet in spriteSheets)
            {
                if (spriteSheet.Name == name)
                {
                    return spriteSheet;
                }
            }
            SpriteSheet newSheet = new SpriteSheet(name);
            spriteSheets.Add(newSheet);
            return newSheet;
        }
    }
}