using SerousEnergyLib.API.Energy.Default;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Machines.UI;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using SerousEnergyLib.Items;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A base implementaiton of <see cref="IMachine"/> and <see cref="IPoweredMachine"/>
	/// </summary>
	public abstract class BasePoweredEntity : ModTileEntity, IMachine, IPoweredMachine {
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

		public abstract double GetPowerConsumption(double ticks);

		public override void SaveData(TagCompound tag) {
			IMachine.SaveUpgrades(this, tag);
			IPoweredMachine.SavePower(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadUpgrades(this, tag);
			IPoweredMachine.LoadPower(this, tag);
		}
	}
}
