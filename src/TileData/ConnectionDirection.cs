using SerousEnergyLib.Tiles;
using System;

namespace SerousEnergyLib.TileData {
	/// <summary>
	/// A set of indicators representing connection directions in a <see cref="BaseNetworkTile"/> tile
	/// </summary>
	[Flags]
	public enum ConnectionDirection : byte {
		/// <summary>
		/// No connections
		/// </summary>
		None = 0x0,
		/// <summary>
		/// The tile is connected to the tlie above it
		/// </summary>
		Up = 0x1,
		/// <summary>
		/// The tile is connected to the tile to the left of it
		/// </summary>
		Left = 0x2,
		/// <summary>
		/// The tile is connected to the tile to the right of it
		/// </summary>
		Right = 0x4,
		/// <summary>
		/// The tile is connected to the tile below it
		/// </summary>
		Down = 0x8
	}
}
