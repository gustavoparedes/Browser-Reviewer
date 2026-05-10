using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Browser_Reviewer
{
    internal static class SearchSql
    {
        public static string TextCondition(params string[] columns)
        {
            if (!Helpers.searchTermExists || columns == null || columns.Length == 0)
                return string.Empty;

            string op = Helpers.searchTermRegExp ? "REGEXP" : "LIKE";
            return "(" + string.Join(" OR ", columns.Select(column => $"{column} {op} @searchTerm")) + ")";
        }

        public static string TimeCondition(params string[] columns)
        {
            if (!Helpers.searchTimeCondition || columns == null || columns.Length == 0)
                return string.Empty;

            return "(" + string.Join(" OR ", columns.Select(column => $"({column} >= @startUtc AND {column} <= @endUtc)")) + ")";
        }

        public static string LabelCondition(string column = "Label")
        {
            if (!Helpers.searchLabelsOnly || string.IsNullOrWhiteSpace(column))
                return string.Empty;

            return $"({column} IS NOT NULL AND TRIM({column}) <> '')";
        }

        public static string Where(params string[] conditions)
        {
            var activeConditions = conditions
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .Select(condition => condition.Trim())
                .ToArray();

            return activeConditions.Length == 0
                ? string.Empty
                : "WHERE " + string.Join(" AND ", activeConditions);
        }

        public static string And(params string[] conditions)
        {
            var activeConditions = conditions
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .Select(condition => condition.Trim())
                .ToArray();

            return activeConditions.Length == 0
                ? string.Empty
                : " AND " + string.Join(" AND ", activeConditions);
        }

        public static string DateExpr(string column)
        {
            string offset = OffsetModifier();
            return string.IsNullOrEmpty(offset)
                ? $"STRFTIME('%Y-%m-%d %H:%M:%f', {column})"
                : $"STRFTIME('%Y-%m-%d %H:%M:%f', {column}, '{offset}')";
        }

        public static string OffsetModifier()
        {
            int utcOffset = Helpers.utcOffset;
            if (utcOffset == 0)
                return string.Empty;

            return utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";
        }

        public static void AddParameters(SQLiteCommand command)
        {
            if (Helpers.searchTermExists)
            {
                command.Parameters.AddWithValue(
                    "@searchTerm",
                    Helpers.searchTermRegExp ? Helpers.searchTerm : $"%{Helpers.searchTerm}%");
            }

            if (Helpers.searchTimeCondition)
            {
                command.Parameters.AddWithValue("@startUtc", Helpers.sd);
                command.Parameters.AddWithValue("@endUtc", Helpers.ed);
            }
        }
    }
}
