using System.Text;

class jsystem {

	public static int strchr(StringBuilder _String, char _Ch) {
		int ret = -1;
		for (int i = 0; i < _String.Length; i++) {
			if (_String[i] == _Ch) {
				ret = i;
				break;
			}
		}
		return ret;
	}

	public static char strpbrk(string cs, string ct) {
		for (int i = 0; i < cs.Length; i++) {
			for (int j = 0; j < ct.Length; j++) {
				if (cs[i] == ct[j]) {
					return cs[i];
				}
			}
		}
		return '\0';
	}
}
