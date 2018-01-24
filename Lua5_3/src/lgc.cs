
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
}
