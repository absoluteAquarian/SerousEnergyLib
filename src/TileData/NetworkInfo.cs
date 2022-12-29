using Terraria;

namespace SerousEnergyLib.TileData {
	public struct NetworkInfo : ITileData {
		/// <summary>
		/// DDDD -TTT<br/><br/>
		/// DDDD = connection flags (0001 = Up, 0010 = Left, 0100 = Right, 1000 = Down)<br/>
		/// TTT = pipe/wire type flags (001 = Item, 010 = Fluid, 100 = Power)
		/// </summary>
		public byte netData;

		public ConnectionDirection Connections {
			get => (ConnectionDirection)TileDataPacking.Unpack(netData, 4, 4);
			set => netData = (byte)TileDataPacking.Pack((byte)value, netData, 4, 4);
		}

		public NetworkType Type {
			get => (NetworkType)TileDataPacking.Unpack(netData, 0, 3);
			set => netData = (byte)TileDataPacking.Pack((byte)value, netData, 0, 3);
		}
	}
}
