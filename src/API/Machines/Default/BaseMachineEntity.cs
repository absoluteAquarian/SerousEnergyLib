using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of an <see cref="IMachine"/>
	/// </summary>
	public abstract class BaseMachineEntity : ModTileEntity, IMachine {
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		public List<BaseUpgrade> Upgrades { get; set; }

		public override void Update() {
			IMachine.Update(this);
		}
	}
}
