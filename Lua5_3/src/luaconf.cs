
using System;

class luaconf {
	public const long LUA_MAXINTEGER = long.MaxValue;
	public const long LUA_MININTEGER = long.MinValue;

	/*
	@@ LUA_EXTRASPACE defines the size of a raw memory area associated with
	** a Lua state with very fast access.
	** CHANGE it if you need a different size.
	*/
	public const int LUA_EXTRASPACE = 4;

	/*
	@@ LUA_IDSIZE gives the maximum size for the description of the source
	@@ of a function in debug information.
	** CHANGE it if you want a different size.
	*/
	public const int LUA_IDSIZE = 60;

	/* The following definitions are good for most cases here */

	public static double l_floor(double x) {
		return Math.Floor(x);
	}
	
	/*
	@@ lua_numbertointeger converts a float number to an integer, or
	** returns 0 if float is not within the range of a lua_Integer.
	** (The range comparisons are tricky because of rounding. The tests
	** here assume a two-complement representation, where MININTEGER always
	** has an exact representation as a float; MAXINTEGER may not have one,
	** and therefore its conversion to float may have an ill-defined value.)
	*/
	public static bool lua_numbertointeger(double n, ref long p) {
		if (n >= LUA_MININTEGER && n < -(double)LUA_MININTEGER) {
			p = (long)n;
			return true;
		}
		return false;
	}

	/*
	@@ lua_getlocaledecpoint gets the locale "radix character" (decimal point).
	** Change that if you do not want to use C locales. (Code using this
	** macro must include header 'locale.h'.)
	*/
	public static char lua_getlocaledecpoint() {
		return '.';
	}
}
