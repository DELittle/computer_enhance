namespace sim86_csharp
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public enum RegisterCode : byte
    {
        al = 0b000_0,
        cl = 0b001_0,
        dl = 0b010_0,
        bl = 0b011_0,
        ah = 0b100_0,
        ch = 0b101_0,
        dh = 0b110_0,
        bh = 0b111_0,
        ax = 0b000_1,
        cx = 0b001_1,
        dx = 0b010_1,
        bx = 0b011_1,
        sp = 0b100_1,
        bp = 0b101_1,
        si = 0b110_1,
        di = 0b111_1,
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public struct RegisterMemory
    {
        [FieldOffset(0)]
        public ushort ax;
        [FieldOffset(0)]
        public byte ah;
        [FieldOffset(1)]
        public byte al;
        [FieldOffset(2)]
        public ushort bx;
        [FieldOffset(2)]
        public byte bh;
        [FieldOffset(3)]
        public byte bl;
        [FieldOffset(4)]
        public ushort cx;
        [FieldOffset(4)]
        public byte ch;
        [FieldOffset(5)]
        public byte cl;
        [FieldOffset(6)]
        public ushort dx;
        [FieldOffset(6)]
        public byte dh;
        [FieldOffset(7)]
        public byte dl;
        [FieldOffset(8)]
        public ushort sp;
        [FieldOffset(10)]
        public ushort bp;
        [FieldOffset(12)]
        public ushort si;
        [FieldOffset(14)]
        public ushort di;
        [FieldOffset(16)]
        public ushort ip;

        [FieldOffset(18)]
        public bool SignFlag;
        [FieldOffset(19)]
        public bool ZeroFlag;

        public ushort Get(RegisterCode registerCode)
        {
            return registerCode switch
            {
                RegisterCode.al => al,
                RegisterCode.cl => cl,
                RegisterCode.dl => dl,
                RegisterCode.bl => bl,
                RegisterCode.ah => ah,
                RegisterCode.ch => ch,
                RegisterCode.dh => dh,
                RegisterCode.bh => bh,
                RegisterCode.ax => ax,
                RegisterCode.cx => cx,
                RegisterCode.dx => dx,
                RegisterCode.bx => bx,
                RegisterCode.sp => sp,
                RegisterCode.bp => bp,
                RegisterCode.si => si,
                RegisterCode.di => di,
                _ => 0
            };
        }

        public void Add(RegisterCode registerCode, int value)
        {
            Set(registerCode, Get(registerCode) + value);
            SetFlags(Get(registerCode));
        }

        public void SetFlags(ushort result)
        {
            ZeroFlag = result == 0;
            SignFlag = (result & 0x8000) == 0x8000;
        }
        
        public void Sub(RegisterCode registerCode, int value)
        {
            Set(registerCode, Get(registerCode) - value);
            SetFlags(Get(registerCode));
        }
        
        public void Cmp(RegisterCode registerCode, int value)
        {
            var result = (ushort)(Get(registerCode) - value);
            SetFlags(result);
        }

        public ushort Get(Sim86.MemoryCode memoryCode)
        {
            return memoryCode switch
            {
                Sim86.MemoryCode.bx_si => (ushort)(bx + si),
                Sim86.MemoryCode.bx_di => (ushort)(bx + di),
                Sim86.MemoryCode.bp_si => (ushort)(bp + si),
                Sim86.MemoryCode.bp_di => (ushort)(bp + di),
                Sim86.MemoryCode.si => si,
                Sim86.MemoryCode.di => di,
                Sim86.MemoryCode.bp => bp,
                Sim86.MemoryCode.bx => bx,
                _ => 0
            };
        }
        
        public void Set(RegisterCode registerCode, int value)
        {
            switch (registerCode)
            {
                case RegisterCode.al:
                    al = (byte)value;
                    break;
                case RegisterCode.cl:
                    cl = (byte)value;
                    break;
                case RegisterCode.dl:
                    dl = (byte)value;
                    break;
                case RegisterCode.bl:
                    bl = (byte)value;
                    break;
                case RegisterCode.ah:
                    ah = (byte)value;
                    break;
                case RegisterCode.ch:
                    ch = (byte)value;
                    break;
                case RegisterCode.dh:
                    dh = (byte)value;
                    break;
                case RegisterCode.bh:
                    bh = (byte)value;
                    break;
                case RegisterCode.ax:
                    ax = (ushort)value;
                    break;
                case RegisterCode.cx:
                    cx = (ushort)value;
                    break;
                case RegisterCode.dx:
                    dx = (ushort)value;
                    break;
                case RegisterCode.bx:
                    bx = (ushort)value;
                    break;
                case RegisterCode.sp:
                    sp = (ushort)value;
                    break;
                case RegisterCode.bp:
                    bp = (ushort)value;
                    break;
                case RegisterCode.si:
                    si = (ushort)value;
                    break;
                case RegisterCode.di:
                    di = (ushort)value;
                    break;
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"ax: {ax:x4} ({ax})");
            stringBuilder.AppendLine($"bx: {bx:x4} ({bx})");
            stringBuilder.AppendLine($"cx: {cx:x4} ({cx})");
            stringBuilder.AppendLine($"dx: {dx:x4} ({dx})");
            stringBuilder.AppendLine($"sp: {sp:x4} ({sp})");
            stringBuilder.AppendLine($"bp: {bp:x4} ({bp})");
            stringBuilder.AppendLine($"si: {si:x4} ({si})");
            stringBuilder.AppendLine($"di: {di:x4} ({di})");
            stringBuilder.AppendLine($"ip: {ip:x4} ({ip})");
            stringBuilder.AppendLine($"SignFlag: {SignFlag}");
            stringBuilder.AppendLine($"ZeroFlag: {ZeroFlag}");
            return stringBuilder.ToString();
        }
    }
    
    public class Sim86
    {
        private static uint LoadMemoryFromFile(string fileName, SimulatorMemory simulatorMemory)
        {
            if (File.Exists(fileName))
            {
                var fileBytes = File.ReadAllBytes(fileName);
                var amountToCopy = (uint)Math.Min(fileBytes.Length, simulatorMemory.Memory.Length);
                simulatorMemory.Offset = 0;
                Array.Clear(simulatorMemory.Memory, 0, simulatorMemory.Memory.Length);
                Array.Copy(fileBytes, simulatorMemory.Memory, amountToCopy);
                return amountToCopy;
            }
            return 0;
        }
        
        public static void Main(string[] args)
        {
            var simulatorMemory = new SimulatorMemory(20);
            if (args.Length > 0)
            {
                var shouldExecute = false;
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg == "-execute")
                    {
                        shouldExecute = true;
                        continue;
                    }
                    var registerMemory = new RegisterMemory();
                    var instructionStreamSize = LoadMemoryFromFile(arg, simulatorMemory);
                    if (instructionStreamSize > 0)
                    {
                        if (shouldExecute)
                        {
                            Console.Out.WriteLine($"; {arg} execute");
                            Execute8086(simulatorMemory, instructionStreamSize, ref registerMemory);
                            Console.Out.WriteLine(registerMemory);
                        }
                        else
                        {
                            Console.Out.WriteLine($"; {arg} disassembly");
                            Console.Out.WriteLine("bit 16");
                            Disassemble8086(simulatorMemory, instructionStreamSize);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unable to load simulator memory from {arg}");
                    }
                }
            }
            else
            {
                Console.Error.WriteLine($"USAGE: sim86_csharp [8086 machine code file] ...");
            }
        }

        private static void Execute8086(SimulatorMemory simulatorMemory, uint size, ref RegisterMemory registerMemory)
        {
            while (registerMemory.ip < size)
            {
                simulatorMemory.Offset = registerMemory.ip;
                var instruction = Decode(simulatorMemory);
                if (instruction.Op != OperationType.none)
                {
                    Console.Out.WriteLine(PrintInstruction(instruction));
                    Execute(simulatorMemory, instruction, ref registerMemory);
                }
                else
                {
                    Console.Error.WriteLine("Unrecognized operating in instruction stream.");
                    break;
                }
            }
        }

        private static void Disassemble8086(SimulatorMemory simulatorMemory, uint size)
        {
            var bytesRemaining = size;
            while (bytesRemaining > 0)
            {
                var instruction = Decode(simulatorMemory);
                if (instruction.Op != OperationType.none)
                {
                    if (bytesRemaining >= instruction.Size)
                    {
                        simulatorMemory.MoveOffset(instruction.Size);
                        bytesRemaining -= instruction.Size;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Instruction {instruction.Op} would go outside instruction stream");
                        break;
                    }
                    Console.Out.WriteLine(PrintInstruction(instruction));
                }
                else
                {
                    Console.Error.WriteLine("Unrecognized operating in instruction stream.");
                    break;
                }
            }
        }

        private static int GetSourceValue(SimulatorMemory simulatorMemory, Instruction instruction, RegisterMemory registerMemory)
        {
            var sourceValue = 0;
            var sourceOperand = instruction.Operands[1];
            switch (sourceOperand.OperandType)
            {
                case OperandType.Immediate:
                    sourceValue = sourceOperand.Immediate.Value;
                    break;
                case OperandType.Register:
                    sourceValue = registerMemory.Get(sourceOperand.RegisterCode);
                    break;
                case OperandType.Memory:
                    var calculatedAddress = sourceOperand.EffectiveAddressExpression.CalculatedAddress(registerMemory);
                    sourceValue = simulatorMemory.ReadValue(calculatedAddress, instruction.IsWide);
                    break;
            }
            return sourceValue;
        }

        private static void Execute(SimulatorMemory simulatorMemory, Instruction instruction, ref RegisterMemory registerMemory)
        {
            registerMemory.ip += (ushort)instruction.Size;
            if (instruction.Op == OperationType.mov)
            {
                var sourceValue = GetSourceValue(simulatorMemory, instruction, registerMemory);
                var destinationOperand = instruction.Operands[0];
                if (destinationOperand.OperandType == OperandType.Register)
                {
                    var destinationRegister = destinationOperand.RegisterCode;
                    registerMemory.Set(destinationRegister, sourceValue);
                }
                else if (destinationOperand.OperandType == OperandType.Memory)
                {
                    var calculatedAddress = destinationOperand.EffectiveAddressExpression.CalculatedAddress(registerMemory);
                    simulatorMemory.WriteValue(calculatedAddress, sourceValue, instruction.IsWide);
                }
            }
            if (instruction.Op == OperationType.add)
            {
                var sourceValue = GetSourceValue(simulatorMemory, instruction, registerMemory);
                var destinationOperand = instruction.Operands[0];
                if (destinationOperand.OperandType == OperandType.Register)
                {
                    var destinationRegister = destinationOperand.RegisterCode;
                    registerMemory.Add(destinationRegister, sourceValue);
                }
            }
            if (instruction.Op == OperationType.sub)
            {
                var sourceValue = GetSourceValue(simulatorMemory, instruction, registerMemory);
                var destinationOperand = instruction.Operands[0];
                if (destinationOperand.OperandType == OperandType.Register)
                {
                    var destinationRegister = destinationOperand.RegisterCode;
                    registerMemory.Sub(destinationRegister, sourceValue);
                }
            }
            if (instruction.Op == OperationType.cmp)
            {
                var sourceValue = GetSourceValue(simulatorMemory, instruction, registerMemory);
                var destinationOperand = instruction.Operands[0];
                if (destinationOperand.OperandType == OperandType.Register)
                {
                    var destinationRegister = destinationOperand.RegisterCode;
                    registerMemory.Cmp(destinationRegister, sourceValue);
                }
            }
            if (instruction.Op == OperationType.jnz)
            {
                if (registerMemory.ZeroFlag == false)
                {
                    var offset = instruction.Operands[0].Immediate.Value;
                    if (offset < 0)
                    {
                        registerMemory.ip -= (ushort)Math.Abs(offset);
                    }
                    else
                    {
                        registerMemory.ip += (ushort)offset;
                    }
                }
            }
        }
        
        private static string PrintInstruction(Instruction instruction)
        {
            if ((instruction.Operands?.Length ?? 0) == 0)
            {
                return $"{instruction.Op}";
            }
            else if (instruction.Operands.Length == 1)
            {
                return $"{instruction.Op} {instruction.Operands[0].ToString(instruction)}";
            }
            else if (instruction.Operands.Length == 2)
            {
                return $"{instruction.Op} {instruction.Operands[0].ToString(instruction)}, {instruction.Operands[1].ToString(instruction)}";
            }
            return "unable to print instruction";
        }
        
        private const byte DirectAddressCode = 0b110;
        private const byte SharedRegMemToFromRegisterMask = 0b1100_0100;
        private const byte SharedImmediateToFromRegMemMask = 0b1111_1100;
        private const byte SharedImmediateToFromAccumulatorMask = 0b1100_0110;
        
        private enum ModeCode : byte
        {
            MemoryMode = 0b00,
            MemoryMode8Bit = 0b01,
            MemoryMode16Bit = 0b10,
            RegisterMode = 0b11
        }


        
        public enum SubOpCode : byte
        {
            decode = 254,
            mov = 255,
            add = 0b000,
            or  = 0b001,
            adc = 0b010,
            sbb = 0b011,
            and = 0b100,
            sub = 0b101,
            cmp = 0b111,
        }

        public enum MemoryCode : byte
        {
            bx_si,
            bx_di,
            bp_si,
            bp_di,
            si,
            di,
            bp,
            bx
        }

        public static OperationType SubOpCodeToOperationType(SubOpCode subOpCode)
        {
            return subOpCode switch
            {
                SubOpCode.decode => OperationType.none,
                SubOpCode.mov => OperationType.mov,
                SubOpCode.add => OperationType.add,
                SubOpCode.or => OperationType.or,
                SubOpCode.adc => OperationType.adc,
                SubOpCode.sbb => OperationType.sbb,
                SubOpCode.and => OperationType.and,
                SubOpCode.sub => OperationType.sub,
                SubOpCode.cmp => OperationType.cmp,
                _ => OperationType.none,
            };
        }

        private static Instruction Decode(SimulatorMemory simulatorMemory)
        {
            var decodeType = GetDecodeType(simulatorMemory);
            if (decodeType == DecodeType.Unknown)
            {
                return new Instruction();
            }
            
            if (decodeType == DecodeType.Shared_RegisterMemoryToFromRegister)
            {
                var subOpCode = (SubOpCode)((simulatorMemory.AccessByte(0) >> 3) & 0b111);
                return DecodeRegMemToFromReg(SubOpCodeToOperationType(subOpCode), simulatorMemory);
            }
            else if (decodeType == DecodeType.MOV_RegisterMemoryToFromRegister)
            {
                return DecodeRegMemToFromReg(OperationType.mov, simulatorMemory);
            }
            else if (decodeType == DecodeType.MOV_ImmediateToRegister)
            {
                var offset = 0u;
                var byte1 = simulatorMemory.AccessByte(offset++);
                // bit 3
                var word = (byte)((byte1 >> 3) & 0b_1);
                // bits 2, 1 and 0
                var register = (byte)(byte1 & 0b_111);
                var registerOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Register,
                    RegisterCode = GetRegisterCode(register, word)
                };
                var immediate = ReadSWordOrByte(word, 0, ref offset, simulatorMemory);
                var immediateOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Immediate,
                    Immediate = new Immediate()
                    {
                        Value = immediate
                    }
                };
                return new Instruction(simulatorMemory, offset, OperationType.mov, word, new[] { registerOperand, immediateOperand });
            }
            else if (decodeType == DecodeType.MOV_ImmediateToRegisterMemory)
            {
                return DecodeImmediateToRegMem(SubOpCode.mov, simulatorMemory);
            }
            else if (decodeType == DecodeType.Shared_ImmediateToRegMem)
            {
                return DecodeImmediateToRegMem(SubOpCode.decode, simulatorMemory);
            }
            else if (decodeType == DecodeType.Shared_ImmediateToFromAccumulator)
            {
                return DecodeImmediateToAccumulator(simulatorMemory);
            }
            else if (decodeType == DecodeType.MOV_MemoryToAccumulator)
            {
                var offset = 0u;
                var byte1 = simulatorMemory.AccessByte(offset++);
                // bit 0
                var word = (byte)(byte1 & 0b_1);
                var memoryAddress = ReadUWordOrByte(word, 0, ref offset, simulatorMemory);
                var registerOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Register,
                    RegisterCode = RegisterCode.ax
                };
                var memoryOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Memory,
                    EffectiveAddressExpression = new EffectiveAddressExpression()
                    {
                        Displacement = memoryAddress
                    }
                };
                return new Instruction(simulatorMemory, offset, OperationType.mov, word, new []{registerOperand, memoryOperand});
            }
            else if (decodeType == DecodeType.MOV_AccumulatorToMemory)
            {
                var offset = 0u;
                var byte1 = simulatorMemory.AccessByte(offset++);
                // bit 0
                var word = (byte)(byte1 & 0b_1);
                var memoryAddress = ReadUWordOrByte(word, 0, ref offset, simulatorMemory);
                var registerOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Register,
                    RegisterCode = RegisterCode.ax
                };
                var memoryOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Memory,
                    EffectiveAddressExpression = new EffectiveAddressExpression()
                    {
                        Displacement = memoryAddress
                    }
                };
                return new Instruction(simulatorMemory, offset, OperationType.mov, word, new []{memoryOperand, registerOperand});
            }
            else if (IsDecodeTypeJump(decodeType))
            {
                uint offset = 1;
                var delta = (sbyte)(ReadByte(ref offset, simulatorMemory));
                var operand = new InstructionOperand()
                {
                    OperandType = OperandType.Immediate,
                    Immediate = new Immediate()
                    {
                        Value = delta,
                        IsRelative = true
                    }
                };
                return new Instruction(simulatorMemory, offset, JumpDecodeToOperationType(decodeType), 0, new[] { operand });
            }

            return new Instruction();
        }

        public static RegisterCode GetRegisterCode(byte register, byte wide)
        {
            return (RegisterCode)(register << 1 | wide);
        }
        
        private static Instruction DecodeRegMemToFromReg(OperationType operationType, SimulatorMemory simulatorMemory)
        {
            var offset = 0u;
            var byte0 = simulatorMemory.AccessByte(offset++);
            // bit 1
            var direction = ((byte0 >> 1) & 0b_1) == 1;
            // bit 0
            var wide = (byte)(byte0 & 0b_1); 
            var byte1 = simulatorMemory.AccessByte(offset++);
            // bits 7 and 6
            var mode = (ModeCode)((byte1 >> 6) & 0b_11);
            // bits 5, 4 and 3
            var register = (byte)((byte1 >> 3) & 0b_111); 
            // bits 2, 1 and 0
            var registerOrMemory = (byte)(byte1 & 0b_111);
            var registerOperand = new InstructionOperand
            {
                OperandType = OperandType.Register,
                RegisterCode = GetRegisterCode(register, wide)
            };
            var registerOrMemoryOperand = DecodeRegisterOrMemory(mode, wide, registerOrMemory, ref offset, simulatorMemory);
            var src = direction ? registerOrMemoryOperand : registerOperand;
            var dest = direction ? registerOperand : registerOrMemoryOperand;

            var instruction = new Instruction()
            {
                Op = operationType,
                Address = simulatorMemory.Offset,
                Operands = new [] {dest, src},
                Size = offset
            };
            if (wide == 1)
            {
                instruction.Flags |= InstructionFlag.wide;
            }
            return instruction;
        }
        
        private static Instruction DecodeImmediateToAccumulator(SimulatorMemory simulatorMemory)
        {
            var offset = 0u;
            var byte1 = simulatorMemory.AccessByte(offset++);
            var word = (byte)(byte1 & 0b1);
            var subOpCode = (SubOpCode)((byte1 >> 3) & 0b111);
            var immediate = ReadSWordOrByte(word, 0, ref offset, simulatorMemory);
            var registerOperand = new InstructionOperand()
            {
                OperandType = OperandType.Register,
                RegisterCode = RegisterCode.ax
            };
            var immediateOperand = new InstructionOperand()
            {
                OperandType = OperandType.Immediate,
                Immediate = new Immediate()
                {
                    Value = immediate
                }
            };
            return new Instruction(simulatorMemory, offset, SubOpCodeToOperationType(subOpCode), word, new[] { registerOperand, immediateOperand });
        }
        
        private static Instruction DecodeImmediateToRegMem(SubOpCode subOpCode, SimulatorMemory simulatorMemory)
        {
            var offset = 0u;
            var byte1 = simulatorMemory.AccessByte(offset++);
            // bit 1
            var sign = (byte)(byte1 >> 1 & 0b_1);
            // bit 0
            var word = (byte)(byte1 & 0b_1);
            var byte2 = simulatorMemory.AccessByte(offset++);
            if (subOpCode == SubOpCode.decode)
            {
                // bits 5, 4  and 3
                subOpCode = (SubOpCode)(byte2 >> 3 & 0b111);
            }
            else
            {
                sign = 0;
            }
            // bits 7 and 6
            var mode = (ModeCode)((byte2 >> 6) & 0b_11);
            // bits 2, 1 and 0
            var registerOrMemory = (byte)(byte2 & 0b_111);
            var registerOrMemoryCode = DecodeRegisterOrMemory(mode, word, registerOrMemory, ref offset, simulatorMemory);
            var immediate = ReadSWordOrByte(word, sign, ref offset, simulatorMemory);
            var immediateOperand = new InstructionOperand()
            {
                OperandType = OperandType.Immediate,
                Immediate = new Immediate()
                {
                    Value = immediate
                }
            };
            var instruction = new Instruction()
            {
                Address = simulatorMemory.Offset,
                Op = SubOpCodeToOperationType(subOpCode),
                Size = offset,
                Operands = new []{ registerOrMemoryCode, immediateOperand }
            };
            if (word == 1)
            {
                instruction.Flags |= InstructionFlag.wide;
            }
            return instruction;
        }
        
        private static byte ReadByte(ref uint offset, SimulatorMemory simulatorMemory)
        {
            return simulatorMemory.AccessByte(offset++);
        }
    
        private static ushort ReadWord(ref uint offset, SimulatorMemory simulatorMemory)
        {
            var loByte = simulatorMemory.AccessByte(offset++);
            var hiByte = simulatorMemory.AccessByte(offset++);
            return (ushort)((hiByte << 8) + loByte);
        }
        
        private static ushort ReadUWordOrByte(byte word, byte sign, ref uint offset, SimulatorMemory simulatorMemory)
        {
            return word == 1 && sign == 0 ? ReadWord(ref offset, simulatorMemory) : ReadByte(ref offset, simulatorMemory);
        }

        private static short ReadSWordOrByte(byte word, byte sign, ref uint offset, SimulatorMemory simulatorMemory)
        {
            return word == 1 && sign == 0 ? (short)ReadWord(ref offset, simulatorMemory) : (sbyte)ReadByte(ref offset, simulatorMemory);
        }
        
        private static InstructionOperand DecodeRegisterOrMemory(ModeCode mode, byte wide, byte registerOrMemory, ref uint offset, SimulatorMemory simulatorMemory)
        {
            var instructionOperand = new InstructionOperand();
            if (mode == ModeCode.RegisterMode)
            {
                instructionOperand.OperandType = OperandType.Register;
                instructionOperand.RegisterCode = GetRegisterCode(registerOrMemory, wide);
            }
            else if (mode == ModeCode.MemoryMode)
            {
                if (registerOrMemory == DirectAddressCode)
                {
                    var displacement = ReadWord(ref offset, simulatorMemory);
                    instructionOperand.OperandType = OperandType.Memory;
                    instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                    {
                        Memory = null,
                        Displacement = displacement
                    };
                }
                else
                {
                    instructionOperand.OperandType = OperandType.Memory;
                    instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                    {
                        Memory = (MemoryCode)registerOrMemory,
                        Displacement = 0
                    };
                }
            }
            else if (mode == ModeCode.MemoryMode8Bit)
            {
                var displacement = (sbyte)ReadByte(ref offset, simulatorMemory);
                instructionOperand.OperandType = OperandType.Memory;
                instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                {
                    Memory = (MemoryCode)registerOrMemory,
                    Displacement = displacement
                };
            }
            else if (mode == ModeCode.MemoryMode16Bit)
            {
                var displacement = (short)ReadWord(ref offset, simulatorMemory);
                instructionOperand.OperandType = OperandType.Memory;
                instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                {
                    Memory = (MemoryCode)registerOrMemory,
                    Displacement = displacement
                };
            }
            return instructionOperand;
        }

        private static DecodeType GetDecodeType(SimulatorMemory simulatorMemory)
        {
            var firstByte = simulatorMemory.AccessByte(0);
            var castOpCode = (DecodeType)firstByte;
            if(IsDecodeTypeJump(castOpCode))
            {
                return castOpCode;
            }
            if ((firstByte & SharedImmediateToFromRegMemMask) == 0b1000_0000)
            {
                return DecodeType.Shared_ImmediateToRegMem;
            }
            if ((firstByte & SharedImmediateToFromAccumulatorMask) == 0b0000_0100)
            {
                return DecodeType.Shared_ImmediateToFromAccumulator;
            }
            if ((firstByte & SharedRegMemToFromRegisterMask) == 0)
            {
                return DecodeType.Shared_RegisterMemoryToFromRegister;
            }
            if ((firstByte & 0b_1111_0000) == (byte)DecodeType.MOV_ImmediateToRegister)
            {
                return DecodeType.MOV_ImmediateToRegister;
            }
            if ((firstByte & 0b_1111_1100) == (byte)DecodeType.MOV_RegisterMemoryToFromRegister)
            {
                return DecodeType.MOV_RegisterMemoryToFromRegister;
            }
            var sevenBitMask = firstByte & 0b_1111_1110;
            return sevenBitMask switch
            {
                (byte)DecodeType.MOV_ImmediateToRegisterMemory => DecodeType.MOV_ImmediateToRegisterMemory,
                (byte)DecodeType.MOV_MemoryToAccumulator => DecodeType.MOV_MemoryToAccumulator,
                (byte)DecodeType.MOV_AccumulatorToMemory => DecodeType.MOV_AccumulatorToMemory,
                _ => DecodeType.Unknown
            };
        }
        
        private static bool IsDecodeTypeJump(DecodeType opCode)
        {
            return opCode switch
            {
                >= DecodeType.jo and <= DecodeType.jg => true,
                >= DecodeType.loopnz and <= DecodeType.jcxz => true,
                _ => false
            };
        }

        public static OperationType JumpDecodeToOperationType(DecodeType decodeType)
        {
            var operationType = decodeType switch
            {
                DecodeType.jo => OperationType.jo,
                DecodeType.jno => OperationType.jno,
                DecodeType.jb => OperationType.jb,
                DecodeType.jnb => OperationType.jnb,
                DecodeType.je => OperationType.je,
                DecodeType.jnz => OperationType.jnz,
                DecodeType.jbe => OperationType.jbe,
                DecodeType.ja => OperationType.ja,
                DecodeType.js => OperationType.js,
                DecodeType.jns => OperationType.jns,
                DecodeType.jp => OperationType.jp,
                DecodeType.jnp => OperationType.jnp,
                DecodeType.jl => OperationType.jl,
                DecodeType.jnl => OperationType.jnl,
                DecodeType.jle => OperationType.jle,
                DecodeType.jg => OperationType.jg,
                DecodeType.loopnz => OperationType.loopnz,
                DecodeType.loopz => OperationType.loopz,
                DecodeType.loop => OperationType.loop,
                DecodeType.jcxz => OperationType.jcxz,
                _ => OperationType.none
            };
            return operationType;
        }

        public enum DecodeType : byte
        {
            Unknown = 0,
            MOV_RegisterMemoryToFromRegister = 0b1000_1000,
            MOV_ImmediateToRegister = 0b1011_0000,
            MOV_ImmediateToRegisterMemory = 0b1100_0110,
            MOV_MemoryToAccumulator = 0b1010_0000,
            MOV_AccumulatorToMemory = 0b1010_0010,
            Shared_RegisterMemoryToFromRegister = 0b0000_1111, // Not used
            Shared_ImmediateToRegMem = 0b0110_1000, // Not used
            Shared_ImmediateToFromAccumulator = 0b0110_1001, // Not used
            jo     = 0b0111_0000,
            jno    = 0b0111_0001,
            jb     = 0b0111_0010,
            jnb    = 0b0111_0011,
            je     = 0b0111_0100,
            jnz    = 0b0111_0101,
            jbe    = 0b0111_0110,
            ja     = 0b0111_0111,
            js     = 0b0111_1000,
            jns    = 0b0111_1001,
            jp     = 0b0111_1010,
            jnp    = 0b0111_1011,
            jl     = 0b0111_1100,
            jnl    = 0b0111_1101,
            jle    = 0b0111_1110,
            jg     = 0b0111_1111,
            loopnz = 0b1110_0000,
            loopz  = 0b1110_0001,
            loop   = 0b1110_0010,
            jcxz   = 0b1110_0011,
        }

        public struct Instruction
        {
            public uint Address;
            public uint Size;
            public OperationType Op;
            public InstructionFlag Flags;
            public InstructionOperand[] Operands;

            public Instruction(SimulatorMemory simulatorMemory, uint size, OperationType operationType, byte wide, InstructionOperand[] operands)
            {
                Address = simulatorMemory.Offset;
                Size = size;
                Op = operationType;
                Flags = wide == 1 ? InstructionFlag.wide : InstructionFlag.none;
                Operands = operands;
            }

            public bool HasRegisterOperand()
            {
                if (Operands == null)
                {
                    return false;
                }
                for (int i = 0; i < Operands.Length; i++)
                {
                    if (Operands[i].OperandType == OperandType.Register)
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool IsWide => (Flags & InstructionFlag.wide) == InstructionFlag.wide;
        }

        public enum OperationType
        {
            none,
            mov,
            add,
            or,
            adc,
            sbb,
            and,
            sub,
            cmp,
            jo,    
            jno,   
            jb,    
            jnb,   
            je,    
            jnz,   
            jbe,   
            ja,    
            js,    
            jns,   
            jp,    
            jnp,   
            jl,    
            jnl,   
            jle,   
            jg,    
            loopnz,
            loopz, 
            loop,  
            jcxz,  
        }

        public enum OperandType
        {
            None,
            Register,
            Memory,
            Immediate
        }

        [Flags]
        public enum InstructionFlag
        {
            none,
            wide = 1 << 0,
        }
        
        public struct InstructionOperand
        {
            public OperandType OperandType;
            public RegisterCode RegisterCode;
            public EffectiveAddressExpression EffectiveAddressExpression;
            public Immediate Immediate;

            public string ToString(Instruction instruction)
            {
                string MemoryCodeString(MemoryCode memoryCode)
                {
                    return memoryCode switch
                    {
                        MemoryCode.bx_si => "bx + si",
                        MemoryCode.bx_di => "bx + di",
                        MemoryCode.bp_si => "bp + si",
                        MemoryCode.bp_di => "bp + di",
                        MemoryCode.si => "si",
                        MemoryCode.di => "di",
                        MemoryCode.bp => "bp",
                        MemoryCode.bx => "bx",
                        _ => "memory_code_error"
                    };
                }
                switch (OperandType)
                {
                    case OperandType.None:
                        return "OperandError";
                    case OperandType.Register:
                        return RegisterCode.ToString();
                    case OperandType.Memory:
                        var explicitSizeString = string.Empty;
                        if (instruction.HasRegisterOperand() == false)
                        {
                            var wide = (instruction.Flags & InstructionFlag.wide) == InstructionFlag.wide;
                            explicitSizeString = wide ? "word " : "byte ";
                        }
                        if (EffectiveAddressExpression.Memory.HasValue)
                        {
                            if (EffectiveAddressExpression.Displacement != 0)
                            {
                                return $"{explicitSizeString}[{MemoryCodeString(EffectiveAddressExpression.Memory.Value)}{EffectiveAddressExpression.Displacement: + 0; - 0;}]";
                            }
                            else
                            {
                                return $"{explicitSizeString}[{MemoryCodeString(EffectiveAddressExpression.Memory.Value)}]";
                            }
                        }
                        else
                        {
                            return $"{explicitSizeString}[{EffectiveAddressExpression.Displacement}]";
                        }
                    case OperandType.Immediate:
                        if (Immediate.IsRelative)
                        {
                            return $"${instruction.Size+Immediate.Value:+0;-0;+0}";
                        }
                        return $"{Immediate.Value}";
                    default:
                        return "OperandError";
                }
            }
            
            public override string ToString()
            {
                return ToString(default);
            }
        }
        
        
        public struct Immediate
        {
            public int Value;
            public bool IsRelative;
        }

        
        public struct EffectiveAddressExpression
        {
            public MemoryCode? Memory;
            public int Displacement;

            public uint CalculatedAddress(RegisterMemory registerMemory)
            {
                var calculatedAddress = (uint)Displacement;
                if (Memory.HasValue)
                {
                    calculatedAddress += registerMemory.Get(Memory.Value);
                }
                return calculatedAddress;
            }
        }
    }
}