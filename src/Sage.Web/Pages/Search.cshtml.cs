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
using XPage = Relay.InteractionModel.Page;

public class SearchModel : PageModel
{
    private readonly DbDataSource db;

    public SearchModel(DbDataSource db)
    {
        this.db = db;
    }

    public IPage<RecipeInfo> Recipes { get; private set; }
    public TimeSpan SearchDuration { get; private set; }

    public async Task OnGet([FromQuery]PageRequest page, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        var prms = new DynamicParameters(page.AsDbPage());
        prms.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        prms.Add("FilterCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        prms.Add("TopN", XPage.AvailablePageSizes[^1] * 7, DbType.Int32, ParameterDirection.Input);

        var recipes =
#if HAVE_FULLTEXT
            await cnn.QueryAsync<RecipeInfo>(
                """
                select @TotalCount = count(r.Id)
                from book.Recipes r;

                select @FilterCount = count(r.Id)
                from book.Recipes r
                inner join freetexttable(book.Recipes, Name, @Search, @TopN) ft on ft.[Key] = r.Id;
            
                select r.Id, r.Name, rs.Link, r.Description
                from book.Recipes r
                inner join book.RecipeSources rs on rs.RecipeId = r.Id
                inner join freetexttable(book.Recipes, Name, @Search, @TopN) ft on ft.[Key] = r.Id
                order by ft.Rank desc, len(r.Instructions) desc
                offset @SkipCount rows fetch next @TakeCount rows only;
                """, prms, commandTimeout: 300);
#else
            await cnn.QueryAsync<RecipeInfo>(
                """
                select @TotalCount = count(r.Id)
                from book.Recipes r;

                --select @FilterCount = count(distinct r.Id)
                --from book.Recipes r
                --left outer join book.RecipeFoods rf on rf.RecipeId = r.Id
                --left outer join book.Foods f on f.Id = rf.FoodId
                --where 
                --    charindex(@Search, r.Name) > 0 or 
                --    charindex(@Search, r.Description) > 0 or 
                --    charindex(@Search, r.Instructions) > 0 or
                --    charindex(@Search, f.Name) > 0;
            
                select distinct r.Id, r.Name, rs.Link, r.Description, len(r.Instructions) as Care
                from book.Recipes r
                inner join book.RecipeSources rs on rs.RecipeId = r.Id
                left outer join book.RecipeFoods rf on rf.RecipeId = r.Id
                left outer join book.Foods f on f.Id = rf.FoodId
                where 
                    charindex(@Search, r.Name) > 0 or 
                    charindex(@Search, r.Description) > 0 or 
                    charindex(@Search, r.Instructions) > 0 or
                    charindex(@Search, f.Name) > 0
                order by Care desc
                offset @SkipCount rows fetch next @TakeCount rows only;

                select @FilterCount = @@RowCount;
                """, prms, commandTimeout: 300);
#endif
        this.Recipes = XPage.From(recipes, prms.Get<int?>("TotalCount") ?? 0, prms.Get<int?>("FilterCount") ?? 0, page);
        this.SearchDuration = stopwatch.Elapsed;
    }
}
