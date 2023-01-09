using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Items;
using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A base implementaiton of <see cref="IMachine"/> and <see cref="IFluidMachine"/>
	/// </summary>
	public abstract class BaseFluidsMachine : ModTileEntity, IMachine, IFluidMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }

		public List<BaseUpgradeItem> Upgrades { get; set; }

		public abstract FluidStorage[] FluidStorage { get; set; }

		public abstract bool CanMergeWithFluidPipe(int pipeX, int pipeY, int machineX, int machineY);

		public virtual bool CanUpgradeApplyTo(BaseUpgrade upgrade, int slot) => true;

		public abstract int SelectFluidExportSource(Point16 pump, Point16 subtile);

		public abstract int SelectFluidImportDestination(Point16 pipe, Point16 subtile);

		public abstract int[] GetInputSlotsForRecipes();

		public abstract int SelectFluidImportDestinationFromType(int fluidType);

		public override void Update() {
			IMachine.Update(this);
			IFluidMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		public override void SaveData(TagCompound tag) {
			IMachine.SaveData(this, tag);
			IFluidMachine.SaveData(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadData(this, tag);
			IFluidMachine.LoadData(this, tag);
		}

		public override void NetSend(BinaryWriter writer) {
			IMachine.NetSend(this, writer);
			IFluidMachine.NetSend(this, writer);
		}

		public override void NetReceive(BinaryReader reader) {
			IMachine.NetReceive(this, reader);
			IFluidMachine.NetReceive(this, reader);
		}
	}
}
