namespace Cook.Data.File;

using System;
using System.Threading;
using System.Threading.Tasks;
using Brackets.Streaming;

public interface ICsvRecordBuilder : IMultilineBuilder
{
    void CreateRecord(ReadOnlySpan<char> recordSpan);

    ValueTask<int> BuildRecordAsync(CancellationToken cancellationToken);

    void SetField(int fieldIndex, ReadOnlySpan<char> fieldSpan, bool enclosed);

    void SetFieldArrayItem(int fieldIndex, int itemIndex, ReadOnlySpan<char> itemSpan, bool enclosed);

    void SetFieldEmptyArray(int fieldIndex);

    void SetFieldNames(string[] fieldNames);
}