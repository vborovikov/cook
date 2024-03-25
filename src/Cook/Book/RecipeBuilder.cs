namespace Cook.Book
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Data.File;

    public class RecipeBuilder : RecordCollector<BasicRecipe>, ICsvRecordBuilder
    {
        private string recipeId;
        private int lastValidRecipeId;
        private string recipeName;
        private string recipeDescription;
        private readonly List<BasicIngredient> recipeIngredients;
        private readonly List<string> recipeRawIngredients;
        private readonly List<string> recipeInstructions;
        private string recipeLink;
        private string recipeSource;
        private readonly List<string> recipeFoods;
        private string recipeContent;

        public RecipeBuilder(ChannelWriter<BasicRecipe> recipes, Encoding encoding)
            : base(recipes, encoding)
        {
            this.recipeIngredients = new List<BasicIngredient>();
            this.recipeRawIngredients = new List<string>();
            this.recipeInstructions = new List<string>();
            this.recipeFoods = new List<string>();
        }

        protected override bool TryBuildRecord(out BasicRecipe recipe)
        {
            recipe = new BasicRecipe
            {
                Id =
                    Int32.TryParse(this.recipeId, out var id) ? id :
                    this.lastValidRecipeId > 0 ? this.lastValidRecipeId + 1 :
                    -1,
                Name = this.recipeName,
                Description = this.recipeDescription,
                Ingredients = this.recipeIngredients.ToArray(),
                RawIngredients = this.recipeRawIngredients.ToArray(),
                Instructions = String.Join(Environment.NewLine, this.recipeInstructions),
                Link = Uri.TryCreate(this.recipeLink, UriKind.RelativeOrAbsolute, out var uri) ? uri : null,
                Source = Enum.TryParse<RecipeSource>(this.recipeSource, ignoreCase: true, out var src) ? src : RecipeSource.Unknown,
                Foods = this.recipeFoods.ToArray(),
                Content = this.recipeContent,
            };

            if (recipe.IsValid)
            {
                this.lastValidRecipeId = recipe.Id;
                return true;
            }

            return false;
        }

        public override void CreateRecord(ReadOnlySpan<char> recordSpan)
        {
            this.recipeId = null;
            this.recipeName = null;
            this.recipeDescription = null;
            this.recipeIngredients.Clear();
            this.recipeRawIngredients.Clear();
            this.recipeInstructions.Clear();
            this.recipeLink = null;
            this.recipeSource = null;
            this.recipeFoods.Clear();
            this.recipeContent = recordSpan.ToString();

            base.CreateRecord(recordSpan);
        }

        public void SetField(int fieldIndex, ReadOnlySpan<char> fieldSpan, bool enclosed)
        {
            var fieldStr = FieldToString(fieldSpan, enclosed);
            switch (fieldIndex)
            {
                case 0:
                    this.recipeId = fieldStr;
                    break;
                case 1:
                    this.recipeName = fieldStr;
                    break;
                case 4:
                    this.recipeLink = fieldStr;
                    break;
                case 5:
                    this.recipeSource = fieldStr;
                    break;
            }
        }

        public void SetFieldEmptyArray(int fieldIndex)
        {
        }

        public void SetFieldArrayItem(int fieldIndex, int itemIndex, ReadOnlySpan<char> itemSpan, bool enclosed)
        {
            var itemStr = FieldToString(itemSpan, enclosed);

            if (fieldIndex == 2)
            {
                this.recipeIngredients.Insert(itemIndex, BasicIngredient.Parse(itemStr));
            }

            var coll = fieldIndex switch
            {
                2 => this.recipeRawIngredients,
                3 => this.recipeInstructions,
                6 => this.recipeFoods,
                _ => null
            };
            coll?.Insert(itemIndex, itemStr);
        }
    }

    public class RecipeElementCollector : RecordBuilderBase, ICsvRecordBuilder
    {
        private readonly RepeatStringCollection ingredients;
        private readonly RepeatStringCollection foods;
        private readonly RepeatStringCollection realFoods;
        private readonly List<BasicIngredient> recipeIngredients;
        private readonly List<string> recipeFoods;

        public RecipeElementCollector(Encoding encoding) : base(encoding)
        {
            this.ingredients = new RepeatStringCollection();
            this.foods = new RepeatStringCollection();
            this.realFoods = new RepeatStringCollection();

            this.recipeIngredients = new List<BasicIngredient>();
            this.recipeFoods = new List<string>();
        }

        public IRepeatStringCollection Ingredients => this.ingredients;
        public IRepeatStringCollection Foods => this.foods;
        public IRepeatStringCollection RealFoods => this.realFoods;

        public void CreateRecord(ReadOnlySpan<char> recordSpan)
        {
            this.recipeIngredients.Clear();
            this.recipeFoods.Clear();
        }

        public ValueTask<int> BuildRecordAsync(CancellationToken cancellationToken)
        {
            for (var i = 0; i < this.recipeIngredients.Count && i < this.recipeFoods.Count; ++i)
            {
                if (String.Equals(this.recipeIngredients[i].Name, this.recipeFoods[i], StringComparison.OrdinalIgnoreCase))
                {
                    this.realFoods.Add(this.recipeFoods[i]);
                }
            }

            return ValueTask.FromResult(0);
        }

        public void SetField(int fieldIndex, ReadOnlySpan<char> fieldSpan, bool enclosed)
        {
        }

        public void SetFieldArrayItem(int fieldIndex, int itemIndex, ReadOnlySpan<char> itemSpan, bool enclosed)
        {
            if (fieldIndex is 2 or 6 && !itemSpan.IsEmpty)
            {
                var itemStr = FieldToString(itemSpan, enclosed);

                if (fieldIndex == 2)
                {
                    this.ingredients.Add(itemStr);
                    this.recipeIngredients.Insert(itemIndex, BasicIngredient.Parse(itemStr));
                }
                else if (fieldIndex == 6)
                {
                    this.foods.Add(itemStr);
                    this.recipeFoods.Insert(itemIndex, itemStr);
                }
            }
        }

        public void SetFieldEmptyArray(int fieldIndex)
        {
        }

        public ValueTask StartAsync()
        {
            this.ingredients.Clear();
            this.foods.Clear();
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync()
        {
            //foreach (var item in this.ingredients.Common)
            //{
            //    if (!Measure.TryParse(item, CultureInfo.InvariantCulture, out _))
            //    {
            //        await Console.Out.WriteLineAsync(item);
            //    }
            //}

            return ValueTask.CompletedTask;
        }

        public ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public interface IRepeatStringCollection
    {
        IReadOnlySet<string> All { get; }
        IReadOnlySet<string> Common { get; }
    }

    public class RepeatStringCollection : IRepeatStringCollection
    {
        private readonly SortedSet<string> all;
        private readonly SortedSet<string> common;

        public RepeatStringCollection()
        {
            this.all = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            this.common = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlySet<string> All => this.all;

        public IReadOnlySet<string> Common => this.common;

        public void Clear()
        {
            this.all.Clear();
            this.common.Clear();
        }

        public void Add(string item)
        {
            if (!this.all.Add(item))
            {
                this.common.Add(item);
            }
        }
    }
}
