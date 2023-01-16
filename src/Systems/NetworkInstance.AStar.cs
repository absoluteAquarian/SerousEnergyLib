using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Pathfinding;
using Terraria.DataStructures;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria;

namespace SerousEnergyLib.Systems {
	#pragma warning disable CS1591
	partial class NetworkInstance {
		private AStar<CoarseNodeEntry> innerCoarseNodePathfinder;
		internal static bool ignoreTravelTimeWhenPathfinding;

		protected ref Point16 PathfindingStartDirection => ref innerCoarseNodePathfinder.startingDirection;

		private CoarseNodeEntry CreatePathfindingEntry(Point16 location, Point16 headingFrom) {
			double time = 0;
			if (!ignoreTravelTimeWhenPathfinding) {
				if (Filter == NetworkType.Items) {
					time = TileLoader.GetTile(Main.tile[location.X, location.Y].TileType) is IItemTransportTile transport
						? transport.TransportSpeed
						: double.PositiveInfinity;
				}

				// TODO: fluid pathfinding?
			}

			return new CoarseNodeEntry(location) {
				TravelTime = time
			};
		}

		private bool HasPathfindingEntry(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			return tile.HasTile && HasEntry(location);
		}

		internal static bool CanContinuePath(Point16 from, Point16 to) {
			Tile fromTile = Main.tile[from.X, from.Y];
			Tile toTile = Main.tile[to.X, to.Y];

			ref NetworkTaggedInfo fromTags = ref fromTile.Get<NetworkTaggedInfo>();
			ref NetworkTaggedInfo toTags = ref toTile.Get<NetworkTaggedInfo>();

			if (!NetworkTaggedInfo.CanMergeColors(fromTags, toTags))
				return false;

			ref NetworkInfo fromInfo = ref fromTile.Get<NetworkInfo>();
			ref NetworkInfo toInfo = ref toTile.Get<NetworkInfo>();

			// Pumps cannot merge with each other
			if (fromInfo.IsPump && toInfo.IsPump)
				return false;

			if (fromInfo.IsPump)
				return NetworkTaggedInfo.DoesOrientationMatchPumpDirection(to - from, fromTags.PumpDirection);

			if (toInfo.IsPump)
				return NetworkTaggedInfo.DoesOrientationMatchPumpDirection(from - to, toTags.PumpDirection);

			return true;
		}

		private List<Point16> GetWalkableDirections(Point16 center, Point16 previous) {
			if (!nodes.TryGetValue(center, out var node))
				return new List<Point16>();

			Tile tile = Main.tile[center.X, center.Y];
			ModTile modTile = TileLoader.GetTile(tile.TileType);

			if (modTile is NetworkJunction) {
				int mode = tile.TileFrameX / 18;

				if (previous == Point16.NegativeOne) {
					// Pathfinding is not permitted to start at a junction due to its adjacent-ness
					//   being based on the heading direction, which can't be easily determined when
					//   the junction is the starting tile.
					// If the current node is at the edge of a coarse node, then assume that the heading
					//   is into the node, aka the direction can be determined
					Point16 coarse = center / CoarseNode.Coarseness * CoarseNode.Coarseness;
					int coarseDiffX = center.X - coarse.X;
					int coarseDiffY = center.Y - coarse.Y;
					
					// If any of the following are true, the junction is at the edge of a coarse node
					if (coarseDiffX == 0) {
						// Junction is at the left edge
						if (mode == 0)
							return new List<Point16>() { new Point16(-1, 0), new Point16(1, 0) };  // Left -> Right
						else if (mode == 1)
							return new List<Point16>() { new Point16(-1, 0), new Point16(0, 1) };  // Left -> Down
						else if (mode == 2)
							return new List<Point16>() { new Point16(-1, 0), new Point16(0, -1) };  // Left -> Up
					} else if (coarseDiffY == 0) {
						// Junction is at the top edge
						if (mode == 0)
							return new List<Point16>() { new Point16(0, -1), new Point16(0, 1) };  // Up -> Down
						else if (mode == 1)
							return new List<Point16>() { new Point16(0, -1), new Point16(1, 0) };  // Up -> Right
						else if (mode == 2)
							return new List<Point16>() { new Point16(0, -1), new Point16(-1, 0) };  // Up -> Left
					} else if (coarseDiffX == CoarseNode.Stride - 1) {
						// Junction is at the right edge
						if (mode == 0)
							return new List<Point16>() { new Point16(1, 0), new Point16(-1, 0) };  // Right -> Left
						else if (mode == 1)
							return new List<Point16>() { new Point16(1, 0), new Point16(0, -1) };  // Right -> Up
						else if (mode == 2)
							return new List<Point16>() { new Point16(1, 0), new Point16(0, 1) };  // Right -> Down
					} else if (coarseDiffY == CoarseNode.Stride - 1) {
						// Junction is at the bottom edge
						if (mode == 0)
							return new List<Point16>() { new Point16(0, 1), new Point16(0, -1) };  // Down -> Up
						else if (mode == 1)
							return new List<Point16>() { new Point16(0, 1), new Point16(-1, 0) };  // Down -> Left
						else if (mode == 2)
							return new List<Point16>() { new Point16(0, 1), new Point16(1, 0) };  // Down -> Right
					}

					// Failsafe: mode was invalid or tile wasn't at a coarse node's edge
					return new List<Point16>();
				}

				// Only one valid direction is allowed
				Point16 moveDir = center - previous;

				// Failsafe
				if ((moveDir.X == 0 && moveDir.Y == 0) || (moveDir.X != 0 && moveDir.Y != 0))
					return new List<Point16>();

				if (mode == 0)
					return new List<Point16>() { moveDir, new Point16(moveDir.X, moveDir.Y) };   // Left -> Right, Up -> Down
				else if (mode == 1)
					return new List<Point16>() { moveDir, new Point16(moveDir.Y, moveDir.X) };   // Left -> Down, Up -> Right
				else if (mode == 2)
					return new List<Point16>() { moveDir, new Point16(-moveDir.Y, -moveDir.X) }; // Left -> Up, Right -> Down
				
				return new List<Point16>();  // Failsafe
			} else if (modTile is IPumpTile && previous != Point16.NegativeOne) {
				// Pathfinding can only approach the pump from the head, so no more walkable directions should be used
				return new List<Point16>();
			}

			// All four directions are allowed
			Point16 loc = node.location;

			// Convert adjacent absolute to adjacent direction
			return node.adjacent.Select(a => a - loc).ToList();
		}
	}
}
