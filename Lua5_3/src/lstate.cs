using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
** 'per thread' state
*/
class lua_State {
	GCObject next;
	byte tt;
	byte marked;
	ushort nci;  /* number of items in 'ci' list */
	byte status;
	TValue top;  /* first free slot in the stack */
	global_State l_G;
	CallInfo ci;  /* call info for current function */
	uint oldpc;  /* last pc traced */
	TValue stack_last;  /* last free slot in the stack */
	TValue stack;  /* stack base */
	UpVal openupval;  /* list of open upvalues in this stack */
	GCObject gclist;
	lua_State twups;  /* list of threads with open upvalues */
	//lua_longjmp *errorJmp;  /* current error recover point */
	// CallInfo base_ci;  /* CallInfo for first level (C calling Lua) */
	//volatile lua_Hook hook;
	//ptrdiff_t errfunc;  /* current error handling function (stack index) */
	//int stacksize;
	//int basehookcount;
	//int hookcount;
	//unsigned short nny;  /* number of non-yieldable calls in stack */
	//unsigned short nCcalls;  /* number of nested C calls */
	//l_signalT hookmask;
	//lu_byte allowhook;
};

/*
** 'global state', shared by all threads of this state
*/
class global_State {
	Func<object, object, object, uint, uint> frealloc;							/* function to reallocate memory */
	object ud;																	/* auxiliary data to 'frealloc' */
	int totalbytes;																/* number of bytes currently allocated - GCdebt */
	int GCdebt;																	/* bytes allocated not yet compensated by the collector */
	uint GCmemtrav;																/* memory traversed by the GC */
	uint GCestimate;															/* an estimate of the non-garbage memory in use */
	stringtable strt;															/* hash table for strings */
	TValue l_registry;
	uint seed;																	/* randomized seed for hashes */
	byte currentwhite;
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
	int gcstepmul;																/* GC 'granularity' */
	Func<int, lua_State> panic;													/* to be called in unprotected errors */
	lua_State mainthread;
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
			Func<int, lua_State, int, int> k;	/* continuation in case of yields */
			int old_errfunc;
			int ctx;							/* context info. in case of yields */
		}
	}
}
