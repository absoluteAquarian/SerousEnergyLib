using SerousEnergyLib.TileData;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a pump tile of any kind
	/// </summary>
	public interface IPumpTile {
		/// <summary>
		/// Return the pump direction of a placed <see cref="IPumpTile"/> tile here
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		PumpDirection GetDirection(int x, int y);

		/// <summary>
		/// Return the value this pump's timer should reset to here
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>The duration of the timer, measured in game ticks</returns>
		int GetMaxTimer(int x, int y);
	}
}
