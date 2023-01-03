using System;
using Terraria;

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

		public static void ImportItem(this Chest chest, Item item) {
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
						return;
					} else if (slot.stack < slot.maxStack) {
						item.stack -= slot.maxStack - slot.stack;
						slot.stack = slot.maxStack;
					}
				}
			}
		}
	}
}
