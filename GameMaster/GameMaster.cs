using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OldBadGameMaster
{
    /* This is old and bad.
    class Master
    {
        public enum Step
        {
            Initialize,
            Update,
            Start,
            Destroy,
            Draw,
        }
        public Step currentStep { get; private set; } = Step.Initialize;

        /// <summary>
        /// The amount of updates that have happened since the game started.
        /// </summary>
        /// <value></value>
        public static uint updates { get; private set; } = 0;

        protected Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
        protected List<Actor> StartWhenDone = new List<Actor>();
        protected List<string> toDestroy = new List<string>();

        public delegate ActorType? GameObject<ActorType>() where ActorType : Actor, new();

        /// <summary>
        /// Create a delegate that returns the desired Actor, instead returning null when the actor is destroyed.
        /// </summary>
        /// <param name="actor">The actor that the delegate will return.</param>
        /// <typeparam name="ActorType">The type the delegate will return.</typeparam>
        /// <returns>The delegate</returns>
        private GameObject<ActorType> CreatePointer<ActorType>(ActorType actor) where ActorType : Actor, new()
        {
            ActorType? reference = actor;

            // When the actor is removed from the game, then the delegate should return null instead.
            reference.AddOnDestroy((object? sender, EventArgs e) => reference = null);

            return () => reference;
        }

        /// <summary>
        /// Create an instant of the actor class and add it to the game logic.
        /// </summary>
        /// <param name="name">The name to give the object. Note that the name may change.</param>
        /// <typeparam name="ActorType">The type of actor to create.</typeparam>
        /// <returns>A delegate that returns the created actor</returns>
        public GameObject<ActorType> Create<ActorType>(string name) where ActorType : Actor, new()
        {
            ActorType? actor = new ActorType();

            // If the name given includes (n), remove it.
            Match m = Regex.Match(name.Trim(), @"^(.*?)\(\d+\)$");
            if (m.Success) name = m.Groups[1].Value;

            int i = 0;
            string newName;

            // Check the name until it doesn't match one that already exists.
            while (actors.ContainsKey(newName = $"{name}{(i++ == 0 ? "" : $"({i - 1})")}")) ;

            actors.Add(newName, actor);
            StartWhenDone.Add(actor);

            return CreatePointer<ActorType>(actor);
        }

        /// <summary>
        /// Destroy the actor at after this update has finished.
        /// </summary>
        /// <param name="name">It's name.</param>
        public void Destroy(string name)
        {
            toDestroy.Add(name);
        }

        /// <summary>
        /// Destroy the actor at after this update has finished.
        /// </summary>
        /// <param name="actor">The actor.</param>
        public void Destroy(Actor actor)
        {
            foreach (KeyValuePair<string, Actor> pair in actors)
            {
                if (actor == pair.Value)
                {
                    toDestroy.Add(pair.Key);
                }
            }
        }

        public void Update()
        {
            // Update logic.
            currentStep = Step.Update;
            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors.ElementAt(i).Value;
                actor.Continue();
                WhenDone();
            }

            // Destroy any objects that should be destroyed.
            currentStep = Step.Destroy;
            while (toDestroy.Count != 0)
            {
                // Takes each actor that is going to be destroyed and removes them from the game logic.
                string name = toDestroy[0];
                Actor? actor;
                if (!actors.TryGetValue(name, out actor))
                {
                    continue;
                }
                actors.Remove(name);
                toDestroy.Remove(name);
                actor.Destroy();

                WhenDone();
            }

            // I want them to move after they all decide to move so that the order in which they are updated won't effect their movement.
            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors.ElementAt(i).Value;
                actor.position += actor.velocity;
            }

            // Draw those who remain.
            currentStep = Step.Draw;
            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors.ElementAt(i).Value;
                actor.Draw();
                WhenDone();
            }
            updates++;
        }

        private void WhenDone()
        {
            while (StartWhenDone.Count != 0)
            {
                Actor actor = StartWhenDone[0];
                actor.Initialize();
                StartWhenDone.Remove(actor);

                if (currentStep > Step.Update)
                {
                    actor.Continue();
                }
            }
        }
    } 
    */
}