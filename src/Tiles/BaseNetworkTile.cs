﻿using SerousEnergyLib.API.Machines;
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
		/// <summary>
		/// The network state to apply to the <see cref="NetworkInfo"/> data when this tile is placed
		/// </summary>
		public abstract NetworkType NetworkTypeToPlace { get; }

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
			Network.PlaceEntry(i, j, NetworkTypeToPlace);
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
			bool canMergeLeft = i > 0 && CheckTileMerge(i, j, dirX: -1, dirY: 0);
			bool canMergeUp = j > 0 && CheckTileMerge(i, j, dirX: 0, dirY: -1);
			bool canMergeRight = i < Main.maxTilesX - 1 && CheckTileMerge(i, j, dirX: 1, dirY: 0);
			bool canMergeDown = j < Main.maxTilesY - 1 && CheckTileMerge(i, j, dirX: 0, dirY: 1);

			//Default to the "no merge" frame
			int frameX = 0;
			int frameY = 0;

			//Fortunately, the tilesets for these tiles are much easier to work with
			if (!canMergeUp && !canMergeLeft && !canMergeRight && !canMergeDown) {  // None connected
				// Only one frame for this, the default
				// 0000
			} else if (canMergeUp && !canMergeLeft && !canMergeRight && !canMergeDown) {  // Main connection: Up
				// 1000
				frameX = 6;
				frameY = 3;
			} else if (canMergeUp && canMergeLeft && !canMergeRight && !canMergeDown) {
				// 1100
				frameX = Main.rand.NextBool() ? 2 : 5;
				frameY = 3;
			} else if (canMergeUp && !canMergeLeft && canMergeRight && !canMergeDown) {
				// 1010
				frameX = Main.rand.NextBool() ? 0 : 3;
				frameY = 3;
			} else if (canMergeUp && canMergeLeft && canMergeRight && !canMergeDown) {
				// 1110
				frameX = Main.rand.NextBool() ? 1 : 4;
				frameY = 3;
			} else if (canMergeUp && !canMergeLeft && !canMergeRight && canMergeDown) {
				// 1001
				frameX = 6;
				frameY = 2;
			} else if (canMergeUp && canMergeLeft && !canMergeRight && canMergeDown) {
				// 1101
				frameX = Main.rand.NextBool() ? 2 : 5;
				frameY = 2;
			} else if (canMergeUp && !canMergeLeft && canMergeRight && canMergeDown) {
				// 1011
				frameX = Main.rand.NextBool() ? 0 : 3;
				frameY = 2;
			} else if (!canMergeUp && canMergeLeft && !canMergeRight && !canMergeDown) {  // Main connection: Left
				// 0100
				frameX = 3;
				frameY = 0;
			} else if (!canMergeUp && canMergeLeft && canMergeRight && !canMergeDown) {
				// 0110
				frameX = 2;
				frameY = 0;
			} else if (!canMergeUp && canMergeLeft && !canMergeRight && canMergeDown) {
				// 0101
				frameX = Main.rand.NextBool() ? 2 : 5;
				frameY = 1;
			} else if (!canMergeUp && canMergeLeft && canMergeRight && canMergeDown) {
				// 0111
				frameX = Main.rand.NextBool() ? 1 : 4;
				frameY = 1;
			} else if (!canMergeUp && !canMergeLeft && canMergeRight && !canMergeDown) {  // Main connection: Right
				// 0010
				frameX = 1;
				frameY = 0;
			} else if (!canMergeUp && !canMergeLeft && canMergeRight && canMergeDown) {
				// 0011
				frameX = Main.rand.NextBool() ? 0 : 3;
				frameY = 1;
			} else if (!canMergeUp && !canMergeLeft && !canMergeRight && canMergeDown) {  // Main connection: Down
				// 0001
				frameX = 6;
				frameY = 1;
			} else if (canMergeUp && canMergeLeft && canMergeRight && canMergeDown) {  // All connected
				// 1111
				frameX = Main.rand.NextBool() ? 1 : 4;
				frameY = 2;
			}

			tile.TileFrameX = (short)(frameX * 18);
			tile.TileFrameY = (short)(frameY * 18);

			// Safety check: modify the directions
			ConnectionDirection dirs = ConnectionDirection.None;
			
			if (canMergeLeft)
				dirs |= ConnectionDirection.Left;
			if (canMergeUp)
				dirs |= ConnectionDirection.Up;
			if (canMergeRight)
				dirs |= ConnectionDirection.Right;
			if (canMergeDown)
				dirs |= ConnectionDirection.Down;

			tile.Get<NetworkInfo>().Connections = dirs;

			// Custom logic is used
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