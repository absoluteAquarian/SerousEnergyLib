using Ionic.Zlib;
using System.IO;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib {
	/// <summary>
	/// A helper class for I/O logic
	/// </summary>
	public static class IOHelper {
		/// <summary>
		/// Compresses <paramref name="data"/> at the provided <paramref name="level"/>
		/// </summary>
		/// <param name="data">The decompressed byte array</param>
		/// <param name="level">The compression level</param>
		/// <returns>The compressed byte array</returns>
		public static byte[] Compress(byte[] data, CompressionLevel level) {
			using MemoryStream decompressed = new MemoryStream(data);
			using DeflateStream compression = new DeflateStream(decompressed, CompressionMode.Compress, level);
			using MemoryStream compressed = new MemoryStream();
			compression.CopyTo(compressed);
			return compressed.ToArray();
		}

		/// <summary>
		/// Decompresses <paramref name="data"/> at the provided <paramref name="level"/>
		/// </summary>
		/// <param name="data">The compressed byte array</param>
		/// <param name="level">The compression level</param>
		/// <returns>The decompressed byte array</returns>
		public static byte[] Decompress(byte[] data, CompressionLevel level) {
			using MemoryStream compressed = new MemoryStream(data);
			using DeflateStream decompression = new DeflateStream(compressed, CompressionMode.Decompress, level);
			using MemoryStream decompressed = new MemoryStream();
			decompression.CopyTo(decompressed);
			return decompressed.ToArray();
		}

		/// <summary>
		/// Generates a hash for a byte array.  This method is <b>NOT</b> optimized for <see cref="object.GetHashCode"/> usage!
		/// </summary>
		public static int ComputeDataHash(byte[] data) {
			// Taken from: https://stackoverflow.com/a/468084
			unchecked {
				const int p = 16777619;
				int hash = (int)2166136261;

				for (int i = 0; i < data.Length; i++)
					hash = (hash ^ data[i]) * p;

				hash += hash << 13;
				hash ^= hash >> 7;
				hash += hash << 3;
				hash ^= hash >> 17;
				hash += hash << 5;
				return hash;
			}
		}

		/// <summary>
		/// Generates a hash for a <see cref="TagCompound"/>.  This method is <b>NOT</b> optimized for <see cref="object.GetHashCode"/> usage!
		/// </summary>
		public static int ComputeDataHash(TagCompound tag) {
			if (tag is null)
				return 0;

			using MemoryStream ms = new();
			TagIO.ToStream(tag, ms);
			return ComputeDataHash(ms.ToArray());
		}
	}
}
