using MessagePack;

namespace InteractiveCodeExecution.VncEvents
{
    [MessagePackObject]
    public class VncKeyboardEvent
    {
        [Key(0)]
        public int UnicodeKey { get; set; }
        [Key(1)]
        public bool KeyPressed { get; set; }
    }
}
