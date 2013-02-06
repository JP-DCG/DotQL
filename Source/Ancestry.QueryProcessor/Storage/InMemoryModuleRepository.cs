using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public class InMemoryModuleRepository : IRepository<ISet<Runtime.ModuleTuple>>
	{
		public InMemoryModuleRepository(InMemoryFactory repository)
		{
			_repository = repository;
		}

		private InMemoryFactory _repository;

		public ISet<Runtime.ModuleTuple> Get(Parse.Expression condition, Name[] order)
		{
			if (condition == null)
				return _repository.Modules;
			else if 
			(
				condition is Parse.BinaryExpression 
					&& ((Parse.BinaryExpression)condition).Operator == Parse.Operator.Equal 
					&& ((Parse.BinaryExpression)condition).Left is Parse.IdentifierExpression
					&& Name.FromQualifiedIdentifier(((Parse.IdentifierExpression)((Parse.BinaryExpression)condition).Left).Target) == Name.FromComponents("Name")
					&& ((Parse.BinaryExpression)condition).Right is Parse.LiteralExpression
			)
				return new HashSet<Runtime.ModuleTuple>(from m in _repository.Modules where m.Name == (Name)((Parse.LiteralExpression)((Parse.BinaryExpression)condition).Right).Value select m);
			else
				throw new Exception("InMemoryModuleRepository is unable to process complex expressions.");
		}

		public void Set(Parse.Expression condition, ISet<Runtime.ModuleTuple> newValue)
		{
			foreach (var m in Get(condition, null).ToArray())
				_repository.Modules.Remove(m);
			foreach (var m in newValue)
				_repository.Modules.Add(m);
		}
	}
}
