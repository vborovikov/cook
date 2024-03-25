namespace Cook.Book
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents time in Jira notation.
    /// </summary>
    [TypeConverter(typeof(DurationTypeConverter)), JsonConverter(typeof(JsonConverter))]
    public readonly struct Duration : IEquatable<Duration>, IComparable<Duration>
    {
        private class DurationTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
                sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
                (value is string str) ? Parse(str) : base.ConvertFrom(context, culture, value);
        }

        private class JsonConverter : JsonConverter<Duration>
        {
            public override Duration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                Parse(reader.GetString());

            public override void Write(Utf8JsonWriter writer, Duration value, JsonSerializerOptions options) =>
                writer.WriteStringValue(value);
        }

        private const string Units = "dhms";
        private readonly TimeSpan value;

        private Duration(double hours)
        {
            this.value = TimeSpan.FromHours(hours);
        }

        private Duration(TimeSpan value)
        {
            this.value = value;
        }

        public static implicit operator TimeSpan(Duration dur) => dur.value;

        public static implicit operator string(Duration dur) => dur.ToString();

        public static bool TryParse(ReadOnlySpan<char> str, out Duration duration)
        {
            if (TimeSpan.TryParse(str, out var timeSpan))
            {
                duration = new Duration(timeSpan);
                return true;
            }

            // the parser is not 100% correct, but good enough for our needs

            timeSpan = TimeSpan.Zero;
            var unitPos = 0;
            if (str[unitPos] == 'p' || str[unitPos] == 'P')
            {
                // P in ISO 8601
                ++unitPos;
            }

            do
            {
                var numberPos = unitPos;
                if (str[numberPos] == 't' || str[numberPos] == 'T')
                {
                    // T in ISO 8601
                    ++numberPos;
                }

                for (; unitPos != str.Length; ++unitPos)
                {
                    if (Units.Contains(str[unitPos], StringComparison.OrdinalIgnoreCase))
                        break;
                }
                var numberStr = str[numberPos..unitPos];
                if (Double.TryParse(numberStr, out var number))
                {
                    if (unitPos < str.Length)
                    {
                        timeSpan += str[unitPos] switch
                        {
                            'd' or 'D' => TimeSpan.FromDays(number),
                            'h' or 'H' => TimeSpan.FromHours(number),
                            'm' or 'M' => TimeSpan.FromMinutes(number),
                            _ => TimeSpan.FromSeconds(number)
                        };
                    }
                    else
                    {
                        timeSpan += TimeSpan.FromSeconds(number);
                    }
                }
                else
                {
                    goto fail;
                }
                ++unitPos;
            } while (unitPos < str.Length);

            if (timeSpan != default)
            {
                duration = new Duration(timeSpan);
                return true;
            }

        fail:
            duration = default;
            return false;
        }

        public static Duration Parse(ReadOnlySpan<char> str)
        {
            if (TryParse(str, out var duration))
                return duration;

            throw new FormatException();
        }

        public static Duration FromMinutes(int value)
        {
            return new Duration(TimeSpan.FromMinutes(value));
        }

        public static bool operator ==(Duration left, Duration right) => left.Equals(right);

        public static bool operator !=(Duration left, Duration right) => !(left == right);

        public static Duration operator +(Duration left, Duration right) => new Duration(left.value + right.value);

        public static Duration operator -(Duration left, Duration right) => new Duration(left.value - right.value);

        public static Duration operator /(Duration duration, int divisor) => new Duration(duration.value / divisor);

        public static Duration operator *(Duration duration, int multiplier) => new Duration(duration.value * multiplier);

        public static bool operator <(Duration left, Duration right) => left.CompareTo(right) < 0;

        public static bool operator <=(Duration left, Duration right) => left.CompareTo(right) <= 0;

        public static bool operator >(Duration left, Duration right) => left.CompareTo(right) > 0;

        public static bool operator >=(Duration left, Duration right) => left.CompareTo(right) >= 0;

        public override string ToString()
        {
            if (this.value == default)
                return String.Empty;

            var notation = new StringBuilder();

            if (this.value.Days > 0)
            {
                notation.Append(this.value.Days).Append('d');
            }
            if (this.value.Hours > 0 || notation.Length > 0)
            {
                EnsureSpace(notation).Append(this.value.Hours).Append('h');
            }
            if (this.value.Minutes > 0 || notation.Length > 0)
            {
                EnsureSpace(notation).Append(this.value.Minutes).Append('m');
            }
            if (this.value.Seconds > 0 || notation.Length > 0)
            {
                EnsureSpace(notation).Append(this.value.Seconds).Append('s');
            }

            return notation.ToString();

            static StringBuilder EnsureSpace(StringBuilder sb)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                return sb;
            }
        }

        public override bool Equals(object obj) => obj is Duration duration && Equals(duration);

        public bool Equals(Duration other) => this.value.Equals(other.value);

        public override int GetHashCode() => HashCode.Combine(this.value);

        public int CompareTo(Duration other) => this.value.CompareTo(other.value);
    }
}