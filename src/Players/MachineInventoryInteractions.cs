using SerousEnergyLib.API.Machines;
using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Players {
	internal class MachineInventoryInteractions : ModPlayer {
		public override bool ShiftClickSlot(Item[] inventory, int context, int slot) {
			// Slot must be a main inventory slot
			if (!object.ReferenceEquals(inventory, Main.LocalPlayer.inventory) || slot == 58)
				return false;

			// A machine UI must be active
			if (UIHandler.ActiveMachine is not IMachine machine || machine.MachineUI is not BaseMachineUI ui)
				return false;

			Item item = inventory[slot];

			// Ignore empty slots and favorited items
			if (item.favorited || item.IsAir)
				return false;

			int oldType = item.type;
			int oldStack = item.stack;

			if (ui.IsUpgradesPageOpen) {
				// Attempt to deposit an item into the machine's upgrades inventory
				IMachine.AddUpgrade(machine, item.ModItem as BaseUpgradeItem);
			} else if (machine is IInventoryMachine inventoryMachine) {
				// Attempt to deposit the item into the machine's input inventory
				IInventoryMachine.ImportItem(inventoryMachine, item, Point16.NegativeOne);
			}

			if (item.type != oldType || item.stack != oldStack) {
				SoundEngine.PlaySound(SoundID.Grab);
				ui.NeedsToRecalculate = true;
			}

			return true;
		}

		// TODO: add the StoragePlayer.GetItem() method from Magic Storage?
	}
}
