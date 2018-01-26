
class lmem {
	static void luaM_newvector(lua_State L, int n, t) {
		cast(t *, luaM_reallocv(L, NULL, 0, n, sizeof(t)))
	}

	/*
	** This macro reallocs a vector 'b' from 'on' to 'n' elements, where
	** each element has size 'e'. In case of arithmetic overflow of the
	** product 'n'*'e', it raises an error (calling 'luaM_toobig'). Because
	** 'e' is always constant, it avoids the runtime division MAX_SIZET/(e).
	**
	** (The macro is somewhat complex to avoid warnings:  The 'sizeof'
	** comparison avoids a runtime comparison when overflow cannot occur.
	** The compiler should be able to optimize the real test by itself, but
	** when it does it, it may give a warning about "comparison is always
	** false due to limited range of data type"; the +1 tricks the compiler,
	** avoiding this warning but also this optimization.)
	*/
	static void luaM_reallocv(lua_State L, b, int on, int n, int e) {
		//(((sizeof(n) >= sizeof(size_t) && cast(size_t, (n)) + 1 > MAX_SIZET / (e)) \
  //    ? luaM_toobig(L) : cast_void(0)) , \
		luaM_realloc_(L, (b), (on) * (e), (n) * (e));
	}

	/*
	** generic allocation routine.
	*/
	static void luaM_realloc_(lua_State L, object block, uint osize, uint nsize) {
		//		void* newblock;
		//		global_State* g = G(L);
		//		size_t realosize = (block) ? osize : 0;
		//		lua_assert((realosize == 0) == (block == NULL));
		//#if defined(HARDMEMTESTS)
		//  if (nsize > realosize && g->gcrunning)
		//    luaC_fullgc(L, 1);  /* force a GC whenever possible */
		//#endif
		//		newblock = (*g->frealloc)(g->ud, block, osize, nsize);
		//		if (newblock == NULL && nsize > 0) {
		//			lua_assert(nsize > realosize);  /* cannot fail when shrinking a block */
		//			if (g->version) {  /* is state fully built? */
		//				luaC_fullgc(L, 1);  /* try to free some memory... */
		//				newblock = (*g->frealloc)(g->ud, block, osize, nsize);  /* try again */
		//			}
		//			if (newblock == NULL)
		//				luaD_throw(L, LUA_ERRMEM);
		//		}
		//		lua_assert((nsize == 0) == (newblock == NULL));
		//		g->GCdebt = (g->GCdebt + nsize) - realosize;
		//		return newblock;
	}

	//l_noret luaM_toobig(lua_State* L) {
	//	luaG_runerror(L, "memory allocation error: block too big");
	//}

	//#define luaM_reallocvector(L, v,oldn,n,t) \
 //  ((v)=cast(t*, luaM_reallocv(L, v, oldn, n, sizeof(t))))
}
