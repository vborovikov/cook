namespace Sage.Web.Pages;

using System.Data.Common;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pantry;
using Sage.Web.Data;
using Spryer;

public record RecipeFoodEdit(Guid Id, string Name);

public record RecipeIngredientEdit
{
    private string[] foodIds;

    public Guid Id { get; init; }
    public string Description { get; set; }
    public Measure? Number { get; set; }
    public Measure? Quantity { get; set; }
    public Measure? AltQuantity { get; set; }

    // Foods...

    public string[] FoodIds
    {
        get => this.foodIds ?? this.Foods.Select(f => f.Id.ToString()).ToArray();
        set => this.foodIds = value;
    }
    public RecipeFoodEdit[] Foods { get; set; } = Array.Empty<RecipeFoodEdit>();
    public MultiSelectList FoodList => new(this.Foods, nameof(RecipeFoodEdit.Id), nameof(RecipeFoodEdit.Name), this.FoodIds);
}

public record RecipeEdit
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public string Description { get; set; }
    public RecipeIngredientEdit[] Ingredients { get; set; } = Array.Empty<RecipeIngredientEdit>();
    public string Instructions { get; set; }
}

[BindProperties]
public class EditModel : PageModel
{
    private readonly DbDataSource db;
    private readonly DbScriptMap sql;

    public EditModel(DbDataSource db, DbScriptMap sql)
    {
        this.db = db;
        this.sql = sql;
    }

    public RecipeEdit Recipe { get; set; }

    public async Task OnGet(Guid id, CancellationToken cancellationToken)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        this.Recipe = await cnn.EditRecipeByIdAsync<RecipeEdit>(id);
    }

    public async Task<IActionResult> OnGetFoods(Guid id, string name, CancellationToken cancellationToken)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        return Content(
            await cnn.GetFoodsByNameAsync<string>(name),
            "application/json");
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return Page();
        }

        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        await using var trn = await cnn.BeginTransactionAsync(cancellationToken);

        try
        {
            // update recipe details
            await cnn.ExecuteAsync(
                this.sql["UpdateRecipeDetails"],
                new
                {
                    Id = this.Recipe.Id,
                    Name = this.Recipe.Name.AsNVarChar(100),
                    Description = this.Recipe.Description.AsNVarChar(500),
                    Instructions = this.Recipe.Instructions.AsNVarChar(),
                }, trn);

            // update ingredients
            foreach (var ingredient in this.Recipe.Ingredients)
            {
                // update an ingredient
                await cnn.ExecuteAsync(
                    this.sql["UpdateIngredient"],
                    new
                    {
                        ingredient.Id,
                        Description = ingredient.Description.AsNVarChar(100),
                        ingredient.Number,
                        NumberValue = ingredient.Number?.Value,
                        NumberUnit = (DbEnum<MeasurementType>?)ingredient.Number?.Unit?.Type,
                        ingredient.Quantity,
                        QuantityValue = ingredient.Quantity?.Value,
                        QuantityUnit = (DbEnum<MeasurementType>?)ingredient.Quantity?.Unit?.Type,
                        ingredient.AltQuantity,
                        AltQuantityValue = ingredient.AltQuantity?.Value,
                        AltQuantityUnit = (DbEnum<MeasurementType>?)ingredient.AltQuantity?.Unit?.Type,
                    }, trn);

                // remove all food associations
                await cnn.ExecuteAsync(this.sql["DeleteIngredientFoods"], new { ingredient.Id }, trn);

                // update food associations
                foreach (var foodIdStr in ingredient.FoodIds)
                {
                    if (!Guid.TryParse(foodIdStr, out var foodId))
                    {
                        // it's a new food
                        foodId = await cnn.ExecuteScalarAsync<Guid>(this.sql["AddFood"],
                            new { Name = foodIdStr.AsNVarChar(50), ShortName = foodIdStr.AsNVarChar(50) }, trn);
                    }
                    // update an association
                    await cnn.ExecuteAsync(this.sql["ConnectIngredientFood"],
                        new { IngredientId = ingredient.Id, FoodId = foodId }, trn);
                }
            }

            await trn.CommitAsync(cancellationToken);
        }
        catch (Exception x)
        {
            await trn.RollbackAsync(cancellationToken);
            this.ModelState.AddModelError(String.Empty, x.Message);
            return Page();
        }

        return RedirectToPage("Index", new { id = this.Recipe.Id });
    }
}
