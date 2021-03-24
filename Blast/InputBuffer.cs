using System.IO;

namespace Blast
{
    public class InputBuffer
    {
        private readonly Stream _inputStream;
        private readonly byte[] _inputBuffer = new byte[16384];

        private int _inputBufferPos = 0;
        private int _inputBufferRemaining = 0; // available input in buffer

        public InputBuffer(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        public byte ConsumeByte()
        {
            if (_inputBufferRemaining == 0)
            {
                DoReadBuffer();

                if (_inputBufferRemaining == 0)
                {
                    throw new BlastException(BlastException.OutOfInputMessage);
                }
            }

            byte b = _inputBuffer[_inputBufferPos++];
            _inputBufferRemaining--;

            return b;
        }

        private void DoReadBuffer()
        {
            _inputBufferRemaining = _inputStream.Read(_inputBuffer, 0, _inputBuffer.Length);
            _inputBufferPos = 0;
        }

        /// <summary>
        /// Check for presence of more input without consuming it.
        /// May refill the input buffer.
        /// </summary>
        /// <returns></returns>
        public bool IsInputRemaining()
        {
            // is there any input in the buffer?
            if (_inputBufferRemaining > 0)
            {
                return true;
            }

            // try to fill it if not
            DoReadBuffer();

            // true if input now available
            return _inputBufferRemaining > 0;
        }

    }
}