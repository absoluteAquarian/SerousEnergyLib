using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.Systems.Networks;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SerousEnergyLib.API {
	partial class Extensions {
		/// <summary>
		/// Checks if <paramref name="chest"/> can have <paramref name="item"/> imported into it
		/// </summary>
		/// <param name="chest">The chest instance</param>
		/// <param name="item">The item to import</param>
		/// <param name="stackImported">The quantity of <paramref name="item"/> that was imported</param>
		/// <returns>Whether the item could be imported</returns>
		public static bool CanImportItem(this Chest chest, Item item, out int stackImported) {
			stackImported = 0;

			if (item.IsAir)
				return false;

			var inv = chest.item;
			int stack = item.stack;

			for (int i = 0; i < inv.Length; i++) {
				Item slot = inv[i];

				if (slot.IsAir)
					return true;

				if (ItemFunctions.AreStrictlyEqual(slot, item) && slot.stack < slot.maxStack) {
					int diff = slot.maxStack - slot.stack;

					stackImported += Math.Min(diff, stack);

					stack -= diff;

					if (stack <= 0)
						return true;
				}
			}

			return stackImported > 0;
		}

		/// <summary>
		/// Imports <paramref name="item"/> into <paramref name="chest"/>
		/// </summary>
		/// <param name="chest">The chest instance</param>
		/// <param name="chestNum">
		/// The index of <paramref name="chest"/> in <see cref="Main.chest"/><br/>
		/// This parameter is required for netcode to properly send updates of the items in <paramref name="chest"/>
		/// </param>
		/// <param name="item">The item to import</param>
		public static void ImportItem(this Chest chest, int chestNum, Item item) {
			if (item.IsAir)
				return;

			var inv = chest.item;

			for (int i = 0; i < inv.Length; i++) {
				ref Item slot = ref inv[i];

				if (slot.IsAir) {
					slot = item.Clone();
					item.stack = 0;
					return;
				}

				if (ItemFunctions.AreStrictlyEqual(slot, item)) {
					if (slot.stack + item.stack <= slot.maxStack) {
						slot.stack += item.stack;
						// TODO: Copy the OnStackHooks DMD code from Magic Storage
						item.stack = 0;

						NetMessage.SendData(MessageID.SyncChestItem, number: chestNum, number2: i);
						return;
					} else if (slot.stack < slot.maxStack) {
						item.stack -= slot.maxStack - slot.stack;
						slot.stack = slot.maxStack;

						NetMessage.SendData(MessageID.SyncChestItem, number: chestNum, number2: i);
					}
				}
			}
		}

		/// <summary>
		/// Extracts items from <paramref name="chest"/> into <paramref name="network"/>
		/// </summary>
		/// <param name="chest">The chest instance</param>
		/// <param name="chestNum">
		/// The index of <paramref name="chest"/> in <see cref="Main.chest"/><br/>
		/// This parameter is required for netcode to properly send updates of the items in <paramref name="chest"/>
		/// </param>
		/// <param name="network">The item network to import the items into</param>
		/// <param name="extractCount">A counter for how many more items can be extracted from <paramref name="chest"/></param>
		/// <param name="simulation">If <see langword="true"/>, items will not be removed from <paramref name="chest"/></param>
		/// <returns>A list of extraction results for use in creating <see cref="PipedItem"/> objects</returns>
		public static List<InventoryExtractionResult> ExtractItems(this Chest chest, int chestNum, ItemNetwork network, ref int extractCount, bool simulation = true) {
			var inv = chest.item;
			
			// Attempt to extract the items from the chest
			List<InventoryExtractionResult> results = new();

			for (int i = 0; i < inv.Length; i++) {
				Item slot = inv[i];

				if (slot.IsAir)
					continue;

				Item import = slot.Clone();
				if (import.stack > extractCount)
					import.stack = extractCount;

				if (network.FindValidImportTarget(import, out Point16 target, out int stackImported)) {
					// There was a valid target
					import.stack = stackImported;
					results.Add(new InventoryExtractionResult(target, import));
					extractCount -= stackImported;

					if (!simulation) {
						slot.stack -= stackImported;

						if (slot.stack <= 0)
							slot.TurnToAir();

						NetMessage.SendData(MessageID.SyncChestItem, number: chestNum, number2: i);
					}

					if (extractCount <= 0)
						break;
				}
			}

			return results;
		}
	}
}
