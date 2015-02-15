using System;
using System.Reflection;

namespace Projector.Data.Tables
{
    public static class TableExtensions
    {
        public static Table<Tsource> CreateTable<Tsource>()
        {
            return CreateTable<Tsource>(1024);
        }

        public static Table<Tsource> CreateTable<Tsource>(int capacity)
        {
            var t = typeof(Tsource);

            var propInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var schema = new Schema(capacity);

            foreach (var propInfoItem in propInfos)
            {
                if (propInfoItem.PropertyType == typeof(string))
                {
                    schema.CreateField<string>(propInfoItem.Name);
                }
                else if (propInfoItem.PropertyType == typeof(int))
                {
                    schema.CreateField<int>(propInfoItem.Name);
                }
                else if (propInfoItem.PropertyType == typeof(long))
                {
                    schema.CreateField<long>(propInfoItem.Name);
                }
                else
                {
                    throw new InvalidOperationException("Type: " + propInfoItem.PropertyType + " is not supported");
                }
            }

            return new Table<Tsource>(schema);
        }
    }
}
