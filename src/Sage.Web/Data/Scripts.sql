--@version 0.2.1

--@namespace Sage.Web.Data
--@using Pantry

use Cook;
go

declare @RecipeId uniqueidentifier;

--@query-json GetRecipeById(@RecipeId uniqueidentifier)
if @RecipeId is null
    select top 1 @RecipeId = r.Id
    from book.Recipes r
    tablesample (1000 rows)
    order by crypt_gen_random(4);

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
for json path, without_array_wrapper;

--@query-first GetImportedRecipeById(@RecipeId uniqueidentifier)
select r.Content as Recipe, r.Link as Source
from book.RecipeSources r
where r.RecipeId = @RecipeId;

/*@
execute ImportRecipe(@Id uniqueidentifier, 
    @Name nvarchar(100), @Description nvarchar(500), @Instructions nvarchar(max), 
    @IsParsed bit, @Content  nvarchar(max), @Link nvarchar(850))
@*/
insert into book.Recipes (Id, Name, Description, Instructions, IsParsed)
values (@Id, @Name, @Description, @Instructions, @IsParsed);
                
insert into book.RecipeSources (RecipeId, Content, ContentType, Link)
values (@Id, @Content, 'text', @Link);

--@execute DiscardIngredients(@RecipeId uniqueidentifier)
delete i from book.Ingredients i
inner join book.RecipeIngredients ri on ri.IngredientId = i.Id
where ri.RecipeId = @RecipeId;

/*@
execute UpdateImportedRecipe(@RecipeId uniqueidentifier, 
    @Name nvarchar(100), @Description nvarchar(500), @Instructions nvarchar(max), 
    @IsParsed bit, @Content  nvarchar(max), @Link nvarchar(850))
@*/
update book.Recipes
set Name = @Name, Description = @Description, Instructions = @Instructions, IsParsed = @IsParsed
where Id = @RecipeId;
                
update book.RecipeSources
set Content = @Content, ContentType = 'text', Link = @Link
where RecipeId = @RecipeId;

/*@
execute-scalar StoreIngredients(@Description nvarchar(100), 
    @Number Measure, @NumberValue Fractional, @NumberUnit DbEnum<MeasurementType>?, 
    @Quantity Measure, @QuantityValue Fractional, @QuantityUnit DbEnum<MeasurementType>?, 
    @AltQuantity Measure, @AltQuantityValue Fractional, @AltQuantityUnit DbEnum<MeasurementType>?)
@*/
insert into book.Ingredients (Description,
    Number, NumberValue, NumberUnit,
    Quantity, QuantityValue, QuantityUnit,
    AltQuantity, AltQuantityValue, AltQuantityUnit)
output inserted.Id
values (@Description,
    @Number, @NumberValue, @NumberUnit,
    @Quantity, @QuantityValue, @QuantityUnit,
    @AltQuantity, @AltQuantityValue, @AltQuantityUnit);

--@execute StoreRecipeIngredients(@RecipeId uniqueidentifier, @IngredientId uniqueidentifier, @Turn tinyint)
insert into book.RecipeIngredients (RecipeId, IngredientId, Turn)
values (@RecipeId, @IngredientId, @Turn);

--@query-json EditRecipeById(@RecipeId uniqueidentifier)
select r.Id, r.Name, r.Description, r.Instructions,
    json_query((
        select i.Id, i.Description,
            i.Number, i.Quantity, i.AltQuantity,
            json_query((
                select f.FoodId as Id, fs.Name
                from book.IngredientFoods f
                inner join book.Foods fs on fs.Id = f.FoodId
                where f.IngredientId = i.Id
                for json path
            )) as Foods
        from book.Ingredients i
        inner join book.RecipeIngredients ri on ri.IngredientId = i.Id
        where ri.RecipeId = r.Id
        order by ri.Turn
        for json path
    )) as Ingredients
from book.Recipes r
where r.Id = @RecipeId
for json path, without_array_wrapper;

--@query-json GetFoodsByName(@Name nvarchar(50))
select f.Id, f.Name
from book.Foods f
where f.Name like '%' + @Name + '%'
for json path;

--@execute UpdateRecipeDetails(@Id uniqueidentifier, @Name nvarchar(100), @Description nvarchar(500), @Instructions nvarchar(max))
update book.Recipes
set Name = @Name, Description = @Description, Instructions = @Instructions
where Id = @Id;

/*@
execute UpdateIngredient(@Id uniqueidentifier, @Description nvarchar(100), 
    @Number Measure, @NumberValue Fractional, @NumberUnit DbEnum<MeasurementType>?, 
    @Quantity Measure, @QuantityValue Fractional, @QuantityUnit DbEnum<MeasurementType>?, 
    @AltQuantity Measure, @AltQuantityValue Fractional, @AltQuantityUnit DbEnum<MeasurementType>?)
@*/
update book.Ingredients
set Description = @Description,
    Number = @Number, NumberValue = @NumberValue, NumberUnit = @NumberUnit,
    Quantity = @Quantity, QuantityValue = @QuantityValue, QuantityUnit = @QuantityUnit,
    AltQuantity = @AltQuantity, AltQuantityValue = @AltQuantityValue, AltQuantityUnit = @AltQuantityUnit
where Id = @Id;

--@execute DeleteIngredientFoods(@Id uniqueidentifier)
delete from book.IngredientFoods
where IngredientId = @Id;

--@execute-scalar AddFood(@Name nvarchar(50), @ShortName nvarchar(50))
insert into book.Foods (Name, ShortName)
output inserted.Id
values (@Name, @ShortName);

--@execute ConnectIngredientFood(@IngredientId uniqueidentifier, @FoodId uniqueidentifier)
insert into book.IngredientFoods (IngredientId, FoodId)
values (@IngredientId, @FoodId);

--@query SearchRecipesFullText(@Parameters object)
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

--@query SearchRecipes(@Parameters object)
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

--@