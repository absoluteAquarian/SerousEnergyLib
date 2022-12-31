using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace SerousEnergyLib.Systems {
	public sealed class UIHandler : ModSystem {
		// TODO: should the "block actions under certain UI things" logic be moved from Magic Storage to the common library?

		public static UserInterface uiInterface;

		public static IMachine ActiveMachine { get; private set; }

		public override void Load() {
			uiInterface = new();
		}

		public override void Unload() {
			base.Unload();
		}

		internal static bool pendingUIChangeForAnyReason;

		private static void PendingResolutionChange(Vector2 resolution) {
			pendingUIChangeForAnyReason = true;
		}

		public static void OpenUI(IMachine machine) {
			if (ActiveMachine is not null)
				CloseUI();

			uiInterface.SetState(machine.MachineUI);
		}

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
