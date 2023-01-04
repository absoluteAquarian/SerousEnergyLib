using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Pathfinding.Nodes {
	public readonly struct CoarseNodePathHeuristic : IPriorityNode<CoarseNodePathHeuristic> {
		public readonly Point16[] path;
		public readonly double travelTime;

		public Point16 Start => path?[0] ?? Point16.Zero;

		public Point16 End => path?[^1] ?? Point16.Zero;

		public CoarseNodePathHeuristic(Point16[] path) {
			this.path = path;
			travelTime = path.Length == 0 ? double.PositiveInfinity : 0;

			for (int i = 0; i < path.Length; i++) {
				Point16 pos = path[i];

				Tile tile = Main.tile[pos.X, pos.Y];

				if (TileLoader.GetTile(tile.TileType) is IItemTransportTile transport)
					travelTime += transport.TransportSpeed;
				else {
					// Path was invalid
					travelTime = double.PositiveInfinity;
					break;
				}
			}
		}

		public CoarseNodePathHeuristic(Point16[] path, double travelTime) {
			this.path = path;
			this.travelTime = travelTime;
		}

		void IPriorityNode<CoarseNodePathHeuristic>.OnNodeUpdate(CoarseNodePathHeuristic existing, ref CoarseNodePathHeuristic replacement) {
			// Assume that the paths connect
			Point16[] totalPath = new Point16[existing.path.Length + replacement.path.Length];
			Array.Copy(existing.path, 0, totalPath, 0, existing.path.Length);
			Array.Copy(replacement.path, 0, totalPath, existing.path.Length, replacement.path.Length);

			CoarseNodePathHeuristic heuristic = new CoarseNodePathHeuristic(totalPath, existing.travelTime + replacement.travelTime);

			replacement = heuristic;
		}
	}

	internal class CoarseNodePathHeuristicComparer : IComparer<CoarseNodePathHeuristic> {
		public static readonly CoarseNodePathHeuristicComparer Instance = new CoarseNodePathHeuristicComparer();

		public int Compare(CoarseNodePathHeuristic x, CoarseNodePathHeuristic y) => x.travelTime.CompareTo(y.travelTime);
	}
}
