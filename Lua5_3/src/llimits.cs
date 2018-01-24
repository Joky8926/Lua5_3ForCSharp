
class llimits {
	/*
	** Size of cache for strings in the API. 'N' is the number of
	** sets (better be a prime) and "M" is the size of each set (M == 1
	** makes a direct cache.)
	*/
	public const int STRCACHE_N = 53;
	public const int STRCACHE_M = 2;

	public static void lua_assert(bool c) {

	}

	/* macro to avoid warnings about unused variables */
	public static void UNUSED(object x) {

	}
}
