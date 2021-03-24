using System;

namespace Blast
{
    public class BlastException : Exception
    {
        public const string OutOfInputMessage = "Ran out of input before completing decompression";
        public const string OutputMessage = "Output error before completing decompression";
        public const string LiteralFlagMessage = "Literal flag not zero or one";
        public const string DictionarySizeMessage = "Dictionary size not in 4..6";
        public const string DistanceMessage = "Distance is too far back";

        public BlastException() : base() { }
        public BlastException(string message) : base(message) { }
        public BlastException(string message, Exception inner) : base(message, inner) { }
    }
}