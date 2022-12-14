using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using GyArte;
using System.Text.Json;
using Raylib_cs;

namespace Hivemind
{
    static class Mastermind
    {
        static public uint cycles { get; private set; } = 0;
        // This needs to happen first, because the player and the hive is initialized they try to access it.
        // I should initialize them in Awaken, but then the IDE complains about possible null reference.
        static private List<SpriteSheet> spriteSheets = new List<SpriteSheet>();

        static public DialogueHandler mouthpiece { get; private set; } = new DialogueHandler();
        static public Player victim { get; private set; } = new Player();
        static public Hive currentHive { get; private set; } = new Hive("Test");
        static public Slave? subject { get; private set; }


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

        static public void ConstructHive(string name)
        {
            // Add a bit more fanfare maybe.
            currentHive = new Hive(name);
        }

        /// <summary>
        /// It's basically Update, but this just fit better with the hivemind theme.
        /// </summary>
        static public void Contemplate()
        {
            if (!mouthpiece.Done)
            {
                Converse();
            }

            victim.Update();
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

        static public byte CheckCollision(int x, int y) => currentHive.CheckCollision(x, y, victim);

        static public Player.State? Interact(int x, int y)
        {
            Slave? interacting = currentHive.Interact(x, y, victim);

            if (interacting?.Interaction == null) return null;

            switch (interacting.Interaction.Type)
            {
                case Interaction.iType.TALK:
                    subject = interacting;
                    mouthpiece.BeginDialogue(subject.Interaction.Extra);
                    mouthpiece.Update(false, 0);
                    mouthpiece.Draw();
                    return Player.State.TALK;
                default:
                    throw new Exception("Not ready for that kind of interaction yet.");
            }
        }

        static public Player.State? Trigger(int x, int y)
        {
            Slave? trigger = currentHive.Trigger(x, y, victim);

            if (trigger?.Interaction == null) return null;

            switch (trigger.Interaction.Type)
            {
                case Interaction.iType.TALK:
                    subject = trigger;
                    mouthpiece.BeginDialogue(subject.Interaction.Extra);
                    return Player.State.TALK;
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