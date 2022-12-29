using System;

namespace SerousEnergyLib.TileData {
	[Flags]
	public enum NetworkType : byte {
		None = 0x0,
		Items = 0x1,
		Fluids = 0x2,
		Power = 0x4
	}
}
