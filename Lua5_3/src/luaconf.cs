
class luaconf {
	/*
	@@ LUA_IDSIZE gives the maximum size for the description of the source
	@@ of a function in debug information.
	** CHANGE it if you want a different size.
	*/
	public const int LUA_IDSIZE = 60;
	/*
	@@ LUA_EXTRASPACE defines the size of a raw memory area associated with
	** a Lua state with very fast access.
	** CHANGE it if you need a different size.
	*/
	public const int LUA_EXTRASPACE = 4;
}
