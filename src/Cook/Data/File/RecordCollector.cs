namespace Cook.Data.File
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Brackets.Streaming;

    public abstract class RecordCollector<TRecord> : RecordBuilderBase, IMultilineBuilder
    {
        private readonly RecordParser parser;
        private readonly ChannelWriter<TRecord> records;
        private int sourceCount;
        private int attemptCount;
        private int recordCount;

        protected RecordCollector(ChannelWriter<TRecord> records, Encoding encoding) : base(encoding)
        {
            this.parser = new RecordParser((ICsvRecordBuilder)this);
            this.records = records;
        }

        public int SourceCount => this.sourceCount;

        public int AttemptCount => this.attemptCount;

        public int RecordCount => this.recordCount;

        public ValueTask StartAsync()
        {
            this.attemptCount = 0;
            this.recordCount = 0;
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync()
        {
            this.records.Complete();
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
        {
            if (this.parser.Parse(recordSpan))
            {
                return BuildRecordAsync(cancellationToken);
            }

            return ValueTask.FromResult(0);
        }

        public virtual void CreateRecord(ReadOnlySpan<char> recordSpan)
        {
            ++this.sourceCount;
        }

        public async ValueTask<int> BuildRecordAsync(CancellationToken cancellationToken)
        {
            ++this.attemptCount;

            if (TryBuildRecord(out var record))
            {
                await this.records.WriteAsync(record, cancellationToken);
                ++this.recordCount;
            }

            return 0;
        }

        protected abstract bool TryBuildRecord(out TRecord record);
    }
}