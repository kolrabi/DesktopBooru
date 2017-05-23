using System;

namespace Booru
{
	public struct Point2D
	{
		public readonly int X;
		public readonly int Y;

		public bool IsZero { get { return this.X == 0 || this.Y == 0; }}

		public Point2D(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public Cairo.Point ToCairo()
		{
			return new Cairo.Point (this.X, this.Y);
		}

		public static Point2D operator+(Point2D a, Point2D b)
		{
			return new Point2D (a.X + b.X, a.Y + b.Y);
		}

		public static Point2D operator-(Point2D a, Point2D b)
		{
			return new Point2D (a.X - b.X, a.Y - b.Y);
		}

		public static Point2D operator/(Point2D a, int b)
		{
			return new Point2D (a.X / b, a.Y / b);
		}

		public override string ToString ()
		{
			return string.Format ("[{0} {1}]", this.X, this.Y);
		}

		public override bool Equals (object obj)
		{
			if (!(obj is Point2D))
				return false;

			var other = (Point2D)obj;
			return other.X == this.X && other.Y == this.Y;
		}

		public override int GetHashCode ()
		{
			return this.X.GetHashCode () ^ this.Y.GetHashCode ();
		}
	}
}

