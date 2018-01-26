
class Table : GCObject {
	byte flags;     /* 1<<p means tagmethod(p) is not present */
	public byte lsizenode; /* log2 of size of 'node' array */
	public uint sizearray; /* size of 'array' array */
	public TValue[] array;   /* array part */
	public Node[] node;
	public Node lastfree;  /* any free position is before this position */
	Table metatable;
	GCObject gclist;

	public Table(lua_State L) : base(L, lua.LUA_TTABLE) {
		metatable = null;
		flags = byte.MaxValue;
		array = null;
		sizearray = 0;
		ltable.setnodevector(L, this, 0);
	}
}

class ltable {
	/*
	** Maximum size of array part (MAXASIZE) is 2^MAXABITS. MAXABITS is
	** the largest integer such that MAXASIZE fits in an unsigned int.
	*/
	const int MAXABITS = 4 * 8 - 1;
	const uint MAXASIZE = 1u << MAXABITS;

	/*
	** Maximum size of hash part is 2^MAXHBITS. MAXHBITS is the largest
	** integer such that 2^MAXHBITS fits in a signed int. (Note that the
	** maximum number of elements in a table, 2^MAXABITS + 2^MAXHBITS, still
	** fits comfortably in an unsigned int.)
	*/
	const int MAXHBITS = MAXABITS - 1;

	static readonly Node[] dummynode = dummynode_;

	static readonly Node[] dummynode_ = { new Node(lobject.NILCONSTANT(), new TKey()) };

	static Node gnode(Table t, int i) {
		return t.node[i];
	}

	static TValue gval(Node n) {
		return n.i_val;
	}

	/* 'const' to avoid wrong writings that can mess up field 'next' */
	static TValue gkey(Node n) {
		return n.i_key.tvk;
	}

	/*
	** writable version of 'gkey'; allows updates to individual fields,
	** but not to the whole (which has incompatible type)
	*/
	static TValue wgkey(Node n) {
		return n.i_key.nk;
	}

	/* true when 't' is using 'dummynode' as its hash part */
	static bool isdummy(Table t) {
		return t.lastfree == null;
	}

	/* allocated size for hash nodes */
	static int allocsizenode(Table t) {
		return isdummy(t) ? 0 : lobject.sizenode(t);
	}

	static Node hashpow2(Table t, long n) {
		return gnode(t, lobject.lmod((int)n, lobject.sizenode(t)));
	}

	static Node hashint(Table t, long i) {
		return hashpow2(t, i);
	}

	static void setarrayvector(lua_State L, Table t, uint size) {
		uint i;
		t.array = new TValue[size];
		for (i = t.sizearray; i < size; i++)
			t.array[i] = new TValue();
		lobject.setnilvalue(t.array[i]);
		t.sizearray = size;
	}

	public static void setnodevector(lua_State L, Table t, uint size) {
		if (size == 0) {  /* no elements to hash part? */
			t.node = dummynode;  /* use common 'dummynode' */
			t.lsizenode = 0;
			t.lastfree = null;  /* signal that it is using dummy node */
		} else {
			int i;
			int lsize = lobject.luaO_ceillog2((int)size);
			if (lsize > MAXHBITS)
				ldebug.luaG_runerror(L, "table overflow");
			size = (uint)lobject.twoto(lsize);
			t.node = new Node[size];
			for (i = 0; i < (int)size; i++) {
				Node n = gnode(t, i);
				n.i_key.nk.next = 0;
				lobject.setnilvalue(wgkey(n));
				lobject.setnilvalue(gval(n));
			}
			t.lsizenode = (byte)lsize;
			t.lastfree = gnode(t, (int)size);  /* all positions are free */
		}
	}

	static void luaH_resize(lua_State L, Table t, uint nasize, uint nhsize) {
		uint i;
		int j;
		uint oldasize = t.sizearray;
		int oldhsize = allocsizenode(t);
		Node[] nold = t.node;  /* save old hash ... */
		if (nasize > oldasize)  /* array part must grow? */
			setarrayvector(L, t, nasize);
		/* create new hash part with appropriate size */
		setnodevector(L, t, nhsize);
		if (nasize < oldasize) {  /* array part must shrink? */
			t.sizearray = nasize;
			/* re-insert elements from vanishing slice */
			for (i = nasize; i < oldasize; i++) {
				if (!lobject.ttisnil(t.array[i]))
					luaH_setint(L, t, i + 1, &t->array[i]);
			}
			/* shrink array */
			luaM_reallocvector(L, t->array, oldasize, nasize, TValue);
		}
		/* re-insert elements from hash part */
		for (j = oldhsize - 1; j >= 0; j--) {
			Node* old = nold + j;
			if (!ttisnil(gval(old))) {
				/* doesn't need barrier/invalidate cache, as entry was
				   already present in the table */
				setobjt2t(L, luaH_set(L, t, gkey(old)), gval(old));
			}
		}
		if (oldhsize > 0)  /* not the dummy node? */
			luaM_freearray(L, nold, cast(size_t, oldhsize)); /* free old hash */
	}

	public static Table luaH_new(lua_State L) {
		return new Table(L);
	}

	/*
	** inserts a new key into a hash table; first, check whether key's main
	** position is free. If not, check whether colliding node is in its main
	** position or not: if it is not, move colliding node to an empty place and
	** put new key in its main position; otherwise (colliding node is in its main
	** position), new key goes to an empty position.
	*/
	TValue luaH_newkey(lua_State L, Table t, TValue key) {
		Node mp;
		TValue aux;
		if (lobject.ttisnil(key))
			ldebug.luaG_runerror(L, "table index is nil");
		else if (lobject.ttisfloat(key)) {
			long k;
			if (luaV_tointeger(key, &k, 0)) {  /* does index fit in an integer? */
      setivalue(&aux, k);
	key = &aux;  /* insert it as an integer */
    }
    else if (luai_numisnan(fltvalue(key)))
      luaG_runerror(L, "table index is NaN");
  }
  mp = mainposition(t, key);
  if (!ttisnil(gval(mp)) || isdummy(t)) {  /* main position is taken? */
    Node* othern;
Node* f = getfreepos(t);  /* get a free place */
    if (f == NULL) {  /* cannot find a free place? */

	  rehash(L, t, key);  /* grow table */
      /* whatever called 'newkey' takes care of TM cache */
      return luaH_set(L, t, key);  /* insert key into grown table */
    }

	lua_assert(!isdummy(t));
    othern = mainposition(t, gkey(mp));
    if (othern != mp) {  /* is colliding node out of its main position? */
      /* yes; move colliding node into free position */
      while (othern + gnext(othern) != mp)  /* find previous */
        othern += gnext(othern);

	  gnext(othern) = cast_int(f - othern);  /* rechain to point to 'f' */

	  * f = *mp;  /* copy colliding node into free pos. (mp->next also goes) */
      if (gnext(mp) != 0) {
        gnext(f) += cast_int(mp - f);  /* correct 'next' */

		gnext(mp) = 0;  /* now 'mp' is free */
      }
      setnilvalue(gval(mp));
    }
    else {  /* colliding node is in its own main position */
      /* new node will go into free position */
      if (gnext(mp) != 0)
        gnext(f) = cast_int((mp + gnext(mp)) - f);  /* chain new position */
      else lua_assert(gnext(f) == 0);
      gnext(mp) = cast_int(f - mp);
mp = f;
    }
  }
  setnodekey(L, &mp->i_key, key);
  luaC_barrierback(L, t, key);
  lua_assert(ttisnil(gval(mp)));
  return gval(mp);
}

	/*
	** search function for integers
	*/
	static TValue luaH_getint(Table t, long key) {
		/* (1 <= key && key <= t->sizearray) */
		if ((ulong)key - 1 < t.sizearray)
			return t.array[key - 1];
		else {
			Node n = hashint(t, key);
			for (;;) {  /* check whether 'key' is somewhere in the chain */
				if (lobject.ttisinteger(gkey(n)) && lobject.ivalue(gkey(n)) == key)
					return gval(n);  /* that's it */
				else {
					int nx = n.i_key.nk.next;
					if (nx == 0)
						break;
					n += nx;
				}
			}
			return lobject.luaO_nilobject;
		}
	}

	static void luaH_setint(lua_State L, Table t, long key, TValue value) {
		TValue p = luaH_getint(t, key);
		TValue cell;
		if (p != lobject.luaO_nilobject)
			cell = p;
		else {
			TValue k = new TValue();
			lobject.setivalue(k, key);
			cell = luaH_newkey(L, t, &k);
		}
		setobj2t(L, cell, value);
	}
}
