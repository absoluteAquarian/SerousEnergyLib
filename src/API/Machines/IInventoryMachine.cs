using System;
using System.Linq;
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

		/// <summary>
		/// Return the slots in <see cref="Inventory"/> which can have items imported into them here, or <see langword="null"/> to indicate that all slots can be imported into.
		/// </summary>
		int[] GetInputSlots();

		public int[] GetInputSlotsOrDefault() => GetInputSlots() ?? Enumerable.Range(0, DefaultInventoryCapacity).ToArray();

		/// <summary>
		/// Whether <paramref name="import"/> can be inserted in this machine's inventory at the given <paramref name="slot"/> in <see cref="Inventory"/>
		/// </summary>
		/// <param name="import">The item to be imported</param>
		/// <param name="slot">The slot in <see cref="Inventory"/></param>
		/// <param name="stackImported">How many items would be imported should the import be successful</param>
		/// <returns>Whether <paramref name="import"/> can be imported into this machine</returns>
		bool CanImportItemAtSlot(Item import, int slot, out int stackImported);

		public bool CanImportItem(Item import, out int stackImported) {
			stackImported = 0;

			var slots = GetInputSlotsOrDefault();

			int capacity = slots.Length;
			int itemStack = import.stack;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (CanImportItemAtSlot(import, slot, out int stack)) {
					stackImported += stack;
					import.stack -= stack;

					if (import.stack <= 0)
						break;
				}
			}

			import.stack = itemStack;

			return stackImported > 0;
		}

		/// <summary>
		/// Attempt to add <paramref name="import"/> to this machine's <see cref="Inventory"/> here.<br/>
		/// If any part of <paramref name="import"/>'s stack is to be sent back to the network, indicate as such by making its stack positive.
		/// </summary>
		/// <param name="import">The item to import</param>
		/// <param name="slot">The slot in <see cref="Inventory"/></param>
		void ImportItemAtSlot(Item import, int slot);

		public void ImportItem(Item import) {
			var slots = GetInputSlotsOrDefault();

			int capacity = slots.Length;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				ImportItemAtSlot(import, slot);

				if (import.stack <= 0)
					return;
			}
		}

		/// <summary>
		/// Return the slots in <see cref="Inventory"/> which can have items exported from them here, or <see langword="null"/> to indicate that all slots can be exported from.
		/// </summary>
		int[] GetExportSlots();

		public int[] GetExportSlotsOrDefault() => GetExportSlots() ?? Enumerable.Range(0, DefaultInventoryCapacity).ToArray();

		// TODO: item pump methods for checking if an item can be exported

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
