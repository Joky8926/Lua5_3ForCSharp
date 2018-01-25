
class Table : GCObject {
	byte flags;     /* 1<<p means tagmethod(p) is not present */
	byte lsizenode; /* log2 of size of 'node' array */
	uint sizearray; /* size of 'array' array */
	TValue array;   /* array part */
	Node node;
	Node lastfree;  /* any free position is before this position */
	Table metatable;
	GCObject gclist;

	public Table() : base() {
		Table* t = gco2t(o);
		t->metatable = NULL;
		t->flags = cast_byte(~0);
		t->array = NULL;
		t->sizearray = 0;
		setnodevector(L, t, 0);
	}
}

class ltable {
	Table luaH_new(lua_State L) {
		GCObject* o = luaC_newobj(L, LUA_TTABLE, sizeof(Table));
		Table* t = gco2t(o);
		t->metatable = NULL;
		t->flags = cast_byte(~0);
		t->array = NULL;
		t->sizearray = 0;
		setnodevector(L, t, 0);
		return t;
	}
}
