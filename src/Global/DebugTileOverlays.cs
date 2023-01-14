using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Tiles;
using System.Linq;

namespace SerousEnergyLib.Global {
	internal class DebugTileOverlays : GlobalTile {
		private static Asset<Texture2D> overlay;

		public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];

			if (Main.LocalPlayer.HeldItem.ModItem is not DebugTool debug || debug.ActiveNetwork == Point16.NegativeOne)
				return;

			int netX = debug.ActiveNetwork.X, netY = debug.ActiveNetwork.Y;
			
			byte red = 0, green = 0, blue = 0;

			// Tile might be an adjacent inventory/storage
			ModTile modTile = TileLoader.GetTile(type);
			if ((modTile is IMachineTile machineTile && machineTile.GetMachineEntity() is IInventoryMachine or IFluidMachine or IPoweredMachine) || NetworkHelper.TileIsChest(type)) {
				Span<Point16> directions = stackalloc Point16[] {
					new(-1, 0), new(0, -1), new(1, 0), new(0, 1)
				};

				Point16 orig = new Point16(i, j);
				bool adjItem = false, adjFluid = false, adjPower = false;
				for (int d = 0; d < directions.Length; d++) {
					Point16 dir = directions[d];
					Point16 netPos = orig + dir;

					if (Network.GetItemNetworkAt(netPos.X, netPos.Y) is ItemNetwork adjacentItemNet && adjacentItemNet.HasEntry(netX, netY) && adjacentItemNet.HasAdjacentInventory(orig)) {
						// This tile is adjacent to an item network
						adjItem = true;
					}

					if (Network.GetFluidNetworkAt(netPos.X, netPos.Y) is FluidNetwork adjacentFluidNet && adjacentFluidNet.HasEntry(netX, netY) && adjacentFluidNet.HasAdjacentFluidStorage(orig)) {
						// This tile is adjacent to a fluid network
						adjFluid = true;
					}

					if (Network.GetPowerNetworkAt(netPos.X, netPos.Y) is PowerNetwork adjacentPowerNet && adjacentPowerNet.HasEntry(netX, netY) && adjacentPowerNet.HasAdjacentFluxStorage(orig)) {
						// This tile is adjacent to a power network
						adjPower = true;
					}
				}

				// Assign a color based on what networks were adjacent
				if (adjItem && !adjFluid && !adjPower) {
					// Deep orange
					red = 0xff;
					green = 0x80;
				} else if (!adjItem && adjFluid && !adjPower) {
					// Lime green
					red = 0xc0;
					green = 0xff;
				} else if (adjItem && adjFluid && !adjPower) {
					// Peach
					red = 0xff;
					green = 0xc0;
					blue = 0x80;
				} else if (!adjItem && !adjFluid && adjPower) {
					// Navy blue
					green = 0x80;
					blue = 0xff;
				} else if (adjItem && !adjFluid && adjPower) {
					// Lavender
					red = 0xc0;
					green = 0x80;
					blue = 0xff;
				} else if (!adjItem && adjFluid && adjPower) {
					// Pale green
					red = 0xc0;
					green = 0xff;
					blue = 0x80;
				} else if (adjItem && adjFluid && adjPower) {
					// Slate gray
					red = 0xc0;
					green = 0xc0;
					blue = 0xc0;
				}

				goto checkColors;
			}

			// Failsafe: tile couldn't possibly be part of a network
			if (tile.Get<NetworkInfo>().Type == NetworkType.None)
				return;

			if (Network.GetItemNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
				red = 0xff;
			if (Network.GetFluidNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
				green = 0xff;
			if (Network.GetPowerNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
				blue = 0xff;

			checkColors:

			// Tile isn't part of the active network
			if (red == 0 && green == 0 && blue == 0)
				return;

			overlay ??= ModContent.Request<Texture2D>("SerousEnergyLib/Assets/Tiles/NetworkTileOverlay");

			Vector2 worldPosition = new Vector2(i * 16, j * 16);
			Vector2 drawPosition = worldPosition - Main.screenPosition + TileFunctions.GetLightingDrawOffset();

			spriteBatch.Draw(overlay.Value, drawPosition, null, new Color(red, green, blue) * 0.4f);
		}
	}
}
