using Ionic.Zlib;
using SerousEnergyLib.TileData;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	public static class Netcode {
		internal static void HandlePacket(BinaryReader reader, int sender) {
			NetcodeMessage msg = (NetcodeMessage)reader.ReadByte();

			switch (msg) {
				case NetcodeMessage.SyncNetworkDataSingle:
					ReceiveNetworkInfoSync(reader, sender);
					break;
				case NetcodeMessage.RequestNetworkDataSingle:
					ReceiveNetworkInfoRequest(reader, sender);
					break;
				case NetcodeMessage.SyncNetworkDataArea:
					ReceiveNetworkInfoAreaSync(reader, sender);
					break;
				case NetcodeMessage.RequestNetworkDataArea:
					ReceiveNetworkInfoAreaRequest(reader, sender);
					break;
				default:
					throw new IOException("Unknown message type: " + msg);
			}
		}

		private static ModPacket GetPacket(NetcodeMessage msg) {
			ModPacket packet = SerousMachines.Instance.GetPacket();
			packet.Write((byte)msg);
			return packet;
		}

		public static void SyncNetworkInfo(int x, int y) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataSingle);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write(Main.tile[x, y].Get<NetworkInfo>().netData);
			packet.Send();
		}

		private static void ReceiveNetworkInfoSync(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte data = reader.ReadByte();

			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.tile[x, y].Get<NetworkInfo>().netData = data;
			else {
				// Forward the data to the other clients
				var packet = GetPacket(NetcodeMessage.SyncNetworkDataArea);
				packet.Write(x);
				packet.Write(y);
				packet.Write(data);
				packet.Send(ignoreClient: sender);
			}
		}

		public static void RequestNetworkInfo(int x, int y) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetworkDataSingle);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Send();
		}

		private static void ReceiveNetworkInfoRequest(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();

			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataSingle);
			packet.Write(x);
			packet.Write(y);
			packet.Write(Main.tile[x, y].Get<NetworkInfo>().netData);
			packet.Send(toClient: sender);
		}

		public static void SyncNetworkInfoArea(int x, int y, int width, int height) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataArea);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write((short)width);
			packet.Write((short)height);
			packet.Send();
		}

		private static void ReceiveNetworkInfoAreaSync(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			short width = reader.ReadInt16();
			short height = reader.ReadInt16();

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				DecompressAndWrite(reader, x, y, width, height);
			} else {
				var packet = GetPacket(NetcodeMessage.SyncNetworkDataArea);

				ReadAndCompress(packet, x, y, width, height);
				
				packet.Send(ignoreClient: sender);
			}
		}

		private static void ReadAndCompress(ModPacket packet, int x, int y, int width, int height) {
			byte[] data = new byte[width * height];
			unsafe {
				fixed (byte* fixedPtr = data) {
					byte* ptr = fixedPtr;

					for (int tileY = y; tileY < y + height; tileY++) {
						for (int tileX = x; tileX < x + width; tileX++) {
							*ptr = Main.tile[tileX, tileY].Get<NetworkInfo>().netData;
							ptr++;
						}
					}
				}
			}

			byte[] compressed = IOHelper.Compress(data, CompressionLevel.BestSpeed);

			packet.Write(x);
			packet.Write(y);
			packet.Write(width);
			packet.Write(height);
			packet.Write(compressed.Length);
			packet.Write(compressed);
		}

		private static void DecompressAndWrite(BinaryReader reader, int x, int y, int width, int height) {
			int compressedLength = reader.ReadInt32();

			byte[] decompressed = IOHelper.Decompress(reader.ReadBytes(compressedLength), CompressionLevel.BestSpeed);

			unsafe {
				fixed (byte* fixedPtr = decompressed) {
					byte* ptr = fixedPtr;

					for (int tileY = y; tileY < y + height; tileY++) {
						for (int tileX = x; tileX < x + width; tileX++) {
							Main.tile[tileX, tileY].Get<NetworkInfo>().netData = *ptr;
							ptr++;
						}
					}
				}
			}
		}

		public static void RequestNetworkInfoArea(int x, int y, int width, int height) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetworkDataArea);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write((short)width);
			packet.Write((short)height);
			packet.Send();
		}

		private static void ReceiveNetworkInfoAreaRequest(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			short width = reader.ReadInt16();
			short height = reader.ReadInt16();

			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataArea);

			ReadAndCompress(packet, x, y, width, height);

			packet.Send(toClient: sender);
		}

		public static void SyncNetworkInfoDiamond(int x, int y) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			bool leftExists = x > 0, upExists = y > 0, rightExists = x < Main.maxTilesX - 1, downExists = y < Main.maxTilesY - 1;
			BitsByte hasTile = new BitsByte(leftExists, upExists, rightExists, downExists);

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataDiamond);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write(Main.tile[x, y].Get<NetworkInfo>().netData);
			packet.Write(hasTile);
			if (leftExists)
				packet.Write(Main.tile[x - 1, y].Get<NetworkInfo>().netData);
			if (upExists)
				packet.Write(Main.tile[x, y - 1].Get<NetworkInfo>().netData);
			if (rightExists)
				packet.Write(Main.tile[x + 1, y].Get<NetworkInfo>().netData);
			if (downExists)
				packet.Write(Main.tile[x, y + 1].Get<NetworkInfo>().netData);
			packet.Send();
		}

		private static void ReceiveNetworkInfoDiamondSync(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte data = reader.ReadByte();
			BitsByte hasTile = reader.ReadByte();
			bool leftExists = false, upExists = false, rightExists = false, downExists = false;
			hasTile.Retrieve(ref leftExists, ref upExists, ref rightExists, ref downExists);

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				Main.tile[x, y].Get<NetworkInfo>().netData = data;

				if (leftExists)
					Main.tile[x - 1, y].Get<NetworkInfo>().netData = reader.ReadByte();
				if (upExists)
					Main.tile[x, y - 1].Get<NetworkInfo>().netData = reader.ReadByte();
				if (rightExists)
					Main.tile[x + 1, y].Get<NetworkInfo>().netData = reader.ReadByte();
				if (downExists)
					Main.tile[x, y + 1].Get<NetworkInfo>().netData = reader.ReadByte();
			} else {
				// Forward the data to the other clients
				var packet = GetPacket(NetcodeMessage.SyncNetworkDataDiamond);
				packet.Write(x);
				packet.Write(y);
				packet.Write(data);
				packet.Write(hasTile);
				if (leftExists)
					packet.Write(reader.ReadByte());
				if (upExists)
					packet.Write(reader.ReadByte());
				if (rightExists)
					packet.Write(reader.ReadByte());
				if (downExists)
					packet.Write(reader.ReadByte());
				packet.Send(ignoreClient: sender);
			}
		}

		public static void RequestNetworkInfoDiamond(int x, int y) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetworkDataDiamond);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Send();
		}

		private static void ReceiveNetworkInfoDiamondRequest(BinaryReader reader, int sender) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();

			if (Main.netMode != NetmodeID.Server)
				return;

			bool leftExists = x > 0, upExists = y > 0, rightExists = x < Main.maxTilesX - 1, downExists = y < Main.maxTilesY - 1;
			BitsByte hasTile = new BitsByte(leftExists, upExists, rightExists, downExists);

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataDiamond);
			packet.Write(x);
			packet.Write(y);
			packet.Write(Main.tile[x, y].Get<NetworkInfo>().netData);
			packet.Write(hasTile);
			if (leftExists)
				packet.Write(Main.tile[x - 1, y].Get<NetworkInfo>().netData);
			if (upExists)
				packet.Write(Main.tile[x, y - 1].Get<NetworkInfo>().netData);
			if (rightExists)
				packet.Write(Main.tile[x + 1, y].Get<NetworkInfo>().netData);
			if (downExists)
				packet.Write(Main.tile[x, y + 1].Get<NetworkInfo>().netData);
			packet.Send(toClient: sender);
		}
	}

	internal enum NetcodeMessage {
		SyncNetworkDataSingle,
		RequestNetworkDataSingle,
		SyncNetworkDataArea,
		RequestNetworkDataArea,
		SyncNetworkDataDiamond,
		RequestNetworkDataDiamond
	}
}
