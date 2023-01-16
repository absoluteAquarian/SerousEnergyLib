using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.Systems {
	partial class NetworkInstance {
		/// <summary>
		/// Returns <see langword="true"/> if this network contains a node at <paramref name="location"/>
		/// </summary>
		/// <param name="location">The tile coordinate</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasEntry(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return nodes.ContainsKey(location);
		}
		
		/// <summary>
		/// Returns <see langword="true"/> if this network contains a node at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasEntry(int x, int y) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return nodes.ContainsKey(new Point16(x, y));
		}

		/// <summary>
		/// Returns <see langword="true"/> if this network has a node adjacent to <paramref name="location"/>
		/// </summary>
		/// <param name="location">The center tile coordinate</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasEntryAdjacentTo(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return nodes.ContainsKey(location + new Point16(-1, 0))
				|| nodes.ContainsKey(location + new Point16(0, -1))
				|| nodes.ContainsKey(location + new Point16(1, 0))
				|| nodes.ContainsKey(location + new Point16(0, 1));
		}

		/// <summary>
		/// Returns <see langword="true"/> if this network has a node adjacent to location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The center tile X-coordinate</param>
		/// <param name="y">The center tile Y-coordinate</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasEntryAdjacentTo(int x, int y) {
			if (disposed)
				throw new ObjectDisposedException("this");

			Point16 location = new Point16(x, y);

			return nodes.ContainsKey(location + new Point16(-1, 0))
				|| nodes.ContainsKey(location + new Point16(0, -1))
				|| nodes.ContainsKey(location + new Point16(1, 0))
				|| nodes.ContainsKey(location + new Point16(0, 1));
		}

		/// <summary>
		/// Attempts to find a node within this network
		/// </summary>
		/// <param name="location">The tile location</param>
		/// <param name="result">The node if it was found, <see langword="default"/> otherwise.</param>
		/// <returns>Whether the node was found</returns>
		/// <exception cref="ObjectDisposedException"/>
		public bool TryGetEntry(Point16 location, out NetworkInstanceNode result) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (nodes.TryGetValue(location, out NetworkInstanceNode value)) {
				result = value;
				return true;
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Whether this network has an entry at <paramref name="location"/> and said entry is a <see cref="NetworkJunction"/> tile
		/// </summary>
		/// <param name="location">The tile location</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasKnownJunction(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return foundJunctions.Contains(location);
		}

		/// <summary>
		/// Whether this network has an entry at location <paramref name="location"/> and said entry is an <see cref="IPumpTile"/> tile
		/// </summary>
		/// <param name="location">The tile location</param>
		/// <param name="direction">The direction of the pump's head if one was found, <see cref="PumpDirection.Left"/> otherwise</param>
		/// <exception cref="ObjectDisposedException"/>
		public bool HasPump(Point16 location, out PumpDirection direction) {
			if (disposed)
				throw new ObjectDisposedException("this");

			direction = PumpDirection.Left;

			if (!HasEntry(location))
				return false;

			Tile tile = Main.tile[location.X, location.Y];
			ref NetworkInfo info = ref tile.Get<NetworkInfo>();
			ref NetworkTaggedInfo tags = ref tile.Get<NetworkTaggedInfo>();

			bool pump = info.IsPump;

			if (pump)
				direction = tags.PumpDirection;

			return pump;
		}
	}
}
