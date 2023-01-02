using System;
using Terraria;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store items
	/// </summary>
	public interface IInventoryMachine : IMachine {
		/// <summary>
		/// The items contained within the machine
		/// </summary>
		Item[] Inventory { get; protected private set; }

		int DefaultInventoryCapacity { get; }

		/// <summary>
		/// Whether the item pipe at position (<paramref name="pipeX"/>, <paramref name="pipeY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="pipeX">The tile X-coordinate for the item pipe</param>
		/// <param name="pipeY">The tile Y-coordinate for the item pipe</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithItemPipe(int pipeX, int pipeY, int machineX, int machineY);

		// TODO: methods for extracting/inserting items (with a "bool simulation" parameter for item pump usage)

		public static void Update(IInventoryMachine machine) {
			if (machine.Inventory is null) {
				int capacity = machine.DefaultInventoryCapacity;
				machine.Inventory = new Item[capacity];

				for (int i = 0; i < capacity; i++)
					machine.Inventory[i] = new Item();
			}
		}

		public void SaveInventory(TagCompound tag) {
			tag["inventory"] = Inventory;
		}

		public void LoadInventory(TagCompound tag) {
			Inventory = null;
			Update(this);

			if (tag.Get<Item[]>("inventory") is Item[] items && Inventory.Length == items.Length)
				Array.Copy(items, Inventory, items.Length);
		}
	}
}
