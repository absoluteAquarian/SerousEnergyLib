using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.Items;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of <see cref="IMachine"/> and <see cref="IInventoryMachine"/>
	/// </summary>
	public abstract class BaseInventoryEntity : ModTileEntity, IMachine, IInventoryMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgradeItem> Upgrades { get; set; }

		public Item[] Inventory { get; set; }

		public abstract int DefaultInventoryCapacity { get; }

		public override void Update() {
			IMachine.Update(this);
			IInventoryMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		public virtual bool CanExportItemAtSlot(int slot) => true;

		public virtual bool CanMergeWithItemPipe(int pipeX, int pipeY, int machineX, int machineY) => true;

		public abstract int[] GetExportSlots();

		public abstract int[] GetInputSlots();

		public abstract int[] GetInputSlotsForRecipes();

		public override void SaveData(TagCompound tag) {
			IMachine.SaveData(this, tag);
			IInventoryMachine.SaveInventory(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadData(this, tag);
			IInventoryMachine.LoadInventory(this, tag);
		}

		public override void NetSend(BinaryWriter writer) {
			IMachine.NetSend(this, writer);
			IInventoryMachine.NetSend(this, writer);
		}

		public override void NetReceive(BinaryReader reader) {
			IMachine.NetReceive(this, reader);
			IInventoryMachine.NetReceive(this, reader);
		}
	}
}
