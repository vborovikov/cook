namespace Sage.Web.Data
{
    using Pantry;
    using Dapper;
    using Spryer;
    using System.Data;

    static class DapperSupport
    {
        private sealed class UriTypeHandler : SqlMapper.TypeHandler<Uri>
        {
            public override Uri Parse(object value) => Uri.TryCreate(value as string, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
            public override void SetValue(IDbDataParameter parameter, Uri value)
            {
                parameter.DbType = DbType.String;
                parameter.Size = 850;
                parameter.Value = value.ToString();
            }
        }

        private sealed class MeasureTypeHandler : SqlMapper.TypeHandler<Measure>
        {
            public override Measure Parse(object value) =>
                Measure.TryParse(value as string, out var measure) ? measure : default;

            public override void SetValue(IDbDataParameter parameter, Measure value)
            {
                parameter.DbType = DbType.String;
                parameter.Size = 20;
                parameter.Value = value != default ? value.ToString() : DBNull.Value;
            }
        }

        private sealed class FractionalTypeHandler : SqlMapper.TypeHandler<Fractional>
        {
            public override Fractional Parse(object value) => value switch
            {
                float num => num,
                string str => Fractional.Parse(str),
                _ => default
            };

            public override void SetValue(IDbDataParameter parameter, Fractional value)
            {
                parameter.DbType = DbType.Single;
                parameter.Value = Fractional.IsNaN(value) ? DBNull.Value : value.Value;
            }
        }

        public static void Initialize()
        {
            SqlMapper.AddTypeHandler(new UriTypeHandler());
            SqlMapper.AddTypeHandler(new MeasureTypeHandler());
            SqlMapper.AddTypeHandler(new FractionalTypeHandler());
            DbEnum<MeasurementType>.Initialize();
        }
    }
}
