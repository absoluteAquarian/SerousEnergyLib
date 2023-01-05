using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using System;

namespace SerousEnergyLib.TileData {
	/// <summary>
	/// A set of indicators for what kind of <see cref="NetworkInstance"/> network a tile can belong to
	/// </summary>
	[Flags]
	public enum NetworkType : byte {
		/// <summary>
		/// The tile does not belong to any network
		/// </summary>
		None = 0x0,
		/// <summary>
		/// The tile can exist in <see cref="ItemNetwork"/> networks
		/// </summary>
		Items = 0x1,
		/// <summary>
		/// The tile can exist in <see cref="FluidNetwork"/> networks
		/// </summary>
		Fluids = 0x2,
		/// <summary>
		/// The tile can exist in <see cref="PowerNetwork"/> networks
		/// </summary>
		Power = 0x4
	}
}
