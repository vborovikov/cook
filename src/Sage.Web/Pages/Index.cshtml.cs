namespace Sage.Web.Pages;

using System.Data.Common;
using System.Text.Json;
using Dapper;
using Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly DbDataSource db;

    public IndexModel(DbDataSource db)
    {
        this.db = db;
    }

    public Recipe Recipe { get; set; }

    public async Task OnGet(Guid? id = default, CancellationToken cancellationToken = default)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        this.Recipe = JsonSerializer.Deserialize<Recipe>(await cnn.ExecuteScalarAsync<string>(
            """
            if @RecipeId is null
                select top 1 @RecipeId = r.Id
                from book.Recipes r
                tablesample (1000 rows)
                order by crypt_gen_random(4);

            select (                
                select r.Id, r.Name, rs.Link, r.Description, r.Instructions,
                    json_query((
                        select i.Id, i.Description
                        from book.Ingredients i
                        inner join book.RecipeIngredients ri on ri.IngredientId = i.Id
                        where ri.RecipeId = r.Id
                        order by ri.Turn
                        for json path
                    )) as Ingredients
                from book.Recipes r
                inner join book.RecipeSources rs on rs.RecipeId = r.Id
                where r.Id = @RecipeId
                for json path, without_array_wrapper
            );
            """, new { RecipeId = id }
        ));
    }
}
