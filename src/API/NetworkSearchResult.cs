using SerousEnergyLib.Systems;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A struct representing a search result for an adjacent network
	/// </summary>
	public readonly struct NetworkSearchResult {
		/// <summary>
		/// The adjacent network instance
		/// </summary>
		public readonly NetworkInstance network;

		/// <summary>
		/// The tile in the network that was adjacent to <see cref="machineTileAdjacentToNetwork"/>
		/// </summary>
		public readonly Point16 tileInNetwork;

		/// <summary>
		/// The tile within this machine that is adjacent to the network
		/// </summary>
		public readonly Point16 machineTileAdjacentToNetwork;

		internal NetworkSearchResult(NetworkInstance instance, Point16 location, Point16 adjacentLocation) {
			network = instance;
			tileInNetwork = location;
			machineTileAdjacentToNetwork = adjacentLocation;
		}
	}
}
