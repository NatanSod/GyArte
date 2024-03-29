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
        // The sprite sheets are not unloaded properly.

        static public DialogueHandler mouthpiece { get; private set; }
        static public Commander commander { get; private set; }
        static public DialogueRunner lore { get; private set; }
        static public Player victim { get; private set; }
        static public Hive currentHive { get; private set; }
        static public Slave? subject { get; private set; }
        static public Vector2 Eyes { get; private set; }

        static List<Slave> hasRun = new List<Slave>();

        static Mastermind()
        {
            mouthpiece = new DialogueHandler();
            commander = new Commander();
            lore = new DialogueRunner(mouthpiece, commander, @"Assets\Dialogue\Bedroom");
            victim = new Player();
            currentHive = new Hive("TheVoid");
            ConstructHive("TheVoid", 0);
            mouthpiece.BeginDialogue(lore);

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
            Vector2 start = currentHive.Entrances[0, position];
            Vector2 facing = currentHive.Entrances[1, position];
            victim.SetPosition(start, facing);
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
            commander.RunAsync();

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
            subject = interacting;
            lore = new DialogueRunner(mouthpiece, commander, @$"Assets\Dialogue\{currentHive.Name}", $"{currentHive.Name}{interacting.Interaction}");
            mouthpiece.BeginDialogue(lore);
            if (mouthpiece.Running)
            {
                mouthpiece.Update(false, 0);
                mouthpiece.Draw();
                return Player.State.TALK;
            }
            return null;
        }

        static public Player.State? Trigger(int x, int y)
        {
            List<Slave> triggers = currentHive.Trigger(x, y);

            for (int i = 0; i < hasRun.Count; i++)
            {
                if (!triggers.Remove(hasRun[i]))
                {
                    hasRun.RemoveAt(i);
                    i--;
                }
            }

            foreach (Slave trigger in triggers)
            {
                if (trigger.Interaction == null) continue;

                // Do not do run trigger dialogue if it has already been run.
                if (hasRun.Contains(trigger)) return null;

                hasRun.Add(trigger);
                subject = trigger;
                lore = new DialogueRunner(mouthpiece, commander, @$"Assets\Dialogue\{currentHive.Name}", $"{currentHive.Name}{trigger.Interaction}");
                mouthpiece.BeginDialogue(lore);

                if (mouthpiece.Running)
                {
                    return Player.State.TALK;
                }
            }
            return null;
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