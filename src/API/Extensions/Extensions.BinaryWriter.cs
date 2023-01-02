using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Systems;
using System.IO;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	public static partial class Extensions {
		public static void Write(this BinaryWriter writer, Point16 point) {
			writer.Write(point.X);
			writer.Write(point.Y);
		}

		public static void Write(this BinaryWriter writer, NetworkInstanceNode node) {
			writer.Write(node.location);
			writer.Write((byte)node.adjacent.Length);

			foreach (var adj in node.adjacent)
				writer.Write(adj);
		}

		public static void Write(this BinaryWriter writer, CoarseNode coarse) {
			writer.Write((byte)coarse.thresholds.Count);

			foreach (var (edgePos, threshold) in coarse.thresholds) {
				writer.Write(edgePos);
				writer.Write(threshold);
			}
		}

		public static void Write(this BinaryWriter writer, CoarseNodeThresholdTile threshold) {
			writer.Write(threshold.location);
			writer.Write((byte)threshold.edge);
			writer.Write((byte)threshold.paths.Length);

			foreach (var heuristic in threshold.paths)
				writer.Write(heuristic);
		}

		public static void Write(this BinaryWriter writer, CoarseNodePathHeuristic heuristic) {
			writer.Write(heuristic.travelTime);
			writer.Write((byte)heuristic.path.Length);

			foreach (var pathNode in heuristic.path)
				writer.Write(pathNode);
		}
	}
}
