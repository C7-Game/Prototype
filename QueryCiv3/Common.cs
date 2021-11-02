using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    public class SectionListItem
    {
        protected Civ3File Bic;
        public int Offset { get; protected set; }
        public int Length { get; protected set; }
        public void Init(Civ3File bic, int offset, int length)
        {
            Bic = bic;
            Offset = offset;
            Length = length;
        }
        public string DevTest { get => "SectionListItem off " + Offset.ToString() + " len " + Length.ToString(); }
        public byte[] RawBytes { get => Bic.GetBytes(Offset, Length); }
    }
    public class ListSection<T> where T : SectionListItem, new()
    {
        Civ3File Bic;
        int Offset;
        int ItemCount;
        public ListSection(Civ3File bic, int offset)
        {
            Bic = bic;
            ItemCount = Bic.ReadInt32(offset);
            Offset = offset;
        }
        public List<T> Sections
        { get {
            int CurrentOffset = Offset + 4;
            List<T> OutList = new List<T>();
            for(int i=0; i<ItemCount; i++)
            {
                int ItemLength = Bic.ReadInt32(CurrentOffset);
                CurrentOffset += 4;
                T Item = new T();
                Item.Init(Bic, CurrentOffset, ItemLength);
                OutList.Add(Item);
                CurrentOffset += ItemLength;

            }
            return OutList;
        }}
        public void Init(Civ3File bic, int offset, int length){}
    }
}