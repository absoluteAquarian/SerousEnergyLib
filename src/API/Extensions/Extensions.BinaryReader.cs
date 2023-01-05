using SerousEnergyLib.API.Energy;
using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using System.IO;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	partial class Extensions {
		/// <summary>
		/// Reads two <see langword="short"/> values from <paramref name="reader"/> and advances the current position of the stream by four bytes
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="Point16"/> value read from the stream</returns>
		public static Point16 ReadPoint16(this BinaryReader reader) {
			return new Point16(reader.ReadInt16(), reader.ReadInt16());
		}

		/// <summary>
		/// Reads one <see langword="double"/> values from <paramref name="reader"/> and advances the current position of the stream by eight bytes
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="TerraFlux"/> value read from the stream</returns>
		public static TerraFlux ReadFlux(this BinaryReader reader) {
			return new TerraFlux(reader.ReadDouble());
		}

		internal static NetworkInstanceNode ReadNetworkInstanceNode(this BinaryReader reader) {
			Point16 location = reader.ReadPoint16();

			byte adjacentLength = reader.ReadByte();
			Point16[] adjacent = new Point16[adjacentLength];

			for (int i = 0; i < adjacentLength; i++)
				adjacent[i] = reader.ReadPoint16();

			return new NetworkInstanceNode(location, adjacent);
		}

		internal static CoarseNode ReadCoarseNode(this BinaryReader reader) {
			byte thresholdCount = reader.ReadByte();

			CoarseNode node = new CoarseNode();
			
			for (int i = 0; i < thresholdCount; i++) {
				Point16 edgePos = reader.ReadPoint16();
				CoarseNodeThresholdTile threshold = reader.ReadeCoarseNodeThresholdTile();

				node.thresholds.Add(edgePos, threshold);
			}

			return node;
		}

		internal static CoarseNodeThresholdTile ReadeCoarseNodeThresholdTile(this BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			ConnectionDirection edge = (ConnectionDirection)reader.ReadByte();

			if (edge != ConnectionDirection.Left && edge != ConnectionDirection.Up && edge != ConnectionDirection.Right && edge != ConnectionDirection.Down)
				throw new IOException("Threshold tile had an invalid direction: " + edge);

			byte pathCount = reader.ReadByte();
			CoarseNodePathHeuristic[] paths = new CoarseNodePathHeuristic[pathCount];

			for (int i = 0; i < pathCount; i++)
				paths[i] = reader.ReadCoarseNodePathHeuristic();

			return new CoarseNodeThresholdTile(location, edge) { paths = paths };
		}

		internal static CoarseNodePathHeuristic ReadCoarseNodePathHeuristic(this BinaryReader reader) {
			double time = reader.ReadDouble();
			byte pathLength = reader.ReadByte();

			Point16[] path = new Point16[pathLength];

			for (int i = 0; i < pathLength; i++)
				path[i] = reader.ReadPoint16();

			return new CoarseNodePathHeuristic(path, time);
		}
	}
}
