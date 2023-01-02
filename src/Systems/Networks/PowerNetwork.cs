using SerousEnergyLib.API.Energy;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class PowerNetwork : NetworkInstance {
		public FluxStorage Storage = new FluxStorage(TerraFlux.Zero);

		internal PowerNetwork() : base(NetworkType.Power) { }

		public override void OnEntryAdded(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IPowerTransportTile transport)
				Storage.MaxCapacity += transport.MaxCapacity;
		}

		public override void OnEntryRemoved(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IPowerTransportTile transport)
				Storage.MaxCapacity -= transport.MaxCapacity;

			// Clamp the storage
			if (Storage.CurrentCapacity > Storage.MaxCapacity)
				Storage.CurrentCapacity = Storage.MaxCapacity;
		}
	}
}
