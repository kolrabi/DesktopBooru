using System;

namespace Booru
{
	public struct Rectangle2D
	{
		public readonly Point2D Position;
		public readonly Point2D Size;

		public Point2D LowerRight { get { return this.Position + this.Size; }}

		public Rectangle2D(Point2D size)
		{
			this.Position = new Point2D (0, 0);
			this.Size = size;
		}

		public Rectangle2D(Point2D position, Point2D size)
		{
			this.Position = position;
			this.Size = size;
		}

		public Cairo.Rectangle ToCairo()
		{
			return new Cairo.Rectangle (this.Position.ToCairo (), this.Size.X, this.Size.Y);
		}
	}
}

