using Microsoft.Xna.Framework.Input;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Players {
	internal class KeybindPlayer : ModPlayer {
		public static ModKeybind showCoarseNodes, showThresholdPaths, nextThresholdPath;

		public override void Load() {
			if (!Main.dedServ) {
				// TODO: only load on debug builds?
				showCoarseNodes = KeybindLoader.RegisterKeybind(SerousMachines.Instance, "ShowCoarseNodes", Keys.NumPad1);
				showThresholdPaths = KeybindLoader.RegisterKeybind(SerousMachines.Instance, "ShowThresholdPaths", Keys.NumPad2);
				nextThresholdPath = KeybindLoader.RegisterKeybind(SerousMachines.Instance, "NextThresholdPath", Keys.NumPad3);
			}
		}

		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (Player.HeldItem.ModItem is DebugTool tool && tool.ActiveNetwork != Point16.NegativeOne) {
				if (showCoarseNodes.JustPressed) {
					DebugTool.ShowCoarseNodesMode = ++DebugTool.ShowCoarseNodesMode % 3;
					SoundEngine.PlaySound(SoundID.MenuTick);

					string mode = DebugTool.ShowCoarseNodesMode switch {
						0 => "Hiding coarse nodes",
						1 => "Showing coarse node thresholds",
						2 => "Showing full coarse node edges",
						_ => null
					};

					Main.NewText($"[c/ff8000:{mode}]");
				}

				if (showThresholdPaths.JustPressed) {
					DebugTool.ShowThresholdPaths = !DebugTool.ShowThresholdPaths;
					SoundEngine.PlaySound(SoundID.MenuTick);

					Main.NewText($"[c/ff8000:{(DebugTool.ShowThresholdPaths ? "Displaying" : "Hiding")} threshold paths]");
				}

				if (nextThresholdPath.JustPressed) {
					if (DebugTool.ShowCoarseNodesMode > 0 && DebugTool.ShowThresholdPaths) {
						var thresholds = DebugTool.GetThresholdsAtTileTarget();

						if (!thresholds.Any())
							DebugTool.thresholdPathIndex = -1;
						else {
							int totalPaths = thresholds.Sum(static t => t.paths.Length);

							DebugTool.thresholdPathIndex = ++DebugTool.thresholdPathIndex % totalPaths;

							SoundEngine.PlaySound(SoundID.MenuTick);
						}
					} else
						DebugTool.thresholdPathIndex = -1;
				}
			}
		}
	}
}
