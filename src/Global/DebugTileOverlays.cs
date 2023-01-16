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
using SerousEnergyLib.Pathfinding.Nodes;

namespace SerousEnergyLib.Global {
	internal class DebugTileOverlays : GlobalTile {
		private static Asset<Texture2D> overlay, coarseOverlay;

		public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];

			if (Main.LocalPlayer.HeldItem.ModItem is not DebugTool debug || debug.ActiveNetwork == Point16.NegativeOne)
				return;

			int netX = debug.ActiveNetwork.X, netY = debug.ActiveNetwork.Y;
			
			byte red = 0, green = 0, blue = 0;

			// Tile might be an adjacent inventory/storage
			Point16 orig = new Point16(i, j);
			Point16 coarse = orig / CoarseNode.Coarseness * CoarseNode.Coarseness;
			int diffX = orig.X - coarse.X, diffY = orig.Y - coarse.Y;

			// If the mode is set to "all coarse nodes", then check the edge and force the drawing, even if the tile isn't part of the network
			bool shouldForceCoarseDraw = DebugTool.ShowCoarseNodesMode == 2 && (diffX != 1 || diffY != 1);

			Vector2 worldPosition = new Vector2(i * 16, j * 16);
			Vector2 drawPosition = worldPosition - Main.screenPosition + TileFunctions.GetLightingDrawOffset();

			ModTile modTile = TileLoader.GetTile(type);
			if ((modTile is IMachineTile machineTile && machineTile.GetMachineEntity() is IInventoryMachine or IFluidMachine or IPoweredMachine) || NetworkHelper.TileIsChest(type)) {
				Span<Point16> directions = stackalloc Point16[] {
					new(-1, 0), new(0, -1), new(1, 0), new(0, 1)
				};

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
			if (tile.Get<NetworkInfo>().Type == NetworkType.None) {
				if (shouldForceCoarseDraw)
					goto skipOverlayDraw;

				return;
			}

			if (DebugTool.thresholdPathIndex == -1 || !DebugTool.GetThresholdsAtTileTarget().Any()) {
				if (Network.GetItemNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
					red = 0xff;
				if (Network.GetFluidNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
					green = 0xff;
				if (Network.GetPowerNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
					blue = 0xff;
			} else {
				var thresholds = DebugTool.GetThresholdsAtTileTarget().GetEnumerator();

				int totalPaths = 0;
				while (thresholds.MoveNext() && totalPaths < DebugTool.thresholdPathIndex)
					totalPaths += thresholds.Current.paths.Length;

				var threshold = thresholds.Current;
				int index = DebugTool.thresholdPathIndex - totalPaths;

				if (threshold.paths is { Length: > 0 } && index >= 0 && index < threshold.paths.Length && threshold.paths[index].path.Any(p => p == orig)) {
					if (Network.GetItemNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
						red = 0xff;
					if (Network.GetFluidNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
						green = 0xff;
					if (Network.GetPowerNetworksAt(netX, netY).Where(n => n.HasEntry(i, j)).Any())
						blue = 0xff;
				}
			}

			checkColors:

			// Tile isn't part of the active network
			if (red == 0 && green == 0 && blue == 0) {
				if (shouldForceCoarseDraw)
					goto skipOverlayDraw;

				return;
			}

			overlay ??= ModContent.Request<Texture2D>("SerousEnergyLib/Assets/Tiles/NetworkTileOverlay");

			spriteBatch.Draw(overlay.Value, drawPosition, null, new Color(red, green, blue) * 0.4f);

			// Check if the node is a threshold
			skipOverlayDraw:

			if (DebugTool.ShowCoarseNodesMode == 2 || (DebugTool.ShowCoarseNodesMode == 1 && Network.GetNetworkAt(i, j, NetworkType.Items | NetworkType.Fluids | NetworkType.Power) is NetworkInstance net && net.TryGetThresholdTile(orig, out _))) {
				coarseOverlay ??= ModContent.Request<Texture2D>("SerousEnergyLib/Assets/Tiles/CoarseNodeThresholdOverlay");

				int frameX, frameY;

				if (diffX == 0)
					frameX = 0;
				else if (diffX == CoarseNode.Stride - 1)
					frameX = 2;
				else
					frameX = 1;

				if (diffY == 0)
					frameY = 0;
				else if (diffY == CoarseNode.Stride - 1)
					frameY = 2;
				else
					frameY = 1;

				// Frame is empty
				if (frameX == 1 && frameY == 1)
					return;

				Vector3 hsl = Main.rgbToHsl(Main.DiscoColor);

				// Offset the hue
				hsl.X += coarse.X * 0.125913f + coarse.Y * 0.378349f;
				hsl.X %= 1;

				Color disco = Main.hslToRgb(hsl);

				spriteBatch.Draw(coarseOverlay.Value, drawPosition, coarseOverlay.Value.Frame(3, 3, frameX, frameY), disco);
			}
		}
	}
}
