using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ancestry.QueryProcessor.Sql
{
	public class SqlRepository<T> : IRepository<T>
	{
		public SqlRepository(SqlFactory factory, System.Type tupleType, string tableName)
		{
			_factory = factory;
			_tupleType = tupleType;
			_fields = tupleType.GetFields(BindingFlags.Public | BindingFlags.Instance).ToArray();
			_tableName = tableName;
			_collectionType = 
				typeof(ISet<>).IsAssignableFrom(typeof(T).GetGenericTypeDefinition())
					? typeof(HashSet<T>)
					: typeof(List<T>);

		}

		private SqlFactory _factory;
		private System.Type _tupleType;
		private FieldInfo[] _fields;
		private string _tableName;
		private System.Type _collectionType;

		public SqlFactory Factory { get { return _factory; } }

		public T Get(Parse.Expression condition, Name[] order)
		{
			ICollection<T> result = (ICollection<T>)Activator.CreateInstance(_collectionType);
			using (var connection = _factory.DbFactory.CreateConnection())
			{
				var command = connection.CreateCommand();
				command.CommandText = "select " + String.Join(", ", from f in _fields select f.Name) 
					+ " from " + _tableName 
					+ (order != null ? (" order by " + String.Join(",", order.ToString())) : "");
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var row = (T)Activator.CreateInstance(_tupleType);
					for (var i = 0; i < _fields.Length; i++)
						_fields[i].SetValue(row, reader[i]);
					result.Add(row);
				}
			}

			return (T)result;
		}

		public void Set(Parse.Expression condition, T newValue)
		{
			throw new NotImplementedException();
		}
	}
}
