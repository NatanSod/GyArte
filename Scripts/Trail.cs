using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using GameMaster;

namespace GyArte
{
    public struct PositionKey
    {
        public Vector3 Pos { get; private set; } // The position.
        public float Creation { get; private set; } // The time it was created. Birth would be more accurate, but it would also be weirder.
        public PositionKey(Vector3 position, float creation)
        {
            Pos = position;
            Creation = creation;
        }
    }

    class Trail : IEnumerable
    {
        // Why did I make delegates to get references of Vector3s and floats?
        // Because I felt it would look better.
        public delegate Vector3 PositionRef();
        public delegate float TimeRef();
        private PositionRef positionRef;
        private TimeRef timeRef;

        float _lifespan;
        public float Lifespan { get => _lifespan; }

        /// <summary>
        /// The order is from oldest to newest. The last position is always going to be the current position.
        /// </summary>
        /// <returns></returns>
        public PositionKey this[int i]
        {
            get
            {
                if (i >= positionKeys.Count) return Current;
                else if (i == 0) return Oldest;
                else return positionKeys[i];
            }
        }
        /// <summary>
        /// The current position of the thing making the trail.
        /// </summary>
        public PositionKey Current { get => new PositionKey(positionRef(), timeRef()); }
        /// <summary>
        /// The oldest position that fits within the lifespan.
        /// </summary>
        public PositionKey Oldest
        {
            get
            {
                PositionKey from = positionKeys[0];
                float age = AgeOf(from);
                
                if (age < _lifespan) return from;

                PositionKey to = this[1];
                float progress = (age - _lifespan) / (to.Creation - from.Creation);
                Vector3 pos = LinearInterpolate(from.Pos, to.Pos, progress);
                return new PositionKey(pos, timeRef() - _lifespan);
            }
        }
        /// <summary>
        /// The amount of keys in the trail including the current position of the trailblazer (the thing making the trail).
        /// </summary>
        public int Count { get => positionKeys.Count + 1; }
        public float Length
        {
            get
            {
                float length = AgeOf(positionKeys[0]);
                return _lifespan != 0 && length > _lifespan ? _lifespan : length;
            }
        }

        public Trail(PositionRef positionReference, TimeRef timeReference, float maxAge)
        {
            positionRef = positionReference;
            timeRef = timeReference;
            positionKeys.Add(new PositionKey(positionReference(), 0));
            _lifespan = maxAge;
        }

        // Save the current position and time, but not if the time since the last one is equal to or less than 0.
        public bool MakeKey()
        {
            if (timeRef() <= positionKeys[positionKeys.Count - 1].Creation) return false; // Don't add one that is older than or equal in age to the youngest position.
            positionKeys.Add(new PositionKey(positionRef(), timeRef()));
            Trim();
            return true;
        }

        // A list of the positions. It is currently assumed that the object moved in straight lines between each.
        private List<PositionKey> positionKeys = new List<PositionKey>();

        public Vector3 GetPositionAt(float timeAgo)
        {
            Trim();

            float timePoint = timeRef() - timeAgo;

            if (timeAgo >= _lifespan || timePoint <= positionKeys[0].Creation)
            {
                if (timePoint > 0)
                {
                    int a = 1;
                    a++;
                }
                return Oldest.Pos;
            }

            int i = 0;
            foreach (PositionKey posKey in this)
            {
                if (posKey.Creation == timePoint)
                {
                    return posKey.Pos;
                }
                else if (timePoint < posKey.Creation)
                {
                    PositionKey previous = positionKeys[i - 1];
                    float progress = (timePoint - previous.Creation) / (posKey.Creation - previous.Creation);
                    return LinearInterpolate(previous.Pos, posKey.Pos, progress);
                }
                i++;
            }
            return positionRef();
        }

        public Vector3 GetDirectionAt(float timeAgo)
        {
            Trim();

            float timePoint = timeRef() - timeAgo;

            if (positionKeys.Count == 1 || timePoint >= positionKeys[positionKeys.Count - 1].Creation)
            {
                // It is asking for the direction from the newest position to the current position.
                Vector3 dir = Vector3.Normalize(positionRef() - positionKeys[positionKeys.Count - 1].Pos);
                return dir;
            }
            else
            {
                int i = 0;
                // Increment i until it reaches the max
                while (i < positionKeys.Count && positionKeys[i].Creation < timePoint)
                {
                    i++;
                }


                Vector3 pos1 = positionKeys[i].Pos;
                Vector3 pos2 = positionKeys[i + 1].Pos;
                Vector3 dir = Vector3.Normalize(pos2 - pos1);
                return dir;
            }
        }

        public IEnumerator GetEnumerator()
        {
            Trim();

            int i = 0;
            if (AgeOf(positionKeys[0]) > _lifespan)
            {
                yield return Oldest;
                i++;
            }
            while (i < positionKeys.Count)
            {
                yield return positionKeys[i];
                i++;
            }
            yield return Current;
        }

        private void Trim()
        {
            if (_lifespan <= 0) return; // If the lifespan is 0 or less, then we assume they want everything to life forever.

            // For it to remove a position, there must be more than 1 position, and the next position must be too old.
            // Because there must always be 1 position left and there must always be one position that is to old (if there is one that is too old).
            while (positionKeys.Count > 1 && timeRef() - positionKeys[1].Creation > _lifespan)
            {
                positionKeys.RemoveAt(0);
            }
        }

        public float AgeOf(PositionKey position) => timeRef() - position.Creation;

        static Vector3 LinearInterpolate(Vector3 from, Vector3 to, float progress)
        {
            if (progress <= 0 || from == to)
            {
                return from;
            }
            else if (progress >= 1)
            {
                return to;
            }
            else
            {
                Vector3 direction = to - from;
                return direction * progress + from;
            }
        }
    }
}