--@version 0.2.0

use Cook;
go

declare @RecipeId uniqueidentifier;

--@script GetRecipeById
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

--@script GetImportedRecipeById
select r.Content as Recipe, r.Link as Source
from book.RecipeSources r
where r.RecipeId = @RecipeId;

--@script ImportRecipe
insert into book.Recipes (Id, Name, Description, Instructions, IsParsed)
values (@Id, @Name, @Description, @Instructions, @IsParsed);
                
insert into book.RecipeSources (RecipeId, Content, ContentType, Link)
values (@Id, @Content, 'text', @Link);

--@script DiscardIngredients
delete i from book.Ingredients i
inner join book.RecipeIngredients ri on ri.IngredientId = i.Id
where ri.RecipeId = @RecipeId;

--@script UpdateImportedRecipe
update book.Recipes
set Name = @Name, Description = @Description, Instructions = @Instructions, IsParsed = @IsParsed
where Id = @RecipeId;
                
update book.RecipeSources
set Content = @Content, ContentType = 'text', Link = @Link
where RecipeId = @RecipeId;

--@script StoreIngredients
insert into book.Ingredients (Description,
    Number, NumberValue, NumberUnit,
    Quantity, QuantityValue, QuantityUnit,
    AltQuantity, AltQuantityValue, AltQuantityUnit)
output inserted.Id
values (@Description,
    @Number, @NumberValue, @NumberUnit,
    @Quantity, @QuantityValue, @QuantityUnit,
    @AltQuantity, @AltQuantityValue, @AltQuantityUnit);

--@script StoreRecipeIngredients
insert into book.RecipeIngredients (RecipeId, IngredientId, Turn)
values (@RecipeId, @IngredientId, @Turn);

--@script EditRecipeById
select (
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
    for json path, without_array_wrapper
);

--@script GetFoodsByName
select (
    select f.Id, f.Name
    from book.Foods f
    where f.Name like '%' + @Name + '%'
    for json path
);

--@script UpdateRecipeDetails
update book.Recipes
set Name = @Name, Description = @Description, Instructions = @Instructions
where Id = @Id;

--@script UpdateIngredient
update book.Ingredients
set Description = @Description,
    Number = @Number, NumberValue = @NumberValue, NumberUnit = @NumberUnit,
    Quantity = @Quantity, QuantityValue = @QuantityValue, QuantityUnit = @QuantityUnit,
    AltQuantity = @AltQuantity, AltQuantityValue = @AltQuantityValue, AltQuantityUnit = @AltQuantityUnit
where Id = @Id;

--@script DeleteIngredientFoods
delete from book.IngredientFoods
where IngredientId = @Id;

--@script AddFood
insert into book.Foods (Name, ShortName)
output inserted.Id
values (@Name, @ShortName);

--@script ConnectIngredientFood
insert into book.IngredientFoods (IngredientId, FoodId)
values (@IngredientId, @FoodId);

--@script SearchRecipesFullText
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

--@script SearchRecipes
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