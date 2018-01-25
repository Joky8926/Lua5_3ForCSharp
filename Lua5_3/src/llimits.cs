
class llimits {
	/*
	** Size of cache for strings in the API. 'N' is the number of
	** sets (better be a prime) and "M" is the size of each set (M == 1
	** makes a direct cache.)
	*/
	public const int STRCACHE_N = 53;
	public const int STRCACHE_M = 2;

	/*
	** Maximum length for short strings, that is, strings that are
	** internalized. (Cannot be smaller than reserved words or tags for
	** metamethods, as these strings must be internalized;
	** #("function") = 8, #("__newindex") = 10.)
	*/
	public const int LUAI_MAXSHORTLEN = 40;

	public const int MAX_INT = int.MaxValue;  /* maximum value of an int */

	public static void lua_assert(bool c) {

	}

	/* macro to avoid warnings about unused variables */
	public static void UNUSED(object x) {

	}

	public static void lua_longassert(bool c) {

	}

	//public static void check_exp(object c, e) { (e)

	//}
}
