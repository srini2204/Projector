using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Utils
{
    public class CircularStream : Stream
    {
        private ReusableTaskCompletionSource<int> _taskCompletionSource;
        private byte[] _buffer;

        private bool _isOpen;
        private int _capacity;
        private int _writePosition;
        private int _readPosition;
        private int _size;

        //0 for false, 1 for true. 
        private long _resourceSync = 0;

        // _readWriteBalance < 0 means there are free awaiters and not enough items.
        // _readWriteBalance > 0 means the opposite is true.
        private int _readWriteBalance = 0;

        public CircularStream()
            : this(0)
        {

        }

        public CircularStream(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            Contract.EndContractBlock();

            _buffer = new byte[capacity];
            _capacity = capacity;
            _isOpen = true;
            _taskCompletionSource = new ReusableTaskCompletionSource<int>();
        }

        public override bool CanRead
        {
            get { return _isOpen; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _isOpen; }
        }

        public override void Flush()
        {

        }

        public override long Length
        {
            get { return Volatile.Read(ref _size); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Not enough bytes in the buffer");
            Contract.EndContractBlock();

            var someoneIsWaiting = (Interlocked.Exchange(ref _readWriteBalance, 1) != 0); // means on the othe side someone is waiting

            //if (!_isOpen) __Error.StreamIsClosed();
            //if (!CanWrite) __Error.WriteNotSupported();

            int i = _writePosition + count;
            // Check for overflow
            if (i < 0)
                throw new IOException("Stream is too long");

            var size = Length;
            if (count > _capacity - size)
            {
                if (Interlocked.Exchange(ref _resourceSync, 1) != 0)
                {
                    var spin = new SpinWait();
                    while (Interlocked.Exchange(ref _resourceSync, 1) != 0)
                    {
                        spin.SpinOnce();
                    }
                }

                bool allocatedNewArray = EnsureCapacity(i);

                Interlocked.Exchange(ref _resourceSync, 0);
            }


            var bytesToWriteBeforeTheEnd = _capacity - _writePosition;
            // check if there is enough space from _writePosition till ...
            if (count < bytesToWriteBeforeTheEnd) // no overlapping
            {
                CopyBuffer(buffer, offset, _buffer, _writePosition, count);
                _writePosition += count;

            }
            else // there will be overlapping
            {
                // write till the end of the buffer first
                CopyBuffer(buffer, offset, _buffer, _writePosition, bytesToWriteBeforeTheEnd);

                _writePosition = 0;

                // then write the rest
                CopyBuffer(buffer, offset + bytesToWriteBeforeTheEnd, _buffer, _writePosition, count - bytesToWriteBeforeTheEnd);
                _writePosition = count - bytesToWriteBeforeTheEnd;
            }


            Interlocked.Add(ref _size, count);

            if (someoneIsWaiting)
            {
                _taskCompletionSource.SetResult(count);
            }

            Interlocked.Exchange(ref _readWriteBalance, 0);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Not enough bytes in the buffer");
            Contract.EndContractBlock();

            //if (!_isOpen) __Error.StreamIsClosed();

            if (Interlocked.Exchange(ref _resourceSync, 1) != 0)
            {
                var spin = new SpinWait();
                while (Interlocked.Exchange(ref _resourceSync, 1) != 0)
                {
                    spin.SpinOnce();
                }

            }


            int bytesToRead = (int)Length;

            if (bytesToRead > count)
            {
                bytesToRead = count;
            }

            if (bytesToRead == 0)
            {
                return 0;
            }

            var bytesToReadBeforeTheEnd = _capacity - _readPosition;
            // check if there is enough space from _writePosition till ...
            if (bytesToRead < bytesToReadBeforeTheEnd) // no overlapping
            {
                CopyBuffer(_buffer, _readPosition, buffer, offset, bytesToRead);
                _readPosition += bytesToRead;

            }
            else // there will be overlapping
            {
                // write till the end of the buffer first
                CopyBuffer(_buffer, _readPosition, buffer, offset, bytesToReadBeforeTheEnd);

                _readPosition = 0;

                // then write the rest
                CopyBuffer(_buffer, _readPosition, buffer, offset + bytesToReadBeforeTheEnd, bytesToRead - bytesToReadBeforeTheEnd);

                _readPosition = bytesToRead - bytesToReadBeforeTheEnd;
            }

            Interlocked.Add(ref _size, -1 * bytesToRead);

            //Release the lock
            Interlocked.Exchange(ref _resourceSync, 0);

            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public async Task WaitForData()
        {
            _taskCompletionSource.Reset();
            var someoneIsWriting = (Interlocked.Exchange(ref _readWriteBalance, 1) != 0); // means on the othe side someone is writing
            if (someoneIsWriting) // data will be ready pretty soon
            {
                var spinWait = new SpinWait();
                while (Length == 0)
                {
                    spinWait.SpinOnce();
                }
            }
            else
            {
                if (Length == 0)
                {
                    await _taskCompletionSource;
                }
            }
        }

        private static void CopyBuffer(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            if ((count <= 8) && (src != dst))
            {
                int byteCount = count;
                while (--byteCount >= 0)
                {
                    dst[dstOffset + byteCount] = src[srcOffset + byteCount];
                }
            }
            else
            {
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException("Stream is too long");
            if (value > _capacity)
            {
                int newCapacity = value;
                if (newCapacity < 256)
                    newCapacity = 256;

                if (newCapacity < _capacity * 2)
                    newCapacity = _capacity * 2;

                Capacity = newCapacity;
                return true;
            }
            return false;
        }




        public virtual int Capacity
        {
            get
            {
                //if (!_isOpen) __Error.StreamIsClosed();
                return _capacity;
            }

            set
            {
                //if (!_isOpen) __Error.StreamIsClosed();
                //if (!_expandable && (value != Capacity)) __Error.MemoryStreamNotExpandable();

                if (value != _capacity)
                {
                    if (value > 0)
                    {
                        var buffer = new byte[value];
                        //we need to copy everything into the new buffer

                        int bytesToRead = (int)Length;

                        if (bytesToRead > 0)
                        {
                            var bytesToReadBeforeTheEnd = _capacity - _readPosition;

                            // check if there is enough space from _writePosition till ...
                            if (bytesToRead < bytesToReadBeforeTheEnd) // no overlapping
                            {
                                CopyBuffer(_buffer, _readPosition, buffer, 0, bytesToRead);

                            }
                            else // there will be overlapping
                            {
                                // write till the end of the buffer first
                                CopyBuffer(_buffer, _readPosition, buffer, 0, bytesToReadBeforeTheEnd);

                                _readPosition = 0;

                                // then write the rest
                                CopyBuffer(_buffer, _readPosition, buffer, bytesToReadBeforeTheEnd, bytesToRead - bytesToReadBeforeTheEnd);
                            }
                        }

                        Interlocked.Exchange(ref _readPosition, 0);
                        Interlocked.Exchange(ref _writePosition, bytesToRead);
                        Interlocked.Exchange(ref _buffer, buffer);

                    }
                    else
                    {
                        _buffer = null;
                    }
                    _capacity = value;
                }
            }
        }

        public void Clear()
        {
            _size = 0;
            _writePosition = 0;
            _readPosition = 0;
        }
    }
}
