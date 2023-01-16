using Microsoft.Xna.Framework;
using SerousEnergyLib.API;
using SerousEnergyLib.Pathfinding.Nodes;
using System.IO;
using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.Systems {
	#pragma warning disable CS1591
	partial class NetworkInstance {
		internal void SendNetworkData(int toClient = -1) {
			// Packet #1
			var packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork0_ResetNetwork);
			Netcode.WriteNetworkInstance(packet, this);
			packet.Send(toClient);

			// Packet #2
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork1_Nodes);
			Netcode.WriteNetworkInstance(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(nodes.Count);

				foreach (var (loc, node) in nodes) {
					writer.Write(loc);
					writer.Write(node);
				}

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #3
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork2_CoarsePath);
			Netcode.WriteNetworkInstance(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(coarsePath.Count);

				foreach (var (loc, coarse) in coarsePath) {
					writer.Write(loc);
					writer.Write(coarse);
				}

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #4
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork3_CoarseInfo);
			Netcode.WriteNetworkInstance(packet, this);
			packet.Write(totalCoarsePaths);
			packet.Write((short)coarseLeft);
			packet.Write((short)coarseTop);
			packet.Write((short)coarseRight);
			packet.Write((short)coarseBottom);
			packet.Send(toClient);

			// Packet #5
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork4_Junctions);
			Netcode.WriteNetworkInstance(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(foundJunctions.Count);

				foreach (var junction in foundJunctions)
					writer.Write(junction);

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #6
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork5_ExtraInfo);
			Netcode.WriteNetworkInstance(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				SendExtraData(writer);

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);
		}

		internal void ReceiveNetworkData_1_Nodes(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int nodeCount = decompressedReader.ReadInt32();

			for (int i = 0; i < nodeCount; i++) {
				Point16 loc = decompressedReader.ReadPoint16();
				NetworkInstanceNode node = decompressedReader.ReadNetworkInstanceNode();

				nodes[loc] = node;
			}
		}

		internal void ReceiveNetworkData_2_CoarsePath(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int pathCount = decompressedReader.ReadInt32();

			for (int i = 0; i < pathCount; i++) {
				Point16 loc = decompressedReader.ReadPoint16();
				CoarseNode node = decompressedReader.ReadCoarseNode();

				coarsePath[loc] = node;
			}
		}

		internal void ReceiveNetworkData_3_CoarseInfo(BinaryReader reader) {
			totalCoarsePaths = reader.ReadInt32();
			coarseLeft = reader.ReadInt16();
			coarseTop = reader.ReadInt16();
			coarseRight = reader.ReadInt16();
			coarseBottom = reader.ReadInt16();

			// Request the map sections intersecting the network's area
			int left = coarseLeft * CoarseNode.Stride / 200;
			int top = coarseTop * CoarseNode.Stride / 150;
			int right = coarseRight * CoarseNode.Stride / 200;
			int bottom = coarseBottom * CoarseNode.Stride / 150;

			for (int y = top; y <= bottom; y++) {
				for (int x = left; x <= right; x++)
					RemoteClient.CheckSection(Main.myPlayer, new Vector2(x * 200 + 1, y * 150 + 1));
			}
		}

		internal void ReceiveNetworkData_4_Junctions(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int count = reader.ReadInt32();

			for (int i = 0; i < count; i++) {
				Point16 junction = reader.ReadPoint16();

				foundJunctions.Add(junction);
			}
		}

		internal void ReceiveNetworkData_5_ExtraInfo(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			ReceiveExtraData(decompressedReader);
		}

		/// <summary>
		/// This method is called when a network is going to be synced to a client
		/// </summary>
		/// <param name="writer">The outgoing data stream</param>
		public virtual void SendExtraData(BinaryWriter writer) { }

		/// <summary>
		/// This method is called when a network sync is being received by a client
		/// </summary>
		/// <param name="reader">The incoming data stream</param>
		public virtual void ReceiveExtraData(BinaryReader reader) { }
	}
}
