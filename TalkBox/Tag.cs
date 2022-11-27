using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    struct Tag
    {
        private string statement;

        [Flags]
        public enum Tags : uint
        {
            // -------------------------- Special Tags ----------------------

            EMPTY = 0, // Nothing

            MASK /*     */ = 0b_1000_0000_0000_0000_0000_0000_0000_0000, // This tag is used to extract values from lineTags.
            EXCLUSIVE /**/ = 0b_0100_0000_0000_0000_0000_0000_0000_0000, // If there is a single positive bit in an exclusive mask, then no bit in that mask can be changed.
            DEFAULT /*  */ = 0b_0010_0000_0000_0000_0000_0000_0000_0000, // A default value will be applied unless it's in conflict.

            STATEMENT /**/ = 0b_0001_0000_0000_0000_0000_0000_0000_0000, // If it has this tag then it wants a statement.
            NUMBER /*   */ = 0b_0000_1000_0000_0000_0000_0000_0000_0000, // If it has this tag then it wants a number.
            VALUE = STATEMENT | NUMBER, // If it overlaps this tag then it wants a value.

            METAMASK /* */ = 0b_1111_1111_0000_0000_0000_0000_0000_0000, // A mask to separate the meta tags

            // ----------------------- Line property tags -------------------

            LAST /* */ = 0b_0000_0001, // This doesn't want a user input before continuing.

            HIDE /* */ = 0b_0000_0010 | STATEMENT, // This option should only be displayed if the included statement evaluates true.
            LOCK /* */ = 0b_0000_0100 | STATEMENT, //            -||-            selectable                -||-

            SLOW /* */ = 0b_0000_1000, // Slow text speed. 
            NORM /* */ = 0b_0001_0000 | DEFAULT, // Normal text speed, the default.
            FAST /* */ = 0b_0001_1000, // Fast text speed. 
            SPEED = (SLOW | NORM | FAST | EXCLUSIVE | NUMBER) ^ DEFAULT, // The area that contains measurements for speed.

            IMAGE /**/ = 0b_1111_0000_0000_0000 | MASK | NUMBER, // The image to be displayed is a 4 bit value in this area. (The image is tied to the speaker.)
        }

        // I don't know why but this feels right.
        public static Tag EMPTY { get { return Tags.EMPTY; } }

        public static Tag MASK { get { return Tags.MASK; } }
        public static Tag EXCLUSIVE { get { return Tags.EXCLUSIVE; } }
        public static Tag DEFAULT { get { return Tags.DEFAULT; } }
        public static Tag STATEMENT { get { return Tags.STATEMENT; } }
        public static Tag NUMBER { get { return Tags.NUMBER; } }
        public static Tag VALUE { get { return Tags.VALUE; } }
        public static Tag METAMASK { get { return Tags.METAMASK; } }

        public static Tag LAST { get { return Tags.LAST; } }
        public static Tag HIDE { get { return Tags.HIDE; } }
        public static Tag LOCK { get { return Tags.LOCK; } }
        public static Tag SLOW { get { return Tags.SLOW; } }
        public static Tag NORM { get { return Tags.NORM; } }
        public static Tag FAST { get { return Tags.FAST; } }
        public static Tag SPEED { get { return Tags.SPEED; } }
        public static Tag IMAGE { get { return Tags.IMAGE; } }

        // It is official, I have gone mad with power.
        public static implicit operator Tags(Tag tag) => tag.lineTags;
        public static implicit operator uint(Tag tag) => (uint)tag.lineTags;

        public static implicit operator Tag(Tags tag) => new Tag(tag);
        public static implicit operator Tag(uint tag) => new Tag((Tags)tag);

        /// <summary>
        /// Checks if all positive bits in <paramref name="tag"/> are shared by <see cref="Tag.lineTags"/>. 
        /// Removes <see cref="METAMASK"/> from all checks only if <see cref="Tag.lineTags"/> or <paramref name="tag"/> has values outside of it.
        /// </summary>
        /// <param name="tag">The <see cref="Tag"/ > that you want to know if it's contain be contained in <see cref="Tag.lineTags"/></param>
        /// <returns></returns>
        private bool Contains(Tag tag)
        {
            if ((lineTags & METAMASK) == lineTags || (tag & METAMASK) == tag)
            {
                // lineTags is only meta, therefore we can assume that they want to know if tag overlaps a meta tag, and no special action is necessary.
                return (tag & lineTags) == tag;
            }
            else
            {
                // In order to avoid problems arising from meta tags which are not parts of the mask, they will be removed.
                tag = tag.Without(METAMASK);
                return (tag & lineTags) == tag;
            }
        }

        /// <summary>
        /// Check if there is overlap between the tags. Does not take <see cref="METAMASK"/> tags into special consideration.
        /// </summary>
        /// <param name="tag1"></param>
        /// <param name="tag2"></param>
        /// <returns></returns>
        private static bool Overlaps(Tag tag1, Tag tag2)
        {
            return (tag1 & tag2) != 0;
        }

        /// <summary>
        /// Checks if <see cref="lineTags"/> has a tag that is in conflict with <paramref name="tag"/>
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns><see cref="true"/> if it's in conflict with a different tag.</returns>
        private bool Conflict(Tag tag)
        {
            // First, a quick simple check to see if there is an overlapping bit in lineTags. Nothing should be applied twice.
            if (Overlaps(lineTags, tag)) return true;

            if (Conflicting(tag) != EMPTY)
            {
                return true;
            }

            return false;
        }

        private Tag Conflicting(Tag tag)
        {
            // Check if it's none. I don't know why it would be, but just in case.
            if (tag == EMPTY) return EMPTY;

            // Check if it's part of a Tag that works in funky ways
            foreach (Tag eTag in Enum.GetValues<Tags>())
            {
                // Pure meta tags should be skipped.
                if (METAMASK.Contains(eTag)) continue;

                if (eTag.Contains(EXCLUSIVE) && eTag.Contains(tag))
                {
                    // The excluding tag mask that has tag within it has been found, check if they overlap.
                    if (Overlaps(eTag, lineTags))
                    {
                        return eTag;
                    }
                }
            }
            return EMPTY;
        }

        private Tag Without(Tag remove)
        {
            return (lineTags | remove) ^ remove;
        }

        /// <summary>
        /// It claims to be <see cref="Tags"/> but at this point it's more of a <see cref="uint"/> mess.
        /// </summary>
        // DO NOT REMOVE THE 'Tags.' BEFORE 'Empty'. IT WILL CREATE AN INFINITE LOOP!
        private Tags lineTags { get; set; } = Tags.EMPTY;

        public Tag(string[]? tags, out string lineStatement)
        {
            statement = string.Empty;
            if (tags != null)
            {
                AddTags(tags);
            }
            AddDefaultTags();
            lineStatement = statement;
        }

        private Tag(Tags tag)
        {
            statement = string.Empty;
            lineTags = tag;
        }

        private void AddTags(string[] tags)
        {
            foreach (string tagString in tags)
            {
                string tagName = tagString.ToUpper();
                string extra = string.Empty;

                int index = tagString.IndexOf(':');
                if (index != -1)
                {
                    tagName = tagName.Substring(0, index);
                    extra = tagString.Substring(index + 1);
                }

                Tags tTag;
                if (!Enum.TryParse<Tags>(tagName, out tTag))
                    throw new ArgumentException($"{tagName} is not a tag that exists");

                Tag tag = tTag;
                if (Conflict(tag))
                    throw new InvalidOperationException($"{tagName} is in conflict with a different tag. Please fix");

                if (Overlaps(tag, VALUE))
                {
                    if (extra == String.Empty) throw new ArgumentNullException($"{tagName} needs to be provided additional parameters");

                    if (tag.Contains(NUMBER))
                    {
                        uint value;
                        if (!uint.TryParse(extra, out value)) throw new ArgumentException($"{tagName} needs a positive number, nothing else");

                        AddTagValue(tag, value);
                    }
                    else if (tag.Contains(STATEMENT))
                    {
                        statement = extra;
                        AddTag(tag);
                    }
                }
                else
                {
                    if (tag.Contains(DEFAULT))
                    {
                        Console.WriteLine($"{tagName} is a default value, it does not need to be especially assigned");
                    }
                    if (index != -1)
                        throw new ArgumentException($"{tagName} does not need any additional parameters");

                    AddTag(tag);
                }
            }
        }

        /// <summary>
        /// Looks through all the tags to find one with the Default tag, checks if it's in conflict with anything, and applies the tag if there isn't 
        /// </summary>
        private void AddDefaultTags()
        {
            foreach (Tag dTag in Enum.GetValues<Tags>())
            {
                // Pure meta tags should be skipped.
                if (METAMASK.Contains(dTag)) continue;

                if (dTag.Contains(DEFAULT) && !Conflict(dTag))
                {
                    AddTag(dTag);
                }
            }
        }

        private void AddTag(Tag tag)
        {
            tag = tag.Without(METAMASK);

            if (GetTag(tag)) throw new ArgumentException("Don't add the same tag twice");

            lineTags |= tag;
        }

        private void AddTagValue(Tag tag, uint value)
        {
            tag = tag.Without(METAMASK);

            if (GetTag(tag)) throw new ArgumentException("Don't add the same tag twice");

            if (value == 0) return;

            uint area = tag;
            uint move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

            uint movedValue = value * move; // And this moves the value to the position of that last 1.

            if (movedValue > area)
                throw new ArgumentOutOfRangeException($"{value} is too large, max value is {(move > 0 ? area / move : area)}");

            lineTags |= (Tags)movedValue;
        }

        public bool GetTag(Tag tag)
        {
            // A tag marked as area 
            if (tag.Contains(MASK))
            {
                return (lineTags & tag) != 0;
            }

            foreach (Tag areaTag in Enum.GetValues<Tags>())
            {
                if (tag.Contains(EXCLUSIVE) && Overlaps(tag, areaTag))
                {
                    return ((uint)(tag & lineTags)) != 0;
                }
            }

            return (lineTags & tag) == tag.Without(METAMASK);
        }

        public uint GetTagValue(Tag tag)
        {
            tag = tag.Without(METAMASK);
            uint value = (uint)(lineTags & tag);
            // Quick check to get the obvious stuff out the way
            if (tag == EMPTY || value == 0) return 0;

            uint area = (uint)tag;
            uint move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

            uint movedValue = value / move; // And this moves the contents of the area tag to the lowest position.

            return movedValue;
        }
    }
}
