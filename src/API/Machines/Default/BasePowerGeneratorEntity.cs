using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Energy.Default;
using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.Items;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A base implementation of <see cref="IMachine"/> and <see cref="IPowerGeneratorMachine"/>
	/// </summary>
	public abstract class BasePowerGeneratorEntity : ModTileEntity, IMachine, IPowerGeneratorMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgradeItem> Upgrades { get; set; }

		public abstract FluxStorage PowerStorage { get; }

		public virtual int EnergyID => SerousMachines.EnergyType<TerraFluxTypeID>();

		public override void Update() {
			IMachine.Update(this);
			IPoweredMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		public virtual bool CanMergeWithWire(int wireX, int wireY, int machineX, int machineY) => true;

		public abstract double GetPowerGeneration(double ticks);
	}
}
