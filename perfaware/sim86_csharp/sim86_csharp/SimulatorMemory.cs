namespace sim86_csharp
{
    public class SimulatorMemory
    {
        public readonly byte[] Memory;
        public uint Offset;

        public SimulatorMemory(int sizePow2)
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

        public void WriteValue(uint absoluteAddress, int value, bool isWide)
        {
            if (isWide)
            {
                Memory[absoluteAddress] = (byte)(value & 0xff);
                Memory[absoluteAddress+1] = (byte)((value >> 8) & 0xff);
            }
            else
            {
                Memory[absoluteAddress] = (byte)(value & 0xff);
            }
        }

        public int ReadValue(uint absoluteAddress, bool isWide)
        {
            var result = 0;
            if (isWide)
            {
                result = Memory[absoluteAddress + 1] << 8;
                result |= Memory[absoluteAddress];
            }
            else
            {
                result = Memory[absoluteAddress];
            }
            return result;
        }
    }
}