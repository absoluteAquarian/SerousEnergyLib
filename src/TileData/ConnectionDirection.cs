using System;

namespace SerousEnergyLib.TileData {
	[Flags]
	public enum ConnectionDirection : byte {
		None = 0x0,
		Up = 0x1,
		Left = 0x2,
		Right = 0x4,
		Down = 0x8
	}
}
