using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SerousEnergyLib {
	public static class ItemFunctions {
		public static int NewClonedItem(IEntitySource source, Vector2 position, Item item, int stack = 1, int prefix = 0) {
			int index = Item.NewItem(source, position, item.type, stack, true, prefix);
			Item clone = Main.item[index] = item.Clone();
			clone.whoAmI = index;
			clone.position = position;
			clone.stack = stack;

			// Sync the item for mp
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f, 0f, 0f, 0, 0, 0);

			return index;
		}

		// Copied from Magic Storage
		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack = false, bool checkPrefix = true) {
			int stack1 = item1.stack;
			int stack2 = item2.stack;
			int prefix1 = item1.prefix;
			int prefix2 = item2.prefix;
			bool favorite1 = item1.favorited;
			bool favorite2 = item2.favorited;

			item1.favorited = false;
			item2.favorited = false;

			bool equal;

			if (!checkPrefix) {
				item1.prefix = 0;
				item2.prefix = 0;
			}

			if (!checkStack) {
				item1.stack = 1;
				item2.stack = 1;
			}

			if (!ItemData.Matches(item1, item2)) {
				equal = false;
				goto ReturnFromMethod;
			}

			try {
				equal = TagIOSave(item1).SequenceEqual(TagIOSave(item2));
			} catch {
				// Swallow the exception and disallow stacking
				equal = false;
			}

ReturnFromMethod:

			item1.stack = stack1;
			item2.stack = stack2;
			item1.prefix = prefix1;
			item2.prefix = prefix2;
			item1.favorited = favorite1;
			item2.favorited = favorite2;

			return equal;
		}

		private static byte[] TagIOSave(Item item) {
			using MemoryStream memoryStream = new();
			TagIO.ToStream(ItemIO.Save(item), memoryStream);
			return memoryStream.ToArray();
		}

		// Derived from https://github.com/Eternal-Team/BaseLibrary/blob/1.3/Utility/RenderingUtility.cs
		public static void DrawItemInWorld(this SpriteBatch spriteBatch, Item item, Vector2 position, float size, float rotation = 0f) {
			if (!item.IsAir) {
				Texture2D itemTexture = TextureAssets.Item[item.type].Value;
				Rectangle rect = Main.itemAnimations[item.type] != null ? Main.itemAnimations[item.type].GetFrame(itemTexture) : itemTexture.Frame();
				Color newColor = Color.White;
				float pulseScale = 1f;
				ItemSlot.GetItemLight(ref newColor, ref pulseScale, item, outInTheWorld: true);

				int width = rect.Width;
				int height = rect.Height;
				float drawScale = 1f;
				if (width > size || height > size) {
					if (width > height)
						drawScale = size / width;
					else
						drawScale = size / height;
				}

				Vector2 origin = rect.Size() * 0.5f;

				float totalScale = pulseScale * drawScale;

				if (ItemLoader.PreDrawInWorld(item, spriteBatch, item.GetColor(Color.White), item.GetAlpha(newColor), ref rotation, ref totalScale, item.whoAmI)) {
					spriteBatch.Draw(itemTexture, position, rect, item.GetAlpha(newColor), rotation, origin, totalScale, SpriteEffects.None, 0f);

					if (item.color != Color.Transparent)
						spriteBatch.Draw(itemTexture, position, rect, item.GetColor(Color.White), rotation, origin, totalScale, SpriteEffects.None, 0f);
				}

				ItemLoader.PostDrawInWorld(item, spriteBatch, item.GetColor(Color.White), item.GetAlpha(newColor), rotation, totalScale, item.whoAmI);

				if (ItemID.Sets.TrapSigned[item.type])
					spriteBatch.Draw(TextureAssets.Wire.Value, position + new Vector2(40f, 40f) * drawScale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), drawScale, SpriteEffects.None, 0f);
			}
		}
	}
}
