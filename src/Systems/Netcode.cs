﻿using SerousEnergyLib.API;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	/// <summary>
	/// The central class for all netcode logic in this mod
	/// </summary>
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
				case NetcodeMessage.SyncPipedItem:
					ReceivePipedItemSync(reader);
					break;
				case NetcodeMessage.SyncPump:
					ReceivePumpTimerSync(reader);
					break;
				case NetcodeMessage.SyncMachinePlacement:
					ReceiveMachinePlacementSync(reader, sender);
					break;
				case NetcodeMessage.SyncMachineRemoval:
					ReceiveMachineRemovalSync(reader, sender);
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

		/// <summary>
		/// Syncs the <see cref="NetworkInfo"/> and <see cref="NetworkTaggedInfo"/> at a tile location
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void SyncNetworkInfo(int x, int y) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataSingle);
			packet.Write((short)x);
			packet.Write((short)y);
			Tile tile = Main.tile[x, y];
			packet.Write(tile.Get<NetworkInfo>().netData);
			packet.Write(tile.Get<NetworkTaggedInfo>().tagData);
			packet.Send();
		}

		private static void ReceiveNetworkInfoSync(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte infoData = reader.ReadByte();
			byte tagData = reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Tile tile = Main.tile[x, y];
			tile.Get<NetworkInfo>().netData = infoData;
			tile.Get<NetworkTaggedInfo>().tagData = tagData;
		}

		/// <summary>
		/// Sends a request to the server for placing a tile that can be be assigned to networks for items, fluids and/or power
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="type"></param>
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

		/// <summary>
		/// Sends a request to the server for removing a tile that can be assigned to networks for items, fluids and/or power
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="type"></param>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void SyncNetworkInfoDiamond(int x, int y) {
			if (Main.netMode != NetmodeID.Server)
				return;

			bool leftExists = x > 0, upExists = y > 0, rightExists = x < Main.maxTilesX - 1, downExists = y < Main.maxTilesY - 1;
			BitsByte hasTile = new BitsByte(leftExists, upExists, rightExists, downExists);

			var packet = GetPacket(NetcodeMessage.SyncNetworkDataDiamond);
			packet.Write((short)x);
			packet.Write((short)y);
			Tile center = Main.tile[x, y];
			packet.Write(center.Get<NetworkInfo>().netData);
			packet.Write(center.Get<NetworkTaggedInfo>().tagData);
			packet.Write(hasTile);
			if (leftExists) {
				Tile left = Main.tile[x - 1, y];
				packet.Write(left.Get<NetworkInfo>().netData);
				packet.Write(left.Get<NetworkTaggedInfo>().tagData);
			}
			if (upExists) {
				Tile up = Main.tile[x, y - 1];
				packet.Write(up.Get<NetworkInfo>().netData);
				packet.Write(up.Get<NetworkTaggedInfo>().tagData);
			}
			if (rightExists) {
				Tile right = Main.tile[x + 1, y];
				packet.Write(right.Get<NetworkInfo>().netData);
				packet.Write(right.Get<NetworkTaggedInfo>().tagData);
			}
			if (downExists) {
				Tile down = Main.tile[x, y + 1];
				packet.Write(down.Get<NetworkInfo>().netData);
				packet.Write(down.Get<NetworkTaggedInfo>().tagData);
			}
			packet.Send();
		}

		private static void ReceiveNetworkInfoDiamondSync(BinaryReader reader) {
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			byte data = reader.ReadByte();
			BitsByte hasTile = reader.ReadByte();
			bool leftExists = false, upExists = false, rightExists = false, downExists = false;
			hasTile.Retrieve(ref leftExists, ref upExists, ref rightExists, ref downExists);

			byte leftInfo = 0, upInfo = 0, rightInfo = 0, downInfo = 0;
			byte leftTags = 0, upTags = 0, rightTags = 0, downTags = 0;

			if (leftExists) {
				leftInfo = reader.ReadByte();
				leftTags = reader.ReadByte();
			}
			if (upExists) {
				upInfo = reader.ReadByte();
				upTags = reader.ReadByte();
			}
			if (rightExists) {
				rightInfo = reader.ReadByte();
				rightTags = reader.ReadByte();
			}
			if (downExists) {
				downInfo = reader.ReadByte();
				downTags = reader.ReadByte();
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Tile center = Main.tile[x, y];
			center.Get<NetworkInfo>().netData = data;

			if (leftExists) {
				Tile left = Main.tile[x - 1, y];
				left.Get<NetworkInfo>().netData = leftInfo;
				left.Get<NetworkTaggedInfo>().tagData = leftTags;
			}
			if (upExists) {
				Tile up = Main.tile[x, y - 1];
				up.Get<NetworkInfo>().netData = upInfo;
				up.Get<NetworkTaggedInfo>().tagData = upTags;
			}
			if (rightExists) {
				Tile right = Main.tile[x + 1, y];
				right.Get<NetworkInfo>().netData = rightInfo;
				right.Get<NetworkTaggedInfo>().tagData = rightTags;
			}
			if (downExists) {
				Tile down = Main.tile[x, y + 1];
				down.Get<NetworkInfo>().netData = downInfo;
				down.Get<NetworkTaggedInfo>().tagData = downTags;
			}
		}

		/// <summary>
		/// Syncs a <see cref="NetworkInstance"/> instance's data to all clients
		/// </summary>
		/// <param name="id">The <see cref="NetworkInstance.ID"/> of the network to sync</param>
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

		internal static void WriteNetworkInstance(BinaryWriter writer, NetworkInstance instance) {
			writer.Write(instance.ID);
			writer.Write((byte)instance.Filter);
		}

		internal static NetworkInstance ReadNetworkInstanceOnClient(BinaryReader reader) {
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

		/// <summary>
		/// Sends a requst to the server for the data of a <see cref="NetworkInstance"/> instance
		/// </summary>
		/// <param name="id">The <see cref="NetworkInstance.ID"/> of the network to sync</param>
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
			WriteNetworkInstance(packet, instance);
			packet.Write(location);
			packet.Send();
		}

		private static void ReceiveNetworkInstanceEntryPlacementSync(BinaryReader reader) {
			NetworkInstance instance = ReadNetworkInstanceOnClient(reader);

			Point16 location = reader.ReadPoint16();

			instance?.AddEntry(location);
		}

		internal static void SyncPipedItem(PipedItem item, bool fullSync) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncPipedItem);
			item.WriteTo(packet, fullSync);
			packet.Send();
		}

		private static void ReceivePipedItemSync(BinaryReader reader) {
			PipedItem.CreateOrUpdateFromNet(reader);
		}

		internal static void SyncPumpTimer(NetworkInstance net, Point16 location, int timer) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncPump);
			WriteNetworkInstance(packet, net);
			packet.Write(location);
			packet.Write((short)timer);
			packet.Send();
		}

		private static void ReceivePumpTimerSync(BinaryReader reader) {
			NetworkInstance net = ReadNetworkInstanceOnClient(reader);

			Point16 location = reader.ReadPoint16();
			int timer = reader.ReadInt16();

			if (net is ItemNetwork itemNet)
				itemNet.AddPumpTimer(location, timer);
		}

		internal static void SyncMachinePlacement(int type, Point16 location) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = GetPacket(NetcodeMessage.SyncMachinePlacement);
			packet.Write(type);
			packet.Write(location);
			packet.Send();
		}

		private static void ReceiveMachinePlacementSync(BinaryReader reader, int sender) {
			int type = reader.ReadInt32();
			Point16 location = reader.ReadPoint16();

			if (!TileEntity.manager.TryGetTileEntity(type, out ModTileEntity entity) || entity is not IMachine machine)
				throw new IOException($"Tile entity ID {type} did not correspond with a valid machine type");

			machine.AddToAdjacentNetworks();

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachinePlacement);
				packet.Write(type);
				packet.Write(location);
				packet.Send(ignoreClient: sender);
			}
		}

		internal static void SyncMachineRemoval(int type, Point16 location) {
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			var packet = GetPacket(NetcodeMessage.SyncMachineRemoval);
			packet.Write(type);
			packet.Write(location);
			packet.Send();
		}

		private static void ReceiveMachineRemovalSync(BinaryReader reader, int sender) {
			int type = reader.ReadInt32();
			Point16 location = reader.ReadPoint16();

			if (!TileEntity.manager.TryGetTileEntity(type, out ModTileEntity entity) || entity is not IMachine machine)
				throw new IOException($"Tile entity ID {type} did not correspond with a valid machine type");

			machine.RemoveFromAdjacentNetworks();

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachineRemoval);
				packet.Write(type);
				packet.Write(location);
				packet.Send(ignoreClient: sender);
			}
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
		SyncNetworkInstanceEntryPlacement,
		SyncPipedItem,
		SyncPump,
		SyncMachinePlacement,
		SyncMachineRemoval
	}
}
