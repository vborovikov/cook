namespace Sage.Web.Data;

using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Options;
using Relay.InteractionModel;
using Spryer;

record DbPage(int SkipCount, int TakeCount, DbString Search, DbString Filter);

static class DbExtensions
{
    public static DbPage AsDbPage(this IPage page)
    {
        return new(page.SkipCount, page.TakeCount,
            page.Search.AsNVarChar(100), page.Filter.AsNVarChar(100));
    }
}

public record RecipeInfo(Guid Id, string Name, Uri Link, string Description)
{
    public RecipeInfo()
        : this(default, default, default, default)
    {
    }
}

public record RecipeIngredient(Guid Id, string Description);

public record Recipe(Guid Id, string Name, Uri Link, string Description, string Instructions) : RecipeInfo(Id, Name, Link, Description)
{
    public IEnumerable<RecipeIngredient> Ingredients { get; init; } = Array.Empty<RecipeIngredient>();

    public IEnumerable<string> EnumerateInstructions()
    {
        foreach (var step in this.Instructions.ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var span = step.AsSpan().TrimStart("0123456789.)");

            if (!span.IsEmpty)
                yield return span.ToString();
        }
    }
}