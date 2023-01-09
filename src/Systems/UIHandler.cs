using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace SerousEnergyLib.Systems {
	/// <summary>
	/// The central class for interfacing with machine UIs
	/// </summary>
	public sealed class UIHandler : ModSystem {
		// TODO: should the "block actions under certain UI things" logic be moved from Magic Storage to the common library?

		#pragma warning disable CS1591
		public static UserInterface uiInterface;

		/// <summary>
		/// Which machine instance is currently open, or <see langword="null"/> if no machine UI is open
		/// </summary>
		public static IMachine ActiveMachine { get; private set; }

		public override void Load() {
			if (Main.dedServ)
				return;

			uiInterface = new();

			Main.OnResolutionChanged += PendingResolutionChange;
		}

		public override void Unload() {
			if (!Main.dedServ)
				Main.OnResolutionChanged -= PendingResolutionChange;

			uiInterface = null;
		}

		internal static bool pendingUIChangeForAnyReason;

		private static void PendingResolutionChange(Vector2 resolution) {
			pendingUIChangeForAnyReason = true;
		}

		/// <summary>
		/// Closes the current machine UI if one is open, then opens the UI for <paramref name="machine"/>
		/// </summary>
		/// <param name="machine"></param>
		public static void OpenUI(IMachine machine) {
			if (ActiveMachine is not null)
				CloseUI();

			uiInterface.SetState(machine.MachineUI);

			ActiveMachine = machine;
		}

		/// <summary>
		/// Closes the current machine UI if one is open
		/// </summary>
		public static void CloseUI() {
			if (ActiveMachine is null || uiInterface.CurrentState is null)
				return;

			uiInterface.SetState(null);
			pendingUIChangeForAnyReason = false;

			ActiveMachine = null;
		}

		private static float lastKnownUIScale = -1;

		public override void UpdateUI(GameTime gameTime) {
			if (lastKnownUIScale != Main.UIScale) {
				lastKnownUIScale = Main.UIScale;
				pendingUIChangeForAnyReason = true;
			}

			if (!Main.playerInventory)
				CloseUI();

			uiInterface?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
			if (inventoryIndex != -1) {
				layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("SerousEnergyLib: Machine UI",
					static () => {
						if (uiInterface?.CurrentState is not null) {
							Main.hidePlayerCraftingMenu = true;

							uiInterface.Draw(Main.spriteBatch, Main.gameTimeCache);
						}

						return true;
					}, InterfaceScaleType.UI));
			}
		}
	}
}
