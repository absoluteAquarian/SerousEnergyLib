using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.CrossMod {
	/// <summary>
	/// An object representing the animation state for a machine being displayed in the Machine Workbench machine from Terran Automation
	/// </summary>
	public class MachineRegistryDisplayAnimationState {
		/// <summary>
		/// The asset to draw
		/// </summary>
		public readonly Asset<Texture2D> asset;
		/// <summary>
		/// The source rectangle within <see cref="asset"/> to draw
		/// </summary>
		public Rectangle frame;

		#pragma warning disable CS1591
		public MachineRegistryDisplayAnimationState(string asset, Rectangle frame) {
			this.asset = ModContent.Request<Texture2D>(asset);
			this.frame = frame;
		}

		public MachineRegistryDisplayAnimationState(Asset<Texture2D> asset, Rectangle frame) {
			this.asset = asset;
			this.frame = frame;
		}

		public MachineRegistryDisplayAnimationState(string asset, int columnCount, int rowCount, int frameX, int frameY, int sizeOffsetX = 0, int sizeOffsetY = 0) {
			this.asset = ModContent.Request<Texture2D>(asset, AssetRequestMode.ImmediateLoad);
			frame = this.asset.Frame(columnCount, rowCount, frameX, frameY, sizeOffsetX, sizeOffsetY);
		}

		public MachineRegistryDisplayAnimationState(string asset, int columnCount, int rowCount, uint frameX, uint frameY, int sizeOffsetX = 0, int sizeOffsetY = 0) {
			this.asset = ModContent.Request<Texture2D>(asset, AssetRequestMode.ImmediateLoad);
			frame = this.asset.Frame(columnCount, rowCount, (int)frameX, (int)frameY, sizeOffsetX, sizeOffsetY);
		}
	}
}
