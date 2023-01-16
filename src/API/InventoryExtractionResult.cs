using System.Collections.Generic;
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

		/// <summary>
		/// The path that this item will use to move to the target inventory
		/// </summary>
		public readonly List<Point16> path;

		/// <summary>
		/// The tile coordinates of the inventory that this item will pathfind from
		/// </summary>
		public readonly Point16 source;

		/// <summary>
		/// The index in the source inventory that this item was extracted from
		/// </summary>
		public readonly int sourceSlot;

		internal InventoryExtractionResult(Point16 target, List<Point16> path, Point16 source, Item item, int sourceSlot) {
			this.target = target;
			this.path = path;
			this.source = source;
			this.item = item;
			this.sourceSlot = sourceSlot;
		}

		internal InventoryExtractionResult ChangeSource(Point16 source, int? slot = null) => new(target, path, source, item, slot ?? sourceSlot);
	}
}
