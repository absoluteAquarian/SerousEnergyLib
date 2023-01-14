using SerousCommonLib.API;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items {
	internal class DebugTool : ModItem {
		public override string Texture => "Terraria/Images/Item_" + ItemID.IronPickaxe;

		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.IronPickaxe);
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.UseSound = null;
			Item.rare = ItemRarityID.Pink;
			Item.value = 0;
			Item.damage = 0;
			Item.knockBack = 0;
			Item.pick = 0;
			Item.useTime = Item.useAnimation = 2;
			Item.autoReuse = false;
		}

		public Point16 ActiveNetwork { get; private set; } = Point16.NegativeOne;

		public override bool AltFunctionUse(Player player) => true;

		public override bool? UseItem(Player player) {
			if (player.altFunctionUse == 2) {
				if (Network.GetNetworkAt(Player.tileTargetX, Player.tileTargetY, NetworkType.Items | NetworkType.Fluids | NetworkType.Power) is not null) {
					ActiveNetwork = new Point16(Player.tileTargetX, Player.tileTargetY);

					if (player.whoAmI == Main.myPlayer)
						Main.NewText($"Debug coordinates set to (X: {Player.tileTargetX}, Y: {Player.tileTargetY})");
				}
			} else {
				ActiveNetwork = Point16.NegativeOne;

				if (player.whoAmI == Main.myPlayer)
					Main.NewText("Debug coordinates cleared");
			}

			return true;
		}

		public override void UpdateInventory(Player player) {
			if (ActiveNetwork != Point16.NegativeOne && Network.GetNetworkAt(ActiveNetwork.X, ActiveNetwork.Y, NetworkType.Items | NetworkType.Fluids | NetworkType.Power) is null)
				ActiveNetwork = Point16.NegativeOne;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			var lines = GetNetworkDebug();

			if (!lines.Any())
				TooltipHelper.FindAndRemoveLine(tooltips, "<NET_INFO>");
			else
				TooltipHelper.FindAndInsertLines(Mod, tooltips, "<NET_INFO>", static i => "NetDebug_" + i, string.Join('\n', lines));
		}

		private IEnumerable<string> GetNetworkDebug() {
			if (ActiveNetwork == Point16.NegativeOne)
				yield break;

			int x = ActiveNetwork.X, y = ActiveNetwork.Y;

			foreach (var net in Network.GetNetworksAt(x, y, NetworkType.Items | NetworkType.Fluids | NetworkType.Power)) {
				if (net is ItemNetwork itemNet) {
					string lang = "Mods.SerousEnergyLib.Debug.Item.";

					yield return Language.GetTextValue(lang + "Header");
					yield return Language.GetTextValue(lang + "ID", itemNet.ID);
					yield return Language.GetTextValue(lang + "NodeCount", itemNet.EntryCount);
					yield return Language.GetTextValue(lang + "MovingItems", itemNet.items.Where(static p => p is { Destroyed: false }).Count());
					yield return Language.GetTextValue(lang + "PumpCount", itemNet.PumpCount);
					yield return Language.GetTextValue(lang + "AdjTiles", itemNet.AdjacentInventoryCount);
				}

				if (net is FluidNetwork fluidNet) {
					string lang = "Mods.SerousEnergyLib.Debug.Fluid.";

					yield return Language.GetTextValue(lang + "Header");
					yield return Language.GetTextValue(lang + "ID", fluidNet.ID);
					yield return Language.GetTextValue(lang + "NodeCount", fluidNet.EntryCount);
					yield return Language.GetTextValue(lang + "Current", fluidNet.Storage.CurrentCapacity, fluidNet.Storage.MaxCapacity);
					yield return Language.GetTextValue(lang + "Net", NetworkHelper.GetNetColor(fluidNet.NetFluid), fluidNet.NetFluid);
					yield return Language.GetTextValue(lang + "PumpCount", fluidNet.PumpCount);
					yield return Language.GetTextValue(lang + "AdjTiles", fluidNet.AdjacentStorageCount);
				}

				if (net is PowerNetwork powerNet) {
					string lang = "Mods.SerousEnergyLib.Debug.Power.";

					yield return Language.GetTextValue(lang + "Header");
					yield return Language.GetTextValue(lang + "ID", powerNet.ID);
					yield return Language.GetTextValue(lang + "NodeCount", powerNet.EntryCount);
					yield return Language.GetTextValue(lang + "Current", (double)powerNet.Storage.CurrentCapacity, (double)powerNet.Storage.MaxCapacity);
					yield return Language.GetTextValue(lang + "Net", NetworkHelper.GetNetColor((double)powerNet.NetPower), (double)powerNet.NetPower);
					yield return Language.GetTextValue(lang + "AdjTiles", powerNet.AdjacentStorageCount);
				}
			}
		}
	}
}
