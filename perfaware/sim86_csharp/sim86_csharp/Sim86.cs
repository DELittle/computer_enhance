namespace sim86_csharp
{
    using System;
    using System.IO;

    public class Sim86
    {
        private static uint LoadMemoryFromFile(string fileName, InstructionStream instructionStream)
        {
            if (File.Exists(fileName))
            {
                var fileBytes = File.ReadAllBytes(fileName);
                var amountToCopy = (uint)Math.Min(fileBytes.Length, instructionStream.Memory.Length);
                instructionStream.Offset = 0;
                Array.Clear(instructionStream.Memory, 0, instructionStream.Memory.Length);
                Array.Copy(fileBytes, instructionStream.Memory, amountToCopy);
                return amountToCopy;
            }
            return 0;
        }
        
        public static void Main(string[] args)
        {
            var instructionStream = new InstructionStream(20);
            if (args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var fileName = args[i];
                    var instructionStreamSize = LoadMemoryFromFile(fileName, instructionStream);
                    if (instructionStreamSize > 0)
                    {
                        Console.Out.WriteLine($"; {fileName} disassembly");
                        Console.Out.WriteLine("bit 16");
                        Disassemble8086(instructionStream, instructionStreamSize);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unable to load simulator memory from {fileName}");
                    }
                }
            }
            else
            {
                Console.Error.WriteLine($"USAGE: sim86_charp [8086 machine code file] ...");
            }
        }

        private static void Disassemble8086(InstructionStream instructionStream, uint size)
        {
            var bytesRemaining = size;
            while (bytesRemaining > 0)
            {
                var instruction = Decode(instructionStream);
                if (instruction.Op != OperationType.none)
                {
                    if (bytesRemaining >= instruction.Size)
                    {
                        instructionStream.MoveOffset(instruction.Size);
                        bytesRemaining -= instruction.Size;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Instruction {instruction.Op} would go outside instruction stream");
                        break;
                    }
                    if ((instruction.Operands?.Length ?? 0) == 0)
                    {
                        Console.Out.WriteLine($"{instruction.Op}");
                    }
                    else if (instruction.Operands.Length == 1)
                    {
                        Console.Out.WriteLine($"{instruction.Op} {instruction.Operands[0]}");
                    }
                    else if (instruction.Operands.Length == 2)
                    {
                        Console.Out.WriteLine($"{instruction.Op} {instruction.Operands[0]}, {instruction.Operands[1]}");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unrecognized operating in instruction stream.");
                    break;
                }
            }
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
            dp_di,
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

        private static Instruction Decode(InstructionStream instructionStream)
        {
            var decodeType = GetDecodeType(instructionStream);
            if (decodeType == DecodeType.Unknown)
            {
                return new Instruction();
            }
            
            if (decodeType == DecodeType.Shared_RegisterMemoryToFromRegister)
            {
                var subOpCode = (SubOpCode)((instructionStream.AccessByte(0) >> 3) & 0b111);
                return DecodeRegMemToFromReg(SubOpCodeToOperationType(subOpCode), instructionStream);
            }
            else if (decodeType == DecodeType.MOV_RegisterMemoryToFromRegister)
            {
                return DecodeRegMemToFromReg(OperationType.mov, instructionStream);
            }
            else if (decodeType == DecodeType.MOV_ImmediateToRegister)
            {
                var offset = 0u;
                var byte1 = instructionStream.AccessByte(offset++);
                // bit 3
                var word = (byte)((byte1 >> 3) & 0b_1);
                // bits 2, 1 and 0
                var register = (byte)(byte1 & 0b_111);
                var registerOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Register,
                    RegisterCode = GetRegisterCode(register, word)
                };
                //var registerCode = RegisterCodes[word, register];
                var immediate = ReadWordOrByte(word, 0, ref offset, instructionStream);
                var immediateOperand = new InstructionOperand()
                {
                    OperandType = OperandType.Immediate,
                    Immediate = new Immediate()
                    {
                        Value = immediate
                    }
                };
                //stringBuilder.AppendLine($"mov {registerCode}, {immediate}");
                return new Instruction(instructionStream, offset, OperationType.mov, word, new[] { registerOperand, immediateOperand });
            }
            else if (decodeType == DecodeType.MOV_ImmediateToRegisterMemory)
            {
                return DecodeImmediateToRegMem(SubOpCode.mov, instructionStream);
            }
            else if (decodeType == DecodeType.Shared_ImmediateToRegMem)
            {
                return DecodeImmediateToRegMem(SubOpCode.decode, instructionStream);
            }
            else if (decodeType == DecodeType.Shared_ImmediateToFromAccumulator)
            {
                //stringBuilder.AppendLine(DecodeImmediateToAccumulator(ref currentByteIndex, instructionStream));
            }
            else if (decodeType == DecodeType.MOV_MemoryToAccumulator)
            {
                var offset = 0u;
                var byte1 = instructionStream.AccessByte(offset++);
                // bit 0
                var word = (byte)(byte1 & 0b_1);
                var memoryAddress = ReadWordOrByte(word, 0, ref offset, instructionStream);
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
                return new Instruction(instructionStream, offset, OperationType.mov, word, new []{registerOperand, memoryOperand});
            }
            else if (decodeType == DecodeType.MOV_AccumulatorToMemory)
            {
                var offset = 0u;
                var byte1 = instructionStream.AccessByte(offset++);
                // bit 0
                var word = (byte)(byte1 & 0b_1);
                var memoryAddress = ReadWordOrByte(word, 0, ref offset, instructionStream);
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
                return new Instruction(instructionStream, offset, OperationType.mov, word, new []{memoryOperand, registerOperand});
            }
            else if (IsDecodeTypeJump(decodeType))
            {
                //stringBuilder.AppendLine(DecodeJump(opcode, ref currentByteIndex, instructionStream));
            }

            return new Instruction();
        }

        public static RegisterCode GetRegisterCode(byte register, byte wide)
        {
            return (RegisterCode)(register << 1 | wide);
        }
        
        private static Instruction DecodeRegMemToFromReg(OperationType operationType, InstructionStream instructionStream)
        {
            var offset = 0u;
            var byte0 = instructionStream.AccessByte(offset++);
            // bit 1
            var direction = ((byte0 >> 1) & 0b_1) == 1;
            // bit 0
            var wide = (byte)(byte0 & 0b_1); 
            var byte1 = instructionStream.AccessByte(offset++);
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
            var registerOrMemoryOperand = DecodeRegisterOrMemory(mode, wide, registerOrMemory, ref offset, instructionStream);
            var src = direction ? registerOrMemoryOperand : registerOperand;
            var dest = direction ? registerOperand : registerOrMemoryOperand;

            var instruction = new Instruction()
            {
                Op = operationType,
                Address = instructionStream.Offset,
                Operands = new [] {dest, src},
                Size = offset
            };
            if (wide == 1)
            {
                instruction.Flags |= InstructionFlag.wide;
            }
            return instruction;
        }
        
        private static Instruction DecodeImmediateToRegMem(SubOpCode subOpCode, InstructionStream instructionStream)
        {
            var offset = 0u;
            var byte1 = instructionStream.AccessByte(offset++);
            // bit 1
            var sign = (byte)(byte1 >> 1 & 0b_1);
            // bit 0
            var word = (byte)(byte1 & 0b_1);
            var byte2 = instructionStream.AccessByte(offset++);
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
            var registerOrMemoryCode = DecodeRegisterOrMemory(mode, word, registerOrMemory, ref offset, instructionStream);
            // var explicitSizeString = string.Empty;
            // if (mode != ModeCode.RegisterMode)
            // {
            //     explicitSizeString = word == 1 ? "word " : "byte ";
            // }
            var immediate = ReadWordOrByte(word, sign, ref offset, instructionStream);
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
                Address = instructionStream.Offset,
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
        
        private static byte ReadByte(ref uint offset, InstructionStream instructionStream)
        {
            return instructionStream.AccessByte(offset++);
        }
    
        private static ushort ReadWord(ref uint offset, InstructionStream instructionStream)
        {
            var loByte = instructionStream.AccessByte(offset++);
            var hiByte = instructionStream.AccessByte(offset++);
            return (ushort)((hiByte << 8) + loByte);
        }
        
        private static ushort ReadWordOrByte(byte word, byte sign, ref uint offset, InstructionStream instructionStream)
        {
            return word == 1 && sign == 0 ? ReadWord(ref offset, instructionStream) : ReadByte(ref offset, instructionStream);
        }
        
        private static InstructionOperand DecodeRegisterOrMemory(ModeCode mode, byte wide, byte registerOrMemory, ref uint offset, InstructionStream instructionStream)
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
                    var displacement = ReadWord(ref offset, instructionStream);
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
                var displacement = (sbyte)ReadByte(ref offset, instructionStream);
                instructionOperand.OperandType = OperandType.Memory;
                instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                {
                    Memory = (MemoryCode)registerOrMemory,
                    Displacement = displacement
                };
            }
            else if (mode == ModeCode.MemoryMode16Bit)
            {
                var displacement = (short)ReadWord(ref offset, instructionStream);
                instructionOperand.OperandType = OperandType.Memory;
                instructionOperand.EffectiveAddressExpression = new EffectiveAddressExpression()
                {
                    Memory = (MemoryCode)registerOrMemory,
                    Displacement = displacement
                };
            }
            return instructionOperand;
        }

        private static DecodeType GetDecodeType(InstructionStream instructionStream)
        {
            var firstByte = instructionStream.AccessByte(0);
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

            public Instruction(InstructionStream instructionStream, uint size, OperationType operationType, byte wide, InstructionOperand[] operands)
            {
                Address = instructionStream.Offset;
                Size = size;
                Op = operationType;
                Flags = wide == 1 ? InstructionFlag.wide : InstructionFlag.none;
                Operands = operands;
            }
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
            
            public override string ToString()
            {
                string MemoryCodeString(MemoryCode memoryCode)
                {
                    return memoryCode switch
                    {
                        MemoryCode.bx_si => "bx + si",
                        MemoryCode.bx_di => "bx + di",
                        MemoryCode.bp_si => "bp + si",
                        MemoryCode.dp_di => "dp + di",
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
                        if (EffectiveAddressExpression.Memory.HasValue)
                        {
                            if (EffectiveAddressExpression.Displacement != 0)
                            {
                                return $"[{MemoryCodeString(EffectiveAddressExpression.Memory.Value)}{EffectiveAddressExpression.Displacement: + 0; - 0;}]";
                            }
                            else
                            {
                                return $"[{MemoryCodeString(EffectiveAddressExpression.Memory.Value)}]";
                            }
                        }
                        else
                        {
                            return $"[{EffectiveAddressExpression.Displacement}]";
                        }
                    case OperandType.Immediate:
                        return Immediate.Value.ToString();
                    default:
                        return "OperandError";
                }
            }
        }
        
        
        public struct Immediate
        {
            public int Value;
        }

        
        public struct EffectiveAddressExpression
        {
            public MemoryCode? Memory;
            public int Displacement;
        }
    }
}