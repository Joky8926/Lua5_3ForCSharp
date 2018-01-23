
/*
** Upvalues for Lua closures
*/
class UpVal {
	TValue v;		/* points to stack or to its own value */
	uint refcount;  /* reference counter */
	_union u;

	class _union {
		_struct open;
		TValue value;  /* the value (when closed) */

		class _struct {  /* (when open) */
			UpVal next;		/* linked list */
			int touched;	/* mark to avoid cycles with dead threads */
		}
	}
}
