using SerousCommonLib.API;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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

			if (Network.GetItemNetworkAt(x, y) is ItemNetwork itemNet) {
				yield return "[c/ff0000:Item Network]";
				yield return $"  - ID: {itemNet.ID}";
				yield return $"  - Nodes: {itemNet.EntryCount}";
				yield return $"  - Items: {itemNet.items.Where(static p => p is { Destroyed: false }).Count()} active";
				yield return $"  - Pumps: {itemNet.PumpCount}";
				yield return $"  - Inventories: {itemNet.AdjacentInventoryCount}";
			}

			if (Network.GetFluidNetworkAt(x, y) is FluidNetwork fluidNet) {
				yield return "[c/00ff00:Fluid Network]";
				yield return $"  - ID: {fluidNet.ID}";
				yield return $"  - Nodes: {fluidNet.EntryCount}";
				yield return $"  - Pumps: {fluidNet.PumpCount}";
				yield return $"  - Storages: {fluidNet.AdjacentStorageCount}";
				yield return $"  - Net Fluid: [c/{NetworkHelper.GetNetColor(fluidNet.Storage.CurrentCapacity, fluidNet.NetFluid)}:{fluidNet.NetFluid:+0.###;-#.###} L/gt]";
			}

			if (Network.GetPowerNetworkAt(x, y) is PowerNetwork powerNet) {
				yield return "[c/0000ff:Power Network]";
				yield return $"  - ID: {powerNet.ID}";
				yield return $"  - Nodes: {powerNet.EntryCount}";
				yield return $"  - Storages: {powerNet.AdjacentStorageCount}";
				yield return $"  - Net Power: [c/{NetworkHelper.GetNetColor((double)powerNet.Storage.CurrentCapacity, (double)powerNet.NetPower)}:{(double)powerNet.NetPower:+0.###;-#.###} TF/gt]";
			}
		}
	}
}
