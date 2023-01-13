﻿using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
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
		/// Return the slots in <see cref="Inventory"/> that can be used in recipes, or <see langword="null"/> to indicate that all slots can be used.
		/// </summary>
		int[] GetInputSlotsForRecipes();

		public int[] GetInputSlotsForRecipesOrDefault() => GetInputSlotsForRecipes() ?? Enumerable.Range(0, Inventory.Length).ToArray();

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

			if (existing.IsAir) {
				stackImported = stack;
				return true;
			}

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
		/// Whether <paramref name="import"/> can be imported into the inventory in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="import">The item to be imported</param>
		/// <param name="stackImported">How many items would be imported should the import be successful</param>
		public static bool CanImportItem(IInventoryMachine machine, Item import, out int stackImported) {
			stackImported = 0;

			var slots = machine.GetInputSlotsOrDefault();

			int capacity = slots.Length;
			int itemStack = import.stack;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (machine.CanImportItemAtSlot(import, slot, out int stack)) {
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
		/// Simulates importing <paramref name="item"/> and the items in <paramref name="network"/> into <paramref name="machine"/> and returns <see langword="true"/> if the import for <paramref name="item"/> would be successful
		/// </summary>
		/// <param name="machine">The machine instance</param>
		/// <param name="network">The item network</param>
		/// <param name="item">The item to import</param>
		/// <param name="stackImported">The quantity of <paramref name="item"/> that was imported into <paramref name="machine"/></param>
		public static bool CheckItemImportPrediction(IInventoryMachine machine, ItemNetwork network, Item item, out int stackImported) {
			stackImported = 0;

			if (item.IsAir)
				return false;

			// Machine must be an entity, otherwise it couldn't be a target for pathfinding anyway
			if (machine is not ModTileEntity entity)
				return false;

			// Make a clone of the machine
			var clone = ModTileEntity.ConstructFromBase(entity) as IInventoryMachine
				?? throw new Exception("Constructed machine object was not an IInventoryMachine");

			var inv = machine.Inventory;
			clone.Inventory = new Item[inv.Length].Populate(index => {
				var slot = inv[index];

				if (slot is null || slot.IsAir)
					return slot;

				return slot.Clone();
			});

			blockSlotSyncing = true;

			// Import the network items
			foreach (var pipedItem in network.items) {
				if (pipedItem is not { Destroyed: false })
					continue;

				if (pipedItem.Target == Point16.NegativeOne)
					continue;

				if (!TryFindMachine(pipedItem.Target, out IInventoryMachine inventory) || inventory is not ModTileEntity target || entity.Position != target.Position)
					continue;

				var import = pipedItem.GetItemClone();

				ImportItem(inventory, item);

				if (!import.IsAir) {
					// Prediction failed -- new item cannot be imported
					return false;
				}
			}

			// Import the actual item
			int oldStack = item.stack;
			ImportItem(clone, item);
			stackImported = oldStack - item.stack;

			blockSlotSyncing = false;

			return stackImported > 0;
		}

		private static bool blockSlotSyncing;

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

			if (blockSlotSyncing)
				Netcode.SyncMachineInventorySlot(this, slot);
		}

		/// <summary>
		/// Attempts to add <paramref name="import"/> to the inventory in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="import">The item to import.  Any leftover stack will be in this parameter after calling this method</param>
		public static void ImportItem(IInventoryMachine machine, Item import) {
			if (import.IsAir)
				return;

			var slots = machine.GetInputSlotsOrDefault();

			int capacity = slots.Length;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (machine.CanImportItemAtSlot(import, slot, out _))
					machine.ImportItemAtSlot(import, slot);

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

					Netcode.SyncMachineInventorySlot(this, slot);
				}

				result = new InventoryExtractionResult(target, Point16.NegativeOne, import, slot);
				return true;
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Extracts items from <paramref name="machine"/> into <paramref name="network"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="network">The item network to import the items into</param>
		/// <param name="inventoryTile">The sub-tile that the extraction is performed from</param>
		/// <param name="extractCount">A counter for how many more items can be extracted from this machine</param>
		/// <param name="simulation">If <see langword="true"/>, items will not be removed from this machine</param>
		/// <returns>A list of extraction results for use in creating <see cref="PipedItem"/> objects</returns>
		public static List<InventoryExtractionResult> ExtractItems(IInventoryMachine machine, ItemNetwork network, Point16 inventoryTile, ref int extractCount, bool simulation = true) {
			// Attempt to extract items from the machine
			List<InventoryExtractionResult> results = new();

			var slots = machine.GetExportSlotsOrDefault();

			int capacity = slots.Length;

			network.ignoredValidTargets.Clear();

			if (machine is ModTileEntity entity && TileLoader.GetTile(Main.tile[entity.Position.X, entity.Position.Y].TileType) is IMachineTile machineTile) {
				machineTile.GetMachineDimensions(out uint width, out uint height);

				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++)
						network.ignoredValidTargets.Add(entity.Position + new Point16(x, y));
				}
			}

			var inv = machine.Inventory;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				if (inv[slot].IsAir)
					continue;

				if (machine.CanExportItemAtSlot(slot)) {
					int count = extractCount;
					if (!machine.ExportItemAtSlot(network, slot, ref extractCount, simulation, out InventoryExtractionResult result)) {
						extractCount = count;  // Safeguard
						continue;
					}

					result = result.ChangeSource(inventoryTile);

					results.Add(result);

					if (extractCount <= 0)
						break;
				}
			}

			network.ignoredValidTargets.Clear();

			return results;
		}

		/// <summary>
		/// Attempts to add <paramref name="item"/> to the export slots of <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="item">The item to insert in the export slots of <paramref name="machine"/>.  Any leftover stack will be in this parameter after calling this method</param>
		protected static void AddItemToExportInventory(IInventoryMachine machine, Item item) {
			var slots = machine.GetExportSlotsOrDefault();

			int capacity = slots.Length;

			for (int i = 0; i < capacity; i++) {
				int slot = slots[i];

				machine.ImportItemAtSlot(item, slot);

				if (item.IsAir)
					return;
			}
		}

		/// <summary>
		/// Returns an enumeration of <see cref="ItemNetwork"/> instances that are adjacent to this machine
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static IEnumerable<ItemNetwork> GetAdjacentItemNetworks(IInventoryMachine machine) {
			return GetAdjacentNetworks(machine, NetworkType.Items)
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

		/// <summary>
		/// Removes the item at index <paramref name="slot"/> within <see cref="Inventory"/> and drops it in the world
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="slot">The index in <see cref="Inventory"/></param>
		/// <param name="quickSpawn">Whether to spawn the item at the local player's center (<see langword="true"/>) or at the machine's center (<see langword="false"/>)</param>
		public static int DropItemInInventory(IInventoryMachine machine, int slot, bool quickSpawn = false) {
			Update(machine);

			var inv = machine.Inventory;
			if (slot < 0 || slot >= inv.Length)
				throw new IndexOutOfRangeException("Inventory slot exceeded the bounds of the IInventoryMachine.Inventory array");

			ref var item = ref inv[slot];

			if (item.IsAir)
				return Main.maxItems;

			// Drop the item
			int drop = Main.maxItems;
			if (quickSpawn)
				drop = Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), item, item.stack);
			else {
				// If the machine is not a tile entity or has no machine tile, just don't spawn an item
				if (machine is TileEntity entity) {
					int x = entity.Position.X;
					int y = entity.Position.Y;

					if (TileLoader.GetTile(Main.tile[x, y].TileType) is IMachineTile machineTile) {
						machineTile.GetMachineDimensions(out uint width, out uint height);

						Vector2 center = new Point16(x, y).ToWorldCoordinates(0, 0) + new Vector2(width * 8, height * 8);

						drop = ItemFunctions.NewClonedItem(new EntitySource_TileEntity(entity), center, item, item.stack);
					}
				}
			}

			// Destroy the slot
			item = new();

			Netcode.SyncMachineInventorySlot(machine, slot);

			return drop;
		}

		public static void SaveInventory(IInventoryMachine machine, TagCompound tag) {
			tag["inventory"] = machine.Inventory.Select(ItemIO.Save).ToList();
		}

		public static void LoadInventory(IInventoryMachine machine, TagCompound tag) {
			machine.Inventory = null;
			Update(machine);

			var inv = machine.Inventory;
			if (tag.GetList<Item>("inventory") is List<Item> items && inv.Length == items.Count) {
				for (int i = 0; i < items.Count; i++)
					inv[i] = items[i];
			}
		}

		public static void NetSend(IInventoryMachine machine, BinaryWriter writer) {
			using (CompressionStream compression = new CompressionStream()) {
				var compressedWriter = compression.writer;

				compressedWriter.Write(machine.Inventory.Length);

				foreach (var item in machine.Inventory)
					ItemIO.Send(item, compressedWriter, writeStack: true, writeFavorite: true);

				compression.WriteToStream(writer);
			}
		}

		public static void NetReceive(IInventoryMachine machine, BinaryReader reader) {
			using (DecompressionStream decompression = new DecompressionStream(reader)) {
				var decompressedReader = decompression.reader;

				int count = decompressedReader.ReadInt32();

				machine.Inventory = new Item[count];

				for (int i = 0; i < count; i++) {
					Item item = ItemIO.Receive(decompressedReader, readStack: true, readFavorite: true);
					machine.Inventory[i] = item;
				}
			}
		}
	}
}
