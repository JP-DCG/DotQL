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
            Console.WriteLine( "Control Enter to Excecute or F5" );
            Console.WriteLine( "Control C to Stop or Control F5" );

            StringBuilder sb = new StringBuilder( );
            bool done = false;

            Command command = Command.None;

            while( !done )
            {
                command = Command.None;

                ConsoleKeyInfo info = Console.ReadKey( false );

                switch(info.Key)
                {
                    case ConsoleKey.F5:
                        break;

                    case ConsoleKey.Enter:
                        break;
                }
            }
        }
    }
}
