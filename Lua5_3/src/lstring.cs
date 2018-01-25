using System.Text;

class lstring {
	/*
	** Lua will use at most ~(2^LUAI_HASHLIMIT) bytes from a string to
	** compute its hash
	*/
	const int LUAI_HASHLIMIT = 5;

	public static uint luaS_hash(byte[] str, uint l, uint seed) {
		uint h = seed ^ l;
		uint step = (l >> LUAI_HASHLIMIT) + 1;
		for (; l >= step; l -= step)
			h ^= (h << 5) + (h >> 2) + str[l - 1];
		return h;
	}

	public static uint luaS_hash(StringBuilder str, uint l, uint seed) {
		uint h = seed ^ l;
		uint step = (l >> LUAI_HASHLIMIT) + 1;
		for (; l >= step; l -= step)
			h ^= (h << 5) + (h >> 2) + str[(int)l - 1];
		return h;
	}

	/*
	** new string (with explicit length)
	*/
	static TString luaS_newlstr(lua_State L, StringBuilder str, uint l) {
		if (l <= llimits.LUAI_MAXSHORTLEN)  /* short string? */
			return internshrstr(L, str, l);
		// else {
		//   TString* ts;
		//   if (l >= (MAX_SIZE - sizeof(TString))/sizeof(char))

		//  luaM_toobig(L);
		//ts = luaS_createlngstrobj(L, l);

		//memcpy(getstr(ts), str, l* sizeof(char));
		//   return ts;
		// }
	}

	/*
	** checks whether short string exists and reuses it or creates a new one
	*/
	static TString internshrstr(lua_State L, StringBuilder str, uint l) {
		TString ts;
		global_State g = L.l_G;
		uint h = luaS_hash(str, l, g.seed);
		TString list = g.strt.hash[lobject.lmod(h, g.strt.size)];
		llimits.lua_assert(str != null);  /* otherwise 'memcmp'/'memcpy' are undefined */
		for (ts = list; ts != null; ts = ts.u.hnext) {
			if (l == ts.shrlen && (memcmp(str, getstr(ts), l * sizeof(char)) == 0)) {
				/* found! */
				if (lgc.isdead(g, ts) != 0)  /* dead (but not collected yet)? */
					lgc.changewhite(ts);  /* resurrect it */
				return ts;
			}
		}
		if (g.strt.nuse >= g.strt.size && g.strt.size <= llimits.MAX_INT / 2) {
			//luaS_resize(L, g->strt.size * 2);
			//list = &g->strt.hash[lmod(h, g->strt.size)];  /* recompute with new size */
		}
		//  ts = createstrobj(L, l, LUA_TSHRSTR, h);
		//  memcpy(getstr(ts), str, l* sizeof(char));
		//  ts->shrlen = cast_byte(l);
		//ts->u.hnext = * list;
		//  * list = ts;
		//g->strt.nuse++;
		//  return ts;
	}

	/*
	** resizes the string table
	*/
	static void luaS_resize(lua_State L, int newsize) {
		int i;
		stringtable tb = L.l_G.strt;
		if (newsize > tb.size) {  /* grow table if needed */
			luaM_reallocvector(L, tb->hash, tb->size, newsize, TString *);
			//for (i = tb->size; i < newsize; i++)
			//	tb->hash[i] = NULL;
		}
		//for (i = 0; i < tb->size; i++) {  /* rehash */
		//	TString* p = tb->hash[i];
		//	tb->hash[i] = NULL;
		//	while (p) {  /* for each node in the list */
		//		TString* hnext = p->u.hnext;  /* save next */
		//		unsigned int h = lmod(p->hash, newsize);  /* new position */
		//		p->u.hnext = tb->hash[h];  /* chain it */
		//		tb->hash[h] = p;
		//		p = hnext;
		//	}
		//}
		//if (newsize < tb->size) {  /* shrink table if needed */
		//						   /* vanishing slice should be empty */
		//	lua_assert(tb->hash[newsize] == NULL && tb->hash[tb->size - 1] == NULL);
		//	luaM_reallocvector(L, tb->hash, tb->size, newsize, TString *);
		//}
		//tb->size = newsize;
	}
}
