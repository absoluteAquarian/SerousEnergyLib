using Ionic.Zlib;
using System;
using System.IO;

namespace SerousEnergyLib {
	internal class CompressionStream : IDisposable {
		private MemoryStream stream;
		public BinaryWriter writer;

		public CompressionStream() {
			stream = new MemoryStream();
			writer = new(stream);
		}

		public void WriteToStream(BinaryWriter writer) {
			byte[] decompressed = stream.ToArray();
			byte[] compressed = IOHelper.Compress(decompressed, CompressionLevel.BestCompression);

			writer.Write(compressed.Length);
			writer.Write(compressed);
		}

		public void Dispose() {
			writer.Dispose();
			stream.Dispose();
		}
	}
}
