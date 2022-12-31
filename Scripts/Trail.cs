using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using OldBadGameMaster;

namespace GyArte
{
    public struct PositionKey
    {
        public Vector2 Pos { get; private set; } // The position.
        public Vector2 Dir { get; private set; } // The direction it's facing
        public float Creation { get; private set; } // The time it was created. Birth would be more accurate, but it would also be weirder.
        public PositionKey(Vector2 position, Vector2 facing, float creation)
        {
            Pos = position;
            Dir = facing;
            Creation = creation;
        }
    }

    /// <summary>
    /// The class that will make the trail should implement this interface.
    /// </summary>
    interface ITrailblazer
    {
        public Vector2 Position { get; } // The position.
        public Vector2 Facing { get; } // The direction it's facing
        public float Time { get; } // The current Time.

        public PositionKey FrozenCopy()
        {
            return new PositionKey(Position, Facing, Time);
        }
    }

    class Trail : IEnumerable
    {
        ITrailblazer trailblazer;
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
        public PositionKey Current { get => trailblazer.FrozenCopy(); }
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
                Vector2 pos = LinearInterpolate(from.Pos, to.Pos, progress);
                return new PositionKey(pos, from.Dir, trailblazer.Time - _lifespan);
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

        public Trail(ITrailblazer livePositionKey, float maxAge)
        {
            trailblazer = livePositionKey;
            positionKeys.Add(trailblazer.FrozenCopy());
            _lifespan = maxAge;
        }

        // Save the current position and time, but not if the time since the last one is equal to or less than 0.
        public bool MakeKey()
        {
            if (trailblazer.Time < positionKeys[positionKeys.Count - 1].Creation) 
            {
                // Don't add one that is older than the youngest position.
                return false; 
            }
            else if (trailblazer.Time == positionKeys[positionKeys.Count - 1].Creation)
            {
                // If the new key would be equal in age to the youngest, then replace it.
                positionKeys[positionKeys.Count - 1] = trailblazer.FrozenCopy();
            }
            else
            {
                positionKeys.Add(trailblazer.FrozenCopy());
                Trim();
            }
            return true;
        }

        // A list of the positions. It is currently assumed that the object moved in straight lines between each.
        private List<PositionKey> positionKeys = new List<PositionKey>();

        public Vector2 GetPositionAt(float timeAgo)
        {
            Trim();

            float timePoint = trailblazer.Time - timeAgo;

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
            return trailblazer.Position;
        }

        public Vector2 GetDirectionAt(float timeAgo)
        {
            Trim();

            float timePoint = trailblazer.Time - timeAgo;

            if (positionKeys.Count == 1 || timePoint >= positionKeys[positionKeys.Count - 1].Creation)
            {
                // It is asking for the direction from the newest position to the current position.
                return trailblazer.Facing;
                // Vector2 dir = Vector2.Normalize(trailblazer.Position - positionKeys[positionKeys.Count - 1].Pos);
                // return dir;
            }
            else
            {
                int i = 1;
                // Increment i until it reaches the max
                while (i < positionKeys.Count && positionKeys[i].Creation < timePoint)
                {
                    i++;
                }

                return positionKeys[i - 1].Dir;
                // Vector2 pos1 = positionKeys[i - 1].Pos;
                // Vector2 pos2 = positionKeys[i].Pos;
                // Vector2 dir = Vector2.Normalize(pos2 - pos1);
                // return dir;
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
            while (positionKeys.Count > 1 && trailblazer.Time - positionKeys[1].Creation > _lifespan)
            {
                positionKeys.RemoveAt(0);
            }
        }

        public float AgeOf(PositionKey position) => trailblazer.Time - position.Creation;

        static Vector2 LinearInterpolate(Vector2 from, Vector2 to, float progress)
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
                Vector2 direction = to - from;
                return direction * progress + from;
            }
        }
    }
}