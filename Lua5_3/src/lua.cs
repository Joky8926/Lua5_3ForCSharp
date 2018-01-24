
delegate int lua_CFunction(lua_State L);
delegate object lua_Alloc(object ud, object ptr, uint osize, uint nsize);
delegate int lua_KFunction(lua_State L, int status, int ctx);
delegate void lua_Hook(lua_State L, lua_Debug ar);

class lua {
	/*
	** basic types
	*/
	public const int LUA_TNONE			= -1;
	public const int LUA_TNIL			= 0;
	public const int LUA_TBOOLEAN		= 1;
	public const int LUA_TLIGHTUSERDATA	= 2;
	public const int LUA_TNUMBER		= 3;
	public const int LUA_TSTRING		= 4;
	public const int LUA_TTABLE			= 5;
	public const int LUA_TFUNCTION		= 6;
	public const int LUA_TUSERDATA		= 7;
	public const int LUA_TTHREAD		= 8;
	public const int LUA_NUMTAGS		= 9;

	/* thread status */
	public const int LUA_OK			= 0;
	public const int LUA_YIELD		= 1;
	public const int LUA_ERRRUN		= 2;
	public const int LUA_ERRSYNTAX	= 3;
	public const int LUA_ERRMEM		= 4;
	public const int LUA_ERRGCMM	= 5;
	public const int LUA_ERRERR		= 6;
}

class lua_Debug {
	int event_;
	byte name;											/* (n) */
	byte namewhat;										/* (n) 'global', 'local', 'field', 'method' */
	byte what;											/* (S) 'Lua', 'C', 'main', 'tail' */
	byte source;										/* (S) */
	int currentline;									/* (l) */
	int linedefined;									/* (S) */
	int lastlinedefined;								/* (S) */
	byte nups;											/* (u) number of upvalues */
	byte nparams;										/* (u) number of parameters */
	byte isvararg;										/* (u) */
	byte istailcall;									/* (t) */
	byte[] short_src = new byte[luaconf.LUA_IDSIZE];	/* (S) */
	/* private part */
	CallInfo i_ci;										/* active function */
};
