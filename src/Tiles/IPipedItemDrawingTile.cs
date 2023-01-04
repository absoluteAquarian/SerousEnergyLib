namespace SerousEnergyLib.Tiles {
	public interface IPipedItemDrawingTile {
		/// <summary>
		/// Return the max width for an item being drawn in this transport tile here.<br/>
		/// E.g. if this method were to return <c>6f</c>, then an item's sprite would be scaled down to fit within a 6x6 pixel area.
		/// </summary>
		/// <param name="x">The tile X-coordinate of the transport tile</param>
		/// <param name="y">The tile Y-coordinate of the transport tile</param>
		float GetItemSize(int x, int y);
	}
}
