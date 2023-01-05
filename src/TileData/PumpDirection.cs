using SerousEnergyLib.Tiles;

namespace SerousEnergyLib.TileData {
	/// <summary>
	/// A set of indicators for the current direction of an <see cref="IPumpTile"/> tile
	/// </summary>
	public enum PumpDirection : byte {
		/// <summary>
		/// The pump's head is facing left
		/// </summary>
		Left = 0,
		/// <summary>
		/// The pump's head is facing up
		/// </summary>
		Up,
		/// <summary>
		/// The pump's head is facing right
		/// </summary>
		Right,
		/// <summary>
		/// The pump's head is facing down
		/// </summary>
		Down
	}
}
