/*
Translation to managed C# of blast.c/h, published by Mark Adler. The copyright notice
for the C implementation is included below. This implementation varies from the C
original.

Any part of this implementation that is not covered by the original
notice is Copyright (c) 2012 James Telfer, and is released under the Apache 2.0 license:
see http://www.apache.org/licenses/LICENSE-2.0.html.

It should be noted that this algorithm was originally implemented by
PKWare, and while there was no reference to their implementation
there may be portions that come under patents originating from that
company.

The license terms do not and cannot cover any part of this work that
is covered by patent claims of any other entity.


https://github.com/madler/zlib/blob/master/contrib/blast/
blast.c

Copyright (C) 2003 Mark Adler
version 1.1, 16 Feb 2003

This software is provided 'as-is', without any express or implied
warranty.  In no event will the author be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
    claim that you wrote the original software. If you use this software
    in a product, an acknowledgment in the product documentation would be
    appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
    misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.

Mark Adler    madler@alumni.caltech.edu
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Blast
{
    public class BlastDecoder
    {
        public const int MAX_WIN = 4096;
        private const int END_OF_STREAM = 519;
        private const int LITERAL_INDICATOR = 0;

        /// <summary>
        /// base for length codes
        /// </summary>
        private static readonly short[] LENGTH_CODE_BASE = { 3, 2, 4, 5, 6, 7, 8, 9, 10, 12, 16, 24, 40, 72, 136, 264 };

        /// <summary>
        /// extra bits for length codes
        /// </summary>
        private static readonly byte[] LENGTH_CODE_EXTRA = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 };


        //
        // state variables
        //
        private readonly InputBuffer _inputBuffer;
        private readonly BitStream _bitStream;


        private Stream _outputStream;
        private byte[] _outputBuffer = new byte[MAX_WIN * 2]; // output buffer and sliding window
        private int _outputBufferPos = 0; // index of next write location in _outputBuffer[]


        /// <summary>
        /// <para>Decompress input to output using the provided infun() and outfun() calls.
        /// On success, the return value of blast() is zero.  If there is an error in
        /// the source data, i.e. it is not in the proper format, then a negative value
        /// is returned.  If there is not enough input available or there is not enough
        /// output space, then a positive error is returned.</para>
        ///
        /// <para>The input function is invoked: len = infun(how, &buf), where buf is set by
        /// infun() to point to the input buffer, and infun() returns the number of
        /// available bytes there.  If infun() returns zero, then blast() returns with
        /// an input error.  (blast() only asks for input if it needs it.)  inhow is for
        /// use by the application to pass an input descriptor to infun(), if desired.</para>
        ///
        /// <para>The output function is invoked: err = outfun(how, buf, len), where the bytes
        /// to be written are buf[0..len-1].  If err is not zero, then blast() returns
        /// with an output error.  outfun() is always called with len &lt;= 4096.  outhow
        /// is for use by the application to pass an output descriptor to outfun(), if
        /// desired.</para>
        ///
        /// <para>The return codes are:</para>
        ///
        ///   2:  ran out of input before completing decompression
        ///   1:  output error before completing decompression
        ///   0:  successful decompression
        ///  -1:  literal flag not zero or one
        ///  -2:  dictionary size not in 4..6
        ///  -3:  distance is too far back
        ///
        /// <para>At the bottom of blast.c is an example program that uses blast() that can be
        /// compiled to produce a command-line decompression filter by defining TEST.</para>
        /// </summary>
        public BlastDecoder(Stream inputStream, Stream outputStream)
        {
            this._inputBuffer = new InputBuffer(inputStream);
            this._bitStream = new BitStream(this._inputBuffer);

            this._outputStream = outputStream;
        }

        /// <summary>
        /// Decode PKWare Compression Library stream.
        /// </summary>
        public void Decompress()
        {
            do
            {
                // some files are composed of multiple compressed streams
                DecompressStream();
            } while (_inputBuffer.IsInputRemaining());
        }

        private void DecompressStream()
        {
            // read header (start of compressed stream)
            bool codedLiteral = ReadCodedLiteralHeader();
            var readLiteral = codedLiteral ? (Func<byte>)ReadCodedLiteral : ReadUncodedLiteral;

            // log2(dictionary size) - 6
            int dictSize = _bitStream.GetBits(8);

            if (dictSize < 4 || dictSize > 6)
            {
                throw new BlastException(BlastException.DictionarySizeMessage);
            }

            // decode the compressed stream
            try
            {
                // decode literals and length/distance pairs
                do
                {
                    // Bit indicates whether to read literal from stream or encoded length+distance pair
                    int nextCodeIndicator = _bitStream.GetBits(1);

                    if (nextCodeIndicator == LITERAL_INDICATOR)
                    {
                        // get literal and write it
                        WriteBuffer(readLiteral());
                    }
                    else
                    {
                        // get length/distance and write buffer segments from current window
                        var (length, distance) = ReadLengthDistance(dictSize);

                        if (length == END_OF_STREAM)
                        {
                            break;
                        }

                        WriteWindowSegment(length, distance);
                    }
                } while (true);
            }
            finally
            {
                // write remaining bytes
                FlushOutputBuffer();
                _bitStream.FlushBits();
            }
        }

        private byte ReadUncodedLiteral()
        {
            return (byte)_bitStream.GetBits(8);
        }
        private byte ReadCodedLiteral()
        {
            return (byte)Decode(HuffmanTable.LITERAL_CODE);
        }

        private bool ReadCodedLiteralHeader()
        {
            int codedLiteral = _bitStream.GetBits(8);
            if (codedLiteral > 1)
            {
                throw new BlastException(BlastException.LiteralFlagMessage);
            }

            return codedLiteral == 1;
        }

        private (int length, int distance) ReadLengthDistance(int dictSize)
        {
            // decode length
            int decodedLengthSymbol = Decode(HuffmanTable.LENGTH_CODE);
            int copyLength = LENGTH_CODE_BASE[decodedLengthSymbol] + _bitStream.GetBits(LENGTH_CODE_EXTRA[decodedLengthSymbol]);

            if (copyLength == END_OF_STREAM) // sentinel value
            {
                // no more for this stream,
                // stop and flush
                return (copyLength, 0);
            }

            // decode distance
            int distanceAdditionalBits = copyLength == 2 ? 2 : dictSize;
            int copyDist = Decode(HuffmanTable.DISTANCE_CODE) << distanceAdditionalBits;
            copyDist += _bitStream.GetBits(distanceAdditionalBits);
            copyDist++;

            // malformed input - you can't go back that far
            if (copyDist > _outputBufferPos)
            {
                throw new BlastException(BlastException.DistanceMessage);
            }

            return (copyLength, copyDist);
        }

        private void WriteWindowSegment(int copyLength, int copyDistance)
        {
            // Copy copyLength bytes from copyDist bytes back.
            // If copyLength is greater than copyDist, repeatedly
            // copy copyDist bytes up to a count of copyLength.
            do
            {
                int fromIndex = _outputBufferPos - copyDistance;
                int copyCount = copyDistance;

                if (copyCount > copyLength)
                {
                    copyCount = copyLength;
                }

                CopyBufferSection(fromIndex, copyCount);

                copyLength -= copyCount;

            } while (copyLength != 0);
        }

        /// <summary>
        /// <para>
        /// Decode a code from the stream using huffman table <c>h</c>.  Return the symbol or
        /// a negative value if there is an error.  If all of the lengths are zero, i.e.
        /// an empty code, or if the code is incomplete and an invalid code is received,
        /// then -9 is returned after reading MAXBITS bits.
        /// </para>
        ///
        /// <para>Format notes:</para>
        ///
        /// <list type="bullet">
        /// <item>The codes as stored in the compressed data are bit-reversed relative to
        ///   a simple integer ordering of codes of the same lengths.  Hence below the
        ///   bits are pulled from the compressed data one at a time and used to
        ///   build the code value reversed from what is in the stream in order to
        ///   permit simple integer comparisons for decoding.</item>
        ///
        /// <item>The first code for the shortest length is all ones.  Subsequent codes of
        ///   the same length are simply integer decrements of the previous code.  When
        ///   moving up a length, a one bit is appended to the code.  For a complete
        ///   code, the last code of the longest length will be all zeros.  To support
        ///   this ordering, the bits pulled during decoding are inverted to apply the
        ///   more "natural" ordering starting with all zeros and incrementing.</item>
        /// </list>
        /// </summary>
        private int Decode(HuffmanTable h)
        {
            int len = 1;   // current number of bits in code
            int code = 0;  // len bits being decoded
            int first = 0; // first code of length len
            int count;     // number of codes of length len
            int index = 0; // index of first code of length len in symbol table
            int bitbuf;    // bits from stream
            int left;      // bits left in next or left to process
            int next = 1;  // next number of codes

            var bufferState = _bitStream.State;

            (bitbuf, left) = bufferState;

            while (true)
            {
                while (left-- > 0)
                {
                    // add the next bit from the stream to the code
                    // this bit is inverted as mentioned above
                    code |= (bitbuf & 1) ^ 1;

                    // advance the buffer
                    bitbuf >>= 1;

                    // grab the count out of the Huffman table
                    count = h.count[next++];

                    if (code < first + count)
                    {
                        // code found, reset bitstream state after local modification
                        _bitStream.State = (bitbuf, ((bufferState.bufferCount - len) & 7));

                        // return decoded symbol
                        return h.symbol[index + (code - first)];
                    }

                    index += count;
                    first += count;

                    first <<= 1;
                    code <<= 1;

                    len++;
                }

                // the code uses bits from the next byte in the stream
                // work out how many this may be based on the current
                // number of bits consumed by the current code
                left = (HuffmanTable.MAX_BITS + 1) - len;

                // all bits of the code have been consumed
                if (left == 0)
                    break;

                // get the next byte from the input stream to continue processing the current code
                bitbuf = _inputBuffer.ConsumeByte();

                // max number of bits to be processed in the
                // current code per iteration is limited to one byte/eight bits
                if (left > 8)
                    left = 8;
            }

            return -9; // invalid code
        }

        #region Output stream

        private void WriteBuffer(byte b)
        {
            EnsureBufferSpace(1);
            //log("lit: {0}", (char)b);
            _outputBuffer[_outputBufferPos++] = b;
        }

        private void CopyBufferSection(int fromIndex, int copyCount)
        {
            fromIndex -= EnsureBufferSpace(copyCount);

            Buffer.BlockCopy(_outputBuffer, fromIndex, _outputBuffer, _outputBufferPos, copyCount);
            _outputBufferPos += copyCount;
        }

        private int EnsureBufferSpace(int required)
        {
            // is there room in the buffer?
            if (_outputBufferPos + required >= _outputBuffer.Length)
            {
                // flush the initial section
                int startWindowOffset = _outputBufferPos - MAX_WIN;

                FlushOutputBufferSection(startWindowOffset); // only flush the section that's not part of the window

                // position the stream further back
                Buffer.BlockCopy(_outputBuffer, startWindowOffset, _outputBuffer, 0, MAX_WIN);
                _outputBufferPos = MAX_WIN;

                return startWindowOffset;
            }

            return 0;
        }

        private void FlushOutputBufferSection(int count)
        {
            _outputStream.Write(_outputBuffer, 0, count);
        }

        private void FlushOutputBuffer()
        {
            if (_outputBufferPos > 0)
            {
                FlushOutputBufferSection(_outputBufferPos);
                _outputBufferPos = 0;
            }
        }

        #endregion
    }
}