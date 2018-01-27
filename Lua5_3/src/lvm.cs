
class lvm {

	static bool cvt2num(TValue o) {
		return lobject.ttisstring(o);
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
}
