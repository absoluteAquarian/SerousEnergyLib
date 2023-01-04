using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	public readonly struct InventoryExtractionResult {
		public readonly Item item;
		public readonly Point16 target;

		internal InventoryExtractionResult(Point16 target, Item item) {
			this.target = target;
			this.item = item;
		}
	}
}
