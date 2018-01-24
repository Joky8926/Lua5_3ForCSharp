
class ldebug {

	public static void resethookcount(lua_State L) {
		L.hookcount = L.basehookcount;
	}
}
