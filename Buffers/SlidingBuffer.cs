using System;
using despBuffers.FlowRider;
using despBuffers.Interfaces;
using despThreading.Primitives;

namespace despBuffers.Buffers
{
    public class SlidingBuffer<T> : BaseBuffer<T>
    {
        private const int InitialBufferSize = 65536 * 2;

        private readonly object _lock = new object();

        private T[] _storeBuffer;
        private int _positionLow, _positionHigh;

        private readonly ThreadSafeLong _dataStored = new ThreadSafeLong();

        private bool _isCanGrow;

        private int _limitBufferSize;

        public int LimitBufferSize
        {
            get => _limitBufferSize;

            set
            {
                _limitBufferSize = value;

                Flush();
                UpsizeBuffer(value);
            }
        }

        public SlidingBuffer(string name, int bufferSize = InitialBufferSize, bool canUpsize = false)
        {
            Name = name;
            _isCanGrow = canUpsize;

            _storeBuffer = new T[bufferSize];

            Flush();

            _dataStored.ThreadSafeItemChanged = sender => { BuffersFlow.Instance?.OnBufferChanged(this); };
        }

        public void Flush()
        {
            lock (_lock)
            {
                _positionLow = _positionHigh = 0;
                _dataStored.Value = 0;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_storeBuffer, 0, _storeBuffer.Length);
            }
        }

        private void UpsizeBuffer(int newSize = -1)
        {
            var newBufferSize = newSize > 0 ? newSize : _storeBuffer.Length * 2;

            var newBuffer = new T[newBufferSize];

            var amountOfData = DataLengthSamples;

            if (amountOfData > newBufferSize)
            {
                Purge(amountOfData - newBufferSize);
                amountOfData = newBufferSize;
            }

            Pop(newBuffer, 0, amountOfData);

            _storeBuffer = newBuffer;
            _positionLow = 0;
            _positionHigh = amountOfData;
            _dataStored.Value = amountOfData;
        }

        public override void Push(IAudioBuffer<T> sourceBuffer)
        {
            Push(sourceBuffer.BufferData, 0, sourceBuffer.DataLengthSamples, sourceBuffer.LastTimestamp);
        }

        public override void Push(T[] buffer, int offset, int length, DateTime lastTimestamp)
        {
            lock (_lock)
            {
                LastTimestamp = lastTimestamp;

                if (length > _storeBuffer.Length)
                {
                    if (_isCanGrow)
                    {
                        UpsizeBuffer(length);
                    }
                    else
                    {
                        throw new BuffersException($"Sliding push overflow of sliding buffer '{Name}'");
                    }
                }

                var storeTop = _storeBuffer.Length;

                if (length > FreeSpace)
                {
                    if (storeTop >= LimitBufferSize && LimitBufferSize != 0)
                    {
                        _isCanGrow = false;
                    }

                    if (_isCanGrow == false)
                    {
                        // Move low pointer up to free enough space for incoming data
                        var moveLength = length - FreeSpace + 1;

                        _positionLow += moveLength;
                        if (_positionLow >= storeTop) _positionLow -= storeTop;
                    }
                    else
                    {
                        UpsizeBuffer();
                        storeTop = _storeBuffer.Length;
                    }
                }

                if (storeTop - _positionHigh >= length)
                {
                    Array.Copy(buffer, offset, _storeBuffer, _positionHigh, length);
                    _positionHigh += length;
                }
                else
                {
                    var firstChunkLength = storeTop - _positionHigh;
                    Array.Copy(buffer, offset, _storeBuffer, _positionHigh, firstChunkLength);

                    var secondChunkLength = length - firstChunkLength;
                    Array.Copy(buffer, offset + firstChunkLength, _storeBuffer, 0, secondChunkLength);

                    _positionHigh = secondChunkLength;
                }

                _dataStored.Value += length;

                if (_isCanGrow == false)
                {
                    _dataStored.Value = Math.Min(_dataStored.Value, storeTop);
                }

                if (LimitBufferSize > 0)
                {
                    _dataStored.Value = Math.Min(_dataStored.Value, LimitBufferSize);
                }
            }
        }

        public void Pop(IAudioBuffer<T> targetBuffer, int samplesToPop)
        {
            targetBuffer.Ensure(samplesToPop);
            Pop(targetBuffer.BufferData, 0, samplesToPop, out DateTime lastTimestamp);

            targetBuffer.DataLengthSamples = 0;
            targetBuffer.Setup(OriginUid, ChannelUid, Priority, SampleRate, Channels, lastTimestamp);
            targetBuffer.DataLengthSamples = samplesToPop;
        }

        public override void Pop(T[] buffer, int offset, int length, out DateTime lastTimestamp)
        {
            PopInternal(buffer, offset, length, false, out lastTimestamp);
        }

        public void PurgeToTimestamp(DateTime timestamp)
        {
            if (SampleRate < float.Epsilon || Channels < 1)
            {
                throw new BuffersException("Unable to execute PurgeToTimestamp with invalid SampleRate or Channels number");
            }

            var mustRemainSeconds = (LastTimestamp - timestamp).TotalSeconds;

            if (mustRemainSeconds <= 0)
            {
                mustRemainSeconds = 0;
            }

            var mustRemainSamples = (int) (mustRemainSeconds * SampleRate * Channels);

            if (mustRemainSamples >= DataLengthSamples)
            {
                return;
            }

            var purgeSamples = DataLengthSamples  - mustRemainSamples;

            Purge(purgeSamples);
        }

        public void Purge(int samplesCount)
        {
            PopInternal(null, 0, samplesCount, true, out DateTime _);
        }

        private void PopInternal(T[] buffer, int offset, int length, bool purge, out DateTime lastTimestamp)
        {
            lock (_lock)
            {
                lastTimestamp = FirstTimestamp + TimeSpan.FromSeconds(SamplesToSeconds(length));

                var copyLength = length;
                var remainingLength = 0;

                if (DataLengthSamples > _storeBuffer.Length)
                {
                    throw new BuffersException("Internal inconsistance in SlidingBuffer code");
                }

                if (copyLength > DataLengthSamples)
                {
                    copyLength = DataLengthSamples;
                    remainingLength = length - copyLength;
                }

                var storageTop = _storeBuffer.Length;

                if (storageTop - _positionLow >= copyLength)
                {
                    if (purge == false)
                    {
                        Array.Copy(_storeBuffer, _positionLow, buffer, offset, copyLength);
                    }

                    _positionLow += copyLength;
                }
                else
                {
                    var firstChunkLength = storageTop - _positionLow;

                    if (purge == false)
                    {
                        Array.Copy(_storeBuffer, _positionLow, buffer, offset, firstChunkLength);
                    }

                    var secondChunkLength = copyLength - firstChunkLength;
                    if (purge == false)
                    {
                        Array.Copy(_storeBuffer, 0, buffer, offset + firstChunkLength, secondChunkLength);
                    }

                    _positionLow = secondChunkLength;
                }

                if (remainingLength > 0 && purge == false)
                {
                    Array.Clear(buffer, offset + copyLength, remainingLength);
                }

                _dataStored.Value -= copyLength;
            }
        }

        public int RewindForward(int samplesCount)
        {
            lock (_lock)
            {
                if (samplesCount >= 0)
                {
                    // Rewind
                    var canRewindUpTo = _positionLow + _storeBuffer.Length - _positionHigh;
                    var willRewindTo = Math.Min(samplesCount, canRewindUpTo - 1);

                    _positionLow -= willRewindTo;
                    if (_positionLow < 0)
                    {
                        _positionLow += _storeBuffer.Length;
                    }

                    _dataStored.Value += willRewindTo;

                    return willRewindTo;
                }

                // Forward
                var willForwardTo = Math.Min((DataLengthSamples * 2) / 3, -samplesCount);
                _positionLow += willForwardTo;

                if (_positionLow >= _storeBuffer.Length)
                {
                    _positionLow -= _storeBuffer.Length;
                }

                _dataStored.Value -= willForwardTo;

                return -willForwardTo;
            }
        }

        public int FreeSpace
        {
            get
            {
                lock (_lock)
                {
                    return (int) (_storeBuffer.Length - _dataStored.Value);
                }
            }
        }

        public override int DataLengthSamples => (int) _dataStored.Value;

        public override T[] BufferData
        {
            get
            {
                throw new BuffersException("Unable to get buffer data of sliding buffer");
            }

            protected set { }
        }

        public override void Ensure(int ensureSize)
        {
            throw new BuffersException("Trying to call Ensure for sliding buffer");
        }
    }
}
