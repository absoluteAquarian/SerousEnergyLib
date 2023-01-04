using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class FluidNetwork : NetworkInstance {
		private HashSet<Point16> adjacentFluidStorageTiles = new();
		private Dictionary<Point16, Ref<int>> pumpTimers = new();
		public FluidStorage Storage = new FluidStorage(0);

		internal FluidNetwork() : base(NetworkType.Fluids) { }

		public override void Update() {
			HashSet<Point16> invalidPumps = new();
			foreach (var (loc, timer) in pumpTimers) {
				if (!UpdatePump(loc, timer))
					invalidPumps.Add(loc);
			}

			foreach (var pump in invalidPumps)
				pumpTimers.Remove(pump);
		}

		internal void AddPumpTimer(Point16 location, int timer) {
			pumpTimers[location] = new Ref<int>(timer);
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

				if (IMachine.TryFindMachine(possibleStorage, out IMachine machine) && machine is IFluidMachine fluid) {
					FluidStorage storage = fluid.SelectFluidExportSource(location, possibleStorage);

					storage?.ExportTo(Storage, extract);
				}
			}

			return true;
		}

		public override void OnNetworkReset() {
			adjacentFluidStorageTiles.Clear();
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
			if (IMachine.TryFindMachine(left, out IMachine machine) && machine is IFluidMachine)
				adjacentFluidStorageTiles.Add(left);
			if (IMachine.TryFindMachine(up, out machine) && machine is IFluidMachine)
				adjacentFluidStorageTiles.Add(up);
			if (IMachine.TryFindMachine(right, out machine) && machine is IFluidMachine)
				adjacentFluidStorageTiles.Add(right);
			if (IMachine.TryFindMachine(down, out machine) && machine is IFluidMachine)
				adjacentFluidStorageTiles.Add(down);
		}

		public void RemoveAdjacentFluidStorage(Point16 storage) {
			adjacentFluidStorageTiles.Remove(storage);
		}

		public override void OnEntryRemoved(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IFluidTransportTile transport)
				Storage.MaxCapacity -= transport.MaxCapacity;

			// Clamp the storage
			if (Storage.CurrentCapacity > Storage.MaxCapacity)
				Storage.CurrentCapacity = Storage.MaxCapacity;
		}

		protected override void SaveExtraData(TagCompound tag) {
			TagCompound storage = new TagCompound();
			Storage.SaveData(storage);
			tag["fluids"] = storage;
		}

		protected override void LoadExtraData(TagCompound tag) {
			if (tag.GetCompound("fluids") is TagCompound fluids)
				Storage.LoadData(fluids);
		}
	}
}
