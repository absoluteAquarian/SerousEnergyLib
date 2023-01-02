using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class FluidNetwork : NetworkInstance {
		public FluidStorage Storage = new FluidStorage(0);

		internal FluidNetwork() : base(NetworkType.Fluids) { }

		public override void OnEntryAdded(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IFluidTransportTile transport)
				Storage.MaxCapacity += transport.MaxCapacity;
		}

		public override void OnEntryRemoved(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			if (TileLoader.GetTile(tile.TileType) is IFluidTransportTile transport)
				Storage.MaxCapacity -= transport.MaxCapacity;

			// Clamp the storage
			if (Storage.CurrentCapacity > Storage.MaxCapacity)
				Storage.CurrentCapacity = Storage.MaxCapacity;
		}
	}
}
