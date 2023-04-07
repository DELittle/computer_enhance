namespace sim86_csharp
{
    public class InstructionStream
    {
        public byte[] Memory;
        public uint Offset;

        public InstructionStream(int sizePow2)
        {
            var bytesToAllocate = 1 << sizePow2;
            Memory = new byte[bytesToAllocate];
        }

        public void MoveOffset(uint offset)
        {
            Offset += offset;
        }

        public byte AccessByte(uint offset)
        {
            return Memory[Offset + offset];
        }
        
    }
}