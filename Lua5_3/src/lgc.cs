
class GCObject : IGCObject {
	public GCObject next;
	public byte tt;
	byte _marked;

	public GCObject() {
		global_State* g = G(L);
		GCObject* o = cast(GCObject *, luaM_newobject(L, novariant(tt), sz));
		o->marked = luaC_white(g);
		o->tt = tt;
		o->next = g->allgc;
		g->allgc = o;
	}

	public byte marked {
		get {
			return _marked;
		}
	}
}

class lgc {
	/* Layout for bit use in 'marked' field: */
	public const int WHITE0BIT		= 0;	/* object is white (type 0) */
	public const int WHITE1BIT		= 1;	/* object is white (type 1) */
	public const int BLACKBIT		= 2;	/* object is black */
	public const int FINALIZEDBIT	= 3;	/* object has been marked for finalization */
	/* bit 7 is currently used by tests (luaL_checkmemory) */

	public static readonly int WHITEBITS = bit2mask(WHITE0BIT, WHITE1BIT);

	/*
	** Possible states of the Garbage Collector
	*/
	public const int GCSpropagate	= 0;
	public const int GCSatomic		= 1;
	public const int GCSswpallgc	= 2;
	public const int GCSswpfinobj	= 3;
	public const int GCSswptobefnz	= 4;
	public const int GCSswpend		= 5;
	public const int GCScallfin		= 6;
	public const int GCSpause		= 7;

	public static int bitmask(int b) {
		return 1 << b;
	}
	
	public static int bit2mask(int b1, int b2) {
		return bitmask(b1) | bitmask(b2);
	}
	
	public static int luaC_white(global_State g) {
		return g.currentwhite & WHITEBITS;
	}

	public static int isdead(global_State g, IGCObject v) {
		return isdeadm(otherwhite(g), v.marked);
	}

	static int isdeadm(int ow, int m) {
		return ~((m ^ WHITEBITS) & ow);
	}

	static int otherwhite(global_State g) {
		return g.currentwhite ^ WHITEBITS;
	}

	public static void changewhite(IGCObject x) {
		x.marked ^= (byte)WHITEBITS;
	}

	/*
	** Performs a full GC cycle; if 'isemergency', set a flag to avoid
	** some operations which could change the interpreter state in some
	** unexpected ways (running finalizers and shrinking some structures).
	** Before running the collection, check 'keepinvariant'; if it is true,
	** there may be some objects marked as black, so the collector has
	** to sweep all objects to turn them back to white (as white has not
	** changed, nothing will be collected).
	*/
	static void luaC_fullgc(lua_State L, int isemergency) {
		global_State g = L.l_G;
		llimits.lua_assert(g.gckind == lstate.KGC_NORMAL);
		if (isemergency != 0)
			g.gckind = lstate.KGC_EMERGENCY;  /* set flag */
		if (keepinvariant(g)) {  /* black objects? */
			entersweep(L); /* sweep everything to turn them back to white */
		}
		/* finish any pending sweep phase to start a new cycle */
		//luaC_runtilstate(L, bitmask(GCSpause));
		//luaC_runtilstate(L, ~bitmask(GCSpause));  /* start new collection */
		//luaC_runtilstate(L, bitmask(GCScallfin));  /* run up to finalizers */
		//										   /* estimate must be correct after a full GC cycle */
		//lua_assert(g->GCestimate == gettotalbytes(g));
		//luaC_runtilstate(L, bitmask(GCSpause));  /* finish collection */
		//g->gckind = KGC_NORMAL;
		//setpause(g);
	}

	static bool keepinvariant(global_State g) {
		return g.gcstate <= GCSatomic;
	}

	/*
	** Enter first sweep phase.
	** The call to 'sweeplist' tries to make pointer point to an object
	** inside the list (instead of to the header), so that the real sweep do
	** not need to skip objects created between "now" and the start of the
	** real sweep.
	*/
	static void entersweep(lua_State L) {
		global_State g = L.l_G;
		g.gcstate = GCSswpallgc;
		llimits.lua_assert(g.sweepgc == null);
		g.sweepgc = sweeplist(L, &g->allgc, 1);
	}

	/*
	** sweep at most 'count' elements from a list of GCObjects erasing dead
	** objects, where a dead object is one marked with the old (non current)
	** white; change all non-dead objects back to white, preparing for next
	** collection cycle. Return where to continue the traversal or NULL if
	** list is finished.
	*/
	static GCObject sweeplist(lua_State L, GCObject p, uint count) {
		global_State g = L.l_G;
		int ow = otherwhite(g);
		int white = luaC_white(g);  /* current white */
		while (p != null && count-- > 0) {
			GCObject curr = p;
			int marked = curr.marked;
			if (isdeadm(ow, marked) != 0) {  /* is 'curr' dead? */
				p = curr.next;  /* remove 'curr' from list */
				freeobj(L, curr);  /* erase 'curr' */
			} else {  /* change mark to 'white' */
				//curr->marked = cast_byte((marked & maskcolors) | white);
				//p = &curr->next;  /* go to next element */
			}
		}
		//return (*p == NULL) ? NULL : p;
	}

	static void freeobj(lua_State L, GCObject o) {
		switch (o.tt) {
			case lobject.LUA_TPROTO:
				luaF_freeproto(L, gco2p(o));
				break;
			//case LUA_TLCL: {
			//		freeLclosure(L, gco2lcl(o));
			//		break;
			//	}
			//case LUA_TCCL: {
			//		luaM_freemem(L, o, sizeCclosure(gco2ccl(o)->nupvalues));
			//		break;
			//	}
			//case LUA_TTABLE:
			//	luaH_free(L, gco2t(o));
			//	break;
			//case LUA_TTHREAD:
			//	luaE_freethread(L, gco2th(o));
			//	break;
			//case LUA_TUSERDATA:
			//	luaM_freemem(L, o, sizeudata(gco2u(o)));
			//	break;
			//case LUA_TSHRSTR:
			//	luaS_remove(L, gco2ts(o));  /* remove it from hash table */
			//	luaM_freemem(L, o, sizelstring(gco2ts(o)->shrlen));
			//	break;
			//case LUA_TLNGSTR: {
			//		luaM_freemem(L, o, sizelstring(gco2ts(o)->u.lnglen));
			//		break;
			//	}
			//default:
			//	lua_assert(0);
		}
	}

}
