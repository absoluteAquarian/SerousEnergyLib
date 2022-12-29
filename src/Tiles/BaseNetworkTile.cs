using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// The base type for a 1x1 network tile<br/>
	/// <b>NOTE:</b> The <see cref="Main.tileSolid"/> index for this type will be modified during runtime!
	/// </summary>
	public abstract class BaseNetworkTile : ModTile {
		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			Main.tileSolid[Type] = false;
			Main.tileNoSunLight[Type] = false;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.FlattenAnchors = false;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.None, 0, 0);
			TileObjectData.newTile.UsesCustomCanPlace = true;

			PreRegisterTileObjectData();

			TileObjectData.addTile(Type);
		}

		protected virtual void SafeSetStaticDefaults() { }

		/// <summary>
		/// This is called after the default values are assigned to <see cref="TileObjectData.newTile"/>, but before it's added
		/// </summary>
		protected virtual void PreRegisterTileObjectData() { }

		public override bool CanPlace(int i, int j) {
			// This hook is called just before the tile is placed, which means we can fool the game into thinking this tile is solid when it really isn't
			NetworkTileHacks.SetNetworkTilesToSolid(solid: true);
			return NetworkHelper.AtLeastOneSurroundingTileIsSolid(i, j);
		}

		public override void PlaceInWorld(int i, int j, Item item) {
			// (Continuing from CanPlace) ... then I can just set it back to not solid here
			NetworkTileHacks.SetNetworkTilesToSolid(solid: false);
		}

		public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) {
			Tile tile = Main.tile[i, j];

			if (this is NetworkJunction) {
				// Special hardcoded case: force the frame to (0, 0)
				tile.TileFrameX = 0;
				tile.TileFrameY = 0;
				return false;
			}

			// All network tiles should use the same spritesheet layout
			// Check which directions can be merged with and merge with them accordingly


			return false;
		}

		private static bool CheckTileMerge(int i, int j, int dirX, int dirY) {
			NetworkType networkType = Main.tile[i, j].Get<NetworkInfo>().Type;

			// Tile must be assigned a valid merge type
			if (networkType == NetworkType.None)
				return false;

			// Ignore the "parent" tile
			if (dirX == 0 && dirY == 0)
				return false;

			// Ignore ordinal tiles
			if (dirX != 0 && dirY != 0)
				return false;

			int targetX = i + dirX;
			int targetY = j + dirY;

			// Target must be within the world
			if (!WorldGen.InWorld(targetX, targetY))
				return false;

			Tile target = Main.tile[targetX, targetY];
			ModTile modTarget = TileLoader.GetTile(target.TileType);

			// Target is an air tile
			if (!target.HasTile)
				return false;

			// Chests can merge with item networks
			if ((networkType & NetworkType.Items) == NetworkType.Items && NetworkHelper.TileIsChest(target.TileType))
				return true;

			// Ignore all other vanilla tiles
			// TODO: allow sinks to pump water into networks?
			if (target.TileType < TileID.Count)
				return false;

			if (modTarget is BaseNetworkTile and not NetworkJunction) {
				bool mergeMatches = (networkType & target.Get<NetworkInfo>().Type) != 0;

				// TODO: check if the tile is an item/fluid pump and it's pointed in the right direction
				return mergeMatches;
			} else if (modTarget is NetworkJunction) {
				// Always mergeable with a junction tile
				return true;
			} else if (modTarget is IMachine) {
				// Certain machine classifications can only merge with certain network types...
				if ((networkType & NetworkType.Items) == NetworkType.Items && modTarget is IInventoryMachine inv && inv.CanMergeWithItemPipe(i, j, targetX, targetY))
					return true;
				else if ((networkType & NetworkType.Fluids) == NetworkType.Fluids && modTarget is IFluidMachine flu && flu.CanMergeWithFluidPipe(i, j, targetX, targetY))
					return true;
				else if ((networkType & NetworkType.Power) == NetworkType.Power && modTarget is IPoweredMachine pow && pow.CanMergeWithWire(i, j, targetX, targetY))
					return true;

				return false;
			}
			// TODO: check item/fluid pump tiles

			// Tile couldn't be merged with
			return false;
		}
	}
}
