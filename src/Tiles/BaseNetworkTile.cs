using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousEnergyLib.API;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Configs;
using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using System;
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
		#pragma warning disable CS1591
		/// <summary>
		/// The network state to apply to the <see cref="NetworkInfo"/> data when this tile is placed
		/// </summary>
		public abstract NetworkType NetworkTypeToPlace { get; }

		/// <summary>
		/// The item ID that's automatically assigned to <see cref="ModBlockType.ItemDrop"/>
		/// </summary>
		public abstract int NetworkItem { get; }

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			Main.tileSolid[Type] = false;
			Main.tileNoSunLight[Type] = false;
			// NOTE: This line is what allows these non-solid tiles to be valid anchors for placing tiles adjacent to them
			TileID.Sets.IsBeam[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.FlattenAnchors = false;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.None, 0, 0);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.HookPlaceOverride = new PlacementHook(CustomPlace, -1, 0, false);

			PreRegisterTileObjectData();

			if (TileObjectData.newTile.Width != 1 || TileObjectData.newTile.Height != 1)
				throw new Exception("Tile object data was not 1x1");

			TileObjectData.addTile(Type);

			ItemDrop = NetworkItem;
		}

		protected virtual void SafeSetStaticDefaults() { }

		/// <summary>
		/// This is called after the default values are assigned to <see cref="TileObjectData.newTile"/>, but before it's added
		/// </summary>
		protected virtual void PreRegisterTileObjectData() { }

		private static int CustomPlace(int i, int j, int type, int style, int direction, int alternative) {
			// Destroy the tile at the location, then place a new one
			Tile tile = Main.tile[i, j];

			if (tile.HasTile && tile.TileType != 484 && (Main.tileCut[tile.TileType] || TileID.Sets.BreakableWhenPlacing[tile.TileType])) {
				WorldGen.KillTile(i, j);

				if (!tile.HasTile && Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: i, number3: j);
			}

			if (!tile.HasTile) {
				tile.HasTile = true;
				tile.TileFrameX = (short)(18 * style);
				tile.TileFrameY = 0;
				tile.TileType = (ushort)type;
			}

			return 1;
		}

		public override bool CanPlace(int i, int j) {
			// This hook is called just before the tile is placed, which means we can fool the game into thinking this tile is solid when it really isn't
			NetworkTileHacks.SetNetworkTilesToSolid(solid: true);
			return NetworkHelper.AtLeastOneSurroundingTileIsSolid(i, j);
		}

		public override void PlaceInWorld(int i, int j, Item item) {
			// (Continuing from CanPlace) ... then I can just set it back to not solid here
			NetworkTileHacks.SetNetworkTilesToSolid(solid: false);
			Network.PlaceEntry(i, j, NetworkTypeToPlace);

			// Force tile merging at this location again
			WorldGen.SquareTileFrame(i, j);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(Main.myPlayer, i, j, centeredSquareSize: 3);
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			if (!fail && !effectOnly)
				Network.RemoveEntry(i, j, NetworkTypeToPlace);
		}

		/// <summary>
		/// Whether this tile should ignore the logic in <see cref="TileFrame(int, int, ref bool, ref bool)"/> that modifies the tile's TileFrameX and TileFrameY
		/// </summary>
		protected virtual bool IgnoreSpriteSheetFraming => false;

		public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) {
			Tile tile = Main.tile[i, j];

			// All network tiles should use the same spritesheet layout
			// Check which directions can be merged with and merge with them accordingly
			bool canMergeLeft = i > 0 && CheckTileMerge(i, j, dirX: -1, dirY: 0);
			bool canMergeUp = j > 0 && CheckTileMerge(i, j, dirX: 0, dirY: -1);
			bool canMergeRight = i < Main.maxTilesX - 1 && CheckTileMerge(i, j, dirX: 1, dirY: 0);
			bool canMergeDown = j < Main.maxTilesY - 1 && CheckTileMerge(i, j, dirX: 0, dirY: 1);

			if (!IgnoreSpriteSheetFraming) {
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
			}

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

			if (modTarget is BaseNetworkTile and not NetworkJunction and not IItemPumpTile) {
				bool mergeMatches = (networkType & target.Get<NetworkInfo>().Type) != 0;

				return mergeMatches;
			} else if (modTarget is NetworkJunction) {
				// Always mergeable with a junction tile
				return true;
			} else if (modTarget is IMachineTile && IMachine.TryFindMachine(new Point16(targetX, targetY), out IMachine machine)) {
				// Certain machine classifications can only merge with certain network types...
				if ((networkType & NetworkType.Items) == NetworkType.Items && machine is IInventoryMachine inv && inv.CanMergeWithItemPipe(i, j, targetX, targetY))
					return true;
				else if ((networkType & NetworkType.Fluids) == NetworkType.Fluids && machine is IFluidMachine flu && flu.CanMergeWithFluidPipe(i, j, targetX, targetY))
					return true;
				else if ((networkType & NetworkType.Power) == NetworkType.Power && machine is IPoweredMachine pow && pow.CanMergeWithWire(i, j, targetX, targetY))
					return true;
			} else if (Chest.FindChestByGuessing(targetX, targetY) > -1) {
				// Merge if, and only if, this tile is part of an item network
				if ((networkType & NetworkType.Items) == NetworkType.Items)
					return true;
			} else if (modTarget is IPumpTile pump) {
				// Merge if the pump's network type matches this tile's network type
				bool attemptMerge = false;

				if ((networkType & NetworkType.Items) == NetworkType.Items && pump is IItemPumpTile)
					attemptMerge = true;
				if ((networkType & NetworkType.Fluids) == NetworkType.Fluids && pump is IFluidPumpTile)
					attemptMerge = true;

				if (attemptMerge)
					return NetworkTaggedInfo.DoesOrientationMatchPumpDirection(new Point16(-dirX, -dirY), target.Get<NetworkTaggedInfo>().PumpDirection);
			}

			// Tile couldn't be merged with
			return false;
		}

		public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) {
			if (this is not IItemTransportTile || !RenderingConfig.RenderItemsInPipes)
				return true;

			Rectangle tileRect = new Rectangle(i * 16, j * 16, 16, 16);

			// For every item in an item network, if the item would be in this network tile, draw it
			// However, if the item is partially in the tile to the left or above this tile, do not draw it in order to prevent clipping issues
			foreach (var network in Network.GetItemNetworksAt(i, j)) {
				foreach (var pipedItem in network.items) {
					if (pipedItem is not { Destroyed: false })
						continue;

					// If the item isn't at a valid location, don't draw it
					if (!pipedItem.GetItemDrawInformation(out Item item, out Vector2 worldCenter, out float size))
						continue;

					if (size < 1f)
						size = 1f;
					if (size > 8f)
						size = 8f;

					// Item must not poke out above this tile or to the left of it
					// If there is no entry in those directions, draw the item anyway
					Vector2 halfSize = new Vector2(size) / 2f;
					Vector2 topLeft = worldCenter - halfSize;

					if (network.HasEntry(topLeft.ToTileCoordinates16()) && (topLeft.X < tileRect.X || topLeft.Y < tileRect.Y))
						continue;

					// Item must at least be partially inside this tile
					Vector2 bottomRight = worldCenter + halfSize;
					if (!tileRect.Contains(topLeft) && !tileRect.Contains(bottomRight))
						continue;

					// Draw the item
					Main.spriteBatch.DrawItemInWorld(item, worldCenter - Main.screenPosition + TileFunctions.GetLightingDrawOffset(), size);
				}
			}

			return true;
		}
	}
}
