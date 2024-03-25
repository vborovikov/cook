namespace Sage.Web.Pages;

using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using Cook.Book;
using Pantry;
using Dapper;
using Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spryer;

public record ImportRecipe
{
    public string Recipe { get; init; }
    public string Source { get; init; }
}

[BindProperties]
public class ImportModel : PageModel
{
    private readonly DbDataSource db;

    public ImportModel(DbDataSource db)
    {
        this.db = db;
    }

    public Guid? RecipeId { get; set; }
    [Required]
    public string Recipe { get; set; }
    [Required, Url]
    public string Source { get; set; }

    public async Task OnGet(Guid? id = null, CancellationToken cancellationToken = default)
    {
        if (id.HasValue && id.Value != default)
        {
            this.RecipeId = id.Value;

            await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
            var info = await cnn.QueryFirstAsync<ImportRecipe>(
                """
                select r.Content as Recipe, r.Link as Source
                from book.RecipeSources r
                where r.RecipeId = @RecipeId;
                """, new { RecipeId = id.Value });

            this.Recipe = info.Recipe;
            this.Source = info.Source;
        }
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        var requestCulture = this.HttpContext.Features.Get<IRequestCultureFeature>();
        var culture = requestCulture?.RequestCulture.Culture ?? CultureInfo.CurrentCulture;

        if (!this.ModelState.IsValid || !BasicRecipe.TryParse(this.Recipe, culture, out var recipe))
        {
            this.ModelState.AddModelError(String.Empty, "Cannot parse the recipe.");
            return Page();
        }

        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        await using var tx = await cnn.BeginTransactionAsync(cancellationToken);

        var hasRecipeId = this.RecipeId.HasValue && this.RecipeId.Value != default;
        var recipeId = hasRecipeId ? this.RecipeId!.Value : Guid.NewGuid();
        try
        {
            if (hasRecipeId)
            {
                await UpdateAsync(recipeId, recipe, tx);
            }
            else
            {
                await ImportAsync(recipeId, recipe, tx);
            }


            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception x)
        {
            await tx.RollbackAsync(cancellationToken);
            this.ModelState.AddModelError(String.Empty, x.Message);
            return Page();
        }

        return RedirectToPage("Index", new { id = recipeId });
    }

    private async Task ImportAsync(Guid recipeId, BasicRecipe recipe, DbTransaction tx)
    {
        // store the recipe info
        await tx.Connection.ExecuteAsync(
            """
            insert into book.Recipes (Id, Name, Description, Instructions, IsParsed)
            values (@Id, @Name, @Description, @Instructions, @IsParsed);
                
            insert into book.RecipeSources (RecipeId, Content, ContentType, Link)
            values (@Id, @Content, 'text', @Link);
            """,
            new
            {
                Id = recipeId,
                Name = recipe.Name.AsNVarChar(100),
                Description = recipe.Description.AsNVarChar(500),
                Instructions = recipe.Instructions.AsNVarChar(),
                IsParsed = true,
                Content = recipe.Content.AsNVarChar(),

                Link = new Uri(this.Source),
            }, tx);

        // store ingredients, obtain their IDs
        await StoreIngredientsAsync(recipeId, recipe.Ingredients, tx);
    }

    private async Task UpdateAsync(Guid recipeId, BasicRecipe recipe, DbTransaction tx)
    {
        // discard ingredients
        await tx.Connection.ExecuteAsync(
            """
            delete i from book.Ingredients i
            inner join book.RecipeIngredients ri on ri.IngredientId = i.Id
            where ri.RecipeId = @RecipeId;
            """, new { RecipeId = recipeId }, tx);

        // update recipe
        await tx.Connection.ExecuteAsync(
            """
            update book.Recipes
            set Name = @Name, Description = @Description, Instructions = @Instructions, IsParsed = @IsParsed
            where Id = @RecipeId;
                
            update book.RecipeSources
            set Content = @Content, ContentType = 'text', Link = @Link
            where RecipeId = @RecipeId;
            """,
            new
            {
                RecipeId = recipeId,
                Name = recipe.Name.AsNVarChar(100),
                Description = recipe.Description.AsNVarChar(500),
                Instructions = recipe.Instructions.AsNVarChar(),
                IsParsed = true,
                Content = recipe.Content.AsNVarChar(),
                Link = new Uri(this.Source),
            }, tx);
        
        // insert ingredients
        await StoreIngredientsAsync(recipeId, recipe.Ingredients, tx);
    }

    private static async Task StoreIngredientsAsync(Guid recipeId, BasicIngredient[] ingredients, DbTransaction tx)
    {
        var recipeIngredientIds = new List<Guid>();
        for (var i = 0; i != ingredients.Length; ++i)
        {
            var ingredient = ingredients[i];
            // insert ingredient
            var ingredientId = await tx.Connection.ExecuteScalarAsync<Guid>(
                """
                insert into book.Ingredients (Description,
                    Number, NumberValue, NumberUnit,
                    Quantity, QuantityValue, QuantityUnit,
                    AltQuantity, AltQuantityValue, AltQuantityUnit)
                output inserted.Id
                values (@Description,
                    @Number, @NumberValue, @NumberUnit,
                    @Quantity, @QuantityValue, @QuantityUnit,
                    @AltQuantity, @AltQuantityValue, @AltQuantityUnit);
                """,
                new
                {
                    Description = ingredient.Description.AsNVarChar(100),
                    ingredient.Number,
                    NumberValue = ingredient.Number.Value,
                    NumberUnit = (DbEnum<MeasurementType>?)ingredient.Number.Unit?.Type,
                    ingredient.Quantity,
                    QuantityValue = ingredient.Quantity.Value,
                    QuantityUnit = (DbEnum<MeasurementType>?)ingredient.Quantity.Unit?.Type,
                    ingredient.AltQuantity,
                    AltQuantityValue = ingredient.AltQuantity.Value,
                    AltQuantityUnit = (DbEnum<MeasurementType>?)ingredient.AltQuantity.Unit?.Type,
                }, tx);

            recipeIngredientIds.Add(ingredientId);
        }

        // store connections between the recipe and the ingredients
        var index = 0;
        var step = Byte.MaxValue / ingredients.Length;
        foreach (var recipeIngredientId in recipeIngredientIds.Distinct())
        {
            await tx.Connection.ExecuteAsync(
                """
                insert into book.RecipeIngredients (RecipeId, IngredientId, Turn)
                values (@RecipeId, @IngredientId, @Turn);
                """, new { RecipeId = recipeId, IngredientId = recipeIngredientId, Turn = ++index * step }, tx);
        }
    }
}
