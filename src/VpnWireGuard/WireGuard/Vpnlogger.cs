/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/VpnWireGuard
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace VPN.WireGuard
{
    public class Vpnlogger : IDisposable
    {
        private struct UnixTimestamp
        {
            private long _ns;
            public UnixTimestamp(long ns) => _ns = ns;
            public bool IsEmpty => _ns == 0;
            public static UnixTimestamp Empty => new UnixTimestamp(0);
            public static UnixTimestamp Now {
                get {
                    var now = DateTimeOffset.UtcNow;
                    var ns = (now.Subtract(DateTimeOffset.FromUnixTimeSeconds(0)).Ticks * 100) % 1000000000;
                    return new UnixTimestamp(now.ToUnixTimeSeconds() * 1000000000 + ns);
                }
            }
            public long Nanoseconds => _ns;
            public override string ToString()
            {
                return DateTimeOffset.FromUnixTimeSeconds(_ns / 1000000000).LocalDateTime.ToString("HH:mm:ss");
            }
        }
        private struct Line
        {
            private const int maxLineLength = 512;
            private const int offsetTimeNs = 0;
            private const int offsetLine = 8;

            private readonly MemoryMappedViewAccessor _view;
            private readonly int _start;
            public Line(MemoryMappedViewAccessor view, uint index) => (_view, _start) = (view, (int)(Log.HeaderBytes + index * Bytes));

            public static int Bytes => maxLineLength + offsetLine;

            public UnixTimestamp Timestamp {
                get => new UnixTimestamp(_view.ReadInt64(_start + offsetTimeNs));
                set => _view.Write(_start + offsetTimeNs, value.Nanoseconds);
            }

            public string Text
            {
                get
                {
                    var textBytes = new byte[maxLineLength];
                    _view.ReadArray(_start + offsetLine, textBytes, 0, textBytes.Length);
                    var nullByte = Array.IndexOf<byte>(textBytes, 0);
                    if (nullByte <= 0)
                        return null;
                    return Encoding.UTF8.GetString(textBytes, 0, nullByte);
                }
                set
                {
                    if (value == null) {
                        _view.WriteArray(_start + offsetLine, new byte[maxLineLength], 0, maxLineLength);
                        return;
                    }
                    var textBytes = Encoding.UTF8.GetBytes(value);
                    var bytesToWrite = Math.Min(maxLineLength - 1, textBytes.Length);
                    _view.Write(_start + offsetLine + bytesToWrite, (byte)0);
                    _view.WriteArray(_start + offsetLine, textBytes, 0, bytesToWrite);
                }
            }

            public override string ToString()
            {
                var text = Text;
#               if TIMESTAMP_LOG_LINE
                if (text == null)
                    return null;
                var time = Timestamp;
                if (time.IsEmpty)
                    return null;
                return string.Format("{0}: {1}", time, text);
#               else
                return text;
#               endif
            }
        }
        private struct Log
        {
            private const uint maxLines = 2048;
            private const uint magic = 0xbadbabe;
            private const int offsetMagic = 0;
            private const int offsetNextIndex = 4;
            private const int offsetLines = 8;

            private readonly MemoryMappedViewAccessor _view;
            public Log(MemoryMappedViewAccessor view) => _view = view;

            public static int HeaderBytes => offsetLines;
            public static int Bytes => (int)(HeaderBytes + Line.Bytes * maxLines);

            public uint ExpectedMagic => magic;
            public uint Magic {
                get => _view.ReadUInt32(offsetMagic);
                set => _view.Write(offsetMagic, value);
            }

            public uint NextIndex {
                get => _view.ReadUInt32(offsetNextIndex);
                set => _view.Write(offsetNextIndex, value);
            }
            public unsafe uint InsertNextIndex() {
                byte* pointer = null;
                _view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                var ret = (uint)Interlocked.Increment(ref Unsafe.AsRef<Int32>(pointer + offsetNextIndex));
                _view.SafeMemoryMappedViewHandle.ReleasePointer();
                return ret;
            }

            public uint LineCount => maxLines;
            public Line this[uint i] => new Line(_view, i % maxLines);
            public void Clear() => _view.WriteArray(0, new byte[Bytes], 0, Bytes);
        }

        private Log log_;
        private FileStream stream_;
        private MemoryMappedFile mmap_;
        private MemoryMappedViewAccessor view_;
        public bool IsDisposed { get; private set; } = false;

        public Vpnlogger(string filename)
        {
            stream_ = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            stream_.SetLength(Log.Bytes);
            mmap_ = MemoryMappedFile.CreateFromFile(stream_, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            view_ = mmap_.CreateViewAccessor(0, Log.Bytes, MemoryMappedFileAccess.ReadWrite);
            log_ = new Log(view_);
            if (log_.Magic != log_.ExpectedMagic) {
                log_.Clear();
                log_.Magic = log_.ExpectedMagic;
            }
        }
        ~Vpnlogger() => Dispose();

        public void Dispose()
        {
            IsDisposed = true;
            log_ = default;
            MemoryMappedViewAccessor v = view_;
            view_ = null;
            if (v != null)
                try { v.Dispose(); } catch { }
            MemoryMappedFile m = mmap_;
            mmap_ = null;
            if (m != null)
                try { m.Dispose(); } catch { }
            FileStream s = stream_;
            stream_ = null;
            if (s != null)
                try { s.Dispose(); } catch { }
        }

        public static readonly uint CursorAll = uint.MaxValue;
        public List<string> FollowFromCursor(ref uint cursor)
        {
            if (IsDisposed)
                return new();

            List<string> list = new((int)log_.LineCount);
            uint i = cursor;
            bool isall = cursor == CursorAll;
            if (isall)
                i = log_.NextIndex;
            for (uint l = 0; l < log_.LineCount; ++l, ++i) {

                if (!isall && i % log_.LineCount == log_.NextIndex % log_.LineCount)
                    break;
                var entry = log_[i];
                cursor = (i + 1) % log_.LineCount;

#               if TIMESTAMP_LOG_LINE
                if (entry.Timestamp.IsEmpty) {
                    if (isall)
                        continue;
                    break;
                }
#               endif
                var text = entry.ToString();
                if (text == null) {
                    if (isall)
                        continue;
                    break;
                }
                list.Add(text);
            }
            return list;
        }
    }
}
