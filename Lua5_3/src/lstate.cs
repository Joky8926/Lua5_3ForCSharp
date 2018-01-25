
using System;

class lstate {
	/* kinds of Garbage Collection */
	public const int KGC_NORMAL = 0;
	public const int KGC_EMERGENCY = 1;    /* gc was forced by an allocation failure */

	const int LUAI_GCPAUSE = 200;  /* 200% */

	const int LUAI_GCMUL = 200; /* GC runs 'twice the speed' of memory allocation */

	const int BASIC_STACK_SIZE = (2 * lua.LUA_MINSTACK);

	/* extra stack space to handle TM calls and some other extras */
	const int EXTRA_STACK = 5;

	public static lua_State lua_newstate(lua_Alloc f, object ud) {
		int i;
		lua_State L;
		global_State g;
		LG l = (LG) f(ud, null, lua.LUA_TTHREAD, 0/*sizeof(LG)*/);
		if (l == null)
			return null;
		L = l.l.l;
		g = l.g;
		L.next = null;
		L.tt = lua.LUA_TTHREAD;
		g.currentwhite = (byte)lgc.bitmask(lgc.WHITE0BIT);
		L.marked = (byte)lgc.luaC_white(g);
		preinit_thread(L, g);
		g.frealloc = f;
		g.ud = ud;
		g.mainthread = L;
		g.seed = makeseed(L);
		g.gcrunning = 0;  /* no GC while building state */
		g.GCestimate = 0;
		g.strt.size = g.strt.nuse = 0;
		g.strt.hash = null;
		lobject.setnilvalue(g.l_registry);
		g.panic = null;
		g.version = 0/*null*/;
		g.gcstate = lgc.GCSpause;
		g.gckind = KGC_NORMAL;
		g.allgc = g.finobj = g.tobefnz = g.fixedgc = null;
		g.sweepgc = null;
		g.gray = g.grayagain = null;
		g.weak = g.ephemeron = g.allweak = null;
		g.twups = null;
		g.totalbytes = 0/*sizeof(LG)*/;
		g.GCdebt = 0;
		g.gcfinnum = 0;
		g.gcpause = LUAI_GCPAUSE;
		g.gcstepmul = LUAI_GCMUL;
		for (i = 0; i < lua.LUA_NUMTAGS; i++)
			g.mt[i] = null;
		//if (luaD_rawrunprotected(L, f_luaopen, NULL) != LUA_OK) {
		//	/* memory allocation error: free partial state */
		//	close_state(L);
		//	L = NULL;
		//}
		return L;
	}

	/*
	** preinitialize a thread with consistent values without allocating
	** any memory (to avoid errors)
	*/
	static void preinit_thread(lua_State L, global_State g) {
		L.l_G = g;
		L.stack = null;
		L.ci = null;
		L.nci = 0;
		L.stacksize = 0;
		L.twups = L;  /* thread has no upvalues */
		L.errorJmp = null;
		L.nCcalls = 0;
		L.hook = null;
		L.hookmask = 0;
		L.basehookcount = 0;
		L.allowhook = 1;
		ldebug.resethookcount(L);
		L.openupval = null;
		L.nny = 1;
		L.status = lua.LUA_OK;
		L.errfunc = 0;
	}

	//static global_State G(lua_State L) {
	//	return L.l_G;
	//}

	static uint luai_makeseed() {
		TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
		return (uint)ts.TotalSeconds;
	}

	/*
	** Compute an initial seed as random as possible. Rely on Address Space
	** Layout Randomization (if present) to increase randomness..
	*/
	static void addbuff(byte[] b, ref int p, uint e) {
		int size = 4;
		for (int i = size - 1; i >= 0; i--) {
			b[p + i] = (byte)(e % 256);
		}
		p += size;
	}

	static uint makeseed(lua_State L) {
		byte[] buff = new byte[4 * 4];
		uint h = luai_makeseed();
		int p = 0;
		Random rd = new Random();
		addbuff(buff, ref p, (uint)rd.Next());  /* heap variable */
		addbuff(buff, ref p, (uint)rd.Next());  /* local variable */
		addbuff(buff, ref p, (uint)rd.Next());  /* global variable */
		addbuff(buff, ref p, (uint)rd.Next());  /* public function */
		llimits.lua_assert(p == buff.Length);
		return lstring.luaS_hash(buff, (uint)p, h);
	}

	/*
	** open parts of the state that may cause memory-allocation errors.
	** ('g->version' != NULL flags that the state was completely build)
	*/
	static void f_luaopen(lua_State L, object ud) {
		global_State g = L.l_G;
		llimits.UNUSED(ud);
		stack_init(L, L);  /* init stack */
		init_registry(L, g);
		//luaS_init(L);
		//luaT_init(L);
		//luaX_init(L);
		//g->gcrunning = 1;  /* allow gc */
		//g->version = lua_version(NULL);
		//luai_userstateopen(L);
	}

	static void stack_init(lua_State L1, lua_State L) {
		int i;
		CallInfo ci;
		/* initialize stack array */
		L1.stack = new TValue[BASIC_STACK_SIZE];
		L1.stacksize = BASIC_STACK_SIZE;
		for (i = 0; i < BASIC_STACK_SIZE; i++) {
			L1.stack[i] = new TValue();
			lobject.setnilvalue(L1.stack[i]);  /* erase new stack */
		}
		L1.top_index = 0;
		L1.stack_last_index = L1.stacksize - EXTRA_STACK;
		/* initialize first ci */
		ci = L1.base_ci;
		ci.next = ci.previous = null;
		ci.callstatus = 0;
		ci.func = L1.top;
		lobject.setnilvalue(L1.stack[++L1.top_index]);  /* 'function' entry for this 'ci' */
		ci.top = L1.stack[L1.top_index + lua.LUA_MINSTACK];
		L1.ci = ci;
	}


	/* macro to convert a Lua object into a GCObject */
	public static GCObject obj2gco(IGCObject v) {
		// novariant((v)->tt) < LUA_TDEADKEY
		return v.gc;
	}

	/*
	** Create registry table and its predefined values
	*/
	static void init_registry(lua_State L, global_State g) {
		TValue temp;
		/* create registry */
		Table registry = luaH_new(L);
		sethvalue(L, &g->l_registry, registry);
		luaH_resize(L, registry, LUA_RIDX_LAST, 0);
		/* registry[LUA_RIDX_MAINTHREAD] = L */
		setthvalue(L, &temp, L);  /* temp = L */
		luaH_setint(L, registry, LUA_RIDX_MAINTHREAD, &temp);
		/* registry[LUA_RIDX_GLOBALS] = table of globals */
		sethvalue(L, &temp, luaH_new(L));  /* temp = new table (global table) */
		luaH_setint(L, registry, LUA_RIDX_GLOBALS, &temp);
	}
}

/*
** 'per thread' state
*/
class lua_State {
	public GCObject next;
	public byte tt;
	public byte marked;
	public ushort nci;				/* number of items in 'ci' list */
	public byte status;
	//public TValue top;             /* first free slot in the stack */
	public int top_index;             /* first free slot in the stack */
	public global_State l_G;
	public CallInfo ci;			/* call info for current function */
	uint oldpc;				/* last pc traced */
	//public TValue stack_last;       /* last free slot in the stack */
	public int stack_last_index;       /* last free slot in the stack */
	public TValue[] stack;			/* stack base */
	public UpVal openupval;		/* list of open upvalues in this stack */
	GCObject gclist;
	public lua_State twups;		/* list of threads with open upvalues */
	public lua_longjmp errorJmp;	/* current error recover point */
	public CallInfo base_ci;		/* CallInfo for first level (C calling Lua) */
	public volatile lua_Hook hook;
	public int errfunc;			/* current error handling function (stack index) */
	public int stacksize;
	public int basehookcount;
	public int hookcount;
	public ushort nny;				/* number of non-yieldable calls in stack */
	public ushort nCcalls;			/* number of nested C calls */
	public int hookmask;
	public byte allowhook;

	public TValue top {
		get {
			return stack[top_index];
		}
	}

	public TValue stack_last {
		get {
			return stack[stack_last_index];
		}
	}
};

/*
** 'global state', shared by all threads of this state
*/
class global_State {
	public lua_Alloc frealloc;															/* function to reallocate memory */
	public object ud;																	/* auxiliary data to 'frealloc' */
	public int totalbytes;																/* number of bytes currently allocated - GCdebt */
	public int GCdebt;																	/* bytes allocated not yet compensated by the collector */
	uint GCmemtrav;																/* memory traversed by the GC */
	public uint GCestimate;															/* an estimate of the non-garbage memory in use */
	public stringtable strt;															/* hash table for strings */
	public TValue l_registry;
	public uint seed;																	/* randomized seed for hashes */
	public byte currentwhite;
	public byte gcstate;																/* state of garbage collector */
	public byte gckind;																/* kind of GC running */
	public byte gcrunning;																/* true if GC is running */
	public GCObject allgc;																/* list of all collectable objects */
	public GCObject[] sweepgc;															/* current position of sweep in list */
	public GCObject finobj;															/* list of collectable objects with finalizers */
	public GCObject gray;																/* list of gray objects */
	public GCObject grayagain;															/* list of objects to be traversed atomically */
	public GCObject weak;																/* list of tables with weak values */
	public GCObject ephemeron;															/* list of ephemeron tables (weak keys) */
	public GCObject allweak;															/* list of all-weak tables */
	public GCObject tobefnz;															/* list of userdata to be GC */
	public GCObject fixedgc;															/* list of objects not to be collected */
	public lua_State twups;															/* list of threads with open upvalues */
	public uint gcfinnum;																/* number of finalizers to call in each GC step */
	public int gcpause;																/* size of pause between successive GCs */
	public int gcstepmul;                                                              /* GC 'granularity' */
	public lua_CFunction panic;														/* to be called in unprotected errors */
	public lua_State mainthread;
	public double version;																/* pointer to version number */
	TString memerrmsg;															/* memory-error message */
	TString[] tmname = new TString[(int)TMS.TM_N];								/* array with tag-method names */
	public Table[] mt = new Table[lua.LUA_NUMTAGS];									/* metatables for basic types */
	TString[,] strcache = new TString[llimits.STRCACHE_N, llimits.STRCACHE_M];  /* cache for strings in API */
}

/*
** Information about a call.
** When a thread yields, 'func' is adjusted to pretend that the
** top function has only the yielded values in its stack; in that
** case, the actual 'func' value is saved in field 'extra'.
** When a function calls another with a continuation, 'extra' keeps
** the function index so that, in case of errors, the continuation
** function can be called with the correct top.
*/
class CallInfo {
	public TValue func;				/* function index in the stack */
	public TValue top;					/* top for this function */
	public CallInfo previous, next;	/* dynamic call link */
	_union u;
	int extra;
	short nresults;				/* expected number of results from this function */
	public ushort callstatus;

	class _union {
		_struct1 l;
		_struct2 c;

		class _struct1 {  /* only for Lua functions */
			TValue base_;  /* base for this function */
			uint savedpc;
		}

		class _struct2 {  /* only for C functions */
			lua_KFunction k;	/* continuation in case of yields */
			int old_errfunc;
			int ctx;							/* context info. in case of yields */
		}
	}
}

/*
** thread state + extra space
*/
class LX {
	byte[] extra_ = new byte[luaconf.LUA_EXTRASPACE];
	public lua_State l;
}

/*
** Main thread combines a thread state and the global state
*/
class LG {
	public LX l;
	public global_State g;
}

/*
** Union of all collectable objects (only for conversions)
*/
class GCUnion {
	public GCObject gc;  /* common header */
	TString ts;
	Udata u;
	Closure cl;
	Table h;
	Proto p;
	lua_State th;  /* thread */
};
