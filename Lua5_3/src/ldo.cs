
/* chain list of long jump buffers */
class lua_longjmp {
	lua_longjmp previous;
	int[] b = new int[16];
	volatile int status;  /* error code */
}
