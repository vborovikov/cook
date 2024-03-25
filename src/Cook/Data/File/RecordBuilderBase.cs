namespace Cook.Data.File
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public abstract class RecordBuilderBase
    {
        private const char EncloserChar = '"';

        protected RecordBuilderBase(Encoding encoding)
        {
            this.Encoding = encoding;
        }

        public Encoding Encoding { get; }

        public char Encloser => EncloserChar;

        public IReadOnlyList<string> FieldNames { get; private set; }

        public void SetFieldNames(string[] fieldNames)
        {
            this.FieldNames = fieldNames;
        }

        protected static string FieldToString(ReadOnlySpan<char> span, bool enclosed, bool trim = false)
        {
            var value = enclosed ? span.Unclose(stackalloc char[span.Length]) : span;
            if (trim)
            {
                value = value.Trim(Fields.TrimChars);
            }

            return value.ToString();
        }
    }
}