using System;

class Table : GCObject {
	public byte flags;     /* 1<<p means tagmethod(p) is not present */
	public byte lsizenode; /* log2 of size of 'node' array */
	public uint sizearray; /* size of 'array' array */
	public TValue[] array;   /* array part */
	public Node[] node;
	//public Node lastfree;  /* any free position is before this position */
	private int _node_index;
	private int _lastfree_index;
	Table metatable;
	GCObject gclist;

	public Table(lua_State L) : base(L, lua.LUA_TTABLE) {
		metatable = null;
		flags = byte.MaxValue;
		array = null;
		sizearray = 0;
		ltable.setnodevector(L, this, 0);
	}

	public int nodeIndex {
		get {
			return _node_index;
		}
	}

	public int lastfreeIndex {
		get {
			return _lastfree_index;
		}
		set {
			_lastfree_index = value;
		}
	}

	public Node lastfree {
		get {
			return node[_lastfree_index];
		}
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

	static int gnext(Node n) {
		return n.i_key.nk.next;
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

	static Node hashstr(Table t, TString str) {
		return hashpow2(t, str.hash);
	}

	static Node hashboolean(Table t, int p) {
		return hashpow2(t, p);
	}

	static Node hashint(Table t, long i) {
		return hashpow2(t, i);
	}

	/*
	** for some types, it is better to avoid modulus by power of 2, as
	** they tend to have many 2 factors.
	*/
	static Node hashmod(Table t, int n) {
		return gnode(t, n % ((lobject.sizenode(t) - 1) | 1));
	}

	static Node hashpointer(Table t, object p) {
		return hashmod(t, (int)llimits.point2uint(p));
	}

	/*
	** Hash for floating-point numbers.
	** The main computation should be just
	**     n = frexp(n, &i); return (n * INT_MAX) + i
	** but there are some numerical subtleties.
	** In a two-complement representation, INT_MAX does not has an exact
	** representation as a float, but INT_MIN does; because the absolute
	** value of 'frexp' is smaller than 1 (unless 'n' is inf/NaN), the
	** absolute value of the product 'frexp * -INT_MIN' is smaller or equal
	** to INT_MAX. Next, the use of 'unsigned int' avoids overflows when
	** adding 'i'; the use of '~u' (instead of '-u') avoids problems with
	** INT_MIN.
	*/
	static int l_hashfloat(double n) {
		int i = 0;
		long ni = 0;
		// TODO: n = l_mathop(frexp)(n, &i) * -(double)int.MinValue;
		if (!luaconf.lua_numbertointeger(n, ref ni)) {  /* is 'n' inf/-inf/NaN? */
			llimits.lua_assert(llimits.luai_numisnan(n) || Math.Abs(n) == double.MaxValue);
			return 0;
		} else {  /* normal case */
			uint u = (uint)i + (uint)ni;
			return u <= int.MaxValue ? (int)u : (int)~u;
		}
	}

	/*
	** returns the 'main' position of an element in a table (that is, the index
	** of its hash value)
	*/
	static Node mainposition(Table t, TValue key) {
		switch (lobject.ttype(key)) {
			case lobject.LUA_TNUMINT:
				return hashint(t, lobject.ivalue(key));
			case lobject.LUA_TNUMFLT:
				return hashmod(t, l_hashfloat(lobject.fltvalue(key)));
			case lobject.LUA_TSHRSTR:
				return hashstr(t, lobject.tsvalue(key));
			case lobject.LUA_TLNGSTR:
				return hashpow2(t, lstring.luaS_hashlongstr(lobject.tsvalue(key)));
			case lua.LUA_TBOOLEAN:
				return hashboolean(t, lobject.bvalue(key));
			case lua.LUA_TLIGHTUSERDATA:
				return hashpointer(t, lobject.pvalue(key));
			case lobject.LUA_TLCF:
				return hashpointer(t, lobject.fvalue(key));
			default:
				llimits.lua_assert(!lobject.ttisdeadkey(key));
				return hashpointer(t, lobject.gcvalue(key));
		}
	}

	/*
	** returns the index for 'key' if 'key' is an appropriate key to live in
	** the array part of the table, 0 otherwise.
	*/
	static uint arrayindex(TValue key) {
		if (lobject.ttisinteger(key)) {
			long k = lobject.ivalue(key);
			if (0 < k && (ulong)k <= MAXASIZE)
				return (uint)k;  /* 'key' is an appropriate array index */
		}
		return 0;  /* 'key' did not match some condition */
	}

	/*
	** {=============================================================
	** Rehash
	** ==============================================================
	*/

	/*
	** Compute the optimal size for the array part of table 't'. 'nums' is a
	** "count array" where 'nums[i]' is the number of integers in the table
	** between 2^(i - 1) + 1 and 2^i. 'pna' enters with the total number of
	** integer keys in the table and leaves with the number of keys that
	** will go to the array part; return the optimal size.
	*/
	static uint computesizes(uint[] nums, ref uint pna) {
		int i;
		uint twotoi;  /* 2^i (candidate for optimal size) */
		uint a = 0;  /* number of elements smaller than 2^i */
		uint na = 0;  /* number of elements to go to array part */
		uint optimal = 0;  /* optimal size for array part */
		/* loop while keys can fill more than half of total size */
		for (i = 0, twotoi = 1; pna > twotoi / 2; i++, twotoi *= 2) {
			if (nums[i] > 0) {
				a += nums[i];
				if (a > twotoi / 2) {  /* more than half elements present? */
					optimal = twotoi;  /* optimal size (till now) */
					na = a;  /* all elements up to 'optimal' will go to array part */
				}
			}
		}
		llimits.lua_assert((optimal == 0 || optimal / 2 < na) && na <= optimal);
		pna = na;
		return optimal;
	}

	static int countint(TValue key, uint[] nums) {
		uint k = arrayindex(key);
		if (k != 0) {  /* is 'key' an appropriate array index? */
			nums[lobject.luaO_ceillog2((int)k)]++;  /* count as such */
			return 1;
		} else
			return 0;
	}

	/*
	** Count keys in array part of table 't': Fill 'nums[i]' with
	** number of keys that will go into corresponding slice and return
	** total number of non-nil keys.
	*/
	static uint numusearray(Table t, uint[] nums) {
		int lg;
		uint ttlg;  /* 2^lg */
		uint ause = 0;  /* summation of 'nums' */
		uint i = 1;  /* count to traverse all array keys */
		/* traverse each slice */
		for (lg = 0, ttlg = 1; lg <= MAXABITS; lg++, ttlg *= 2) {
			uint lc = 0;  /* counter */
			uint lim = ttlg;
			if (lim > t.sizearray) {
				lim = t.sizearray;  /* adjust upper limit */
				if (i > lim)
					break;  /* no more elements to count */
			}
			/* count elements in range (2^(lg - 1), 2^lg] */
			for (; i <= lim; i++) {
				if (!lobject.ttisnil(t.array[i - 1]))
				lc++;
			}
			nums[lg] += lc;
			ause += lc;
		}
		return ause;
	}

	static int numusehash(Table t, uint[] nums, ref uint pna) {
		int totaluse = 0;  /* total number of elements */
		int ause = 0;  /* elements added to 'nums' (can go to array part) */
		int i = lobject.sizenode(t);
		while (i-- != 0) {
			Node n = t.node[i];
			if (!lobject.ttisnil(gval(n))) {
				ause += countint(gkey(n), nums);
				totaluse++;
			}
		}
		pna += (uint)ause;
		return totaluse;
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
			//t.lastfree = null;  /* signal that it is using dummy node */
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
			t.lastfreeIndex = (int)size;  /* all positions are free */
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

	/*
	** nums[i] = number of keys 'k' where 2^(i - 1) < k <= 2^i
	*/
	static void rehash(lua_State L, Table t, TValue ek) {
		uint asize;  /* optimal size for array part */
		uint na;  /* number of keys in the array part */
		uint[] nums = new uint[MAXABITS + 1];
		int i;
		int totaluse;
		for (i = 0; i <= MAXABITS; i++)
			nums[i] = 0;  /* reset counts */
		na = numusearray(t, nums);  /* count keys in array part */
		totaluse = (int)na;  /* all those keys are integer keys */
		totaluse += numusehash(t, nums, ref na);  /* count keys in hash part */
		/* count extra key */
		na += (uint)countint(ek, nums);
		totaluse++;
		/* compute new size for array part */
		asize = computesizes(nums, ref na);
		/* resize the table to new computed sizes */
		luaH_resize(L, t, asize, (uint)totaluse - na);
	}

	public static Table luaH_new(lua_State L) {
		return new Table(L);
	}

	static Node getfreepos(Table t) {
		if (!isdummy(t)) {
			while (t.lastfreeIndex > t.nodeIndex) {
				t.lastfreeIndex--;
				if (lobject.ttisnil(gkey(t.lastfree)))
					return t.lastfree;
			}
		}
		return null;  /* could not find a free place */
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
		TValue aux = new TValue();
		if (lobject.ttisnil(key))
			ldebug.luaG_runerror(L, "table index is nil");
		else if (lobject.ttisfloat(key)) {
			long k = 0;
			if (lvm.luaV_tointeger(key, ref k, 0)) {  /* does index fit in an integer? */
				lobject.setivalue(aux, k);
				// TODO: copy
				key = aux;  /* insert it as an integer */
			} else if (llimits.luai_numisnan(lobject.fltvalue(key)))
				ldebug.luaG_runerror(L, "table index is NaN");
			}
			mp = mainposition(t, key);
			if (!lobject.ttisnil(gval(mp)) || isdummy(t)) {  /* main position is taken? */
				Node othern;
				Node f = getfreepos(t);  /* get a free place */
				if (f == null) {  /* cannot find a free place? */
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

	/*
	** search function for short strings
	*/
	public static TValue luaH_getshortstr (Table t, TString key) {
		Node n = hashstr(t, key);
		llimits.lua_assert(key.tt == lobject.LUA_TSHRSTR);
		for (;;) {  /* check whether 'key' is somewhere in the chain */
			TValue k = gkey(n);
			if (lobject.ttisshrstring(k) && lstring.eqshrstr(lobject.tsvalue(k), key))
				return gval(n);  /* that's it */
			else {
				int nx = gnext(n);
				if (nx == 0)
					return lobject.luaO_nilobject;  /* not found */
				n += nx;
			}
		}
	}

	/*
	** "Generic" get version. (Not that generic: not valid for integers,
	** which may be in array part, nor for floats with integral values.)
	*/
	static TValue getgeneric (Table t, TValue key) {
		Node n = mainposition(t, key);
		for (;;) {  /* check whether 'key' is somewhere in the chain */
			if (luaV_rawequalobj(gkey(n), key))
      return gval(n);  /* that's it */
    else {
      int nx = gnext(n);
      if (nx == 0)
        return luaO_nilobject;  /* not found */
      n += nx;
    }
  }
}

	/*
	** main search function
	*/
	static TValue luaH_get (Table t, TValue key) {
		switch (lobject.ttype(key)) {
			case lobject.LUA_TSHRSTR:
				return luaH_getshortstr(t, lobject.tsvalue(key));
			case lobject.LUA_TNUMINT:
				return luaH_getint(t, lobject.ivalue(key));
			case lua.LUA_TNIL:
				return lobject.luaO_nilobject;
			case lobject.LUA_TNUMFLT: {
				long k = 0;
				if (lvm.luaV_tointeger(key, ref k, 0)) /* index is int? */
					return luaH_getint(t, k);  /* use specialized version */
				/* else... */
				break;
			}  /* FALLTHROUGH */
			default:
				return getgeneric(t, key);
		}
	}

	/*
	** beware: when using this function you probably need to check a GC
	** barrier and invalidate the TM cache.
	*/
	TValue luaH_set(lua_State L, Table t, TValue key) {
		TValue p = luaH_get(t, key);
  if (p != luaO_nilobject)
    return cast(TValue*, p);
  else return luaH_newkey(L, t, key);
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
