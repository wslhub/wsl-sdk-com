using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

// Excerpted from https://stackoverflow.com/questions/2586159/does-a-wrapper-class-for-a-com-interop-istream-already-exist

namespace WslSdk.Interop
{
    internal sealed class ComIStreamWrapper : Stream
    {
        private IStream nativeStream;
        private IntPtr handle;

        public ComIStreamWrapper(IStream source)
        {
            nativeStream = source;
            handle = Marshal.AllocCoTaskMem(8);
        }

        ~ComIStreamWrapper()
        {
            Marshal.FreeCoTaskMem(handle);
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }

        public override void Flush()
        {
            nativeStream.Commit(0);
        }

        public override long Length
        {
            get
            {
                STATSTG stat;
                nativeStream.Stat(out stat, 1);
                return stat.cbSize;
            }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
                throw new NotImplementedException();

            nativeStream.Read(buffer, count, handle);
            return Marshal.ReadInt32(handle);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            nativeStream.Seek(offset, (int)origin, handle);
            return Marshal.ReadInt64(handle);
        }

        public override void SetLength(long value)
        {
            nativeStream.SetSize(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
                throw new NotImplementedException();

            nativeStream.Write(buffer, count, IntPtr.Zero);
        }
    }
}
