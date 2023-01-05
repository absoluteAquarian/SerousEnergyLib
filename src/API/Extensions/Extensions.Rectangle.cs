using Microsoft.Xna.Framework;

namespace SerousEnergyLib.API {
	partial class Extensions {
		/// <inheritdoc cref="Rectangle.Contains(Point)"/>
		public static bool Contains(this Rectangle rectangle, Vector2 vector) {
			return vector.X >= rectangle.X && vector.Y >= rectangle.Y && vector.X <= rectangle.X + rectangle.Width && vector.Y <= rectangle.Y + rectangle.Height;
		}
	}
}
