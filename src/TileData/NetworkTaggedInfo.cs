using System;
using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.TileData {
	public struct NetworkTaggedInfo : ITileData {
		/// <summary>
		/// --PP CCCC<para/>
		/// PP = pump direction (00 = left, 01 = up, 10 = right, 11 = down)<br/>
		/// CCCC = network color (see: <see cref="NetworkColor"/>)
		/// </summary>
		public byte tagData;

		/// <summary>
		/// If this network tile is a pump (see: <see cref="NetworkInfo.IsPump"/>), this property indicates which direction its head is facing.
		/// </summary>
		public PumpDirection PumpDirection {
			get => (PumpDirection)TileDataPacking.Unpack(tagData, 4, 2);
			set => tagData = (byte)TileDataPacking.Pack((byte)value, tagData, 4, 2);
		}

		/// <summary>
		/// The color of the network tile.  Network tiles with differing colors cannot merge nor pathfind to each other, but can merge to a network tile set to <see cref="NetworkColor.None"/>
		/// </summary>
		public NetworkColor Color {
			get => (NetworkColor)TileDataPacking.Unpack(tagData, 0, 4);
			set => tagData = (byte)TileDataPacking.Pack((byte)value, tagData, 0, 4);
		}

		public static bool CanMergeColors(NetworkTaggedInfo from, NetworkTaggedInfo to) {
			return from.Color == NetworkColor.None || to.Color == NetworkColor.None || from.Color == to.Color;
		}

		public static bool DoesOrientationMatchPumpDirection(Point16 offset, PumpDirection direction) {
			// Offset must match the orientation of the pump's "head"
			return direction switch {
				PumpDirection.Left => offset == new Point16(-1, 0),
				PumpDirection.Up => offset == new Point16(0, -1),
				PumpDirection.Right => offset == new Point16(1, 0),
				PumpDirection.Down => offset == new Point16(0, 1),
				_ => throw new ArgumentOutOfRangeException(nameof(direction))
			};
		}
	}
}
