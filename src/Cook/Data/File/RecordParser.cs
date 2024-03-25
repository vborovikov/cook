namespace Cook.Data.File
{
    using System;
    using System.Collections.Generic;
    using Text;

    public class RecordParser
    {
        private readonly ICsvRecordBuilder builder;
        private bool parseHeader;

        public RecordParser(ICsvRecordBuilder builder, bool expectHeader = true)
        {
            this.builder = builder;
            this.parseHeader = expectHeader;
        }

        public bool Parse(ReadOnlySpan<char> record)
        {
            if (this.parseHeader)
            {
                this.parseHeader = false;
                ParseHeader(record);
                return false;
            }
            
            ParseRecord(record);
            return true;
        }

        private void ParseHeader(ReadOnlySpan<char> record)
        {
            var fieldNames = new List<string>();
            foreach (var fieldName in Fields.Parse(record))
            {
                fieldNames.Add(fieldName.Span.ToString());
            }

            if (fieldNames.Count > 0)
            {
                this.builder.SetFieldNames(fieldNames.ToArray());
            }
        }

        private void ParseRecord(ReadOnlySpan<char> record)
        {
            this.builder.CreateRecord(record);

            var fieldIndex = 0;
            foreach (var fieldSpan in Fields.Parse(record))
            {
                ParseField(fieldSpan, fieldIndex++);
            }
        }

        private void ParseField(FieldSpan fieldSpan, int fieldIndex)
        {
            if (fieldSpan.Category == FieldCategory.Regular)
            {
                this.builder.SetField(fieldIndex, fieldSpan, enclosed: false);
            }
            else if (fieldSpan.Category == FieldCategory.Array)
            {
                ParseArray(fieldSpan, fieldIndex);
            }
            else
            {
                if (IsEnclosedArray(fieldSpan))
                {
                    ParseEnclosedArray(fieldSpan, fieldIndex);
                }
                else
                {
                    this.builder.SetField(fieldIndex, fieldSpan, enclosed: true);
                }
            }
        }

        private void ParseArray(FieldSpan fieldSpan, int fieldIndex)
        {
            if (fieldSpan.Span.Length == 2) // only array brackets
            {
                this.builder.SetFieldEmptyArray(fieldIndex);
            }
            else
            {
                var itemIndex = 0;
                foreach (var item in Fields.Parse(fieldSpan.Span[1..^1]))
                {
                    this.builder.SetFieldArrayItem(fieldIndex, itemIndex++, item, enclosed: item.Category == FieldCategory.Enclosed);
                }
            }
        }

        private void ParseEnclosedArray(FieldSpan fieldSpan, int fieldIndex)
        {
            var itemCount = 0;
            var itemStart = 0;
            var itemLength = 0;
            foreach (var enclosure in Fields.Unclose(fieldSpan))
            {
                var nq = enclosure.Span[1..^1];

                if (nq.IsChar(Fields.ArrayOpener))
                {
                    if (enclosure.Index > 0)
                    {
                        // it's not the first array opener
                        if (itemStart == 0)
                        {
                            itemStart = enclosure.Index;
                        }
                        itemLength += enclosure.Span.Length;
                    }
                }
                else if (nq.IsChar(Fields.ArrayCloser))
                {
                    if (enclosure.EndIndex < fieldSpan.Span.Length)
                    {
                        // it's not the last array opener
                        if (itemStart == 0)
                        {
                            itemStart = enclosure.Index;
                        }
                        itemLength += enclosure.Span.Length;
                    }
                }
                else if (nq.IsChar(Fields.ArrayItemDelimiter))
                {
                    if (itemStart > 0)
                    {
                        var itemSpan = fieldSpan.Span.Slice(itemStart, itemLength);
                        // "### \" -- "," -- ", "
                        if (itemSpan.Length > 2 && itemSpan[^2] == Fields.EncloserEscape && itemSpan[^3] != Fields.EncloserEscape)
                        {
                            itemLength += enclosure.Span.Length;
                        }
                        else
                        {
                            this.builder.SetFieldArrayItem(fieldIndex, itemCount++,
                                itemSpan, enclosed: true);
                            itemStart = itemLength = 0;
                        }
                    }
                    else
                    {
                        // the delimiter is a part of an item
                        itemStart = enclosure.Index;
                        itemLength += enclosure.Span.Length;
                    }
                }
                else
                {
                    if (itemStart == 0)
                    {
                        itemStart = enclosure.Index;
                    }
                    itemLength += enclosure.Span.Length;
                }
            }

            if (itemStart > 0)
            {
                this.builder.SetFieldArrayItem(fieldIndex, itemCount++,
                    fieldSpan.Span.Slice(itemStart, itemLength), enclosed: true);
            }
            else if (itemCount == 0)
            {
                this.builder.SetFieldEmptyArray(fieldIndex);
            }
        }

        private static bool IsEnclosedArray(FieldSpan fieldSpan)
        {
            var arrayClampStart = fieldSpan.Span.IndexOfChar(Fields.Encloser, 1) + 1;
            var arrayClampEnd = fieldSpan.Span[..^1].LastIndexOf(Fields.Encloser);

            var arrayOpen = fieldSpan.Span[1..(arrayClampStart - 1)];
            var arrayClose = fieldSpan.Span[(arrayClampEnd + 1)..^1];

            return arrayOpen.IsChar(Fields.ArrayOpener) && arrayClose.IsChar(Fields.ArrayCloser);
        }
    }
}
