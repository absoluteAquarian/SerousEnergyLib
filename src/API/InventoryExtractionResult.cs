using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

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
		/// The tile coordinates of the inventory that this item will pathfind from
		/// </summary>
		public readonly Point16 source;

		/// <summary>
		/// The index in the source inventory that this item was extracted from
		/// </summary>
		public readonly int sourceSlot;

		internal InventoryExtractionResult(Point16 target, Point16 source, Item item, int sourceSlot) {
			this.target = target;
			this.source = source;
			this.item = item;
			this.sourceSlot = sourceSlot;
		}

		internal InventoryExtractionResult ChangeSource(Point16 source, int? slot = null) => new(target, source, item, slot ?? sourceSlot);

		/// <summary>
		/// Forces this extraction result back into the inventory that it originated from
		/// </summary>
		internal void UndoExtraction() {
			if (NetworkHandler.locationToChest.TryGetValue(source, out int chestNum)) {
				ref var slotItem = ref Main.chest[chestNum].item[sourceSlot];

				if (slotItem.IsAir)
					slotItem = item;
				else
					slotItem.stack += item.stack;

				NetMessage.SendData(MessageID.SyncChestItem, number: chestNum, number2: sourceSlot);
			} else if (IMachine.TryFindMachine(source, out IInventoryMachine machine)) {
				ref var slotItem = ref machine.Inventory[sourceSlot];

				if (slotItem.IsAir)
					slotItem = item;
				else
					slotItem.stack += item.stack;

				Netcode.SyncMachineInventorySlot(machine, sourceSlot);
			}
		}
	}
}
