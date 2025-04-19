using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using System.Text.RegularExpressions;

namespace App.Repositories.Context
{
    public static class DbContextExtensions
    {
        private static readonly Regex _keysRegex = new Regex("^(PK|FK|IX)_", RegexOptions.Compiled);
        private static readonly Regex _splitWordsRegex = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])");

        public static void UseSnakeCaseNames(this ModelBuilder modelBuilder)
        {
            var mapper = new NpgsqlSnakeCaseNameTranslator();
            
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = HandleSpecialCases(entity.GetTableName()!);
                entity.SetTableName(mapper.TranslateTypeName(tableName));

                if (entity.GetSchema() != null)
                    entity.SetSchema(mapper.TranslateTypeName(entity.GetSchema()!));

                foreach (var property in entity.GetProperties())
                {
                    var columnName = HandleSpecialCases(property.GetColumnName()!);
                    property.SetColumnName(mapper.TranslateMemberName(columnName));
                }

                foreach (var key in entity.GetKeys())
                {
                    var keyName = HandleSpecialCases(key.GetName()!);
                    key.SetName(_keysRegex.Replace(keyName, m => m.Value.ToLower()));
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    var fkName = HandleSpecialCases(foreignKey.GetConstraintName()!);
                    foreignKey.SetConstraintName(_keysRegex.Replace(fkName, m => m.Value.ToLower()));
                }

                foreach (var index in entity.GetIndexes())
                {
                    var indexName = HandleSpecialCases(index.GetDatabaseName()!);
                    index.SetDatabaseName(_keysRegex.Replace(indexName, m => m.Value.ToLower()));
                }
            }
        }

        private static string HandleSpecialCases(string name)
        {
            name = name.Replace("AspNet", "_"); 
            
            name = _splitWordsRegex.Replace(name, "_");

            return name.ToLower();
        }
    }
}
