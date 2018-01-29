
class lvm {
	const int LUA_FLOORN2I = 0;

	static bool cvt2num(TValue o) {
		return lobject.ttisstring(o);
	}

	static bool tointeger(TValue o, ref long i) {
		if (lobject.ttisinteger(o)) {
			i = lobject.ivalue(o);
			return true;
		} else {
			return luaV_tointeger(o, ref i, LUA_FLOORN2I);
		}
	}

	public static void luaV_rawequalobj(TValue t1, TValue t2) {
		luaV_equalobj(null, t1, t2);
	}

	/*
	** try to convert a value to an integer, rounding according to 'mode':
	** mode == 0: accepts only integral values
	** mode == 1: takes the floor of the number
	** mode == 2: takes the ceil of the number
	*/
	public static bool luaV_tointeger(TValue obj, ref long p, int mode) {
		TValue v = new TValue();
	again:
		if (lobject.ttisfloat(obj)) {
			double n = lobject.fltvalue(obj);
			double f = luaconf.l_floor(n);
			if (n != f) {  /* not an integral value? */
				if (mode == 0)
					return false;  /* fails if mode demands integral value */
				else if (mode > 1)  /* needs ceil? */
					f += 1;  /* convert floor to ceil (remember: n != f) */
			}
			return luaconf.lua_numbertointeger(f, ref p);
		} else if (lobject.ttisinteger(obj)) {
			p = lobject.ivalue(obj);
			return true;
		} else if (cvt2num(obj) && lobject.luaO_str2num(lobject.svalue(obj), v) == lobject.vslen(obj) + 1) {
			obj = v;	// TODO: copy
			goto again;  /* convert result from 'luaO_str2num' to an integer */
		}
		return false;  /* conversion failed */
	}


	/*
	** Main operation for equality of Lua values; return 't1 == t2'.
	** L == NULL means raw equality (no metamethods)
	*/
	static bool luaV_equalobj(lua_State L, TValue t1, TValue t2) {
		TValue tm;
		if (lobject.ttype(t1) != lobject.ttype(t2)) {  /* not the same variant? */
			if (lobject.ttnov(t1) != lobject.ttnov(t2) || lobject.ttnov(t1) != lua.LUA_TNUMBER)
				return false;  /* only numbers can be equal with different variants */
			else {  /* two numbers with different variants */
				long i1 = 0, i2 = 0;  /* compare them as integers */
				return tointeger(t1, ref i1) && tointeger(t2, ref i2) && i1 == i2;
			}
		}
		/* values have same type and same variant */
		switch (lobject.ttype(t1)) {
			case lua.LUA_TNIL:
				return true;
			case lobject.LUA_TNUMINT:
				return lobject.ivalue(t1) == lobject.ivalue(t2);
			case lobject.LUA_TNUMFLT:
				return llimits.luai_numeq(lobject.fltvalue(t1), lobject.fltvalue(t2));
			case lua.LUA_TBOOLEAN:
				return lobject.bvalue(t1) == lobject.bvalue(t2);  /* true must be 1 !! */
			case lua.LUA_TLIGHTUSERDATA:
				return lobject.pvalue(t1) == lobject.pvalue(t2);
			case lobject.LUA_TLCF:
				return lobject.fvalue(t1) == lobject.fvalue(t2);
			case lobject.LUA_TSHRSTR:
				return lstring.eqshrstr(lobject.tsvalue(t1), lobject.tsvalue(t2));
			case lobject.LUA_TLNGSTR:
				return lstring.luaS_eqlngstr(lobject.tsvalue(t1), lobject.tsvalue(t2));
			case lua.LUA_TUSERDATA: {
				if (lobject.uvalue(t1) == lobject.uvalue(t2))
					return true;
				else if (L == null)
					return false;
				tm = ltm.fasttm(L, lobject.uvalue(t1).metatable, TMS.TM_EQ);
				if (tm == null)
					tm = ltm.fasttm(L, lobject.uvalue(t2).metatable, TMS.TM_EQ);
				break;  /* will try TM */
			}
			case lua.LUA_TTABLE: {
				if (lobject.hvalue(t1) == lobject.hvalue(t2)) return 1;
      else if (L == NULL) return 0;
      tm = fasttm(L, hvalue(t1)->metatable, TM_EQ);
      if (tm == NULL)
        tm = fasttm(L, hvalue(t2)->metatable, TM_EQ);
      break;  /* will try TM */
    }
    default:
      return gcvalue(t1) == gcvalue(t2);
  }
  if (tm == NULL)  /* no TM? */
    return 0;  /* objects are different */
  luaT_callTM(L, tm, t1, t2, L->top, 1);  /* call TM */
  return !l_isfalse(L->top);
}

}
