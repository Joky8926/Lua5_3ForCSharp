
class lfunc {

	static void luaF_freeproto(lua_State L, Proto f) {
		//luaM_freearray(L, f->code, f->sizecode);
		//luaM_freearray(L, f->p, f->sizep);
		//luaM_freearray(L, f->k, f->sizek);
		//luaM_freearray(L, f->lineinfo, f->sizelineinfo);
		//luaM_freearray(L, f->locvars, f->sizelocvars);
		//luaM_freearray(L, f->upvalues, f->sizeupvalues);
		//luaM_free(L, f);
	}
}

/*
** Upvalues for Lua closures
*/
class UpVal {
	TValue v;		/* points to stack or to its own value */
	uint refcount;  /* reference counter */
	_union u;

	class _union {
		_struct open;
		TValue value;  /* the value (when closed) */

		class _struct {  /* (when open) */
			UpVal next;		/* linked list */
			int touched;	/* mark to avoid cycles with dead threads */
		}
	}
}
