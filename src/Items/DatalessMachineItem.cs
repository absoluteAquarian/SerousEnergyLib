using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Items {
	/// <summary>
	/// A base implementation of a <see cref="BaseMachineItem"/> that destroys its data and has a max stack of 999
	/// </summary>
	public abstract class DatalessMachineItem<T> : BaseMachineItem<T> where T : ModTile, IMachineTile {
		#pragma warning disable CS1591
		public override void SafeSetDefaults() {
			base.SafeSetDefaults();

			Item.maxStack = 999;
		}

		public override void PostUpdate() {
			MachineData = null;
		}

		public override void UpdateInventory(Player player) {
			MachineData = null;
		}

		public override void SaveData(TagCompound tag) {
			// If data ends up on this item, do not save it
		}

		public override void LoadData(TagCompound tag) {
			// Do not load any data to this item
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			MachineData = null;

			base.ModifyTooltips(tooltips);
		}
	}
}
