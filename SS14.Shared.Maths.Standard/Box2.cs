﻿using System;

namespace SS14.Shared.Maths
{
    /// <summary>
    ///     Axis Aligned rectangular box.
    /// </summary>
    [Serializable]
    public struct Box2 : IEquatable<Box2>
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public Vector2 BottomRight => new Vector2(Right, Bottom);
        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        public float Width => Math.Abs(Right - Left);
        public float Height => Math.Abs(Top - Bottom);
        public Vector2 Size => new Vector2(Width, Height);

        public Box2(Vector2 leftTop, Vector2 rightBottom)
        {
            Left = leftTop.X;
            Top = leftTop.Y;
            Right = rightBottom.X;
            Bottom = rightBottom.Y;
        }

        public Box2(float left, float top, float right, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static Box2 FromDimensions(float left, float top, float width, float height)
        {
            return new Box2(left, top, left + width, top - height);
        }

        public static Box2 FromDimensions(Vector2 leftTopPosition, Vector2 size)
        {
            return FromDimensions(leftTopPosition.X, leftTopPosition.Y, size.X, size.Y);
        }

        public bool Intersects(Box2 other)
        {
            return other.Bottom <= this.Top && other.Top >= this.Bottom && other.Right >= this.Left && other.Left <= this.Right;
        }

        public bool IsEmpty()
        {
            return FloatMath.CloseTo(Width, 0.0f) && FloatMath.CloseTo(Height, 0.0f);
        }

        public bool Encloses(Box2 inner)
        {

            return this.Left < inner.Left && this.Bottom < inner.Bottom && this.Right > inner.Right && this.Top > inner.Top;
        }

        public bool Contains(float x, float y)
        {
            return Contains(new Vector2(x, y));
        }

        public bool Contains(Vector2 point, bool closedRegion = true)
        {
            var xOK = closedRegion == Left <= Right ? point.X >= Left != point.X > Right : point.X > Left != point.X >= Right;
            var yOK = closedRegion == Top <= Bottom ? point.Y >= Top != point.Y > Bottom : point.Y > Top != point.Y >= Bottom;
            return xOK && yOK;
        }

        /// <summary>
        ///     Uniformly scales the box by a given scalar.
        /// </summary>
        /// <param name="scalar">Value to scale the box by.</param>
        /// <returns>Scaled box.</returns>
        public Box2 Scale(float scalar)
        {
            return new Box2(
                Left * scalar,
                Top * scalar,
                Right * scalar,
                Bottom * scalar);
        }

        /// <summary>Returns a Box2 translated by the given amount.</summary>
        public Box2 Translated(Vector2 point)
        {
            return new Box2(Left + point.X, Top + point.Y, Right + point.X, Bottom + point.Y);
        }

        public bool Equals(Box2 other)
        {
            return Left.Equals(other.Left) && Right.Equals(other.Right) && Top.Equals(other.Top) && Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is Box2 box2 && Equals(box2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Compares two objects for equality by value.
        /// </summary>
        public static bool operator ==(Box2 a, Box2 b)
        {
            return FloatMath.CloseTo(a.Bottom, b.Bottom) &&
                   FloatMath.CloseTo(a.Right, b.Right) &&
                   FloatMath.CloseTo(a.Top, b.Top) &&
                   FloatMath.CloseTo(a.Left, b.Left);
        }

        public static bool operator !=(Box2 a, Box2 b)
        {
            return !(a == b);
        }
    }
}
