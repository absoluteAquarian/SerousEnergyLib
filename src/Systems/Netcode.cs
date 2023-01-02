using SerousEnergyLib.API;
using SerousEnergyLib.TileData;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	public static class Netcode {
		internal static void HandlePacket(BinaryReader reader, int sender) {
			NetcodeMessage msg = (NetcodeMessage)reader.ReadByte();

			switch (msg) {
				case NetcodeMessage.SyncNetworkDataSingle:
					ReceiveNetworkInfoSync(reader);
					break;
				case NetcodeMessage.RequestNetworkEntryPlacement:
					ReceiveNetworkEntryPlacementRequest(reader);
					break;
				case NetcodeMessage.RequestNetworkEntryRemoval:
					ReceiveNetworkEntryRemovalRequest(reader);
					break;
				case NetcodeMessage.SyncNetworkDataDiamond:
					ReceiveNetworkInfoDiamondSync(reader);
					break;
				case NetcodeMessage.SyncNetwork0_ResetNetwork:
					ReceiveFullNetworkDataSync_0_ResetNetwork(reader);
					break;
				case NetcodeMessage.SyncNetwork1_Nodes:
					ReceiveFullNetworkDataSync_1_Nodes(reader);
					break;
				case NetcodeMessage.SyncNetwork2_CoarsePath:
					ReceiveFullNetworkDataSync_2_CoarsePath(reader);
					break;
				case NetcodeMessage.SyncNetwork3_CoarseInfo:
					ReceiveFullNetworkDataSync_3_CoarseInfo(reader);
					break;
				case NetcodeMessage.SyncNetwork4_Junctions:
					ReceiveFullNetworkDataSync_4_Junctions(reader);
					break;
				case NetcodeMessage.SyncNetwork5_ExtraInfo:
					ReceiveFullNetworkDataSync_5_ExtraInfo(reader);
					break;
				case NetcodeMessage.RequestNetwork:
					ReceiveFullNetworkDataRequest(reader, sender);
					break;
				case NetcodeMessage.RemoveNetwork:
					ReceiveNetworkRemovalResponse(reader);
					break;
				case NetcodeMessage.SyncNetworkInstanceEntryPlacement:
					ReceiveNetworkInstanceEntryPlacementSync(reader);
					break;
				default:
					throw new IOException("Unknown message type: " + msg);
			}
		}

		internal static ModPacket GetPacket(NetcodeMessage msg) {
			ModPacket packet = SerousMachines.Instance.GetPacket();
			packet.Write((byte)msg);
			return packet;
		}

		public static void SyncNetworkInfo(int x, int y) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataSingle);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write(Main.tile[x, y].Get<NetworkInfo>().netData);
			packet.Send();
		}

		private static void ReceiveNetworkInfoSync(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte data = reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Main.tile[x, y].Get<NetworkInfo>().netData = data;
		}

		public static void RequestNetworkEntryPlacement(int x, int y, NetworkType type) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetworkEntryPlacement);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write((byte)type);
			packet.Send();
		}

		private static void ReceiveNetworkEntryPlacementRequest(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			NetworkType type = (NetworkType)reader.ReadByte();

			if (Main.netMode != NetmodeID.Server)
				return;

			Network.PlaceEntry(x, y, type);
		}

		public static void RequestNetworkEntryRemoval(int x, int y, NetworkType type) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetworkEntryRemoval);
			packet.Write((short)x);
			packet.Write((short)y);
			packet.Write((byte)type);
			packet.Send();
		}

		private static void ReceiveNetworkEntryRemovalRequest(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			NetworkType type = (NetworkType)reader.ReadByte();

			if (Main.netMode != NetmodeID.Server)
				return;

			Network.RemoveEntry(x, y, type);
		}

		public static void SyncNetworkInfoDiamond(int x, int y) {
			if (Main.netMode != NetmodeID.Server)
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

		private static void ReceiveNetworkInfoDiamondSync(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte data = reader.ReadByte();
			BitsByte hasTile = reader.ReadByte();
			bool leftExists = false, upExists = false, rightExists = false, downExists = false;
			hasTile.Retrieve(ref leftExists, ref upExists, ref rightExists, ref downExists);

			byte left = 0, up = 0, right = 0, down = 0;

			if (leftExists)
				left = reader.ReadByte();
			if (upExists)
				up = reader.ReadByte();
			if (rightExists)
				right = reader.ReadByte();
			if (downExists)
				down = reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Main.tile[x, y].Get<NetworkInfo>().netData = data;

			if (leftExists)
				Main.tile[x - 1, y].Get<NetworkInfo>().netData = left;
			if (upExists)
				Main.tile[x, y - 1].Get<NetworkInfo>().netData = up;
			if (rightExists)
				Main.tile[x + 1, y].Get<NetworkInfo>().netData = right;
			if (downExists)
				Main.tile[x, y + 1].Get<NetworkInfo>().netData = down;
		}

		public static void SyncFullNetworkData(int id) => SyncFullNetworkDataToClient(id);

		private static void SyncFullNetworkDataToClient(int id, int sender = -1) {
			if (Main.netMode != NetmodeID.Server)
				return;

			NetworkInstance instance = FindInstance(id);
			if (instance is null)
				return;

			instance.SendNetworkData(sender);
		}

		private static void ReceiveFullNetworkDataSync_0_ResetNetwork(BinaryReader reader) {
			int id = reader.ReadInt32();
			byte filter = reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			NetworkInstance instance = FindInstance(id);
			if (instance is null || (byte)instance.Filter != filter) {
				if (instance is not null)
					instance.Reset();
				else {
					NetworkType type = (NetworkType)filter;

					try {
						// CreateNetwork() throws if the filter is invalid
						instance = NetworkInstance.CreateNetwork(type);

						switch (type) {
							case NetworkType.Items:
								Network.itemNetworks.Add(instance);
								break;
							case NetworkType.Fluids:
								Network.fluidNetworks.Add(instance);
								break;
							case NetworkType.Power:
								Network.powerNetworks.Add(instance);
								break;
						}
					} catch (Exception ex) {
						SerousMachines.Instance.Logger.Error("An error was thrown while receiving network sync header", ex);
					}
				}
			}
		}

		private static void ReceiveFullNetworkDataSync_1_Nodes(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			instance?.ReceiveNetworkData_1_Nodes(reader);
		}

		private static void ReceiveFullNetworkDataSync_2_CoarsePath(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			instance?.ReceiveNetworkData_2_CoarsePath(reader);
		}

		private static void ReceiveFullNetworkDataSync_3_CoarseInfo(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			instance?.ReceiveNetworkData_3_CoarseInfo(reader);
		}

		private static void ReceiveFullNetworkDataSync_4_Junctions(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			instance?.ReceiveNetworkData_4_Junctions(reader);
		}

		private static void ReceiveFullNetworkDataSync_5_ExtraInfo(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			instance?.ReceiveNetworkData_5_ExtraInfo(reader);
		}

		internal static void WriteNetworkInstanceToPacket(ModPacket packet, NetworkInstance instance) {
			packet.Write(instance.ID);
			packet.Write((byte)instance.Filter);
		}

		private static NetworkInstance ReadNetworkInstanceOnClient(BinaryReader reader) {
			int id = reader.ReadInt32();
			byte filter = reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return null;

			NetworkInstance instance = FindInstance(id);
			if (instance is null)
				throw new IOException($"A network with ID {id} could not be found");

			if ((byte)instance.Filter != filter)
				throw new IOException($"The network with ID {id} did not match the filter specified in the packet (incoming = {(NetworkType)filter}, existing = {instance.Filter})");

			return instance;
		}

		public static void RequestFullNetworkData(int id) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.RequestNetwork);
			packet.Write(id);
			packet.Send();
		}

		private static void ReceiveFullNetworkDataRequest(BinaryReader reader, int sender) {
			int id = reader.ReadInt32();

			if (Main.netMode != NetmodeID.Server)
				return;

			SyncFullNetworkDataToClient(id, sender);
		}

		internal static void SendNetworkRemoval(int id) {
			if (Main.netMode != NetmodeID.Server)
				return;

			NetworkInstance instance = FindInstance(id);
			if (instance is null)
				return;

			var packet = GetPacket(NetcodeMessage.RemoveNetwork);
			packet.Write(id);
			packet.Send();
		}

		private static void ReceiveNetworkRemovalResponse(BinaryReader reader) {
			int id = reader.ReadInt32();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			NetworkInstance instance = FindInstance(id, out var source, out int index);
			if (instance is null)
				return;
			
			source.RemoveAt(index);
			instance.Dispose();
		}

		private static NetworkInstance FindInstance(int id) {
			foreach (NetworkInstance instance in Network.itemNetworks) {
				if (instance.ID == id)
					return instance;
			}

			foreach (NetworkInstance instance in Network.fluidNetworks) {
				if (instance.ID == id)
					return instance;
			}

			foreach (NetworkInstance instance in Network.powerNetworks) {
				if (instance.ID == id)
					return instance;
			}

			return null;
		}

		private static NetworkInstance FindInstance(int id, out List<NetworkInstance> source, out int indexInSource) {
			source = null;

			indexInSource = -1;
			foreach (NetworkInstance instance in Network.itemNetworks) {
				indexInSource++;

				if (instance.ID == id) {
					source = Network.itemNetworks;
					return instance;
				}
			}

			indexInSource = -1;
			foreach (NetworkInstance instance in Network.fluidNetworks) {
				indexInSource++;

				if (instance.ID == id) {
					source = Network.fluidNetworks;
					return instance;
				}
			}

			indexInSource = -1;
			foreach (NetworkInstance instance in Network.powerNetworks) {
				indexInSource++;

				if (instance.ID == id) {
					source = Network.powerNetworks;
					return instance;
				}
			}

			indexInSource = -1;
			return null;
		}

		internal static void SyncNetworkInstanceEntryPlacement(NetworkInstance instance, Point16 location) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkInstanceEntryPlacement);
			WriteNetworkInstanceToPacket(packet, instance);
			packet.Write(location);
			packet.Send();
		}

		private static void ReceiveNetworkInstanceEntryPlacementSync(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			Point16 location = reader.ReadPoint16();

			instance?.AddEntry(location);
		}
	}

	internal enum NetcodeMessage {
		SyncNetworkDataSingle,
		RequestNetworkEntryPlacement,
		RequestNetworkEntryRemoval,
		SyncNetworkDataDiamond,
		SyncNetwork0_ResetNetwork,
		SyncNetwork1_Nodes,
		SyncNetwork2_CoarsePath,
		SyncNetwork3_CoarseInfo,
		SyncNetwork4_Junctions,
		SyncNetwork5_ExtraInfo,
		RequestNetwork,
		RemoveNetwork,
		SyncNetworkInstanceEntryPlacement
	}
}
