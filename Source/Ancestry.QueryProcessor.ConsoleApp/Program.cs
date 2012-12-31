using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.ConsoleApp
{
	class Program
	{
		internal enum Command
		{
			None = 0,
			Execute,
			Stop
		}

		static void Main( string[] args )
		{
			Console.WriteLine( "Execute: Control Enter or F5" );
			Console.WriteLine( "Stop: Control C or Control F5" );

			StringBuilder sb = new StringBuilder( );
			bool done = false;

			Command command = Command.None;

			while( !done )
			{
				command = Command.None;

				ConsoleKeyInfo info = Console.ReadKey( false );

				switch( info.Key )
				{
					case ConsoleKey.F5:
						if( info.Modifiers == ConsoleModifiers.Control )
						{
							command = Command.Stop;
						}
						else
						{
							command = Command.Execute;
						}
						break;

					case ConsoleKey.Enter:
						if( info.Modifiers == ConsoleModifiers.Control )
						{
							command = Command.Execute;
						}
						break;

					default:
						command = Command.None;
						break;
				}

				switch( command )
				{
					case Command.Execute:
						Execute( sb );
						break;

					case Command.Stop:
						done = true;
						Console.WriteLine( "Stop" );
						break;

					case Command.None:
						if( info.Key == ConsoleKey.Enter )
						{
							sb.AppendLine( );
						}
						else
						{
							sb.Append( info.KeyChar );
						}
						break;

					default:
						break;
				}

				if( info.Key == ConsoleKey.Enter )
				{
					Console.WriteLine( );
				}

			}
		}

		private static void Execute( StringBuilder sb )
		{
			Console.WriteLine( );
			Console.WriteLine( "Executing:" );
			Console.WriteLine( "\"{0}\"", sb.ToString( ) );
			Console.WriteLine( );

			try
			{

				var connection = new Connection( );

				var result = connection.Evaluate( sb.ToString( ) );
				Console.WriteLine( "Results: {0}", result.ToString( ) );
			}
			catch( Exception e )
			{
				Console.WriteLine( e.ToString( ) );
			}
			finally
			{
				sb.Clear( );
			}
		}
	}
}
