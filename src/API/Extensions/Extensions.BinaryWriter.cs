using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Systems;
using System.IO;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A class containing a collection of extension methods
	/// </summary>
	public static partial class Extensions {
		/// <summary>
		/// Writes a <see cref="Point16"/> value to the current stream and advances the stream position by four bytes
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="point">The <see cref="Point16"/> value to write</param>
		public static void Write(this BinaryWriter writer, Point16 point) {
			writer.Write(point.X);
			writer.Write(point.Y);
		}

		/// <summary>
		/// Writes a <see cref="Vector2"/> value to the current stream and advances the stream position by eight bytes
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="point">The <see cref="Vector2"/> value to write</param>
		public static void Write(this BinaryWriter writer, Vector2 point) {
			writer.Write(point.X);
			writer.Write(point.Y);
		}

		/// <summary>
		/// Writes a <see cref="TerraFlux"/> value to the current stream and advances the stream position by eight bytes
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="flux">The <see cref="TerraFlux"/> value to write</param>
		public static void Write(this BinaryWriter writer, TerraFlux flux) {
			writer.Write((double)flux);
		}

		internal static void Write(this BinaryWriter writer, NetworkInstanceNode node) {
			writer.Write(node.location);
			writer.Write((byte)node.adjacent.Length);

			foreach (var adj in node.adjacent)
				writer.Write(adj);
		}

		internal static void Write(this BinaryWriter writer, CoarseNode coarse) {
			writer.Write((byte)coarse.thresholds.Count);

			foreach (var (edgePos, threshold) in coarse.thresholds) {
				writer.Write(edgePos);
				writer.Write(threshold);
			}
		}

		internal static void Write(this BinaryWriter writer, CoarseNodeThresholdTile threshold) {
			writer.Write(threshold.location);
			writer.Write((byte)threshold.edge);
			writer.Write((byte)threshold.paths.Length);

			foreach (var heuristic in threshold.paths)
				writer.Write(heuristic);
		}

		internal static void Write(this BinaryWriter writer, CoarseNodePathHeuristic heuristic) {
			writer.Write(heuristic.travelTime);
			writer.Write((byte)heuristic.path.Length);

			foreach (var pathNode in heuristic.path)
				writer.Write(pathNode);
		}
	}
}
