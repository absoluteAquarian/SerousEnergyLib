using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A structure representing the insertion of an item into an inventory
	/// </summary>
	public readonly struct InventoryInsertionResult {
		/// <summary>
		/// The tile coordinates of the inventory that this item will attempt to pathfind to
		/// </summary>
		public readonly Point16 target;

		/// <summary>
		/// The quantity of the item that would be imported into the target inventory
		/// </summary>
		public readonly int stackImported;

		internal InventoryInsertionResult(Point16 target, int stackImported) {
			this.target = target;
			this.stackImported = stackImported;
		}
	}
}
