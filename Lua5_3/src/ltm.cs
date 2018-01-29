
class ltm {


	static TValue gfasttm(global_State g, Table et, TMS e) {
		return et == null ? null : (et.flags & (1u << (int)e)) != 0 ? null : luaT_gettm(et, e, g.tmname[(int)e]);
	}


	public static TValue fasttm(lua_State l, Table et, TMS e) {
		return gfasttm(l.l_G, et, e);
	}

	/*
	** function to be used with macro "fasttm": optimized for absence of
	** tag methods
	*/
	static TValue luaT_gettm (Table events, TMS event_, TString ename) {
		TValue tm = ltable.luaH_getshortstr(events, ename);
		llimits.lua_assert(event_ <= TMS.TM_EQ);
		if (lobject.ttisnil(tm)) {  /* no tag method? */
			events.flags |= (byte)(1u << (int)event_);  /* cache this fact */
			return null;
		} else
			return tm;
	}
}

/*
* WARNING: if you change the order of this enumeration,
* grep "ORDER TM" and "ORDER OP"
*/
enum TMS {
	TM_INDEX,
	TM_NEWINDEX,
	TM_GC,
	TM_MODE,
	TM_LEN,
	TM_EQ,	/* last tag method with fast access */
	TM_ADD,
	TM_SUB,
	TM_MUL,
	TM_MOD,
	TM_POW,
	TM_DIV,
	TM_IDIV,
	TM_BAND,
	TM_BOR,
	TM_BXOR,
	TM_SHL,
	TM_SHR,
	TM_UNM,
	TM_BNOT,
	TM_LT,
	TM_LE,
	TM_CONCAT,
	TM_CALL,
	TM_N	/* number of elements in the enum */
}