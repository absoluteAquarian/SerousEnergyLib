using Humanizer;
using SerousEnergyLib.Systems.Networks;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SerousEnergyLib.API {
	partial class Extensions {
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
