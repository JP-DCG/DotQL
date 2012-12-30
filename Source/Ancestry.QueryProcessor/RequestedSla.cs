using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	[Flags]
	public enum SlaCapability
	{
		Updates = 1,
		DeclareModules = 2,
		Upgrade = 3,
		Downgrade = 4
	}

	public class RequestedSla
	{
		private SlaCapability _flags;
		/// <summary> Requested Capabilities. </summary>
		/// <remarks> These capabilities must be authorize by an appropriate authorization token. </remarks>
		public SlaCapability Flags { get { return _flags; } set { _flags = value; } }

		// TODO: who is requesting

		// TODO: authorization token

		public const int DefaultMaximumTime = 5000;
		private int _maximumTime = DefaultMaximumTime;
		/// <summary> The maximum number of milliseconds that may transpire before the query exceeds its SLA. </summary>
		public int MaximumTime { get { return _maximumTime; } set { _maximumTime = value; } }

		public const int DefaultAverageTime = 5000;
		private int _averageTime = DefaultAverageTime;
		/// <summary> The average number of milliseconds for queries under this SLA. </summary>
		public int AverageTime { get { return _averageTime; } set { _averageTime = value; } }
	}
}
