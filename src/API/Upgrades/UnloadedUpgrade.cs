using SerousEnergyLib.API.Machines;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Upgrades {
	[Autoload(false)]
	internal class UnloadedUpgrade : BaseUpgrade {
		public override int MaxUpgradesPerMachine => int.MaxValue;

		public override bool CanApplyTo(IMachine machine) => false;
	}
}
