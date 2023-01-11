using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
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
		/// Closes the current machine UI if one is open, then opens the UI for <paramref name="machine"/>.<br/>
		/// If the current machine UI is the same object from <paramref name="machine"/>, the UI is not reopened.
		/// </summary>
		/// <param name="machine">The machine entity which opened the UI</param>
		public static void OpenUI(IMachine machine) {
			if (object.ReferenceEquals(ActiveMachine, machine)) {
				// Only close the UI
				CloseUI();
				return;
			}

			bool hadOtherOpen = false;
			if (ActiveMachine is not null) {
				hadOtherOpen = true;
				CloseUI();
			}

			Main.playerInventory = true;

			bool hadChestOpen = Main.LocalPlayer.chest != -1;

			CloseEverythingButUI();

			uiInterface.SetState(machine.MachineUI);

			ActiveMachine = machine;

			// Other misc things that wouldn't belong in CloseEverythingButUI()
			if (PlayerInput.GrappleAndInteractAreShared)
				PlayerInput.Triggers.JustPressed.Grapple = false;
			Main.recBigList = false;
			SoundEngine.PlaySound(hadChestOpen || hadOtherOpen ? SoundID.MenuTick : SoundID.MenuOpen);
			Recipe.FindRecipes();
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

		/// <summary>
		/// This method closes everything in preparation for opening a machine's UI
		/// </summary>
		public static void CloseEverythingButUI() {
			Player player = Main.LocalPlayer;

			Main.mouseRightRelease = false;
			
			if (player.sign > -1) {
				// Close the active sign UI
				SoundEngine.PlaySound(SoundID.MenuClose);
				player.sign = -1;
				Main.editSign = false;
				Main.npcChatText = string.Empty;
			}

			if (Main.editChest) {
				// Close the active chest name UI
				SoundEngine.PlaySound(SoundID.MenuTick);
				Main.editChest = false;
				Main.npcChatText = string.Empty;
			}

			if (player.editedChestName) {
				// Close the active ches namet UI
				NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
				player.editedChestName = false;
			}

			if (player.talkNPC > -1) {
				// Close the active town NPC dialogue/shop UI
				player.SetTalkNPC(-1);
				Main.npcChatCornerItem = 0;
				Main.npcChatText = string.Empty;
			}

			// Close the active chest UI
			player.chest = -1;
			Main.stackSplit = 600;
		}
	}
}
