
using System;

class lstate {

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
		//g->seed = makeseed(L);
		//g->gcrunning = 0;  /* no GC while building state */
		//g->GCestimate = 0;
		//g->strt.size = g->strt.nuse = 0;
		//g->strt.hash = NULL;
		//setnilvalue(&g->l_registry);
		//g->panic = NULL;
		//g->version = NULL;
		//g->gcstate = GCSpause;
		//g->gckind = KGC_NORMAL;
		//g->allgc = g->finobj = g->tobefnz = g->fixedgc = NULL;
		//g->sweepgc = NULL;
		//g->gray = g->grayagain = NULL;
		//g->weak = g->ephemeron = g->allweak = NULL;
		//g->twups = NULL;
		//g->totalbytes = sizeof(LG);
		//g->GCdebt = 0;
		//g->gcfinnum = 0;
		//g->gcpause = LUAI_GCPAUSE;
		//g->gcstepmul = LUAI_GCMUL;
		//for (i = 0; i < LUA_NUMTAGS; i++)
		//	g->mt[i] = NULL;
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
	TValue top;             /* first free slot in the stack */
	public global_State l_G;
	public CallInfo ci;			/* call info for current function */
	uint oldpc;				/* last pc traced */
	TValue stack_last;		/* last free slot in the stack */
	public TValue stack;			/* stack base */
	public UpVal openupval;		/* list of open upvalues in this stack */
	GCObject gclist;
	public lua_State twups;		/* list of threads with open upvalues */
	public lua_longjmp errorJmp;	/* current error recover point */
	CallInfo base_ci;		/* CallInfo for first level (C calling Lua) */
	public volatile lua_Hook hook;
	public int errfunc;			/* current error handling function (stack index) */
	public int stacksize;
	public int basehookcount;
	public int hookcount;
	public ushort nny;				/* number of non-yieldable calls in stack */
	public ushort nCcalls;			/* number of nested C calls */
	public int hookmask;
	public byte allowhook;
};

/*
** 'global state', shared by all threads of this state
*/
class global_State {
	public lua_Alloc frealloc;															/* function to reallocate memory */
	public object ud;																	/* auxiliary data to 'frealloc' */
	int totalbytes;																/* number of bytes currently allocated - GCdebt */
	int GCdebt;																	/* bytes allocated not yet compensated by the collector */
	uint GCmemtrav;																/* memory traversed by the GC */
	uint GCestimate;															/* an estimate of the non-garbage memory in use */
	stringtable strt;															/* hash table for strings */
	TValue l_registry;
	uint seed;																	/* randomized seed for hashes */
	public byte currentwhite;
	byte gcstate;																/* state of garbage collector */
	byte gckind;																/* kind of GC running */
	byte gcrunning;																/* true if GC is running */
	GCObject allgc;																/* list of all collectable objects */
	GCObject[] sweepgc;															/* current position of sweep in list */
	GCObject finobj;															/* list of collectable objects with finalizers */
	GCObject gray;																/* list of gray objects */
	GCObject grayagain;															/* list of objects to be traversed atomically */
	GCObject weak;																/* list of tables with weak values */
	GCObject ephemeron;															/* list of ephemeron tables (weak keys) */
	GCObject allweak;															/* list of all-weak tables */
	GCObject tobefnz;															/* list of userdata to be GC */
	GCObject fixedgc;															/* list of objects not to be collected */
	lua_State twups;															/* list of threads with open upvalues */
	uint gcfinnum;																/* number of finalizers to call in each GC step */
	int gcpause;																/* size of pause between successive GCs */
	int gcstepmul;                                                              /* GC 'granularity' */
	lua_CFunction panic;														/* to be called in unprotected errors */
	public lua_State mainthread;
	double version;																/* pointer to version number */
	TString memerrmsg;															/* memory-error message */
	TString[] tmname = new TString[(int)TMS.TM_N];								/* array with tag-method names */
	Table[] mt = new Table[lua.LUA_NUMTAGS];									/* metatables for basic types */
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
	TValue func;				/* function index in the stack */
	TValue top;					/* top for this function */
	CallInfo previous, next;	/* dynamic call link */
	_union u;
	int extra;
	short nresults;				/* expected number of results from this function */
	ushort callstatus;

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
