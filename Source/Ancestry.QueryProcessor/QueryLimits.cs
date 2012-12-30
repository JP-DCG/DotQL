using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor
{
	public class QueryLimits
	{
		public const int DefaultMaximumTime = 3000;
		private int _maximumTime = DefaultMaximumTime;
		/// <summary> The maximum number of milliseconds that may transpire before the query exceeds its SLA. </summary>
		public int MaximumTime { get { return _maximumTime; } set { _maximumTime = value; } }

		public const int DefaultMaximumRows = 5000;
		private int _maximumRows = DefaultMaximumRows;
		/// <summary> The maximum number of total rows that may be accessed before the query exceeds its SLA. </summary>
		public int MaximumRows { get { return _maximumRows; } set { _maximumRows = value; } }
	}
}
