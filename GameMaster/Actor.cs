using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace OldBadGameMaster
{
    /* This is old and bad.
    abstract class Actor
    {
        public enum Activity
        {
            Starting,
            Active,
            Destroyed,
        }

        public Vector3 position;
        public Vector3 velocity;
        public Activity activity { get; private set; } = Activity.Starting;

        public void Initialize()
        {
            if (activity != Activity.Starting) return;

            Start();
            activity = Activity.Active;
        }

        public void Continue()
        {
            if (activity != Activity.Active) return;
            Update();
        }

        protected virtual void Start() { }
        protected virtual void Update() { }
        public virtual void Draw() { }

        /// <summary>
        /// Add a function to be called when this object is removed from the game logic.
        /// </summary>
        /// <param name="function"></param>
        public void AddOnDestroy(EventHandler function)
        {
            OnDestroy += function;
        }
        private event EventHandler? OnDestroy;

        /// <summary>
        /// Call each function that is added to <see cref="OnDestroy"/> through <see cref="AddOnDestroy"/>.
        /// </summary>
        public void Destroy()
        {
            EventArgs e = new EventArgs();
            OnDestroy?.Invoke(this, e);
            OnDestroy = null;
            activity = Activity.Destroyed;
        }
    } 
    */
}