using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store items
	/// </summary>
	public interface IInventoryMachine : IMachine, IPipedItemDrawingTile {
		#pragma warning disable CS1591
		float IPipedItemDrawingTile.GetItemSize(int x, int y) => 3.85f * 2;

		/// <summary>
		/// The items contained within the machine
		/// </summary>
		Item[] Inventory { get; set; }

		/// <summary>
		/// The default capacity of <see cref="Inventory"/><br/>
		/// In <see cref="Update(IInventoryMachine)"/>, if <see cref="Inventory"/> is <see langword="null"/>, it is initialzed to an array of empty items whose length is this property's value
		/// </summary>
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
		public virtual bool CanImportItemAtSlot(Item import, int slot, out int stackImported) {
			stackImported = 0;

			if (import.IsAir)
				return false;

			var inv = Inventory;
			var stack = import.stack;

			Item existing = inv[slot];

			if (existing.IsAir)
				return true;

			if (ItemFunctions.AreStrictlyEqual(import, existing) && existing.stack < existing.maxStack) {
				int diff = existing.maxStack - existing.stack;

				stackImported += Math.Min(diff, stack);

				stack -= diff;

				if (stack <= 0)
					return true;
			}

			return stackImported > 0;
		}

		/// <summary>
		/// Whether <paramref name="import"/> can be imported into this machine's inventory
		/// </summary>
		/// <param name="import">The item to be imported</param>
		/// <param name="stackImported">How many items would be imported should the import be successful</param>
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
		public virtual void ImportItemAtSlot(Item import, int slot) {
			var inv = Inventory;

			Item existing = inv[slot];

			if (existing.IsAir) {
				inv[slot] = import.Clone();
				import.TurnToAir();
				return;
			}

			if (existing.stack + import.stack <= existing.maxStack) {
				existing.stack += import.stack;
				// TODO: Copy the OnStackHooks DMD code from Magic Storage
				import.stack = 0;
			} else if (existing.stack < existing.maxStack) {
				import.stack -= existing.maxStack - existing.stack;
				existing.stack = existing.maxStack;
			}
		}

		public void ImportItem(Item import) {
			if (import.IsAir)
				return;

			var slots = GetInputSlotsOrDefault();

			int capacity = slots.Length;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (CanImportItemAtSlot(import, slot, out _))
					ImportItemAtSlot(import, slot);

				// TODO: netcode message for syncing machine inventory slot?

				if (import.stack <= 0)
					return;
			}
		}

		/// <summary>
		/// Return the slots in <see cref="Inventory"/> which can have items exported from them here, or <see langword="null"/> to indicate that all slots can be exported from.
		/// </summary>
		int[] GetExportSlots();

		public int[] GetExportSlotsOrDefault() => GetExportSlots() ?? Enumerable.Range(0, DefaultInventoryCapacity).ToArray();

		/// <summary>
		/// Whether the given <paramref name="slot"/> in <see cref="Inventory"/> can be exported from
		/// </summary>
		/// <param name="slot">The slot in <see cref="Inventory"/></param>
		bool CanExportItemAtSlot(int slot);

		/// <summary>
		/// Attempt to extract an item at the given <paramref name="slot"/> in <see cref="Inventory"/><br/>
		/// By default, this method acts like extracting items from a chest
		/// </summary>
		/// <param name="network">The network to extract items to</param>
		/// <param name="slot">The slot in <see cref="Inventory"/></param>
		/// <param name="extractCount">The remaining count of items to extract from this machine</param>
		/// <param name="simulation">Whether to actually remove items from this inventory or just simulate the removal.</param>
		/// <param name="result">A valid extraction result if the extraction was successful, <see langword="default"/> otherwise.</param>
		/// <returns>Whether the extraction was successful</returns>
		public virtual bool ExportItemAtSlot(ItemNetwork network, int slot, ref int extractCount, bool simulation, out InventoryExtractionResult result) {
			Item item = Inventory[slot];
			Item import = Inventory[slot].Clone();

			if (network.FindValidImportTarget(import, out Point16 target, out int stackImported)) {
				// There was a valid target
				import.stack = stackImported;
				extractCount -= stackImported;

				if (!simulation) {
					item.stack -= stackImported;

					if (item.stack <= 0)
						item.TurnToAir();
				}

				result = new InventoryExtractionResult(target, import);
				return true;
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Extracts items from this machine into <paramref name="network"/>
		/// </summary>
		/// <param name="network">The item network to import the items into</param>
		/// <param name="extractCount">A counter for how many more items can be extracted from this machine</param>
		/// <param name="simulation">If <see langword="true"/>, items will not be removed from this machine</param>
		/// <returns>A list of extraction results for use in creating <see cref="PipedItem"/> objects</returns>
		public List<InventoryExtractionResult> ExtractItems(ItemNetwork network, ref int extractCount, bool simulation = true) {
			// Attempt to extract items from the machine
			List<InventoryExtractionResult> results = new();

			var slots = GetExportSlotsOrDefault();

			int capacity = slots.Length;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (CanExportItemAtSlot(slot)) {
					int count = extractCount;
					if (!ExportItemAtSlot(network, slot, ref extractCount, simulation, out InventoryExtractionResult result)) {
						extractCount = count;  // Safeguard
						continue;
					}

					results.Add(result);

					if (extractCount <= 0)
						break;
				}
			}

			return results;
		}

		/// <summary>
		/// Returns an enumeration of <see cref="ItemNetwork"/> instances that are adjacent to this machine
		/// </summary>
		public IEnumerable<ItemNetwork> GetAdjacentItemNetworks() {
			return GetAdjacentNetworks(NetworkType.Items)
				.Select(r => r.network as ItemNetwork)
				.OfType<ItemNetwork>();
		}

		/// <summary>
		/// This method ensures that <see cref="Inventory"/> is not <see langword="null"/>.
		/// If it is <see langword="null"/>, then it is initialized to an array of empty items whose length is <see cref="DefaultInventoryCapacity"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
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
