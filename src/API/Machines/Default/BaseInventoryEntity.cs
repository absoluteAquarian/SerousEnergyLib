using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of <see cref="IMachine"/> and <see cref="IInventoryMachine"/>
	/// </summary>
	public abstract class BaseInventoryEntity : ModTileEntity, IMachine, IInventoryMachine {
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgrade> Upgrades { get; set; }

		public Item[] Inventory { get; set; }

		public int DefaultInventoryCapacity { get; }

		public override void Update() {
			IMachine.Update(this);
			IInventoryMachine.Update(this);
		}

		public virtual bool CanExportItemAtSlot(int slot) => true;

		public virtual bool CanMergeWithItemPipe(int pipeX, int pipeY, int machineX, int machineY) => true;

		public abstract int[] GetExportSlots();

		public abstract int[] GetInputSlots();
	}
}
