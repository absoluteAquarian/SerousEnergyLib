using Microsoft.Xna.Framework;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A structure representing data used to draw an overlay behind or in front of a machine
	/// </summary>
	public readonly struct MachineSpriteEffectInformation {
		/// <summary>
		/// The path to the effect's sprite
		/// </summary>
		public readonly string asset;

		/// <summary>
		/// The offset from the top-left corner of the machine tile to draw the effect at<br/>
		/// <b>NOTE:</b> This field is in world coordinates, not tile coordinates
		/// </summary>
		public readonly Vector2 offset;

		/// <summary>
		/// The source rectangle within this effect's sprite to draw
		/// </summary>
		public readonly Rectangle? frame;

		/// <summary>
		/// Whether this effect's draw color is affected by the lighting in the world where it's drawn
		/// </summary>
		public readonly bool affectedByLight;

		/// <summary>
		/// Creates a new <see cref="MachineSpriteEffectInformation"/> instance
		/// </summary>
		/// <param name="asset">The path to the effect's sprite</param>
		/// <param name="offset">
		/// The offset from the top-left corner of the machine tile to draw the effect at<br/>
		/// <b>NOTE:</b> This parameter is in world coordinates, not tile coordinates
		/// </param>
		/// <param name="frame">The source rectangle within this effect's sprite to draw</param>
		/// <param name="affectedByLight">Whether this effect's draw color is affected by the lighting in the world where it's drawn</param>
		public MachineSpriteEffectInformation(string asset, Vector2 offset, Rectangle? frame, bool affectedByLight) {
			this.asset = asset;
			this.offset = offset;
			this.frame = frame;
			this.affectedByLight = affectedByLight;
		}

		/// <summary>
		/// Creates a new <see cref="MachineSpriteEffectData"/> instance based on this effect information
		/// </summary>
		public MachineSpriteEffectData GetDrawInformation() => new MachineSpriteEffectData(this);
	}
}
