using System.Text;

class lobject {
	/* Bit mark for collectable types */
	const int BIT_ISCOLLECTABLE = 1 << 6;

	/*
	** Extra tags for non-values
	*/
	public const int LUA_TPROTO		= lua.LUA_NUMTAGS;      /* function prototypes */
	public const int LUA_TDEADKEY	= lua.LUA_NUMTAGS + 1;	/* removed keys in tables */

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
	string luaO_pushvfstring(lua_State L, StringBuilder fmt, params object[] argp) {
		int n = 0;
		for (;;) {
			int e = jsystem.strchr(fmt, '%');
			if (e == -1)
				break;
			pushstr(L, fmt, e - fmt);
			//	switch (*(e + 1)) {
			//		case 's': {  /* zero-terminated string */
			//				const char* s = va_arg(argp, char *);
			//				if (s == NULL)
			//					s = "(null)";

			//				pushstr(L, s, strlen(s));
			//				break;
			//			}
			//		case 'c': {  /* an 'int' as a character */
			//				char buff = cast(char, va_arg(argp, int));
			//				if (lisprint(cast_uchar(buff)))
			//					pushstr(L, &buff, 1);
			//				else  /* non-printable character; print its code */

			//					luaO_pushfstring(L, "<\\%d>", cast_uchar(buff));
			//				break;
			//			}
			//		case 'd': {  /* an 'int' */
			//				setivalue(L->top, va_arg(argp, int));
			//				goto top2str;
			//			}
			//		case 'I': {  /* a 'lua_Integer' */

			//				setivalue(L->top, cast(lua_Integer, va_arg(argp, l_uacInt)));
			//				goto top2str;
			//			}
			//		case 'f': {  /* a 'lua_Number' */

			//				setfltvalue(L->top, cast_num(va_arg(argp, l_uacNumber)));
			//				top2str:  /* convert the top element to a string */

			//				luaD_inctop(L);

			//				luaO_tostring(L, L->top - 1);
			//				break;
			//			}
			//		case 'p': {  /* a pointer */
			//				char buff[4 * sizeof(void*) + 8]; /* should be enough space for a '%p' */
			//				int l = l_sprintf(buff, sizeof(buff), "%p", va_arg(argp, void *));

			//				pushstr(L, buff, l);
			//				break;
			//			}
			//		case 'U': {  /* an 'int' as a UTF-8 sequence */
			//				char buff[UTF8BUFFSZ];
			//				int l = luaO_utf8esc(buff, cast(long, va_arg(argp, long)));

			//				pushstr(L, buff + UTF8BUFFSZ - l, l);
			//				break;
			//			}
			//		case '%': {

			//				pushstr(L, "%", 1);
			//				break;
			//			}
			//		default: {

			//				luaG_runerror(L, "invalid option '%%%c' to 'lua_pushfstring'",

			//								 *(e + 1));
			//			}
			//	}
			//	n += 2;
			//	fmt = e + 2;
		}
		//luaD_checkstack(L, 1);
		//pushstr(L, fmt, strlen(fmt));
		//if (n > 0)
		//	luaV_concat(L, n + 1);
		//return svalue(L->top - 1);
	}


	static void pushstr(lua_State L, StringBuilder str, uint l) {
	  setsvalue2s(L, L.top, luaS_newlstr(L, str, l));
      //luaD_inctop(L);
	}

	static void setsvalue2s(lua_State L, TValue obj, TString x) {
		setsvalue(L, obj, x);
	}

	static void setsvalue(lua_State L, TValue obj, TString x) {
		TValue io = (obj);
		TString x_ = (x);
		val_(io).gc = lstate.obj2gco(x_);
		settt_(io, ctb(x_.tt));
		checkliveness(L, io);
	}

	static Value val_(TValue o) {
		return o.value_;
	}

	/* mark a tag as collectable */
	static int ctb(int t) {
		return t | BIT_ISCOLLECTABLE;
	}

	static void checkliveness(lua_State L, TValue obj) {
		llimits.lua_longassert(0 == iscollectable(obj) || (righttt(obj) && (L == null || 0 == lgc.isdead(L.l_G, gcvalue(obj)))));
	}

	static int iscollectable(TValue o) {
		return rttype(o) & BIT_ISCOLLECTABLE;
	}

	/* raw type tag of a TValue */
	static int rttype(TValue o) {
		return o.tt_;
	}


	/* Macros for internal tests */
	static bool righttt(TValue obj) {
		return (ttype(obj) == gcvalue(obj).tt);
	}

	/* type tag of a TValue (bits 0-3 for tags + variant bits 4-5) */
	static int ttype(TValue o) {
		return rttype(o) & 0x3F;
	}

	static GCObject gcvalue(TValue o) {
		//iscollectable(o);
		return val_(o).gc;
	}

	/*
	** 'module' operation for hashing (size is always a power of 2)
	*/
	public static int lmod(uint s, int size) {
		// (size & (size - 1)) == 0
		return  (int)s & (size - 1);
	}


	/*
	** Get the actual string (array of bytes) from a 'TString'.
	** (Access to 'extra' ensures that value is really a 'TString'.)
	*/
	//static void getstr(TString ts) {
	//	// sizeof((ts)->extra)
	//	return check_exp(, cast(char *, (ts)) + sizeof(UTString));
	//}

}

/*
** Union of all Lua values
*/
class Value {
	public GCObject gc;		/* collectable objects */
	object p;			/* light userdata */
	int b;				/* booleans */
	lua_CFunction f;	/* light C functions */
	ulong i;			/* integer numbers */
	double n;			/* float numbers */
}

class TValue {
	public Value value_;
	public int tt_;
}

/*
** Header for string value; string bytes follow the end of this structure
** (aligned according to 'UTString'; see next).
*/
class TString : IGCObject {
	GCObject next;
	public byte tt;
	byte _marked;
	byte extra;		/* reserved words for short strings; "has hash" for longs */
	public byte shrlen;	/* length for short strings */
	uint hash;
	public _union u;

	public byte marked {
		get {
			return _marked;
		}
		set {
			_marked = value;
		}
	}

	public GCObject gc {
		get {
			return next;
		}
	}

	public class _union {
		uint lnglen;	/* length for long strings */
		public TString hnext;  /* linked list for hash table */
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

/*
** Header for userdata; memory area follows the end of this structure
** (aligned according to 'UUdata'; see next).
*/
class Udata {
	GCObject next;
	byte tt;
	byte marked;
	byte ttuv_;		/* user value's tag */
	Table metatable;
	uint len;		/* number of bytes */
	Value user_;	/* user value */
}

class Closure {
	CClosure c;
	LClosure l;
}

class CClosure {
	GCObject next;
	byte tt;
	byte marked;
	byte nupvalues;
	GCObject gclist;
	lua_CFunction f;
	TValue[] upvalue = new TValue[1];  /* list of upvalues */
}

class LClosure {
	GCObject next;
	byte tt;
	byte marked;
	byte nupvalues;
	GCObject gclist;
	Proto p;
	UpVal[] upvals = new UpVal[1];  /* list of upvalues */
}

/*
** Function Prototypes
*/
class Proto {
	GCObject next;
	byte tt;
	byte marked;
	byte numparams;			/* number of fixed parameters */
	byte is_vararg;
	byte maxstacksize;		/* number of registers needed by this function */
	int sizeupvalues;		/* size of 'upvalues' */
	int sizek;				/* size of 'k' */
	int sizecode;
	int sizelineinfo;
	int sizep;				/* size of 'p' */
	int sizelocvars;
	int linedefined;		/* debug information  */
	int lastlinedefined;	/* debug information  */
	TValue k;				/* constants used by the function */
	uint code;				/* opcodes */
	Proto[] p;				/* functions defined inside the function */
	int lineinfo;			/* map from opcodes to source lines (debug information) */
	LocVar locvars;			/* information about local variables (debug information) */
	Upvaldesc upvalues;		/* upvalue information */
	LClosure cache;			/* last-created closure with this prototype */
	TString source;			/* used for debug information */
	GCObject gclist;
}

/*
** Description of a local variable for function prototypes
** (used for debug information)
*/
class LocVar {
	TString varname;
	int startpc;  /* first point where variable is active */
	int endpc;    /* first point where variable is dead */
}

/*
** Description of an upvalue for function prototypes
*/
class Upvaldesc {
	TString name;	/* upvalue name (for debug information) */
	byte instack;	/* whether it is in stack (register) */
	byte idx;		/* index of upvalue (in stack or in outer function's list) */
}
