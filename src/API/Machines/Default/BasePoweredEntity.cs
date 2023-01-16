using SerousEnergyLib.API.Energy.Default;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Machines.UI;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using SerousEnergyLib.Items;
using System.IO;
using SerousEnergyLib.API.Upgrades;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A base implementaiton of <see cref="IMachine"/> and <see cref="IPoweredMachine"/>
	/// </summary>
	public abstract class BasePoweredEntity : ModTileEntity, IMachine, IPoweredMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		/// <inheritdoc cref="IMachine.Upgrades"/>
		public List<BaseUpgradeItem> Upgrades { get; set; }

		public abstract FluxStorage PowerStorage { get; }

		/// <inheritdoc cref="IPoweredMachine.EnergyID"/>
		public virtual int EnergyID => SerousMachines.EnergyType<TerraFluxTypeID>();

		/// <summary>
		/// Whether this entity instance is a clone used for item tooltips
		/// </summary>
		public bool IsDummyInstance => ID == -1;

		/// <inheritdoc cref="IMachine.CanUpgradeApply(BaseUpgrade)"/>
		public virtual bool CanUpgradeApply(BaseUpgrade upgrade) => true;

		public override void Update() {
			IMachine.Update(this);
			IPoweredMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		/// <inheritdoc cref="IPoweredMachine.CanMergeWithWire(int, int, int, int)"/>
		public virtual bool CanMergeWithWire(int wireX, int wireY, int machineX, int machineY) => true;

		public abstract double GetPowerConsumption(double ticks);

		public override void SaveData(TagCompound tag) {
			IMachine.SaveData(this, tag);
			IPoweredMachine.SaveData(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadData(this, tag);
			IPoweredMachine.LoadData(this, tag);
		}

		public override void NetSend(BinaryWriter writer) {
			IMachine.NetSend(this, writer);
			IPoweredMachine.NetSend(this, writer);
		}

		public override void NetReceive(BinaryReader reader) {
			IMachine.NetReceive(this, reader);
			IPoweredMachine.NetReceive(this, reader);
		}
	}
}
