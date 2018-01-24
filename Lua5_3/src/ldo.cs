using System;

/* type of protected functions, to be ran by 'runprotected' */
delegate void Pfunc(lua_State L, object ud);

class ldo {

	static int luaD_rawrunprotected(lua_State L, Pfunc f, object ud) {
		ushort oldnCcalls = L.nCcalls;
		lua_longjmp lj = new lua_longjmp();
		lj.status = lua.LUA_OK;
		lj.previous = L.errorJmp;  /* chain new error handler */
		L.errorJmp = lj;
		try {
			f(L, ud);
		} catch (Exception e) {
			Console.WriteLine("luaD_rawrunprotected Exception: {0}", e.Message);
		}
		L.errorJmp = lj.previous;  /* restore old error handler */
		L.nCcalls = oldnCcalls;
		return lj.status;
}

	/* ISO C handling with long jumps */
	//#define LUAI_THROW(L,c)		longjmp((c)->b, 1)
	//#define LUAI_TRY(L,c,a)		if (setjmp((c)->b) == 0) { a }
	//static void LUAI_TRY() {
	//	try {

	//	} catch {

	//	}
	//}
}

/* chain list of long jump buffers */
class lua_longjmp {
	public lua_longjmp previous;
	int[] b = new int[16];
	public volatile int status;  /* error code */
}
