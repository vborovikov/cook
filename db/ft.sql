-- Full-text search --
-- 0x0409 en-US

use Cook;
go

-- enable full-text search for the database
exec sp_fulltext_database 'enable';

create fulltext catalog CookFullText;

create fulltext stoplist CookStopList from system stoplist;
alter fulltext stoplist CookStopList add 'c' language 0x0409;
alter fulltext stoplist CookStopList add 'coffeespoon' language 0x0409;
alter fulltext stoplist CookStopList add 'coffeespoons' language 0x0409;
alter fulltext stoplist CookStopList add 'csp' language 0x0409;
alter fulltext stoplist CookStopList add 'cup' language 0x0409;
alter fulltext stoplist CookStopList add 'cups' language 0x0409;
alter fulltext stoplist CookStopList add 'dash' language 0x0409;
alter fulltext stoplist CookStopList add 'dashes' language 0x0409;
alter fulltext stoplist CookStopList add 'dessertspoon' language 0x0409;
alter fulltext stoplist CookStopList add 'dessertspoons' language 0x0409;
alter fulltext stoplist CookStopList add 'dr' language 0x0409;
alter fulltext stoplist CookStopList add 'dram' language 0x0409;
alter fulltext stoplist CookStopList add 'drams' language 0x0409;
alter fulltext stoplist CookStopList add 'drop' language 0x0409;
alter fulltext stoplist CookStopList add 'drops' language 0x0409;
alter fulltext stoplist CookStopList add 'ds' language 0x0409;
alter fulltext stoplist CookStopList add 'dsp' language 0x0409;
alter fulltext stoplist CookStopList add 'dssp' language 0x0409;
alter fulltext stoplist CookStopList add 'dstspn' language 0x0409;
alter fulltext stoplist CookStopList add 'fl' language 0x0409;
alter fulltext stoplist CookStopList add 'fluid' language 0x0409;
alter fulltext stoplist CookStopList add 'g' language 0x0409;
alter fulltext stoplist CookStopList add 'gal' language 0x0409;
alter fulltext stoplist CookStopList add 'gallon' language 0x0409;
alter fulltext stoplist CookStopList add 'gallons' language 0x0409;
alter fulltext stoplist CookStopList add 'gill' language 0x0409;
alter fulltext stoplist CookStopList add 'glass' language 0x0409;
alter fulltext stoplist CookStopList add 'glasses' language 0x0409;
alter fulltext stoplist CookStopList add 'gr' language 0x0409;
alter fulltext stoplist CookStopList add 'gram' language 0x0409;
alter fulltext stoplist CookStopList add 'gramme' language 0x0409;
alter fulltext stoplist CookStopList add 'grammes' language 0x0409;
alter fulltext stoplist CookStopList add 'grams' language 0x0409;
alter fulltext stoplist CookStopList add 'gt' language 0x0409;
alter fulltext stoplist CookStopList add 'gtt' language 0x0409;
alter fulltext stoplist CookStopList add 'kg' language 0x0409;
alter fulltext stoplist CookStopList add 'kilogram' language 0x0409;
alter fulltext stoplist CookStopList add 'kilogramme' language 0x0409;
alter fulltext stoplist CookStopList add 'kilogrammes' language 0x0409;
alter fulltext stoplist CookStopList add 'kilograms' language 0x0409;
alter fulltext stoplist CookStopList add 'lb' language 0x0409;
alter fulltext stoplist CookStopList add 'lbs' language 0x0409;
alter fulltext stoplist CookStopList add 'liter' language 0x0409;
alter fulltext stoplist CookStopList add 'liters' language 0x0409;
alter fulltext stoplist CookStopList add 'litre' language 0x0409;
alter fulltext stoplist CookStopList add 'litres' language 0x0409;
alter fulltext stoplist CookStopList add 'milliliter' language 0x0409;
alter fulltext stoplist CookStopList add 'milliliters' language 0x0409;
alter fulltext stoplist CookStopList add 'millilitre' language 0x0409;
alter fulltext stoplist CookStopList add 'millilitres' language 0x0409;
alter fulltext stoplist CookStopList add 'mL' language 0x0409;
alter fulltext stoplist CookStopList add 'ounce' language 0x0409;
alter fulltext stoplist CookStopList add 'ounces' language 0x0409;
alter fulltext stoplist CookStopList add 'oz' language 0x0409;
alter fulltext stoplist CookStopList add 'ozs' language 0x0409;
alter fulltext stoplist CookStopList add 'pcs' language 0x0409;
alter fulltext stoplist CookStopList add 'pinch' language 0x0409;
alter fulltext stoplist CookStopList add 'pinches' language 0x0409;
alter fulltext stoplist CookStopList add 'pint' language 0x0409;
alter fulltext stoplist CookStopList add 'pints' language 0x0409;
alter fulltext stoplist CookStopList add 'pn' language 0x0409;
alter fulltext stoplist CookStopList add 'pot' language 0x0409;
alter fulltext stoplist CookStopList add 'pottle' language 0x0409;
alter fulltext stoplist CookStopList add 'pottles' language 0x0409;
alter fulltext stoplist CookStopList add 'pound' language 0x0409;
alter fulltext stoplist CookStopList add 'pounds' language 0x0409;
alter fulltext stoplist CookStopList add 'pt' language 0x0409;
alter fulltext stoplist CookStopList add 'pts' language 0x0409;
alter fulltext stoplist CookStopList add 'qt' language 0x0409;
alter fulltext stoplist CookStopList add 'qts' language 0x0409;
alter fulltext stoplist CookStopList add 'quart' language 0x0409;
alter fulltext stoplist CookStopList add 'quarts' language 0x0409;
alter fulltext stoplist CookStopList add 'saltspoon' language 0x0409;
alter fulltext stoplist CookStopList add 'saltspoons' language 0x0409;
alter fulltext stoplist CookStopList add 'scruple' language 0x0409;
alter fulltext stoplist CookStopList add 'scruples' language 0x0409;
alter fulltext stoplist CookStopList add 'smdg' language 0x0409;
alter fulltext stoplist CookStopList add 'smi' language 0x0409;
alter fulltext stoplist CookStopList add 'smidgen' language 0x0409;
alter fulltext stoplist CookStopList add 'smidgens' language 0x0409;
alter fulltext stoplist CookStopList add 'ssp' language 0x0409;
alter fulltext stoplist CookStopList add 't' language 0x0409;
alter fulltext stoplist CookStopList add 'tablespoon' language 0x0409;
alter fulltext stoplist CookStopList add 'tablespoons' language 0x0409;
alter fulltext stoplist CookStopList add 'tbsp' language 0x0409;
alter fulltext stoplist CookStopList add 'tbsps' language 0x0409;
alter fulltext stoplist CookStopList add 'tcf' language 0x0409;
alter fulltext stoplist CookStopList add 'teacup' language 0x0409;
alter fulltext stoplist CookStopList add 'teacups' language 0x0409;
alter fulltext stoplist CookStopList add 'teaspoon' language 0x0409;
alter fulltext stoplist CookStopList add 'teaspoons' language 0x0409;
alter fulltext stoplist CookStopList add 'tsp' language 0x0409;
alter fulltext stoplist CookStopList add 'tsps' language 0x0409;
alter fulltext stoplist CookStopList add 'wgf' language 0x0409;
alter fulltext stoplist CookStopList add 'wineglass' language 0x0409;
alter fulltext stoplist CookStopList add 'wineglasses' language 0x0409;

create fulltext index
    on book.Recipes (Name language 0x0409, Description language 0x0409, Instructions language 0x0409)
    key index PK_Recipes_Id
    on CookFullText with (change_tracking = manual, stoplist = CookStopList);

create fulltext index
    on book.Foods (Name language 0x0409)
    key index PK_Foods_Id
    on CookFullText with (change_tracking = auto, stoplist = CookStopList);

create fulltext index
    on book.Ingredients (Description language 0x0409)
    key index PK_Ingredients_Id
    on CookFullText with (change_tracking = auto, stoplist = CookStopList);

--create fulltext index
--    on book.RecipeSources (Content language 0x0409)
--    key index UQ_RecipeSources_Link
--    on CookFullText with (change_tracking = auto, stoplist = CookStopList);
