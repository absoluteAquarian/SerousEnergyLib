using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.CrossMod {
	#pragma warning disable CS1591
	public struct MachineSpriteEffectData {
		public Asset<Texture2D> asset;
		public Vector2 offset;
		public Rectangle? frame;
		public Color color;
		public float rotation;
		public Vector2 origin;
		public Vector2 scale;
		public SpriteEffects effects;

		public bool affectedByLight;

		public MachineSpriteEffectData(MachineSpriteEffectInformation info) {
			asset = ModContent.Request<Texture2D>(info.asset);
			offset = info.offset;
			frame = info.frame;
			color = Color.White;
			rotation = 0;
			origin = Vector2.Zero;
			scale = Vector2.One;
			effects = SpriteEffects.None;
			affectedByLight = info.affectedByLight;
		}

		/// <summary>
		/// Uses the data in this instance to draw the effect sprite
		/// </summary>
		/// <param name="spriteBatch">The sprite batch</param>
		/// <param name="entityLocation">The location of the source <see cref="ModTileEntity"/> that this effect is drawn for</param>
		public void Draw(SpriteBatch spriteBatch, Point16 entityLocation) {
			Vector2 worldPosition = entityLocation.ToWorldCoordinates(0, 0) + offset;
			Vector2 drawPosition = worldPosition - Main.screenPosition + TileFunctions.GetLightingDrawOffset();

			Point tilePosition = worldPosition.ToTileCoordinates();

			Color drawColor = affectedByLight ? Lighting.GetColor(tilePosition, color) : color;

			spriteBatch.Draw(asset.Value, drawPosition, frame, drawColor, rotation, origin, scale, effects, 0);
		}
	}
}
