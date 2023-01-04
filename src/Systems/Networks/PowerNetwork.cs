using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class PowerNetwork : NetworkInstance {
		private HashSet<Point16> adjacentFluxStorageTiles = new();
		public FluxStorage Storage = new FluxStorage(TerraFlux.Zero);

		private TerraFlux netPower;
		/// <summary>
		/// The net gain/loss of power in this power network
		/// </summary>
		public TerraFlux NetPower => netPower;

		internal PowerNetwork() : base(NetworkType.Power) { }

		internal void ResetNetStats() {
			netPower = TerraFlux.Zero;
		}

		public override void Update() {
			// Called separately from when item/fluid networks are updated due to TileEntity update order
			TerraFlux previousPower = Storage.CurrentCapacity;

			// Generators have already been processed.  Only send power to machines that store/consume it
			IEnumerable<IPoweredMachine> machines = adjacentFluxStorageTiles.Select(a => IMachine.TryFindMachine(a, out IMachine machine) ? machine : null)
				.OfType<IPoweredMachine>()
				.Where(p => p is not IPowerGeneratorMachine);

			foreach (var machine in machines)
				machine.ImportPower(Storage);

			netPower = Storage.CurrentCapacity - previousPower;
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
			if (IMachine.TryFindMachine(left, out IMachine machine) && machine is IPoweredMachine)
				adjacentFluxStorageTiles.Add(left);
			if (IMachine.TryFindMachine(up, out machine) && machine is IPoweredMachine)
				adjacentFluxStorageTiles.Add(up);
			if (IMachine.TryFindMachine(right, out machine) && machine is IPoweredMachine)
				adjacentFluxStorageTiles.Add(right);
			if (IMachine.TryFindMachine(down, out machine) && machine is IPoweredMachine)
				adjacentFluxStorageTiles.Add(down);
		}

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
	}
}
