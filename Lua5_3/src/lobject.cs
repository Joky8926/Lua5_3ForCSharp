
class lobject {

	/* Macros to set values */
	static void settt_(TValue o, int t) {
		o.tt_ = t;
	}
	
	public static void setnilvalue(TValue obj) {
		settt_(obj, lua.LUA_TNIL);
	}


	/*
	** this function handles only '%d', '%c', '%f', '%p', and '%s'
	   conventional formats, plus Lua-specific '%I' and '%U'
	*/
//	string luaO_pushvfstring (lua_State L, string, va_list argp) {
//  int n = 0;
//  for (;;) {
//    const char* e = strchr(fmt, '%');
//    if (e == NULL) break;

//	pushstr(L, fmt, e - fmt);
//    switch (*(e+1)) {
//      case 's': {  /* zero-terminated string */
//        const char* s = va_arg(argp, char *);
//        if (s == NULL) s = "(null)";

//		pushstr(L, s, strlen(s));
//        break;
//      }
//      case 'c': {  /* an 'int' as a character */
//        char buff = cast(char, va_arg(argp, int));
//        if (lisprint(cast_uchar(buff)))
//          pushstr(L, &buff, 1);
//        else  /* non-printable character; print its code */

//		  luaO_pushfstring(L, "<\\%d>", cast_uchar(buff));
//        break;
//      }
//      case 'd': {  /* an 'int' */
//        setivalue(L->top, va_arg(argp, int));
//        goto top2str;
//      }
//      case 'I': {  /* a 'lua_Integer' */

//		setivalue(L->top, cast(lua_Integer, va_arg(argp, l_uacInt)));
//        goto top2str;
//      }
//      case 'f': {  /* a 'lua_Number' */

//		setfltvalue(L->top, cast_num(va_arg(argp, l_uacNumber)));
//      top2str:  /* convert the top element to a string */

//		luaD_inctop(L);

//		luaO_tostring(L, L->top - 1);
//        break;
//      }
//      case 'p': {  /* a pointer */
//        char buff[4 * sizeof(void*) + 8]; /* should be enough space for a '%p' */
//int l = l_sprintf(buff, sizeof(buff), "%p", va_arg(argp, void *));

//		pushstr(L, buff, l);
//        break;
//      }
//      case 'U': {  /* an 'int' as a UTF-8 sequence */
//        char buff[UTF8BUFFSZ];
//int l = luaO_utf8esc(buff, cast(long, va_arg(argp, long)));

//		pushstr(L, buff + UTF8BUFFSZ - l, l);
//        break;
//      }
//      case '%': {

//		pushstr(L, "%", 1);
//        break;
//      }
//      default: {

//		luaG_runerror(L, "invalid option '%%%c' to 'lua_pushfstring'",

//						 *(e + 1));
//      }
//    }
//    n += 2;
//    fmt = e+2;
//  }
//  luaD_checkstack(L, 1);
//  pushstr(L, fmt, strlen(fmt));
//  if (n > 0) luaV_concat(L, n + 1);
//  return svalue(L->top - 1);
//}

}

class GCObject {
	GCObject next;
	byte tt;
	byte marked;
}

/*
** Union of all Lua values
*/
class Value {
	GCObject gc;		/* collectable objects */
	object p;			/* light userdata */
	int b;				/* booleans */
	lua_CFunction f;	/* light C functions */
	ulong i;			/* integer numbers */
	double n;			/* float numbers */
}

class TValue {
	Value value_;
	public int tt_;
}

/*
** Header for string value; string bytes follow the end of this structure
** (aligned according to 'UTString'; see next).
*/
class TString {
	GCObject next;
	byte tt;
	byte marked;
	byte extra;		/* reserved words for short strings; "has hash" for longs */
	byte shrlen;	/* length for short strings */
	uint hash;
	_union u;

	class _union {
		uint lnglen;	/* length for long strings */
		TString hnext;  /* linked list for hash table */
	}
}

class stringtable {
	public TString[] hash;
	public int nuse;  /* number of elements */
	public int size;
}

class TKey {
	TValue tvk;
	_struct nk;

	class _struct {
		Value value_;
		int tt_;
		int next;  /* for chaining (offset for next node) */
	}
}

class Node {
	TValue i_val;
	TKey i_key;
}

class Table {
	GCObject next;
	byte tt;
	byte marked;
	byte flags;		/* 1<<p means tagmethod(p) is not present */
	byte lsizenode;	/* log2 of size of 'node' array */
	uint sizearray; /* size of 'array' array */
	TValue array;	/* array part */
	Node node;
	Node lastfree;  /* any free position is before this position */
	Table metatable;
	GCObject gclist;
}
