using SerousEnergyLib.API;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	/// <summary>
	/// An object representing a <see cref="NetworkInstance"/> for power tiles
	/// </summary>
	public sealed class PowerNetwork : NetworkInstance {
		private HashSet<Point16> adjacentFluxStorageTiles = new();

		/// <summary>
		/// The power storage within this network
		/// </summary>
		public FluxStorage Storage = new FluxStorage(TerraFlux.Zero);

		internal TerraFlux netPower;
		/// <summary>
		/// The net gain/loss of power in this power network
		/// </summary>
		public TerraFlux NetPower => netPower;

		internal PowerNetwork() : base(NetworkType.Power) { }

		internal void ResetNetStats() {
			netPower = TerraFlux.Zero;
		}

		#pragma warning disable CS1591
		public override void Update() {
			// Called separately from when item/fluid networks are updated due to TileEntity update order
			TerraFlux previousPower = Storage.CurrentCapacity;

			// Generators have already been processed.  Only send power to machines that store/consume it
			IEnumerable<IPoweredMachine> machines = adjacentFluxStorageTiles.Select(a => IMachine.TryFindMachine(a, out IPoweredMachine machine) ? machine : null)
				.OfType<ModTileEntity>()
				.OfType<IPoweredMachine>()
				.Where(p => p is not IPowerGeneratorMachine);

			foreach (var machine in machines) {
				if (!IPoweredMachine.TryGetHighestTransferRate(machine, this, out TerraFlux rate, out _, out _))
					continue;

				Storage.ExportTo(machine.PowerStorage, rate);
			}

			netPower = Storage.CurrentCapacity - previousPower;

			Netcode.SyncNetworkPowerStorage(this, FirstNode);
		}

		public override void OnEntryAdded(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IPowerTransportTile transport)
				Storage.MaxCapacity += transport.MaxCapacity;

			Point16 left = location + new Point16(-1, 0),
				up = location + new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);

			// Add adjacent machines
			if (IMachine.TryFindMachine(left, out IPoweredMachine _))
				adjacentFluxStorageTiles.Add(left);
			if (IMachine.TryFindMachine(up, out IPoweredMachine _))
				adjacentFluxStorageTiles.Add(up);
			if (IMachine.TryFindMachine(right, out IPoweredMachine _))
				adjacentFluxStorageTiles.Add(right);
			if (IMachine.TryFindMachine(down, out IPoweredMachine _))
				adjacentFluxStorageTiles.Add(down);
		}

		/// <summary>
		/// Attempts to add an adjacent <see cref="IPoweredMachine"/> machine entry location at <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The tile location of the adjacent machine to add</param>
		public void AddAdjacentFluxStorage(Point16 storage) {
			if (IMachine.TryFindMachine(storage, out IPoweredMachine _))
				adjacentFluxStorageTiles.Add(storage);
		}

		/// <summary>
		/// Removes an adjacent <see cref="IPoweredMachine"/> machine entry location
		/// </summary>
		/// <param name="storage">The tile location of the adjacent machine to remove</param>
		public void RemoveAdjacentFluxStorage(Point16 storage) {
			adjacentFluxStorageTiles.Remove(storage);
		}

		public override void OnEntryRemoved(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IPowerTransportTile transport)
				Storage.MaxCapacity -= transport.MaxCapacity;

			// Clamp the storage
			if (Storage.CurrentCapacity > Storage.MaxCapacity)
				Storage.CurrentCapacity = Storage.MaxCapacity;
		}

		protected override void SaveExtraData(TagCompound tag) {
			TagCompound storage = new TagCompound();
			Storage.SaveData(storage);
			tag["flux"] = storage;
		}

		protected override void LoadExtraData(TagCompound tag) {
			if (tag.GetCompound("flux") is TagCompound flux)
				Storage.LoadData(flux);
		}

		public override void SendExtraData(BinaryWriter writer) {
			Storage.Send(writer);

			writer.Write(adjacentFluxStorageTiles.Count);
			foreach (var adjacent in adjacentFluxStorageTiles)
				writer.Write(adjacent);
		}

		public override void ReceiveExtraData(BinaryReader reader) {
			Storage ??= new(TerraFlux.Zero);

			adjacentFluxStorageTiles.Clear();

			int adjacentCount = reader.ReadInt32();
			for (int i = 0; i < adjacentCount; i++)
				adjacentFluxStorageTiles.Add(reader.ReadPoint16());
		}
	}
}
