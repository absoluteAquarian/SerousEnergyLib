using Microsoft.Xna.Framework;
using SerousEnergyLib.API;
using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	/// <summary>
	/// An object representing a <see cref="NetworkInstance"/> for fluid tiles
	/// </summary>
	public sealed class FluidNetwork : NetworkInstance {
		private HashSet<Point16> adjacentFluidStorageTiles = new();
		private Dictionary<Point16, Ref<int>> pumpTimers = new();

		internal int AdjacentStorageCount => adjacentFluidStorageTiles.Count;
		internal int PumpCount => pumpTimers.Count;

		/// <summary>
		/// The fluid storage within this network
		/// </summary>
		public FluidStorage Storage = new FluidStorage(0);

		internal double netFluid;
		/// <summary>
		/// The net gain/loss of fluids in this fluid network
		/// </summary>
		public double NetFluid => netFluid;

		internal FluidNetwork() : base(NetworkType.Fluids) { }

		#pragma warning disable CS1591
		public override void Update() {
			HashSet<Point16> invalidPumps = new();
			foreach (var (loc, timer) in pumpTimers) {
				if (!UpdatePump(loc, timer))
					invalidPumps.Add(loc);
			}

			foreach (var pump in invalidPumps)
				pumpTimers.Remove(pump);

			double previousCapacity = Storage.CurrentCapacity;

			// Export fluids from this network into adjacent machines
			IEnumerable<IFluidMachine> machines = adjacentFluidStorageTiles.Select(a => IMachine.TryFindMachine(a, out IFluidMachine machine) ? machine : null)
				.OfType<ModTileEntity>()
				.OfType<IFluidMachine>();

			foreach (var machine in machines) {
				if (!IFluidMachine.TryGetHighestExportRate(machine, this, out double rate, out Point16 pipeTile, out Point16 machineSubTile))
					continue;

				int slot = machine.SelectFluidImportDestination(pipeTile, machineSubTile);

				if (slot >= 0 && slot < machine.FluidStorage.Length) {
					FluidStorage storage = machine.FluidStorage[slot];
					storage?.ImportFrom(Storage, rate);

					Netcode.SyncMachineFluidStorageSlot(machine, slot);
				}
			}

			netFluid = Storage.CurrentCapacity - previousCapacity;

			Netcode.SyncNetworkFluidStorage(this, FirstNode);
		}

		internal void AddPumpTimer(Point16 location, int timer) {
			pumpTimers[location] = new Ref<int>(timer);
		}

		public bool TryGetPumpTimer(Point16 location, out int timer) {
			if (pumpTimers.TryGetValue(location, out Ref<int> timerRef)) {
				timer = timerRef.Value;
				return true;
			}

			timer = -1;
			return false;
		}

		private bool UpdatePump(Point16 location, Ref<int> timerRef) {
			ref int timer = ref timerRef.Value;

			timer--;

			if (timer <= 0) {
				// Reset the timer according to the max value for the pump type
				Tile tile = Main.tile[location.X, location.Y];

				if (TileLoader.GetTile(tile.TileType) is not IFluidPumpTile pump)
					return false;

				timer = pump.GetMaxTimer(location.X, location.Y);

				// Check if there's a fluid storage behind this pump
				PumpDirection direction = tile.Get<NetworkTaggedInfo>().PumpDirection;

				Point16 tailOffset = direction switch {
					PumpDirection.Left => new Point16(1, 0),
					PumpDirection.Up => new Point16(0, 1),
					PumpDirection.Right => new Point16(-1, 0),
					PumpDirection.Down => new Point16(0, -1),
					_ => throw new Exception("Invalid pump direction detected"),
				};

				Point16 possibleStorage = location + tailOffset;
				double extract = pump.MaxCapacity;

				// TODO: allow sinks to pump water into networks?

				if (IMachine.TryFindMachine(possibleStorage, out IFluidMachine machine)) {
					int slot = machine.SelectFluidExportSource(location, possibleStorage);

					if (slot >= 0 && slot < machine.FluidStorage.Length) {
						FluidStorage storage = machine.FluidStorage[slot];
						storage?.ExportTo(Storage, extract);

						Netcode.SyncMachineFluidStorageSlot(machine, slot);
					}
				}
			}

			return true;
		}

		public override void OnNetworkReset() {
			adjacentFluidStorageTiles.Clear();
		}

		protected override void CopyExtraData(NetworkInstance source) {
			FluidNetwork src = source as FluidNetwork;

			TagCompound tag = new();
			src.Storage.SaveData(tag);
			Storage = new FluidStorage(0);
			Storage.LoadData(tag);

			foreach (var (loc, pump) in src.pumpTimers)
				pumpTimers[loc] = pump;

			foreach (var loc in src.adjacentFluidStorageTiles)
				adjacentFluidStorageTiles.Add(loc);
		}

		protected override void OnNetworkCloned(NetworkInstance orig) {
			// Find the "percentage capacity" that the original network had and apply it to this network's current storage
			FluidNetwork src = orig as FluidNetwork;

			var storage = src.Storage;

			double percentage = storage.MaxCapacity <= 0 ? 0 : storage.CurrentCapacity / storage.MaxCapacity;

			Storage.CurrentCapacity = Storage.MaxCapacity * percentage;
		}

		public override void OnEntryAdded(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IFluidTransportTile transport)
				Storage.MaxCapacity += transport.MaxCapacity;

			Point16 left = location + new Point16(-1, 0),
				up = location + new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);

			// Add adjacent machines
			if (IMachine.TryFindMachine(left, out IFluidMachine _))
				adjacentFluidStorageTiles.Add(left);
			if (IMachine.TryFindMachine(up, out IFluidMachine _))
				adjacentFluidStorageTiles.Add(up);
			if (IMachine.TryFindMachine(right, out IFluidMachine _))
				adjacentFluidStorageTiles.Add(right);
			if (IMachine.TryFindMachine(down, out IFluidMachine _))
				adjacentFluidStorageTiles.Add(down);
		}

		/// <summary>
		/// Attempts to add an adjacent <see cref="IFluidMachine"/> machine entry location at <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The tile location of the adjacent machine to add</param>
		public void AddAdjacentFluidStorage(Point16 storage) {
			if (IMachine.TryFindMachine(storage, out IFluidMachine _))
				adjacentFluidStorageTiles.Add(storage);
		}

		/// <summary>
		/// Removes an adjacent <see cref="IFluidMachine"/> machine entry location
		/// </summary>
		/// <param name="storage">The tile location of the adjacent machine to remove</param>
		public void RemoveAdjacentFluidStorage(Point16 storage) {
			adjacentFluidStorageTiles.Remove(storage);
		}

		/// <summary>
		/// Returns whether the tile at <paramref name="storage"/> is considered an adjacent fluid storage to this network
		/// </summary>
		/// <param name="storage">The tile location of the adjacent tile</param>
		public bool HasAdjacentFluidStorage(Point16 storage) => adjacentFluidStorageTiles.Contains(storage);

		protected override void DisposeSelf(bool disposing) {
			if (disposing) {
				adjacentFluidStorageTiles.Clear();
				pumpTimers.Clear();
			}

			adjacentFluidStorageTiles = null;
			pumpTimers = null;
			Storage = null;
		}

		public override void OnEntryRemoved(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IFluidTransportTile transport)
				Storage.MaxCapacity -= transport.MaxCapacity;

			// Clamp the storage
			if (Storage.CurrentCapacity > Storage.MaxCapacity)
				Storage.CurrentCapacity = Storage.MaxCapacity;

			// Remove any adjacent storages
			Point16 left = location + new Point16(-1, 0),
				up = location = new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);

			adjacentFluidStorageTiles.Remove(left);
			adjacentFluidStorageTiles.Remove(up);
			adjacentFluidStorageTiles.Remove(right);
			adjacentFluidStorageTiles.Remove(down);

			// Remove the pump timer if it's present
			pumpTimers.Remove(location);
		}

		protected override void SaveExtraData(TagCompound tag) {
			static TagCompound SavePump(Point16 location, Ref<int> timer) {
				Tile tile = Main.tile[location.X, location.Y];

				return new TagCompound() {
					["x"] = location.X,
					["y"] = location.Y,
					["time"] = timer.Value,
					["dir"] = (byte)(tile.TileFrameX / 18)
				};
			}

			TagCompound storage = new TagCompound();
			Storage.SaveData(storage);
			tag["fluids"] = storage;

			tag["pumps"] = pumpTimers.Select(static kvp => SavePump(kvp.Key, kvp.Value)).ToList();
		}

		protected override void LoadExtraData(TagCompound tag) {
			Storage = new(0);

			if (tag.GetCompound("fluids") is TagCompound fluids)
				Storage.LoadData(fluids);

			pumpTimers.Clear();

			if (tag.GetList<TagCompound>("pumps") is List<TagCompound> pumpTags) {
				foreach (var pump in pumpTags) {
					if (!pump.TryGet("x", out short x))
						continue;

					if (!pump.TryGet("y", out short y))
						continue;

					if (!pump.TryGet("time", out int time))
						continue;

					if (!pump.TryGet("dir", out byte dir))
						continue;

					pumpTimers[new Point16(x, y)] = new Ref<int>(time);

					Tile tile = Main.tile[x, y];

					ref var netInfo = ref tile.Get<NetworkInfo>();

					if (netInfo.IsPump && (netInfo.Type & NetworkType.Fluids) == NetworkType.Fluids) {
						tile.TileFrameX = (short)(dir * 18);
						tile.TileFrameY = 0;
					}
				}
			}
		}

		public override void SendExtraData(BinaryWriter writer) {
			Storage.Send(writer);

			writer.Write(adjacentFluidStorageTiles.Count);
			foreach (var adjacent in adjacentFluidStorageTiles)
				writer.Write(adjacent);

			writer.Write(pumpTimers.Count);
			foreach (var (position, timer) in pumpTimers) {
				writer.Write(position);
				writer.Write(timer.Value);
			}
		}

		public override void ReceiveExtraData(BinaryReader reader) {
			Storage ??= new(0);
			Storage.Receive(reader);

			adjacentFluidStorageTiles.Clear();

			int adjacentCount = reader.ReadInt32();
			for (int i = 0; i < adjacentCount; i++)
				adjacentFluidStorageTiles.Add(reader.ReadPoint16());

			pumpTimers.Clear();

			int pumpCount = reader.ReadInt32();
			for (int i = 0; i < pumpCount; i++)
				pumpTimers.Add(reader.ReadPoint16(), new Ref<int>(reader.ReadInt32()));
		}
	}
}
