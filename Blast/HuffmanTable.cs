namespace Blast
{
    /// <summary>
    /// Huffman code decoding tables.  count[1..MAXBITS] is the number of symbols of
    /// each length, which for a canonical code are stepped through in order.
    /// symbol[] are the symbol values in canonical order, where the number of
    /// entries is the sum of the counts in count[].  The decoding process can be
    /// seen in the function decode() below.
    /// </summary>
    internal class HuffmanTable
    {
        public const int MAX_BITS = 13;

        /// <summary>
        /// Bit lengths of literal codes.
        /// </summary>
        private static readonly byte[] LITERAL_BIT_LENGTHS = {
                11, 124, 8, 7, 28, 7, 188, 13, 76, 4, 10, 8, 12, 10, 12, 10, 8, 23, 8,
                9, 7, 6, 7, 8, 7, 6, 55, 8, 23, 24, 12, 11, 7, 9, 11, 12, 6, 7, 22, 5,
                7, 24, 6, 11, 9, 6, 7, 22, 7, 11, 38, 7, 9, 8, 25, 11, 8, 11, 9, 12,
                8, 12, 5, 38, 5, 38, 5, 11, 7, 5, 6, 21, 6, 10, 53, 8, 7, 24, 10, 27,
                44, 253, 253, 253, 252, 252, 252, 13, 12, 45, 12, 45, 12, 61, 12, 45,
                44, 173};

        /// <summary>
        /// Bit lengths of length codes 0..15.
        /// </summary>
        private static readonly byte[] LENGTH_BIT_LENGTHS = { 2, 35, 36, 53, 38, 23 };

        /// <summary>
        /// Bit lengths of distance codes 0..63.
        /// </summary>
        private static readonly byte[] DISTANCE_BIT_LENGTHS = { 2, 20, 53, 230, 247, 151, 248 };

        public static readonly HuffmanTable LITERAL_CODE = new HuffmanTable(256, LITERAL_BIT_LENGTHS);
        public static readonly HuffmanTable LENGTH_CODE = new HuffmanTable(16, LENGTH_BIT_LENGTHS);
        public static readonly HuffmanTable DISTANCE_CODE = new HuffmanTable(64, DISTANCE_BIT_LENGTHS);

        public readonly short[] count;
        public readonly short[] symbol;

        public HuffmanTable(int symbolSize, byte[] compacted)
        {
            count = new short[MAX_BITS + 1];
            symbol = new short[symbolSize];

            Construct(compacted);
        }

        /// <summary>
        /// Given a list of repeated code lengths rep[0..n-1], where each byte is a
        /// count (high four bits + 1) and a code length (low four bits), generate the
        /// list of code lengths.  This compaction reduces the size of the object code.
        /// Then given the list of code lengths length[0..n-1] representing a canonical
        /// Huffman code for n symbols, construct the tables required to decode those
        /// codes.  Those tables are the number of codes of each length, and the symbols
        /// sorted by length, retaining their original order within each length.  The
        /// return value is zero for a complete code set, negative for an over-
        /// subscribed code set, and positive for an incomplete code set.  The tables
        /// can be used if the return value is zero or positive, but they cannot be used
        /// if the return value is negative.  If the return value is zero, it is not
        /// possible for decode() using that table to return an error -- any stream of
        /// enough bits will resolve to a symbol.  If the return value is positive, then
        /// it is possible for decode() using that table to return an error for received
        /// codes past the end of the incomplete lengths.
        /// </summary>
        private int Construct(byte[] rep)
        {
            short symbol;// current symbol when stepping through length[]
            int len;// current length when stepping through h->count[]
            int left;// number of possible codes left of current length
            short[] offs = new short[MAX_BITS + 1]; // offsets in symbol table for each length
            short[] length = new short[256]; // code lengths

            int n;

            // convert compact repeat counts into symbol bit length list
            symbol = 0;
            for (int ri = 0; ri < rep.Length; ri++)
            {
                len = rep[ri];
                left = (len >> 4) + 1;
                len &= 0xf;
                do
                {
                    length[symbol++] = (short)len;
                } while (--left > 0);
            }

            // count number of codes of each length
            n = symbol;
            for (len = 0; len <= MAX_BITS; len++)
                this.count[len] = 0;

            for (symbol = 0; symbol < n; symbol++)
                (this.count[length[symbol]])++;// assumes lengths are within bounds

            if (this.count[0] == n)// no codes!
                return 0;   // complete, but decode() will fail

            // check for an over-subscribed or incomplete set of lengths
            left = 1; // one possible code of zero length
            for (len = 1; len <= MAX_BITS; len++)
            {
                left <<= 1; // one more bit, double codes left
                left -= this.count[len]; // deduct count from possible codes
                if (left < 0)
                    return left; // over-subscribed--return negative
            } // left > 0 means incomplete

            // generate offsets into symbol table for each length for sorting
            offs[1] = 0;

            for (len = 1; len < MAX_BITS; len++)
            {
                offs[len + 1] = (short)(offs[len] + this.count[len]);
            }

            //
            // put symbols in table sorted by length, by symbol order within each
            // length
            //
            for (symbol = 0; symbol < n; symbol++)
            {
                if (length[symbol] != 0)
                {
                    this.symbol[offs[length[symbol]]++] = symbol;
                }
            }

            // return zero for complete set, positive for incomplete set
            return left;
        }
    }
}