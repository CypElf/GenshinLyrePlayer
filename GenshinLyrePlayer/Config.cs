using System.Windows.Input;

namespace GenshinLyrePlayer
{
    public class Config
    {
        public string keyboardLayout { get; set; }
        public bool useAutoRoot { get; set; }
        public int? customRoot { get; set; }
        public string startKey { get; set; }
        public string stopKey { get; set; }

        public Config(string keyboardLayout = "QWERTY", bool useAutoRoot = true, int? customRoot = null, string startKey = "F6", string stopKey = "F7")
        {
            this.keyboardLayout = keyboardLayout;
            this.useAutoRoot = useAutoRoot;
            this.customRoot = customRoot;
            this.startKey = startKey;
            this.stopKey = stopKey;
        }
    }
}
