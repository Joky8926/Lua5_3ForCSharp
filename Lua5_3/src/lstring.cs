
class lstring {
	/*
	** Lua will use at most ~(2^LUAI_HASHLIMIT) bytes from a string to
	** compute its hash
	*/
	const int LUAI_HASHLIMIT = 5;

	static uint luaS_hash(byte[] str, uint l, uint seed) {
		uint h = seed ^ l;
		uint step = (l >> LUAI_HASHLIMIT) + 1;
		for (; l >= step; l -= step)
			h ^= (h << 5) + (h >> 2) + str[l - 1];
		return h;
	}
}
