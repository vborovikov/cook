namespace Sage.Web.Pages;

using System.Data.Common;
using System.Text.Json;
using Dapper;
using Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spryer;

public class IndexModel : PageModel
{
    private readonly DbDataSource db;
    private readonly DbScriptMap sql;

    public IndexModel(DbDataSource db, DbScriptMap sql)
    {
        this.db = db;
        this.sql = sql;
    }

    public Recipe Recipe { get; set; }

    public async Task OnGet(Guid? id = default, CancellationToken cancellationToken = default)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        this.Recipe = JsonSerializer.Deserialize<Recipe>(
            await cnn.ExecuteScalarAsync<string>(this.sql["GetRecipeById"], new { RecipeId = id }));
    }
}
