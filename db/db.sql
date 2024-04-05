-- Database --

use master;
if DB_ID('Cook') is not null 
begin
    alter database Cook set single_user with rollback immediate;
    drop database Cook;
end;

if @@Error = 3702
   RaisError('Cannot delete the database because of the open connections.', 127, 127) with nowait, log;

create database Cook collate Latin1_General_100_CI_AS;
go

use Cook;
go

-- Schemas --

create schema book authorization dbo;
go

-- Tables --

-- Url data-type is nvarchar(850) (850 characters because of the 'unique' constraint)
-- Email data-type is varchar(254)


create table book.Recipes
(
    Id uniqueidentifier not null default newid(),
    Name nvarchar(100) not null check (Name != N'') index IXC_Recipes_Name clustered,
    Description nvarchar(500) null,
    Instructions nvarchar(max) not null check (Instructions != N''),
    IsParsed bit not null default 0,
    constraint PK_Recipes_Id primary key (Id)
);

create table book.Foods
(
    Id uniqueidentifier not null default newid(),
    Name nvarchar(50) not null check (Name != N'') index IXC_Foods_Name unique clustered,
    ShortName nvarchar(50) null,
    constraint PK_Foods_Id primary key (Id)
);

create table book.Ingredients
(
    Id uniqueidentifier not null default newid(),
    Description nvarchar(100) not null check (Description != N''),
    --todo: add DescriptionNormalized to compare with existing ingredients, make unique clustered index
    Number nvarchar(20) null,  -- Measure.ToString()
    NumberValue float null,    -- Fractional.Value
    NumberUnit char(3) null,   -- MeasurementType
    Quantity nvarchar(20) null,
    QuantityValue float null,
    QuantityUnit char(3) null,
    AltQuantity nvarchar(20) null,
    AltQuantityValue float null,
    AltQuantityUnit char(3) null,
    constraint PK_Ingredients_Id primary key (Id)
);

create table book.IngredientFoods
(
    IngredientId uniqueidentifier not null foreign key references book.Ingredients(Id) on delete cascade index IX_IngredientFoods_IngredientId nonclustered,
    FoodId uniqueidentifier not null foreign key references book.Foods(Id) on delete cascade,
    constraint PK_IngredientFoods primary key (IngredientId, FoodId)
);

create table book.RecipeIngredients
(
    RecipeId uniqueidentifier not null foreign key references book.Recipes(Id) on delete cascade index IX_RecipeIngredients_RecipeId nonclustered,
    IngredientId uniqueidentifier not null foreign key references book.Ingredients(Id) on delete cascade,
    Turn tinyint null,
    constraint PK_RecipeIngredients primary key (RecipeId, IngredientId)
);

create table book.RecipeSources
(
    RecipeId uniqueidentifier not null foreign key references book.Recipes(Id) on delete cascade index IX_RecipeSources_RecipeId nonclustered,
    Content nvarchar(max) not null check(Content != N''),
    ContentType varchar(100) not null default 'text/csv' check (ContentType != ''),
    Link nvarchar(850) not null unique check (Link != N'')
);
go

create view book.RecipeFoods with schemabinding as
    select distinct r.RecipeId, f.FoodId
    from book.RecipeIngredients r
    inner join book.IngredientFoods f on f.IngredientId = r.IngredientId
go
