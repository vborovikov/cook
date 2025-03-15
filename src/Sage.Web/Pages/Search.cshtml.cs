#undef HAVE_FULLTEXT

namespace Sage.Web.Pages;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Relay.InteractionModel;
using Spryer;
using XPage = Relay.InteractionModel.Page;

public class SearchModel : PageModel
{
    private readonly DbDataSource db;
    private readonly DbScriptMap sql;

    public SearchModel(DbDataSource db, DbScriptMap sql)
    {
        this.db = db;
        this.sql = sql;
    }

    public IPage<RecipeInfo> Recipes { get; private set; } = XPage.Empty<RecipeInfo>();
    public TimeSpan SearchDuration { get; private set; }

    public async Task OnGet([FromQuery] PageRequest page, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        var prms = new DynamicParameters(page.AsDbPage());
        prms.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        prms.Add("FilterCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        prms.Add("TopN", XPage.AvailablePageSizes[^1] * 7, DbType.Int32, ParameterDirection.Input);

        var recipes =
            await
#if HAVE_FULLTEXT
                cnn.SearchRecipesFullTextAsync<RecipeInfo>(
#else
                cnn.SearchRecipesAsync<RecipeInfo>(
#endif
                prms, commandTimeout: 300);
        this.Recipes = XPage.From(recipes, prms.Get<int?>("TotalCount") ?? 0, prms.Get<int?>("FilterCount") ?? 0, page);
        this.SearchDuration = stopwatch.Elapsed;
    }
}
