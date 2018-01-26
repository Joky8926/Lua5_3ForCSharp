using System.Text;

class lobject {
	/*
	** Extra tags for non-values
	*/
	public const int LUA_TPROTO = lua.LUA_NUMTAGS;      /* function prototypes */
	public const int LUA_TDEADKEY = lua.LUA_NUMTAGS + 1;  /* removed keys in tables */

	/* Variant tags for numbers */
	const int LUA_TNUMFLT = lua.LUA_TNUMBER | (0 << 4); /* float numbers */
	const int LUA_TNUMINT = lua.LUA_TNUMBER | (1 << 4);  /* integer numbers */

	/* Bit mark for collectable types */
	const int BIT_ISCOLLECTABLE = 1 << 6;

	/*
	** (address of) a fixed nil value
	*/
	public static readonly TValue luaO_nilobject = luaO_nilobject_;

	static readonly TValue luaO_nilobject_ = NILCONSTANT();

	const ulong MAXBY10 = luaconf.LUA_MAXINTEGER / 10;
	const int MAXLASTD = (int)(luaconf.LUA_MAXINTEGER % 10);

	static byte[] log_2 = {  /* log_2[i] = ceil(log2(i - 1)) */
		0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
		6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
		7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
		7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
		8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
	};

	/* mark a tag as collectable */
	static int ctb(int t) {
		return t | BIT_ISCOLLECTABLE;
	}

	/* macro defining a nil value */
	public static TValue NILCONSTANT() {
		return new TValue();
	}

	static Value val_(TValue o) {
		return o.value_;
	}

	/* raw type tag of a TValue */
	static int rttype(TValue o) {
		return o.tt_;
	}

	/* tag with no variants (bits 0-3) */
	static int novariant(int x) {
		return x & 0x0F;
	}

	/* type tag of a TValue (bits 0-3 for tags + variant bits 4-5) */
	static int ttype(TValue o) {
		return rttype(o) & 0x3F;
	}

	/* type tag of a TValue with no variants (bits 0-3) */
	static int ttnov(TValue o) {
		return novariant(rttype(o));
	}

	/* Macros to test type */
	static bool checktag(TValue o, int t) {
		return rttype(o) == t;
	}

	static bool checktype(TValue o, int t) {
		return ttnov(o) == t;
	}

	public static bool ttisfloat(TValue o) {
		return checktag(o, LUA_TNUMFLT);
	}

	public static bool ttisinteger(TValue o) {
		return checktag(o, LUA_TNUMINT);
	}

	public static bool ttisnil(TValue o) {
		return checktag(o, lua.LUA_TNIL);
	}

	public static bool ttisstring(TValue o) {
		return checktype((o), lua.LUA_TSTRING);
	}

	/* Macros to access values */
	public static long ivalue(TValue o) {
		return val_(o).i;
	}

	public static double fltvalue(TValue o) {
		return val_(o).n;
	}

	static GCObject gcvalue(TValue o) {
		//iscollectable(o);
		return val_(o).gc;
	}

	static TString tsvalue(TValue o) {
		return val_(o)._str;
	}

	static int iscollectable(TValue o) {
		return rttype(o) & BIT_ISCOLLECTABLE;
	}

	/* Macros for internal tests */
	static bool righttt(TValue obj) {
		return (ttype(obj) == gcvalue(obj).tt);
	}

	static void checkliveness(lua_State L, TValue obj) {
		llimits.lua_longassert(0 == iscollectable(obj) || (righttt(obj) && (L == null || 0 == lgc.isdead(L.l_G, gcvalue(obj)))));
	}

	/* Macros to set values */
	static void settt_(TValue o, int t) {
		o.tt_ = t;
	}

	public static void setivalue(TValue obj, long x) {
		val_(obj).i = (x);
		settt_(obj, LUA_TNUMINT);
	}

	public static void setnilvalue(TValue obj) {
		settt_(obj, lua.LUA_TNIL);
	}

	static void setsvalue(lua_State L, TValue obj, TString x) {
		TValue io = (obj);
		TString x_ = (x);
		val_(io).gc = lstate.obj2gco(x_);
		settt_(io, ctb(x_.tt));
		checkliveness(L, io);
	}

	public static void sethvalue(lua_State L, TValue obj, Table x) {
		TValue io = obj;
		Table x_ = x;
		val_(io).gc = x_;
		settt_(io, ctb(lua.LUA_TTABLE));
		checkliveness(L, io);
	}

	static void setsvalue2s(lua_State L, TValue obj, TString x) {
		setsvalue(L, obj, x);
	}

	/*
	** Get the actual string (array of bytes) from a 'TString'.
	** (Access to 'extra' ensures that value is really a 'TString'.)
	*/
	static string getstr(TString ts) {
		// check_exp(sizeof((ts)->extra), cast(char *, (ts)) + sizeof(UTString))
		return null;
	}

	/* get the actual string (array of bytes) from a Lua value */
	public static void svalue(TValue o) {
		getstr(tsvalue(o));
	}

	/*
	** 'module' operation for hashing (size is always a power of 2)
	*/
	public static int lmod(int s, int size) {
		return s & (size - 1);
	}

	public static int twoto(int x) {
		return 1 << x;
	}

	public static int sizenode(Table t) {
		return twoto(t.lsizenode);
	}

	/*
	** Computes ceil(log2(x))
	*/
	public static int luaO_ceillog2(int x) {
		int l = 0;
		x--;
		while (x >= 256) {
			l += 8;
			x >>= 8;
		}
		return l + log_2[x];
	}

	static int luaO_hexavalue(byte c) {
		if (lctype.lisdigit(c))
			return c - '0';
		else
			return lctype.ltolower(c) - 'a' + 10;
	}

	static int isneg(char s, ref int strIndex) {
		if (s == '-') {
			strIndex++;
			return 1;
		} else if (s == '+')
			strIndex++;
		return 0;
	}

	/*
	** convert an hexadecimal numeric string to a number, following
	** C99 specification for 'strtod'
	*/
	static double lua_strx2number(string s, char endptr) {
		int dot = luaconf.lua_getlocaledecpoint();
		double r = 0.0;  /* result (accumulator) */
		int sigdig = 0;  /* number of significant digits */
		int nosigdig = 0;  /* number of non-significant digits */
		int e = 0;  /* exponent correction */
		int neg;  /* 1 if number is negative */
		bool hasdot = false;  /* true after seen a dot */
		int strIndex = 0;
		endptr = s[strIndex];  /* nothing is valid yet */
		while (strIndex < s.Length && lctype.lisspace((byte)s[strIndex]))
			strIndex++;  /* skip initial spaces */
		neg = isneg(s[strIndex], ref strIndex);  /* check signal */
		if (!(s[strIndex] == '0' && (s[strIndex + 1] == 'x' || s[strIndex + 1] == 'X')))  /* check '0x' */
			return 0.0;  /* invalid format (no '0x') */
		for (strIndex += 2; strIndex < s.Length; strIndex++) {  /* skip '0x' and read numeral */
			if (s[strIndex] == dot) {
				if (hasdot)
					break;  /* second dot? stop loop */
				else
					hasdot = true;
			} else if (lctype.lisxdigit((byte)s[strIndex])) {
				if (sigdig == 0 && s[strIndex] == '0')  /* non-significant digit (zero)? */
					nosigdig++;
      else if (++sigdig <= MAXSIGDIG)  /* can read it without overflow? */
          r = (r* cast_num(16.0)) + luaO_hexavalue(*s);
      else e++; /* too many digits; ignore, but still count for exponent */
      if (hasdot) e--;  /* decimal digit? correct exponent */
    }
    else break;  /* neither a dot nor a digit */
  }
  if (nosigdig + sigdig == 0)  /* no digits? */
    return 0.0;  /* invalid format */
  * endptr = cast(char *, s);  /* valid up to here */
e *= 4;  /* each digit multiplies/divides value by 2^4 */
  if (* s == 'p' || * s == 'P') {  /* exponent part? */
    int exp1 = 0;  /* exponent value */
int neg1;  /* exponent signal */
s++;  /* skip 'p' */
    neg1 = isneg(&s);  /* signal */
    if (!lisdigit(cast_uchar(*s)))
      return 0.0;  /* invalid; must have at least one digit */
    while (lisdigit(cast_uchar(*s)))  /* read exponent */
      exp1 = exp1* 10 + *(s++) - '0';
    if (neg1) exp1 = -exp1;
    e += exp1;

	* endptr = cast(char *, s);  /* valid up to here */
  }
  if (neg) r = -r;
  return l_mathop(ldexp)(r, e);
}

	static char l_str2dloc (string s, ref double result, int mode) {
		char endptr;
		result = (mode == 'x') ? lua_strx2number(s, &endptr)  /* try to convert */
                          : lua_str2number(s, &endptr);
  if (endptr == s) return NULL;  /* nothing recognized? */
  while (lisspace(cast_uchar(*endptr))) endptr++;  /* skip trailing spaces */
  return (*endptr == '\0') ? endptr : NULL;  /* OK if no trailing characters */
}

/*
** Convert string 's' to a Lua number (put in 'result'). Return NULL
** on fail or the address of the ending '\0' on success.
** 'pmode' points to (and 'mode' contains) special things in the string:
** - 'x'/'X' means an hexadecimal numeral
** - 'n'/'N' means 'inf' or 'nan' (which should be rejected)
** - '.' just optimizes the search for the common case (nothing special)
** This function accepts both the current locale or a dot as the radix
** mark. If the convertion fails, it may mean number has a dot but
** locale accepts something else. In that case, the code copies 's'
** to a buffer (because 's' is read-only), changes the dot to the
** current locale radix mark, and tries to convert again.
*/
static bool l_str2d (string s, ref double result) {
		char endptr;
		char pmode = jsystem.strpbrk(s, ".xXnN");
		int mode = pmode != '\0' ? lctype.ltolower((byte)pmode) : 0;
		if (mode == 'n')  /* reject 'inf' and 'nan' */
			return false;
		endptr = l_str2dloc(s, result, mode);  /* try to convert */
  if (endptr == NULL) {  /* failed? may be a different locale */
    char buff[L_MAXLENNUM + 1];
	const char* pdot = strchr(s, '.');
    if (strlen(s) > L_MAXLENNUM || pdot == NULL)
      return NULL;  /* string too long or no dot; fail */

	strcpy(buff, s);  /* copy string to buffer */
	buff[pdot - s] = lua_getlocaledecpoint();  /* correct decimal point */
	endptr = l_str2dloc(buff, result, mode);  /* try again */
    if (endptr != NULL)
      endptr = s + (endptr - buff);  /* make relative to 's' */
  }
  return endptr;
}

	static bool l_str2int (string s, ref long result) {
		ulong a = 0;
		bool empty = true;
		int neg;
		int strIndex = 0;
		while (strIndex < s.Length && lctype.lisspace((byte)s[strIndex]))
			strIndex++;  /* skip initial spaces */
		neg = isneg(s[strIndex], ref strIndex);
		if (s[strIndex] == '0' && (s[strIndex + 1] == 'x' || s[strIndex + 1] == 'X')) {  /* hex? */
			strIndex += 2;  /* skip '0x' */
			for (; strIndex < s.Length && lctype.lisxdigit((byte)s[strIndex]); strIndex++) {
				a = a * 16 + (ulong)luaO_hexavalue((byte)s[strIndex]);
				empty = false;
			}
		} else {  /* decimal */
			for (; strIndex < s.Length && lctype.lisdigit((byte)s[strIndex]); strIndex++) {
				int d = s[strIndex] - '0';
				if (a >= MAXBY10 && (a > MAXBY10 || d > MAXLASTD + neg))  /* overflow? */
					return false;  /* do not accept it (as integer) */
				a = a * 10 + (ulong)d;
				empty = false;
			}
		}
		while (strIndex < s.Length && lctype.lisspace((byte)s[strIndex]))
			strIndex++;  /* skip trailing spaces */
		if (empty || strIndex < s.Length)
			return false;  /* something wrong in the numeral */
		else {
			result = (neg != 0) ? -(long)a : (long)a;
		return true;
	}
}

	uint luaO_str2num(string s, TValue o) {
		long i = 0;
		double n;
		bool e;
		if (e = l_str2int(s, ref i)) {  /* try as an integer */
			setivalue(o, i);
		} else if ((e = l_str2d(s, &n)) != NULL) {  /* else try as a float */

	setfltvalue(o, n);
  }
  else
    return 0;  /* conversion failed */
  return (e - s) + 1;  /* success; return string size */
}

	static void pushstr(lua_State L, StringBuilder str, uint l) {
		setsvalue2s(L, L.top, luaS_newlstr(L, str, l));
		//luaD_inctop(L);
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



}

/*
** Union of all Lua values
*/
class Value {   // union
	public GCObject gc;		/* collectable objects */
	object p;			/* light userdata */
	int b;				/* booleans */
	lua_CFunction f;	/* light C functions */
	public long i;			/* integer numbers */
	public double n;            /* float numbers */
	public TString _str;
}

class TValue {
	public Value value_;
	public int tt_;

	public TValue() {
		value_ = null;
		tt_ = lua.LUA_TNIL;
	}
}

/*
** Header for string value; string bytes follow the end of this structure
** (aligned according to 'UTString'; see next).
*/
class TString : GCObject {
	byte extra;		/* reserved words for short strings; "has hash" for longs */
	public byte shrlen;	/* length for short strings */
	uint hash;
	public _union u;

	public TString(lua_State L) : base(L, 0) {

	}
	
	public class _union {
		uint lnglen;	/* length for long strings */
		public TString hnext;  /* linked list for hash table */
	}
}

/*
** Header for userdata; memory area follows the end of this structure
** (aligned according to 'UUdata'; see next).
*/
class Udata {
	GCObject next;
	byte tt;
	byte marked;
	byte ttuv_;     /* user value's tag */
	Table metatable;
	uint len;       /* number of bytes */
	Value user_;    /* user value */
}

/*
** Description of an upvalue for function prototypes
*/
class Upvaldesc {
	TString name;   /* upvalue name (for debug information) */
	byte instack;   /* whether it is in stack (register) */
	byte idx;       /* index of upvalue (in stack or in outer function's list) */
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
** Function Prototypes
*/
class Proto {
	GCObject next;
	byte tt;
	byte marked;
	byte numparams;         /* number of fixed parameters */
	byte is_vararg;
	byte maxstacksize;      /* number of registers needed by this function */
	int sizeupvalues;       /* size of 'upvalues' */
	int sizek;              /* size of 'k' */
	int sizecode;
	int sizelineinfo;
	int sizep;              /* size of 'p' */
	int sizelocvars;
	int linedefined;        /* debug information  */
	int lastlinedefined;    /* debug information  */
	TValue k;               /* constants used by the function */
	uint code;              /* opcodes */
	Proto[] p;              /* functions defined inside the function */
	int lineinfo;           /* map from opcodes to source lines (debug information) */
	LocVar locvars;         /* information about local variables (debug information) */
	Upvaldesc upvalues;     /* upvalue information */
	LClosure cache;         /* last-created closure with this prototype */
	TString source;         /* used for debug information */
	GCObject gclist;
}

/*
** Closures
*/

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

class Closure {
	CClosure c;
	LClosure l;
}

/*
** Tables
*/

class TKey {
	public _struct nk;

	public TKey() {
		nk = new _struct();
	}

	public TValue tvk {
		get {
			return nk;
		}
	}

	public class _struct : TValue {
		public int next;  /* for chaining (offset for next node) */

		public _struct() : base() {
			next = 0;
		}
	}
}

class Node {
	public TValue i_val;
	public TKey i_key;
	private Node[] _arrNodes;	// 容器对象。
	private int _index;

	public Node(Node[] arrNodes, int index) {
		_arrNodes = arrNodes;
		_index = index;
	}

	public Node(TValue val, TKey key) {
		i_val = val;
		i_key = key;
	}

	public static Node operator +(Node b, int c) {
		return b._arrNodes[b._index + c];
	}
}
