
class ldebug {

	public static void resethookcount(lua_State L) {
		L.hookcount = L.basehookcount;
	}

	static void luaG_runerror(lua_State L, string fmt, params object[] argp) {
		CallInfo ci = L.ci;
		string msg;
	//va_list argp;
 // va_start(argp, fmt);
	//msg = luaO_pushvfstring(L, fmt, argp);  /* format message */
 // va_end(argp);
 // if (isLua(ci))  /* if Lua function, add source:line information */
 //   luaG_addinfo(L, msg, ci_func(ci)->p->source, currentline(ci));
 // luaG_errormsg(L);
	}
}
