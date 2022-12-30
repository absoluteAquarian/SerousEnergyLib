using Ionic.Zlib;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
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
			tag["network"] = IOHelper.Compress(data, CompressionLevel.BestSpeed);

			tag["maps"] = Network.networks
				.Select(static kvp => new TagCompound() {
					["type"] = (byte)kvp.Key,
					["data"] = kvp.Value
						.Select(static n => {
							TagCompound data = new();
							n.SaveData(data);
							return data;
						})
						.ToList()
				})
				.ToList();
		}

		public override void LoadWorldData(TagCompound tag) {
			if (tag.GetByteArray("network") is byte[] data) {
				// Decompress the data
				TransformData(IOHelper.Decompress(data, CompressionLevel.BestSpeed), Main.tile.GetData<NetworkInfo>());
			}

			Network.networks.Clear();
			if (tag.GetList<TagCompound>("maps") is List<TagCompound> maps) {
				foreach (var map in maps) {
					byte type = map.GetByte("type");

					if (type == 0 || type > (byte)(NetworkType.Items | NetworkType.Fluids | NetworkType.Power))
						throw new IOException("Invalid network type: " + type);

					NetworkType filter = (NetworkType)type;

					if (map.GetList<TagCompound>("data") is List<TagCompound> list) {
						List<NetworkInstance> instances;
						Network.networks.Add(filter, instances = new());

						foreach (var net in list) {
							NetworkInstance instance = new NetworkInstance(filter);
							instance.ReserveNextID();

							instance.LoadData(net);

							instances.Add(instance);
						}
					}
				}
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
