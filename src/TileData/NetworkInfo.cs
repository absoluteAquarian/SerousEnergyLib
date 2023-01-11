using SerousEnergyLib.Tiles;
using Terraria;

namespace SerousEnergyLib.TileData {
	/// <summary>
	/// A structure representing information for a <see cref="BaseNetworkTile"/> tile
	/// </summary>
	public struct NetworkInfo : ITileData {
		/// <summary>
		/// DDDD PTTT<para/>
		/// DDDD = connection flags (0001 = Up, 0010 = Left, 0100 = Right, 1000 = Down)<br/>
		/// P = pump flag<br/>
		/// TTT = pipe/wire type flags (001 = Item, 010 = Fluid, 100 = Power)
		/// </summary>
		public byte netData;

		/// <summary>
		/// The directions that this network tile is connected with.  Due to the nature of <see cref="ConnectionDirection"/>, one tile can be connected to multiple directions at once.
		/// </summary>
		public ConnectionDirection Connections {
			get => (ConnectionDirection)TileDataPacking.Unpack(netData, 4, 4);
			set => netData = (byte)TileDataPacking.Pack((byte)value, netData, 4, 4);
		}

		/// <summary>
		/// Whether this network tile is classified as a pump.  If it is, the pump direction will be derived from <see cref="NetworkTaggedInfo"/>
		/// </summary>
		public bool IsPump {
			get => TileDataPacking.GetBit(netData, 3);
			set => TileDataPacking.SetBit(value, netData, 3);
		}

		/// <summary>
		/// The type of network(s) contained at this tile.  Due to the nature of <see cref="NetworkType"/>, one tile can contain all 3 of the possible network types.
		/// </summary>
		public NetworkType Type {
			get => (NetworkType)TileDataPacking.Unpack(netData, 0, 3);
			set => netData = (byte)TileDataPacking.Pack((byte)value, netData, 0, 3);
		}
	}
}
