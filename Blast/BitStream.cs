namespace Blast
{
    public class BitStream
    {
        private readonly InputBuffer _inputBuffer;

        private int _bitBuffer = 0; // bit buffer
        private int _bitBufferCount = 0; // number of bits in bit buffer

        public BitStream(InputBuffer inputBuffer)
        {
            _inputBuffer = inputBuffer;
        }

        // FIXME stop allowing internal state to be mutable
        public (int buffer, int bufferCount) State
        {
            get
            {
                return (_bitBuffer, _bitBufferCount);
            }

            set
            {
                _bitBuffer = value.buffer;
                _bitBufferCount = value.bufferCount;
            }
        }

        public int GetBits(int need)
        {
            int val = _bitBuffer;

            while (_bitBufferCount < need)
            {
                val |= ((int)_inputBuffer.ConsumeByte()) << _bitBufferCount;
                _bitBufferCount += 8;
            }

            _bitBuffer = val >> need;
            _bitBufferCount -= need;

            return val & ((1 << need) - 1);
        }

        public void FlushBits()
        {
            _bitBufferCount = 0;
        }
    }
}