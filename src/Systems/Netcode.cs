using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using SerousEnergyLib.API;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.API.Sounds;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
				case NetcodeMessage.SyncMachineInventorySlot:
					ReceiveMachineInventorySlotSync(reader, sender);
					break;
				case NetcodeMessage.SyncMachineUpgrades:
					RecieveMachineUpgradesSync(reader, sender);
					break;
				case NetcodeMessage.SyncFullTileEntityData:
					ReceiveTileEntitySync(reader, sender);
					break;
				case NetcodeMessage.SendSoundPlay:
					ReceiveSoundPlayingPacket(reader, out _);
					break;
				case NetcodeMessage.SendSoundPlayWithEmitter:
					ReceiveSoundPlayingPacketWithEmitter(reader);
					break;
				case NetcodeMessage.SendSoundStop:
					ReceiveSoundStopPacket(reader);
					break;
				case NetcodeMessage.SendSoundUpdate:
					ReceiveSoundUpdatePacket(reader);
					break;
				case NetcodeMessage.SyncReducedMachineData:
					ReceiveReducedData(reader);
					break;
				case NetcodeMessage.SyncMachinePowerStorage:
					ReceiveMachinePowerStorageSync(reader, sender);
					break;
				case NetcodeMessage.SyncMachineFluidStorageSlot:
					ReceiveMachineFluidStorageSlotSync(reader, sender);
					break;
				case NetcodeMessage.SyncNetworkFluidStorage:
					ReceiveNetworkFluidStorageSync(reader);
					break;
				case NetcodeMessage.SyncNetworkPowerStorage:
					ReceiveNetworkPowerStorageSync(reader);
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
				return;  // Machine does not exist

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
				return;  // Machine does not exist

			machine.RemoveFromAdjacentNetworks();

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachineRemoval);
				packet.Write(type);
				packet.Write(location);
				packet.Send(ignoreClient: sender);
			}
		}

		/// <summary>
		/// Syncs the inventory <paramref name="slot"/> in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process.  Must refer to a <see cref="ModTileEntity"/> instance or an error will be thrown</param>
		/// <param name="slot">The slot in the machine's inventory</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachineInventorySlot(IInventoryMachine machine, int slot) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("Machine was not a ModTileEntity", nameof(machine));

			var inv = machine.Inventory;
			if (slot < 0 || slot >= inv.Length)
				return;

			inv[slot] ??= new();

			SyncMachineInventorySlot_DoSync(entity.Position, slot, inv[slot]);
		}

		/// <summary>
		/// Syncs the inventory <paramref name="slot"/> in the machine entity at the provided <paramref name="location"/>
		/// </summary>
		/// <param name="location">The tile coordinates of othe machine.  Must refer to a <see cref="ModTileEntity"/> and <see cref="IInventoryMachine"/> instance or an error will be thrown</param>
		/// <param name="slot">The slot in the machine's inventory</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachineInventorySlot(Point16 location, int slot) {
			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not ModTileEntity || entity is not IInventoryMachine machine)
				throw new ArgumentException($"Tile entity at location (X: {location.X}, Y: {location.Y}) did not have a valid machine type", nameof(location));

			var inv = machine.Inventory;
			if (slot < 0 || slot >= inv.Length)
				return;

			inv[slot] ??= new();

			SyncMachineInventorySlot_DoSync(location, slot, inv[slot]);
		}

		private static void SyncMachineInventorySlot_DoSync(Point16 location, int slot, Item item) {
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			var packet = GetPacket(NetcodeMessage.SyncMachineInventorySlot);
			packet.Write(location);
			packet.Write((short)slot);
			ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
			packet.Send();
		}

		private static void ReceiveMachineInventorySlotSync(BinaryReader reader, int sender) {
			Point16 location = reader.ReadPoint16();
			short slot = reader.ReadInt16();
			Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not IInventoryMachine machine)
				return;  // Machine does not exist

			machine.Inventory[slot] = item;

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachineInventorySlot);
				packet.Write(location);
				packet.Write(slot);
				ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
				packet.Send(ignoreClient: sender);
			}
		}

		/// <summary>
		/// Syncs the upgrades within <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachineUpgrades(IMachine machine) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("IMachine instance was not a ModTileEntity", nameof(machine));

			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			SyncMachineUpgrades_SendPacket(machine, entity.Position, Main.netMode == NetmodeID.MultiplayerClient ? Main.myPlayer : -1);
		}

		private static void SyncMachineUpgrades_SendPacket(IMachine machine, Point16 location, int sender = -1) {
			var packet = GetPacket(NetcodeMessage.SyncMachineUpgrades);
			packet.Write(location);

			IMachine.NetSend(machine, packet);

			packet.Send(ignoreClient: sender);
		}

		private static void RecieveMachineUpgradesSync(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not ModTileEntity || te is not IMachine machine)
				throw new IOException("Position was either invalid or did not refer to a valid IMachine instance");

			IMachine.NetReceive(machine, reader);

			if (Main.netMode == NetmodeID.Server)
				SyncMachineUpgrades_SendPacket(machine, position, sender);
		}

		/// <summary>
		/// Syncs a tile entity from a client to the server via <see cref="TileEntity.Write(BinaryWriter, TileEntity, bool, bool)"/>
		/// </summary>
		/// <param name="location">The tile coordinates of the tile entity</param>
		public static void SyncTileEntity(Point16 location) {
			if (Main.netMode != NetmodeID.MultiplayerClient || !TileEntity.ByPosition.TryGetValue(location, out TileEntity te))
				return;

			var packet = GetPacket(NetcodeMessage.SyncFullTileEntityData);
			packet.Write(location);
			
			using (CompressionStream compression = new CompressionStream()) {
				TileEntity.Write(compression.writer, te, networkSend: true);

				compression.WriteToStream(packet);
			}

			packet.Send();
		}

		private static void ReceiveTileEntitySync(BinaryReader reader, int sender) {
			Point16 location = reader.ReadPoint16();

			if (Main.netMode != NetmodeID.Server)
				return;

			using (DecompressionStream decompression = new DecompressionStream(reader)) {
				TileEntity te = TileEntity.Read(decompression.reader, networkSend: true);

				te.Position = location;

				TileEntity.ByID[te.ID] = te;
				TileEntity.ByPosition[te.Position] = te;

				NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, null, te.ID, te.Position.X, te.Position.Y);
			}
		}

		/// <summary>
		/// Sends a packet from the server to all clients telling them to play a sound
		/// </summary>
		/// <param name="data">The base instance to retrieve data from</param>
		/// <param name="mode">A set of flags used to specify what data from <paramref name="data"/> is sent</param>
		/// <param name="source">
		/// The location of the sound in the world.<br/>
		/// This argument is only used when <paramref name="mode"/> has <see cref="NetcodeSoundMode.SendPosition"/> set.<br/>
		/// If that is not the case, this argument is ignored and the sound is treated as a "directionless" sound
		/// </param>
		/// <param name="allowClientsideSoundMuting">Whether clients should be able to mute the sound while their game window is inactive</param>
		public static void SendSoundToClients(in SoundStyle data, NetcodeSoundMode mode, Vector2? source = null, bool allowClientsideSoundMuting = true) {
			if (Main.netMode != NetmodeID.Server)
				return;

			int id = MachineSounds.GetID(data);
			if (id < 0)
				throw new ArgumentException("Data instance did not have a registered ID", nameof(data));

			var packet = GetPacket(NetcodeMessage.SendSoundPlay);
			
			SendSoundInformationToPacket(packet, id, data, mode, source, allowClientsideSoundMuting);

			packet.Send();
		}

		private static void SendSoundInformationToPacket(ModPacket packet, int id, in SoundStyle data, NetcodeSoundMode mode, Vector2? source, bool allowClientsideSoundMuting) {
			packet.Write((short)id);
			packet.Write((byte)mode);

			if ((mode & NetcodeSoundMode.SendPosition) == NetcodeSoundMode.SendPosition)
				packet.Write(source ?? -Vector2.One);

			if ((mode & NetcodeSoundMode.SendVolume) == NetcodeSoundMode.SendVolume)
				packet.Write(data.Volume);

			if ((mode & NetcodeSoundMode.SendPitch) == NetcodeSoundMode.SendPitch) {
				packet.Write(data.Pitch);
				packet.Write(data.PitchVariance);
			}

			packet.Write(allowClientsideSoundMuting);
		}

		private static SlotId ReceiveSoundPlayingPacket(BinaryReader reader, out int id) {
			if (!ReadSoundStyle(reader, out id, out SoundStyle style, out _, out Vector2? location, out bool allowClientsideSoundMuting))
				return SlotId.Invalid;

			style = ISoundEmittingMachine.AdjustSoundForMuting(style, allowClientsideSoundMuting);

			return SoundEngine.PlaySound(style, location);
		}

		private static bool ReadSoundStyle(BinaryReader reader, out int id, out SoundStyle style, out NetcodeSoundMode mode, out Vector2? location, out bool allowClientsideSoundMuting) {
			id = reader.ReadInt16();
			mode = (NetcodeSoundMode)reader.ReadByte();

			location = null;
			if ((mode & NetcodeSoundMode.SendPosition) == NetcodeSoundMode.SendPosition) {
				Vector2 loc = API.Extensions.ReadVector2(reader);

				location = loc == -Vector2.One ? null : loc;
			}

			float volume = -1;
			if ((mode & NetcodeSoundMode.SendVolume) == NetcodeSoundMode.SendVolume)
				volume = reader.ReadSingle();

			float pitch = -2, pitchVariance = -1;
			if ((mode & NetcodeSoundMode.SendPitch) == NetcodeSoundMode.SendPitch) {
				pitch = reader.ReadSingle();
				pitchVariance = reader.ReadSingle();
			}

			allowClientsideSoundMuting = reader.ReadBoolean();

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				id = -1;
				style = default;
				location = null;
				allowClientsideSoundMuting = false;
				return false;
			}

			style = MachineSounds.GetSound(id);
			if (style.SoundPath is null) {
				id = -1;
				style = default;
				location = null;
				allowClientsideSoundMuting = false;
				return false;
			}

			if (volume > -1)
				style.Volume = volume;

			if (pitch > -2)
				style.Pitch = pitch;

			if (pitchVariance > -1)
				style.PitchVariance = pitchVariance;

			return true;
		}

		/// <summary>
		/// Sends a packet from the server to all clients telling them to play a sound
		/// </summary>
		/// <param name="emitter">The machine that emitted the sound.  This information is used to allow the client to track the played sound</param>
		/// <param name="data">The base instance to retrieve data from</param>
		/// <param name="mode">A set of flags used to specify what data from <paramref name="data"/> is sent</param>
		/// <param name="source">
		/// The location of the sound in the world.<br/>
		/// This argument is only used when <paramref name="mode"/> has <see cref="NetcodeSoundMode.SendPosition"/> set.<br/>
		/// If that is not the case, this argument is ignored and the sound is treated as a "directionless" sound
		/// </param>
		/// <param name="extraInformation">An optional argument for specifying any additional information that should be conveyed to clients</param>
		/// <param name="allowClientsideSoundMuting">Whether clients should be able to mute the sound while their game window is inactive</param>
		public static void SendSoundToClients<T>(T emitter, in SoundStyle data, NetcodeSoundMode mode, Vector2? source = null, int extraInformation = 0, bool allowClientsideSoundMuting = true) where T : ModTileEntity, IMachine, ISoundEmittingMachine {
			if (Main.netMode != NetmodeID.Server)
				return;

			int id = MachineSounds.GetID(data);
			if (id < 0)
				throw new ArgumentException("Data instance did not have a registered ID", nameof(data));

			var packet = GetPacket(NetcodeMessage.SendSoundPlayWithEmitter);
			
			packet.Write(emitter.Position);
			packet.Write(extraInformation);
			SendSoundInformationToPacket(packet, id, data, mode, source, allowClientsideSoundMuting);

			packet.Send();
		}

		private static void ReceiveSoundPlayingPacketWithEmitter(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			int extraInformation = reader.ReadInt32();

			SlotId slot = ReceiveSoundPlayingPacket(reader, out int id);

			if (!slot.IsValid)
				return;

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity te) || te is not ModTileEntity || te is not ISoundEmittingMachine machine)
				return;

			// Inform the machine instance that it's playing a sound
			machine.OnSoundPlayingPacketReceived(slot, id, extraInformation);
		}

		/// <summary>
		/// Sends a packet from the server to all clients telling them to stop playing a sound
		/// </summary>
		/// <param name="emitter">The machine that emitted the sound</param>
		/// <param name="id">The registered ID for the sound</param>
		/// <param name="extraInformation">An optional argument for specifying any additional information that should be conveyed to clients</param>
		/// <exception cref="ArgumentException"/>
		public static void SendSoundStopToClients<T>(T emitter, int id, int extraInformation = 0) where T : ModTileEntity, IMachine, ISoundEmittingMachine {
			if (Main.netMode != NetmodeID.Server)
				return;

			if (id < 0 || id >= MachineSounds.Count)
				throw new ArgumentException("Sound ID did not correspond to a valid registered ID");

			var packet = GetPacket(NetcodeMessage.SendSoundStop);
			packet.Write(emitter.Position);
			packet.Write((short)id);
			packet.Write(extraInformation);
			packet.Send();
		}

		private static void ReceiveSoundStopPacket(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			short id = reader.ReadInt16();
			int extraInformation = reader.ReadInt32();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity te) || te is not ModTileEntity || te is not ISoundEmittingMachine machine)
				return;

			// Inform the machine instance that it needs to stop playing a sound
			machine.OnSoundStopPacketReceived(id, extraInformation);
		}

		/// <summary>
		/// Sends a packet from the server to all clients telling them to update a sound
		/// </summary>
		/// <inheritdoc cref="SendSoundToClients{T}(T, in SoundStyle, NetcodeSoundMode, Vector2?, int, bool)"/>
		public static void SendSoundUpdateToClients<T>(T emitter, SoundStyle data, NetcodeSoundMode mode, Vector2? source = null, int extraInformation = 0, bool allowClientsideSoundMuting = true) where T : ModTileEntity, IMachine, ISoundEmittingMachine {
			if (Main.netMode != NetmodeID.Server)
				return;

			int id = MachineSounds.GetID(data);
			if (id < 0)
				throw new ArgumentException("Data instance did not have a registered ID", nameof(data));

			var packet = GetPacket(NetcodeMessage.SendSoundUpdate);
			
			packet.Write(emitter.Position);
			packet.Write(extraInformation);
			SendSoundInformationToPacket(packet, id, data, mode, source, allowClientsideSoundMuting);

			packet.Send();
		}

		private static void ReceiveSoundUpdatePacket(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			int extraInformation = reader.ReadInt32();

			if (!ReadSoundStyle(reader, out int id, out SoundStyle data, out NetcodeSoundMode mode, out Vector2? soundLocation, out bool allowClientsideSoundMuting))
				return;

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity te) || te is not ModTileEntity || te is not ISoundEmittingMachine machine)
				return;

			data = ISoundEmittingMachine.AdjustSoundForMuting(data, allowClientsideSoundMuting);

			// Inform the machine instance that it's updating a sound
			machine.OnSoundUpdatePacketReceived(id, data, mode, soundLocation, extraInformation);
		}

		/// <summary>
		/// Sends the stream of information written by <see cref="IReducedNetcodeMachine.ReducedNetSend(BinaryWriter)"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <exception cref="ArgumentException"/>
		public static void SendReducedData(IReducedNetcodeMachine machine) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("IReducedNetcodeMachine instance was not a ModTileEntity", nameof(machine));

			SendReducedData_DoSync(machine, entity.Position);
		}

		/// <inheritdoc cref="SendReducedData(IReducedNetcodeMachine)"/>
		public static void SendReducedData<T>(T machine) where T : ModTileEntity, IReducedNetcodeMachine, IMachine {
			SendReducedData_DoSync(machine, machine.Position);
		}

		private static void SendReducedData_DoSync(IReducedNetcodeMachine machine, Point16 entityLocation) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncReducedMachineData);
			packet.Write(entityLocation);

			using (MemoryStream ms = new MemoryStream()) using (BinaryWriter writer = new(ms)) {
				machine.ReducedNetSend(writer);

				packet.Write((short)ms.Length);
				packet.Write(ms.ToArray());
			}

			packet.Send();
		}

		private static void ReceiveReducedData(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			short count = reader.ReadInt16();
			byte[] data = reader.ReadBytes(count);

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity te) || te is not ModTileEntity || te is not IReducedNetcodeMachine machine)
				return;

			using MemoryStream ms = new(data);
			using BinaryReader msReader = new(ms);
			machine.ReducedNetReceive(msReader);
		}

		/// <summary>
		/// Syncs the power storage in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process.  Must refer to a <see cref="ModTileEntity"/> instance or an error will be thrown</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachinePowerStorage(IPoweredMachine machine) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("Machine was not a ModTileEntity", nameof(machine));

			SyncMachinePowerStorage_DoSync(entity.Position, machine.PowerStorage);
		}

		/// <summary>
		/// Syncs the power storage in the machine entity at the provided <paramref name="location"/>
		/// </summary>
		/// <param name="location">The tile coordinates of othe machine.  Must refer to a <see cref="ModTileEntity"/> and <see cref="IPoweredMachine"/> instance or an error will be thrown</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachinePowerStorage(Point16 location) {
			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not ModTileEntity || entity is not IPoweredMachine machine)
				throw new ArgumentException($"Tile entity at location (X: {location.X}, Y: {location.Y}) did not have a valid machine type", nameof(location));

			SyncMachinePowerStorage_DoSync(location, machine.PowerStorage);
		}

		private static void SyncMachinePowerStorage_DoSync(Point16 location, FluxStorage storage) {
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			var packet = GetPacket(NetcodeMessage.SyncMachinePowerStorage);
			packet.Write(location);
			storage.Send(packet);
			packet.Send();
		}

		private static void ReceiveMachinePowerStorageSync(BinaryReader reader, int sender) {
			Point16 location = reader.ReadPoint16();

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not IPoweredMachine machine) {
				// Machine does not exist
				FluxStorage storage = new FluxStorage(TerraFlux.Zero);
				storage.Receive(reader);
				return;
			}

			machine.PowerStorage.Receive(reader);

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachineInventorySlot);
				packet.Write(location);
				machine.PowerStorage.Send(packet);
				packet.Send(ignoreClient: sender);
			}
		}

		/// <summary>
		/// Syncs the fluid storage <paramref name="slot"/> in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process.  Must refer to a <see cref="ModTileEntity"/> instance or an error will be thrown</param>
		/// <param name="slot">The slot in the machine's inventory</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachineFluidStorageSlot(IFluidMachine machine, int slot) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("Machine was not a ModTileEntity", nameof(machine));

			var storage = machine.FluidStorage;
			if (slot < 0 || slot >= storage.Length)
				return;

			storage[slot] ??= new(0);

			SyncMachineFluidStorageSlot_DoSync(entity.Position, slot, storage[slot]);
		}

		/// <summary>
		/// Syncs the fluid storage <paramref name="slot"/> in the machine entity at the provided <paramref name="location"/>
		/// </summary>
		/// <param name="location">The tile coordinates of othe machine.  Must refer to a <see cref="ModTileEntity"/> and <see cref="IFluidMachine"/> instance or an error will be thrown</param>
		/// <param name="slot">The slot in the machine's inventory</param>
		/// <exception cref="ArgumentException"/>
		public static void SyncMachineFluidStorageSlot(Point16 location, int slot) {
			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not ModTileEntity || entity is not IFluidMachine machine)
				throw new ArgumentException($"Tile entity at location (X: {location.X}, Y: {location.Y}) did not have a valid machine type", nameof(location));

			var storage = machine.FluidStorage;
			if (slot < 0 || slot >= storage.Length)
				return;

			storage[slot] ??= new(0);

			SyncMachineFluidStorageSlot_DoSync(location, slot, storage[slot]);
		}

		private static void SyncMachineFluidStorageSlot_DoSync(Point16 location, int slot, FluidStorage storage) {
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			var packet = GetPacket(NetcodeMessage.SyncMachineInventorySlot);
			packet.Write(location);
			packet.Write((short)slot);
			storage.Send(packet);
			packet.Send();
		}

		private static void ReceiveMachineFluidStorageSlotSync(BinaryReader reader, int sender) {
			Point16 location = reader.ReadPoint16();
			short slot = reader.ReadInt16();

			if (!TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) || entity is not IFluidMachine machine) {
				// Machine does not exist
				FluidStorage storage = new(0);
				storage.Receive(reader);
				return;
			}

			machine.FluidStorage[slot].Receive(reader);

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				var packet = GetPacket(NetcodeMessage.SyncMachineInventorySlot);
				packet.Write(location);
				packet.Write(slot);
				machine.FluidStorage[slot].Send(packet);
				packet.Send(ignoreClient: sender);
			}
		}

		/// <summary>
		/// Syncs the fluid storage within <paramref name="network"/> to all clients
		/// </summary>
		/// <param name="network">The fluid network</param>
		/// <param name="networkEntry">A location within the network used to identify it on the client's end</param>
		public static void SyncNetworkFluidStorage(FluidNetwork network, Point16 networkEntry) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkFluidStorage);
			packet.Write(networkEntry);
			packet.Write(network.netFluid);
			network.Storage.Send(packet);
			packet.Send();
		}

		private static void ReceiveNetworkFluidStorageSync(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			double netFluid = reader.ReadDouble();

			if (Network.GetFluidNetworkAt(location.X, location.Y) is not FluidNetwork net) {
				// Network does not exist
				FluidStorage storage = new(0);
				storage.Receive(reader);
				return;
			}

			net.netFluid = netFluid;
			net.Storage.Receive(reader);
		}

		/// <summary>
		/// Syncs the power storage within <paramref name="network"/> to all clients
		/// </summary>
		/// <param name="network">The power network</param>
		/// <param name="networkEntry">A location within the network used to identify it on the client's end</param>
		public static void SyncNetworkPowerStorage(PowerNetwork network, Point16 networkEntry) {
			if (Main.netMode != NetmodeID.Server)
				return;

			var packet = GetPacket(NetcodeMessage.SyncNetworkPowerStorage);
			packet.Write(networkEntry);
			packet.Write(network.netPower);
			network.Storage.Send(packet);
			packet.Send();
		}

		private static void ReceiveNetworkPowerStorageSync(BinaryReader reader) {
			Point16 location = reader.ReadPoint16();
			TerraFlux netPower = reader.ReadFlux();

			if (Network.GetPowerNetworkAt(location.X, location.Y) is not PowerNetwork net) {
				// Network does not exist
				FluxStorage storage = new(TerraFlux.Zero);
				storage.Receive(reader);
				return;
			}

			net.netPower = netPower;
			net.Storage.Receive(reader);
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
		SyncMachineRemoval,
		SyncMachineInventorySlot,
		SyncMachineUpgrades,
		SyncFullTileEntityData,
		SendSoundPlay,
		SendSoundPlayWithEmitter,
		SendSoundStop,
		SendSoundUpdate,
		SyncReducedMachineData,
		SyncMachinePowerStorage,
		SyncMachineFluidStorageSlot,
		SyncNetworkFluidStorage,
		SyncNetworkPowerStorage
	}
}
