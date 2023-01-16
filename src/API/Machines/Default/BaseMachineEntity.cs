using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Items;
using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of <see cref="IMachine"/>
	/// </summary>
	public abstract class BaseMachineEntity : ModTileEntity, IMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgradeItem> Upgrades { get; set; }

		/// <summary>
		/// Whether this entity instance is a clone used for item tooltips
		/// </summary>
		public bool IsDummyInstance => ID == -1;

		/// <inheritdoc cref="IMachine.CanUpgradeApply(BaseUpgrade)"/>
		public virtual bool CanUpgradeApply(BaseUpgrade upgrade) => true;

		public override void Update() {
			IMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		public override void SaveData(TagCompound tag) {
			IMachine.SaveData(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadData(this, tag);
		}

		public override void NetSend(BinaryWriter writer) {
			IMachine.NetSend(this, writer);
		}

		public override void NetReceive(BinaryReader reader) {
			IMachine.NetReceive(this, reader);
		}
	}
}
