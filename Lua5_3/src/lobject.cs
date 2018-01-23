using System;

class GCObject {
	GCObject next;
	byte tt;
	byte marked;
}

/*
** Union of all Lua values
*/
class Value {
	GCObject gc;			/* collectable objects */
	object p;				/* light userdata */
	int b;					/* booleans */
	Func<int, lua_State> f; /* light C functions */
	ulong i;				/* integer numbers */
	double n;				/* float numbers */
}

class TValue {
	Value value_;
	int tt_;
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
	TString[] hash;
	int nuse;  /* number of elements */
	int size;
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
	byte flags;  /* 1<<p means tagmethod(p) is not present */
	byte lsizenode;  /* log2 of size of 'node' array */
	uint sizearray;  /* size of 'array' array */
	TValue array;  /* array part */
	Node node;
	Node lastfree;  /* any free position is before this position */
	Table metatable;
	GCObject gclist;
}
