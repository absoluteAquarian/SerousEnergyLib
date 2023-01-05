using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Items;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// The base implementation for a placeable machine's tiles
	/// </summary>
	public abstract class BaseMachineTile : ModTile, IMachineTile {
		#pragma warning disable CS1591
		public abstract int MachineItem { get; }

		public virtual Color MapEntryColor => new Color(0xd1, 0x89, 0x32);

		public abstract void GetMachineDimensions(out uint width, out uint height);

		public abstract IMachine GetMachineEntity();

		public abstract string GetMachineMapEntryName();

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			IMachineTile.MultitileDefaults(this, MapEntryColor);
			PreRegisterTileObjectData();
			TileObjectData.addTile(Type);
		}

		protected virtual void SafeSetStaticDefaults() { }

		/// <summary>
		/// This is called after the default values are assigned to <see cref="TileObjectData.newTile"/>, but before it's added
		/// </summary>
		protected virtual void PreRegisterTileObjectData() { }

		public override void PlaceInWorld(int i, int j, Item item) {
			IMachineTile.DefaultPlaceInWorld(this, i, j, item);
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			IMachineTile.DefaultKillMultitile(this, i, j);
		}
	}

	/// <inheritdoc cref="BaseMachineTile"/>
	public abstract class BaseMachineTile<TEntity, TItem> : BaseMachineTile where TEntity : ModTileEntity, IMachine where TItem : BaseMachineItem {
		public sealed override int MachineItem => ModContent.ItemType<TItem>();

		public sealed override IMachine GetMachineEntity() => ModContent.GetInstance<TEntity>();
	}
}
