using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.Items;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of <see cref="IMachine"/>
	/// </summary>
	public abstract class BaseMachineEntity : ModTileEntity, IMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgradeItem> Upgrades { get; set; }

		public override void Update() {
			IMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);
	}
}
