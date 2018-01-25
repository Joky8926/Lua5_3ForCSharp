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
}
