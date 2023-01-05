using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
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
			uiInterface = new();

			Main.OnResolutionChanged += PendingResolutionChange;
		}

		public override void Unload() {
			base.Unload();
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
		}

		/// <summary>
		/// Closes the current machine UI if one is open
		/// </summary>
		public static void CloseUI() {
			if (ActiveMachine is null || uiInterface.CurrentState is null)
				return;

			uiInterface.SetState(null);
			pendingUIChangeForAnyReason = false;
		}

		private static float lastKnownUIScale = -1;

		public override void UpdateUI(GameTime gameTime) {
			if (lastKnownUIScale != Main.UIScale) {
				lastKnownUIScale = Main.UIScale;
				pendingUIChangeForAnyReason = true;
			}

			if (!Main.playerInventory)
				CloseUI();
		}
	}
}
