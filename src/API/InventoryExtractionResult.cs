using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A structure representing the extraction of an item from an inventory
	/// </summary>
	public readonly struct InventoryExtractionResult {
		/// <summary>
		/// The item that was extracted
		/// </summary>
		public readonly Item item;

		/// <summary>
		/// The tile coordinates of the inventory that this item will pathfind to
		/// </summary>
		public readonly Point16 target;

		internal InventoryExtractionResult(Point16 target, Item item) {
			this.target = target;
			this.item = item;
		}
	}
}
