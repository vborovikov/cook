namespace Cook.Book
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public record RawRecipe
    {
        public string Name { get; init; }

        public string Description { get; init; }

        public string PrepTime { get; init; }

        public string CookTime { get; init; }

        public string TotalTime { get; init; }

        public string[] Ingredients { get; init; }

        public string[] Directions { get; init; }

        public string Nutrition { get; init; }
    }
}
