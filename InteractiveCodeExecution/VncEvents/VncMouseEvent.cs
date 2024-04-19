using MessagePack;

namespace InteractiveCodeExecution.VncEvents
{
    [MessagePackObject]
    public class VncMouseEvent
    {
        [Key(0)]
        public int MouseX { get; set; }

        [Key(1)]
        public int MouseY { get; set; }

        [Key(2)]
        public bool LeftMouse { get; set; }

        [Key(3)]
        public bool RightMouse { get; set; }

        [Key(4)]
        public bool MiddleMouse { get; set; }

        [Key(5)]
        public bool ScrollDown { get; set; }

        [Key(6)]
        public bool ScrollUp { get; set; }
    }
}
