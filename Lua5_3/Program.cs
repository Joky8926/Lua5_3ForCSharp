using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lua5_3 {
	class Program {
		static void Main(string[] args) {
			Console.WriteLine("test:{0}", GetTimeStamp());
			Console.Read();
		}

		public static uint GetTimeStamp() {
			TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return (uint)ts.TotalSeconds;
		}
	}
}
