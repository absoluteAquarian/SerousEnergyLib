using Ionic.Zlib;
using SerousEnergyLib.TileData;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems {
	internal class WorldNetworkIO : ModSystem {
		public override void SaveWorldData(TagCompound tag) {
			byte[] data = TransformData(Main.tile.GetData<NetworkInfo>());

			// Compress the data
			using MemoryStream decompressed = new MemoryStream(data);
			using DeflateStream compression = new DeflateStream(decompressed, CompressionMode.Compress, CompressionLevel.BestSpeed);
			using MemoryStream compressed = new MemoryStream();
			compression.CopyTo(compressed);

			tag["network"] = compressed.ToArray();
		}

		public override void LoadWorldData(TagCompound tag) {
			if (tag.GetByteArray("network") is byte[] data) {
				// Decompress the data
				using MemoryStream compressed = new MemoryStream(data);
				using DeflateStream decompression = new DeflateStream(compressed, CompressionMode.Decompress, CompressionLevel.BestSpeed);
				using MemoryStream decompressed = new MemoryStream();
				decompression.CopyTo(decompressed);

				TransformData(decompressed.ToArray(), Main.tile.GetData<NetworkInfo>());
			}
		}

		private static byte[] TransformData(NetworkInfo[] data) {
			byte[] converted = new byte[data.Length];

			unsafe {
				fixed (NetworkInfo* fixedPtr = data) fixed (byte* fixedConvPtr = converted) {
					NetworkInfo* ptr = fixedPtr;
					byte* convPtr = fixedConvPtr;
					int length = data.Length;

					for (int i = 0; i < length; i++, ptr++, convPtr++)
						*convPtr = ptr->netData;
				}
			}

			return converted;
		}

		private static void TransformData(byte[] data, NetworkInfo[] existing) {
			if (data.Length != existing.Length) {
				SerousMachines.Instance.Logger.Warn($"Saved data length ({data.Length}) did not match the world data length ({existing.Length}), data will not be loaded.");
				return;
			}

			unsafe {
				fixed (byte* fixedPtr = data) fixed (NetworkInfo* fixedConvPtr = existing) {
					byte* ptr = fixedPtr;
					NetworkInfo* convPtr = fixedConvPtr;
					int length = data.Length;

					for (int i = 0; i < length; i++, ptr++, convPtr++)
						convPtr->netData = *ptr;
				}
			}
		}
	}
}
