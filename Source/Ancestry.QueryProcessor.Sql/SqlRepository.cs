using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Ancestry.QueryProcessor.Sql
{
	public class SqlRepository<T> : IRepository<T>
	{
		public static readonly MethodInfo DataReaderIndexer = typeof(DbDataReader).GetMethod("get_Item", new System.Type[] { typeof(int) });

		public SqlRepository(SqlFactory factory, System.Type tupleType, string tableName)
		{
			_factory = factory;
			_tupleType = tupleType;
			_fields = tupleType.GetFields(BindingFlags.Public | BindingFlags.Instance).ToArray();
			_tableName = tableName;
			_collectionType = 
				typeof(ISet<>).MakeGenericType(tupleType).IsAssignableFrom(typeof(T))
					? typeof(Runtime.Set<>).MakeGenericType(tupleType)
					: typeof(Runtime.ListEx<>).MakeGenericType(tupleType);
			_addRowDelegate = CompileAddRowDelegate();
		}

		private Action<DbDataReader, T> CompileAddRowDelegate()
		{
			var reader = Expression.Parameter(typeof(DbDataReader), "reader");
			var result = Expression.Parameter(typeof(T), "result");
			
			var row = Expression.Parameter(_tupleType, "row");

			var block = new List<Expression>
				{
					Expression.Assign(row, Expression.New(_tupleType)),
				};
			for (var i = 0; i < _fields.Length; i++)
				block.Add
				(
					Expression.Assign
					(
						Expression.Field(row, _fields[i]),
						Expression.Convert
						(
							Expression.Call(reader, DataReaderIndexer, Expression.Constant(i)),
							_fields[i].FieldType
						)
					)
				);
			block.Add(Expression.Call(result, typeof(ICollection<>).MakeGenericType(_tupleType).GetMethod("Add"), row));


			var body = 
				Expression.Block
				(
					new ParameterExpression[] { row },
					block
				);

			var lambda = Expression.Lambda<Action<DbDataReader, T>>(body, reader, result);
			return lambda.Compile();
		}

		private SqlFactory _factory;
		private System.Type _tupleType;
		private FieldInfo[] _fields;
		private string _tableName;
		private System.Type _collectionType;
		private Action<DbDataReader, T> _addRowDelegate;

		public SqlFactory Factory { get { return _factory; } }

		public T Get(Parse.Expression condition, Name[] order)
		{
			T result = (T)Activator.CreateInstance(_collectionType);
			using (var connection = _factory.DbFactory.CreateConnection())
			{
				connection.ConnectionString = _factory.ConnectionString;
				connection.Open();
				var command = connection.CreateCommand();
				command.CommandText = "select " + String.Join(", ", from f in _fields select f.Name) 
					+ " from " + _tableName 
					+ (order != null ? (" order by " + String.Join(",", order.ToString())) : "");
				var reader = command.ExecuteReader();
				while (reader.Read())
					_addRowDelegate(reader, result);
			}

			return result;
		}

		public void Set(Parse.Expression condition, T newValue)
		{
			throw new NotImplementedException();
		}
	}
}
