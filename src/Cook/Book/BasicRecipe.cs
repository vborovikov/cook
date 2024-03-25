namespace Cook.Book
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using Data.Text;
    using Pantry;

    public record BasicRecipe
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public BasicIngredient[] Ingredients { get; init; }

        public string[] RawIngredients { get; init; }

        public string Instructions { get; init; }

        public Uri Link { get; init; }

        public RecipeSource Source { get; init; }

        public string[] Foods { get; init; }

        public string Content { get; init; }

        public bool IsValid =>
            this.Id >= 0 &&
            !String.IsNullOrWhiteSpace(this.Name) &&
            this.RawIngredients.Any() &&
            this.Ingredients.Any(/*ing => ing.IsValid*/) &&
            !String.IsNullOrWhiteSpace(this.Instructions) &&
            this.Link is not null &&
            this.Source != RecipeSource.Unknown;

        public static bool TryParse(ReadOnlySpan<char> entry, IFormatProvider formatProvider, out BasicRecipe recipe)
        {
            if (entry.IsEmpty || entry.IsWhiteSpace())
            {
                recipe = null;
                return false;
            }

            if (entry[0] == '{' || entry[0] == '[')
                return TryParseJson(entry, formatProvider, out recipe);

            return TryParseText(entry.ToString(), formatProvider, out recipe);
        }

        private static bool TryParseJson(ReadOnlySpan<char> entry, IFormatProvider formatProvider, out BasicRecipe recipe)
        {
            var entryStr = entry.ToString();
            var document = JsonDocument.Parse(entryStr);
            var json = document.RootElement.ValueKind == JsonValueKind.Array ?
                document.RootElement.EnumerateArray()
                    .FirstOrDefault(el =>
                        el.ValueKind == JsonValueKind.Object &&
                        el.TryGetProperty("@type", out var prop) &&
                        prop.ValueEquals("Recipe")) :
                document.RootElement;

            if (!json.TryGetProperty("@type", out var prop) || !prop.ValueEquals("Recipe"))
            {
                recipe = null;
                return false;
            }

            var rawIngredients = json.GetProperty("recipeIngredient").EnumerateArray()
                .Select(el => el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetProperty("text").GetString())
                .ToArray();

            recipe = new BasicRecipe
            {
                Name = json.GetProperty("name").GetString(),
                Description =
                    json.TryGetProperty("description", out var el) ? el.GetString() :
                    json.TryGetProperty("headline", out el) ? el.GetString() : null,
                RawIngredients = rawIngredients,
                Ingredients = rawIngredients.Select(BasicIngredient.Parse).ToArray(),
                Instructions = String.Join(Environment.NewLine, 
                    json.GetProperty("recipeInstructions").EnumerateArray()
                    .Select(el => el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetProperty("text").GetString())),
                Content = entryStr,
            };
            return true;
        }

        private static bool TryParseText(string entry, IFormatProvider formatProvider, out BasicRecipe recipe)
        {
            var parts = entry.ReplaceLineEndings("\n")
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                recipe = null;
                return false;
            }

            var hasDescription = parts.Length > 3;
            var rawIngredients = parts[hasDescription ? 2 : 1]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ingredients = rawIngredients.Select(BasicIngredient.Parse).ToArray();

            recipe = new BasicRecipe
            {
                Name = formatProvider is CultureInfo culture ? culture.TextInfo.ToTitleCase(parts[0]) : parts[0],
                Description = hasDescription ? parts[1] : null,
                RawIngredients = rawIngredients,
                Ingredients = ingredients,
                Instructions = parts[hasDescription ? 3 : 2],
                Content = entry,
            };

            return true;
        }

        public static BasicRecipe Parse(ReadOnlySpan<char> entry, IFormatProvider formatProvider)
        {
            if (entry.IsEmpty || entry.IsWhiteSpace())
                throw new ArgumentException(null, nameof(entry));
            if (!TryParse(entry, formatProvider, out var recipe))
                throw new FormatException();

            return recipe;
        }
    }

    public record BasicIngredient
    {
        private const string NameTrimChars = """ _-;:.`'"()[]{}@*\/&#+=<>~$!?""";

        // Quantity and AltQuantity can also be a range if the measurement type is the same for both

        public string Description { get; init; }
        public Measure Number { get; init; }
        public Measure Quantity { get; init; }
        public Measure AltQuantity { get; init; }

        public string Name { get; init; }

        public bool IsValid =>
            !String.IsNullOrWhiteSpace(this.Name) &&
            (this.Number != default || this.Quantity != default);

        public static BasicIngredient Parse(string entry) => Parse(entry.AsSpan());

        public static BasicIngredient Parse(ReadOnlySpan<char> entry)
        {
            Span<char> span = stackalloc char[entry.Length];
            entry.CopyTo(span);

            var rest = ExtractQuantity(span, out var number);
            rest = ExtractQuantity(rest, out var quantity);
            rest = ExtractQuantity(rest, out var altQuantity);

            if (number == default && quantity == default && altQuantity == default)
            {
                // no quantity found at all, assume it's just one piece of an ingredient
                number = 1;
            }
            else if (!IsNumber(number))
            {
                if (altQuantity != default)
                {
                    // 3 quantities found

                    if (IsNumber(altQuantity))
                    {
                        Swap(ref altQuantity, ref number);
                    }
                    if (IsNumber(quantity))
                    {
                        Swap(ref quantity, ref number);
                    }
                }
                else if (quantity != default)
                {
                    // 2 quantities found

                    if (IsNumber(quantity))
                    {
                        // number <-> quantity
                        Swap(ref quantity, ref number);
                    }
                    else
                    {
                        // number -> quantity -> altQuantity
                        Swap(ref number, ref quantity);
                        Swap(ref number, ref altQuantity);
                    }
                }
                else
                {
                    // 1 quantity found

                    // number -> quantity
                    quantity = number;
                    number = default;
                }
            }

            return new BasicIngredient
            {
                Name = TrimName(rest),
                Description = entry.ToString(),
                Number = number,
                Quantity = quantity,
                AltQuantity = altQuantity,
            };

            static bool IsNumber(Measure measure)
            {
                return
                    measure.Unit.Type == MeasurementType.Count ||
                    measure.Unit.Type == MeasurementType.Percentage;
            }
        }

        private static Span<char> ExtractQuantity(Span<char> span, out Measure quantity)
        {
            var index = 0;
            do
            {
                if (Measure.TryParse(span[index..], null, out quantity, out var length))
                {
                    var start = 0;
                    var end = span.Length;

                    if (index <= 1)
                    {
                        start = index + length;
                    }
                    else if ((index + length) >= (span.Length - 1))
                    {
                        end = index;
                    }
                    else
                    {
                        var endIndex = index + length;
                        span[index..endIndex].Clear(' ');
                    }

                    return span[start..end];
                }
            }
            while (++index < span.Length);

            quantity = default;
            return span;
        }

        private static string TrimName(Span<char> span)
        {
            Clear(span, "makes");
            Clear(span, "about");
            span = span.Trim(NameTrimChars);

            var textIndex = -1;
            var textLength = 0;

            for (var i = 0; i != span.Length; ++i)
            {
                if (NameTrimChars.Contains(span[i]))
                {
                    if (--textLength <= 0)
                    {
                        textIndex = -1;
                    }
                }
                else
                {
                    if (textIndex < 0)
                    {
                        textIndex = i;
                        textLength = 0;
                    }
                    ++textLength;
                }
            }

            if (textIndex > 0)
            {
                span = span[textIndex..];
            }

            // remove instructions
            for (var pos = LastIndexOfInstruction(span); pos > 0; pos = LastIndexOfInstruction(span))
            {
                span = span[..pos];
            }

            return span.Trim(NameTrimChars).ToString();
        }

        private static void Clear(Span<char> span, ReadOnlySpan<char> word)
        {
            var start = ((ReadOnlySpan<char>)span).IndexOf(word, StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                span[start..(start + word.Length)].Clear(' ');
            }
        }

        private static void Swap(ref Measure quantity, ref Measure number)
        {
            Measure t = quantity;
            quantity = number;
            number = t;
        }

        private static int LastIndexOfInstruction(ReadOnlySpan<char> span)
        {
            var index = span.LastIndexOfAny(",([{");
            if (index < 0)
                index = span.LastIndexOf(" for ", StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
                index = span.LastIndexOf(" to ", StringComparison.CurrentCultureIgnoreCase);
            return index;
        }
    }
}
