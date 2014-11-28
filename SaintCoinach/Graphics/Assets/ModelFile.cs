﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Assets {
    using IO;

    /// <summary>
    /// Model file stored inside SqPack.
    /// </summary>
    public class ModelFile : File {
        const int PartsCount = 0x0B;

        #region Fields
        private int[] _BlockOffsets = null;
        private WeakReference<Model> _ModelCache = null;
        private WeakReference<byte[]> _CombinedCache = null;
        private WeakReference<byte[]>[] _PartsCache = new WeakReference<byte[]>[PartsCount];
        #endregion

        #region Constructor
        public ModelFile(Directory directory, FileCommonHeader commonHeader) : base(directory, commonHeader) { }
        #endregion

        #region Read
        public Model GetModel() {
            if (Directory.Pack.Collection == null)
                throw new NotSupportedException();

            Model model;

            if (_ModelCache == null || !_ModelCache.TryGetTarget(out model)) {
                model = new Model(this);

                if (_ModelCache == null)
                    _ModelCache = new WeakReference<Model>(model);
                else
                    _ModelCache.SetTarget(model);
            }

            return model;
        }
        public override byte[] GetData() {
            byte[] data;

            if (_CombinedCache == null || !_CombinedCache.TryGetTarget(out data)) {
                data = new byte[0];
                var o = 0;

                for (var i = 0; i < PartsCount; ++i) {
                    var part = GetPart(i);
                    Array.Resize(ref data, data.Length + part.Length);
                    Array.Copy(data, o, part, 0, part.Length);
                    o += part.Length;
                }

                if (_CombinedCache == null)
                    _CombinedCache = new WeakReference<byte[]>(data);
                else
                    _CombinedCache.SetTarget(data);
            }

            return data;
        }
        public byte[] GetPart(int part) {
            if (part >= PartsCount)
                throw new ArgumentOutOfRangeException("part");

            byte[] buffer;

            if (_PartsCache[part] == null || !_PartsCache[part].TryGetTarget(out buffer)) {
                buffer = ReadPart(part);

                if (_PartsCache[part] == null)
                    _PartsCache[part] = new WeakReference<byte[]>(buffer);
                else
                    _PartsCache[part].SetTarget(buffer);
            }

            return buffer;
        }
        private byte[] ReadPart(int part) {
            const int MinimumBlockOffset = 0x9C;
            const int BlockCountOffset = 0xB2;

            if (_BlockOffsets == null)
                _BlockOffsets = GetBlockOffsets();

            var minBlock = BitConverter.ToInt16(CommonHeader._Buffer, MinimumBlockOffset + 2 * part);
            var blockCount = BitConverter.ToInt16(CommonHeader._Buffer, BlockCountOffset + 2 * part);

            var sourceStream = GetSourceStream();
            byte[] buffer;

            using (var dataStream = new MemoryStream()) {
                for (var i = 0; i < blockCount; ++i) {
                    sourceStream.Position = CommonHeader.EndOfHeader + _BlockOffsets[minBlock + i];
                    ReadBlock(sourceStream, dataStream);
                }

                buffer = dataStream.ToArray();
            }

            return buffer;
        }
        private int[] GetBlockOffsets() {
            const int BlockInfoOffset = 0xD0;

            var currentOffset = 0;
            var offsets = new List<int>();
            for (int i = BlockInfoOffset; i + 2 <= CommonHeader._Buffer.Length; i += 2) {
                var len = BitConverter.ToUInt16(CommonHeader._Buffer, i);
                if (len == 0)
                    break;
                offsets.Add(currentOffset);
                currentOffset += len;
            }

            return offsets.ToArray();
        }
        #endregion
    }
}