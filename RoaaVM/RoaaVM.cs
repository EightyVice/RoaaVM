using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RoaaVirtualMachine
{
using static JavaClass;

    internal class Frame
    {
        public object[] LocalVariables { get; set; }
        public Method CurrentMethod { get; set; }
        public int ReturnPC { get; set; }

    }
    internal class RoaaVM
    {
        public Stack<object> OperandStack { get; } = new Stack<object>();
        public Constant[] Constants { get; }
        public Class CurrentClass { get; } = new Class();

        List<object> Heap { get; } = new List<object>();

        ITraceWriter tracer;

        Stack<Frame> frames = new Stack<Frame>();
        Frame currFrame { get => frames.Peek(); }
        int currLine { get => currFrame.CurrentMethod.LineNumberTable[PC - 1]; }

        BigEndianBinaryReader reader;
        int PC { get => (int)reader.BaseStream.Position; set { if (value == -1) return; else reader.BaseStream.Position = value; } }
        void offsetPC(int offset) { Debug.WriteLine($"--> {PC + offset}"); PC += offset;  }

        object Pop() => OperandStack.Pop();
        void Push(object val) => OperandStack.Push(val);

        void CallStandards(MethodReferenceConstant methodRef)
        {
            string method_sign = methodRef.ClassInfo.Name + "." + methodRef.NameAndType.Name;

            switch (method_sign)
            {
                case "java/io/PrintStream.println": 
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(Pop().ToString());
                    Console.ResetColor();
                    break; 

                #region java.lang.Math

                case "java/lang/Math.min":
                    {
                        // Handling Overloads
                        switch (methodRef.NameAndType.Descriptor)
                        {
                            case "(II)I": { int b = (int)Pop(); int a = (int)Pop(); Push(Math.Min(a,b)); }  break; // min(int, int): int
                        }
                    }
                    break;
                #endregion
            }

        }
        public RoaaVM(JavaClass javaClass, ITraceWriter traceWriter)
        {
            ConstantPoolEntry[] constantsPool = javaClass.ConstantPool.ToArray();
            MethodInfo[] methodInfos = javaClass.Methods.ToArray();
            FieldInfo[] fieldInfos = javaClass.Fields.ToArray();

            tracer = traceWriter;
            Constants = new Constant[constantsPool.Length + 1];

            // Populate Constants
            for (int i = 0; i < constantsPool.Length; i++)
            {
                switch (constantsPool[i].Tag)
                {
                    case ConstantPoolEntry.TagEnum.ClassType:
                        var class_cp = (ClassCpInfo)constantsPool[i].CpInfo;
                        Constants[i + 1] = new ClassInfoConstant(class_cp.NameAsStr, class_cp.NameIndex);
                        break;
                    case ConstantPoolEntry.TagEnum.Utf8:
                        Constants[i + 1] = new UTF8Constant(((Utf8CpInfo)constantsPool[i].CpInfo).Value);
                        break;
                    case ConstantPoolEntry.TagEnum.MethodRef:
                        var method_cp = ((MethodRefCpInfo)constantsPool[i].CpInfo);
                        Constants[i + 1] = new MethodReferenceConstant(
                            new ClassInfoConstant(method_cp.ClassAsInfo.NameAsStr, method_cp.ClassIndex),
                            new NameAndTypeConstant(method_cp.NameAndTypeAsInfo.NameAsStr, method_cp.NameAndTypeAsInfo.DescriptorAsStr)
                        );
                        break;
                    case ConstantPoolEntry.TagEnum.FieldRef:
                        var field_cp = ((FieldRefCpInfo)constantsPool[i].CpInfo);
                        Constants[i + 1] = new FieldRefernceConstant(
                            new ClassInfoConstant(field_cp.ClassAsInfo.NameAsStr, field_cp.ClassIndex),
                            new NameAndTypeConstant(field_cp.NameAndTypeAsInfo.NameAsStr, field_cp.NameAndTypeAsInfo.DescriptorAsStr)
                        );
                        break;
                    case ConstantPoolEntry.TagEnum.String:
                        var string_cp = (StringCpInfo)constantsPool[i].CpInfo;
                        Constants[i + 1] = new StringConstant(string_cp.StringIndex, ((Utf8CpInfo)constantsPool[string_cp.StringIndex - 1].CpInfo).Value);
                        break;
                }

            }

            var this_class = (ClassInfoConstant)Constants[javaClass.ThisClass];
            // Populate Fields
            foreach (var field in fieldInfos)
            {
                CurrentClass.Fields.Add(field.NameAsStr, new Field(field.NameAsStr,
                    Descriptor.GetDescriptorFromString(((UTF8Constant)Constants[field.DescriptorIndex]).Value)));
            }

            // Populate Methods
            for (int i = 0; i < methodInfos.Length; i++)
            {
                var method_info = methodInfos[i];
                var codeAttr = (AttributeInfo.AttrBodyCode)method_info.Attributes[0].Info;

                // Build Line Number Table
                var lntAttr = (AttributeInfo.AttrBodyLineNumberTable)codeAttr.Attributes.Find(attr => attr.NameAsStr == "LineNumberTable").Info;
                var lnt = new Dictionary<int, int>();

                for (int j = 0; j < lntAttr.LineNumberTableLength; j++)
                {
                    int start_pc = lntAttr.LineNumberTable[j].StartPc;
                    int line = lntAttr.LineNumberTable[j].LineNumber;
                    int end_pc = (int)codeAttr.CodeLength;

                    if (j + 1 < lntAttr.LineNumberTableLength) // Not final one
                        end_pc = lntAttr.LineNumberTable[j + 1].StartPc;

                    for (int k = start_pc; k < end_pc; k++) lnt.Add(k, line);
                }

                // Build Local Variable Table
                LocalVar[] lctArr = null;
                var _localVarTable = new Dictionary<(int pc, int index), LocalVar>();
                if (codeAttr.Attributes.Find(attr => attr.NameAsStr == "LocalVariableTable") != null)
                {
                    var lctAttr = (AttributeInfo.AttrLocalVariableTable)codeAttr.Attributes.Find(attr => attr.NameAsStr == "LocalVariableTable").Info;
                    lctArr = new LocalVar[lctAttr.LocalVariableTableLength];
                    for (int j = 0; j < lctAttr.LocalVariableTableLength; j++)
                    {
                        var lctEntry = lctAttr.LocalVariableTable[j];

                        int start_pc = lctEntry.StartPc;
                        int length = lctEntry.Length;
                        int index = lctEntry.Index;
                        for (int pc = start_pc; pc < start_pc + length; pc++)
                        {
                            _localVarTable[(pc, index)] = new LocalVar(lctEntry.StartPc, lctEntry.Length, lctEntry.Index, lctEntry.NameIndexAsStr, lctEntry.DescriptorIndexAsStr);
                        }
                        Console.WriteLine($"{lctEntry.StartPc}\t{lctEntry.Length}\t{lctEntry.Index}\t{lctEntry.NameIndexAsStr}\t{lctEntry.DescriptorIndexAsStr}");
                    }

                }

                var method = new Method(
                                        methodInfos[i].NameAsStr,
                                        ((UTF8Constant)Constants[methodInfos[i].DescriptorIndex]).Value,
                                        codeAttr.MaxStack,
                                        codeAttr.MaxLocals,
                                        codeAttr.Code,
                                        lnt,
                                        _localVarTable
                );

                CurrentClass.Methods.Add(methodInfos[i].NameAsStr, method);
                tracer.DefineFunction(method.Name, "todo", method.ParametersNames);
            }

            // Allocate Object in Heap
            tracer.DefineClass(this_class.Name, CurrentClass.Fields.Keys.ToArray());
            Heap.Add(CurrentClass.NewObject());
        }

        public void Run(params string[] arguments)
        {
            // Find main
            if(CurrentClass.Methods.ContainsKey("Main"))
            {
                // Allocate arguments
                Push(arguments);
                InvokeStatic("Main");
            }
            else
            {
                Console.WriteLine("Can't find method Main(String[]), Terminating...");
            }
        }

        public bool InvokeStatic(string methodName)
        {
            int line = reader == null ? -1 : currLine;
            string name = methodName;

            frames.Push(new Frame());
            var method = CurrentClass.Methods[methodName];
            currFrame.LocalVariables = new object[method.MaxLocals];
            var parameters_desc = method.Descriptor.ParametersDescriptors;

            var args = new object[parameters_desc.Length];
            for (int i = parameters_desc.Length - 1; i >= 0; i--)
            {
                args[i] = Pop();
                currFrame.LocalVariables[i] = args[i];
            }
            
            currFrame.CurrentMethod = method;
            currFrame.ReturnPC = reader == null ? -1 : PC;

            tracer.Call(line, name, "static", args.Select(a => a.ToString()).ToArray());
            execute(method.Code);
            return true;
        }
        
        public void InvokeVirtual(string methodName)
        {
            int line = currLine;
            frames.Push(new Frame());
            var method = CurrentClass.Methods[methodName];
            currFrame.LocalVariables = new object[method.MaxLocals];
            var parameters_desc = method.Descriptor.ParametersDescriptors;

            var args = new object[parameters_desc.Length];
            for (int i = parameters_desc.Length - 1; i >= 0; i--)
            {
                args[i] = Pop();
                currFrame.LocalVariables[i + 1] = args[i];
            }

            currFrame.LocalVariables[0] = Pop(); // this Parameter
            tracer.Call(line, method.Name, "method", args.Select(a => a.ToString()).ToArray());
            currFrame.CurrentMethod = method;
            currFrame.ReturnPC = PC;
            execute(method.Code);
        }
        private void InvokePrintLn()
        {
            Console.WriteLine(Pop().ToString());
            //Pop(); // The out reference
        }
        private void store(int index, object val)
        {
            object oldVal = currFrame.LocalVariables[index];
            currFrame.LocalVariables[index] = val;
            tracer.Assign(currLine, currFrame.CurrentMethod.LocalVariablesTable[(PC, index)].Name, oldVal?.ToString(), val?.ToString());
        }
        private void execute(byte[] bytecode)
        {
            reader = new BigEndianBinaryReader(new MemoryStream(bytecode));

            while(PC < bytecode.Length)
            {
                PC = Convert.ToInt32(PC);
                Opcode op;
                Debug.Write($"{PC}: {op = (Opcode)reader.ReadByte()}\t");
                
                switch (op)
                {

                    #region Constants
                    case Opcode.nop: /* DO NOTHING */ break;
                    case Opcode.aconst_null: Push(null);break;
                    case Opcode.iconst_m1: Push(-1); break;
                    case Opcode.iconst_0: Push(0); break;
                    case Opcode.iconst_1: Push(1); break;
                    case Opcode.iconst_2: Push(2); break;
                    case Opcode.iconst_3: Push(3); break;
                    case Opcode.iconst_4: Push(4); break;
                    case Opcode.iconst_5: Push(5); break;
                    case Opcode.lconst_0: Push((long)0); break;
                    case Opcode.lconst_1: Push((long)1); break;
                    case Opcode.fconst_0: Push(0.0f); break;
                    case Opcode.fconst_1: Push(1.0f); break;
                    case Opcode.fconst_2: Push(2.0f); break;
                    case Opcode.dconst_0: Push(Convert.ToDouble(0.0f)); break;
                    case Opcode.dconst_1: Push(Convert.ToDouble(1.0f)); break;
                    case Opcode.bipush: Push(reader.ReadSByte()); break;
                    case Opcode.sipush: Push(reader.ReadUInt16()); break;
                    case Opcode.ldc: Push(Constants[reader.ReadByte()]); break;
                    case Opcode.ldc_w: Push(Constants[reader.ReadUInt16()]); break;
                    case Opcode.ldc2_w: /*TODO*/ break;
                    #endregion

                    #region Loads
                    case Opcode.iload:
                    case Opcode.lload:
                    case Opcode.fload:
                    case Opcode.dload:
                    case Opcode.aload:
                        Push(currFrame.LocalVariables[reader.ReadByte()]);  
                        break;

                    case Opcode.iload_0: Push(Convert.ToInt32(currFrame.LocalVariables[0])); break;
                    case Opcode.iload_1: Push(Convert.ToInt32(currFrame.LocalVariables[1])); break;
                    case Opcode.iload_2: Push(Convert.ToInt32(currFrame.LocalVariables[2])); break;
                    case Opcode.iload_3: Push(Convert.ToInt32(currFrame.LocalVariables[3])); break;

                    case Opcode.lload_0: Push(Convert.ToInt64(currFrame.LocalVariables[0])); break;
                    case Opcode.lload_1: Push(Convert.ToInt64(currFrame.LocalVariables[1])); break;
                    case Opcode.lload_2: Push(Convert.ToInt64(currFrame.LocalVariables[2])); break;
                    case Opcode.lload_3: Push(Convert.ToInt64(currFrame.LocalVariables[3])); break;

                    case Opcode.fload_0: Push(Convert.ToInt32(currFrame.LocalVariables[0])); break;
                    case Opcode.fload_1: Push(Convert.ToInt32(currFrame.LocalVariables[1])); break;
                    case Opcode.fload_2: Push(Convert.ToInt32(currFrame.LocalVariables[2])); break;
                    case Opcode.fload_3: Push(Convert.ToInt32(currFrame.LocalVariables[3])); break;

                    case Opcode.dload_0: Push(Convert.ToInt32(currFrame.LocalVariables[0])); break;
                    case Opcode.dload_1: Push(Convert.ToInt32(currFrame.LocalVariables[1])); break;
                    case Opcode.dload_2: Push(Convert.ToInt32(currFrame.LocalVariables[2])); break;
                    case Opcode.dload_3: Push(Convert.ToInt32(currFrame.LocalVariables[3])); break;

                    case Opcode.aload_0: Push(currFrame.LocalVariables[0]); break;
                    case Opcode.aload_1: Push(currFrame.LocalVariables[1]); break;
                    case Opcode.aload_2: Push(currFrame.LocalVariables[2]); break;
                    case Opcode.aload_3: Push(currFrame.LocalVariables[3]); break;

                    /*TODO: ARRAYS ELEMENTS LOADING OPCODES*/
                    case Opcode.iaload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.laload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.faload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.daload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.aaload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.baload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.caload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    case Opcode.saload: { int index = Convert.ToInt32(Pop()); var arr = (object[])Pop(); Push(arr[index]); } break;
                    #endregion

                    #region Stores
                    case Opcode.istore:
                    case Opcode.lstore:
                    case Opcode.fstore:
                    case Opcode.dstore:
                    case Opcode.astore:
                        store(reader.ReadByte(), Pop());
                        break;

                    case Opcode.istore_0: store(0, Pop());break;
                    case Opcode.istore_1: store(1, Pop());break;
                    case Opcode.istore_2: store(2, Pop());break;
                    case Opcode.istore_3: store(3, Pop());break;

                    case Opcode.lstore_0: currFrame.LocalVariables[0] = Pop(); break;
                    case Opcode.lstore_1: currFrame.LocalVariables[1] = Pop(); break;
                    case Opcode.lstore_2: currFrame.LocalVariables[2] = Pop(); break;
                    case Opcode.lstore_3: currFrame.LocalVariables[3] = Pop(); break;

                    case Opcode.fstore_0: currFrame.LocalVariables[0] = Pop(); break;
                    case Opcode.fstore_1: currFrame.LocalVariables[1] = Pop(); break;
                    case Opcode.fstore_2: currFrame.LocalVariables[2] = Pop(); break;
                    case Opcode.fstore_3: currFrame.LocalVariables[3] = Pop(); break;

                    case Opcode.dstore_0: currFrame.LocalVariables[0] = Pop(); break;
                    case Opcode.dstore_1: currFrame.LocalVariables[1] = Pop(); break;
                    case Opcode.dstore_2: currFrame.LocalVariables[2] = Pop(); break;
                    case Opcode.dstore_3: currFrame.LocalVariables[3] = Pop(); break;

                    case Opcode.astore_0: currFrame.LocalVariables[0] = Pop(); break;
                    case Opcode.astore_1: currFrame.LocalVariables[1] = Pop(); break;
                    case Opcode.astore_2: currFrame.LocalVariables[2] = Pop(); break;
                    case Opcode.astore_3: currFrame.LocalVariables[3] = Pop(); break;

                    /*TODO: Array elements storing opcodes*/
                    case Opcode.iastore: {
                            int value = Convert.ToInt32(Pop());
                            int index = Convert.ToInt32(Pop());
                            object[] arr = (object[])Pop();
                            object oldVal = arr[index];
                            arr[index] = value;
                            tracer.SetArrayElement(currLine, Heap.IndexOf(arr), index, oldVal?.ToString(), value.ToString());
                        } break;
                    #endregion

                    #region Stack
                    case Opcode.pop: Pop(); break;
                    case Opcode.pop2: Pop(); Pop(); break;
                    case Opcode.dup: Push(OperandStack.Peek()); break;
                    case Opcode.dup2: { object val1 = Pop(); object val2 = Pop(); Push(val2); Push(val1); Push(val2); Push(val1); } break;
                    /* TODO Duplicates */
                    case Opcode.swap:
                        {
                            object val1 = Pop();
                            object val2 = Pop();
                            Push(val1);
                            Push(val2);
                        }
                        break;
                    #endregion

                    #region Math
                    case Opcode.iadd: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 + val2); } break;
                    case Opcode.ladd: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 + val2); } break;
                    case Opcode.fadd: { float val2  = (float)Pop();     float val1 = (float)Pop();    Push(val1 + val2); } break;
                    case Opcode.dadd: { double val2 = Convert.ToDouble(Pop());   double val1 = Convert.ToDouble(Pop());   Push(val1 + val2); } break;

                    case Opcode.isub: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 - val2); } break;
                    case Opcode.lsub: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 - val2); } break;
                    case Opcode.fsub: { float val2  = (float)Pop();     float val1 = (float)Pop();    Push(val1 - val2); } break;
                    case Opcode.dsub: { double val2 = Convert.ToDouble(Pop());   double val1 = Convert.ToDouble(Pop());   Push(val1 - val2); } break;

                    case Opcode.imul: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 * val2); } break;
                    case Opcode.lmul: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 * val2); } break;
                    case Opcode.fmul: { float val2  = (float)Pop();     float val1 = (float)Pop();    Push(val1 * val2); } break;
                    case Opcode.dmul: { double val2 = Convert.ToDouble(Pop());   double val1 = Convert.ToDouble(Pop());   Push(val1 * val2); } break;

                    case Opcode.idiv: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 / val2); } break;
                    case Opcode.ldiv: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 / val2); } break;
                    case Opcode.fdiv: { float val2  = (float)Pop();     float val1 = (float)Pop();    Push(val1 / val2); } break;
                    case Opcode.ddiv: { double val2 = Convert.ToDouble(Pop());   double val1 = Convert.ToDouble(Pop());   Push(val1 / val2); } break;

                    case Opcode.irem: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 % val2); } break;
                    case Opcode.lrem: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 % val2); } break;
                    case Opcode.frem: { float val2  = (float)Pop();     float val1 = (float)Pop();    Push(val1 % val2); } break;
                    case Opcode.drem: { double val2 = Convert.ToDouble(Pop());   double val1 = Convert.ToDouble(Pop());   Push(val1 % val2); } break;


                    case Opcode.ineg: Push(-Convert.ToInt32(Pop()));      break;
                    case Opcode.lneg: Push(-(long)Pop());     break;
                    case Opcode.fneg: Push(-(float)Pop());    break;
                    case Opcode.dneg: Push(-Convert.ToDouble(Pop()));   break;

                    case Opcode.ishl: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 << val2); } break;
                    //case Opcode.lshl: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 << val2); } break;
                                                                                                                                                       
                    case Opcode.ishr: { int val2    = Convert.ToInt32(Pop());         int val1 = Convert.ToInt32(Pop());      Push(val1 >> val2); } break;
                    //case Opcode.lshr: { long val2   = (long)Pop();       long val1 = (long)Pop();     Push(val1 >> val2); } break;

                    case Opcode.iand: { int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); Push(val1 & val2); } break;
                    case Opcode.land: { int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); Push(val1 & val2); } break;
                    case Opcode.ior: { int val2 = Convert.ToInt32(Pop());  int val1 = Convert.ToInt32(Pop()); Push(val1 | val2); } break;
                    case Opcode.lor: { int val2 = Convert.ToInt32(Pop());  int val1 = Convert.ToInt32(Pop()); Push(val1 | val2); } break;
                    case Opcode.ixor: { int val2 = Convert.ToInt32(Pop());  int val1 = Convert.ToInt32(Pop()); Push(val1 ^ val2); } break;
                    case Opcode.iinc: { 
                            int index = reader.ReadByte();  
                            currFrame.LocalVariables[index] = Convert.ToInt32(currFrame.LocalVariables[index]) + reader.ReadByte(); 
                        } break;

                    #endregion

                    #region Conversions

                    case Opcode.l2i:
                    case Opcode.f2i:
                    case Opcode.d2i:
                        Push(Convert.ToInt32(Pop()));
                        break;

                    case Opcode.i2l:
                    case Opcode.f2l:
                    case Opcode.d2l:
                        Push(Convert.ToInt64(Pop()));
                        break;

                    case Opcode.i2f:
                    case Opcode.l2f:
                    case Opcode.d2f:
                        Push(Convert.ToSingle(Pop()));
                        break;

                    case Opcode.i2d:
                    case Opcode.l2d:
                    case Opcode.f2d:
                        Push(Convert.ToDouble(Pop()));
                        break;
                    case Opcode.i2b: Push(Convert.ToByte(Pop())); break;
                    case Opcode.i2c: Push(Convert.ToChar(Pop())); break;
                    case Opcode.i2s: Push(Convert.ToInt16(Pop())); break;
                    #endregion

                    #region Comparisons 

                    case Opcode.lcmp: { long val2 = (long)Pop(); long val1 = (long)Pop(); Push(val1.CompareTo(val2)); } break;
                    case Opcode.fcmpg:
                    case Opcode.fcmpl:
                        { float val2 = (float)Pop(); float val1 = (float)Pop(); Push(val1.CompareTo(val2)); }
                        break;

                    case Opcode.dcmpg:
                    case Opcode.dcmpl:
                        { double val2 = Convert.ToDouble(Pop()); double val1 = Convert.ToDouble(Pop()); Push(val1.CompareTo(val2)); }
                        break;

                    case Opcode.ifeq: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val == 0) offsetPC(offset - 3); } break;
                    case Opcode.ifne: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val != 0) offsetPC(offset - 3); } break;
                    case Opcode.iflt: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val < 0)  offsetPC(offset - 3); } break;
                    case Opcode.ifge: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val <= 0) offsetPC(offset - 3); } break;
                    case Opcode.ifgt: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val > 0)  offsetPC(offset - 3); } break;
                    case Opcode.ifle: { int offset = reader.ReadInt16(); int val = Convert.ToInt32(Pop()); if (val >= 0) offsetPC(offset - 3); } break;

                    case Opcode.if_icmpeq: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 == val2) offsetPC(offset - 3); } break;
                    case Opcode.if_icmpne: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 != val2) offsetPC(offset - 3); } break;
                    case Opcode.if_icmplt: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 < val2)  offsetPC(offset - 3); } break;
                    case Opcode.if_icmpge: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 >= val2) offsetPC(offset - 3); } break;
                    case Opcode.if_icmpgt: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 > val2)  offsetPC(offset - 3); } break;
                    case Opcode.if_icmple: { int offset = reader.ReadInt16(); int val2 = Convert.ToInt32(Pop()); int val1 = Convert.ToInt32(Pop()); if (val1 <= val2) offsetPC(offset - 3); } break;

                    /* TODO REFERENCS COMPARISONS */
                    #endregion

                    #region References
                    case Opcode.getstatic: reader.ReadInt16(); break;
                    case Opcode.putfield:
                        {
                            var fieldRef = (FieldRefernceConstant)Constants[reader.ReadInt16()];
                            object val = Pop();
                            Object obj = (Object)Pop();
                            object old_val = obj[fieldRef.NameAndType.Name];
                            tracer.SetField(currLine, Heap.IndexOf(obj), fieldRef.NameAndType.Name, old_val?.ToString(), val.ToString());
                            obj[fieldRef.NameAndType.Name] = val;
                        }
                        break;
                    case Opcode.getfield:
                        {
                            var field_ref = (FieldRefernceConstant)Constants[reader.ReadInt16()];
                            Object obj = (Object)Pop();
                            Push(obj[field_ref.NameAndType.Name]);
                        }
                        break;
                    case Opcode.invokevirtual:
                        {
                            int methodRefIndex = reader.ReadInt16();
                            var methodRef = (MethodReferenceConstant)Constants[methodRefIndex];

                            if (methodRef.ClassInfo.Name.StartsWith("java/"))
                                CallStandards(methodRef);
                            else
                                InvokeVirtual(methodRef.NameAndType.Name);
                        }
                        break;
                    case Opcode.invokestatic:
                        {
                            int methodRefIndex = reader.ReadInt16();
                            var methodRef = (MethodReferenceConstant)Constants[methodRefIndex];

                            if (methodRef.ClassInfo.Name.StartsWith("java/"))
                                CallStandards(methodRef);
                            else
                                InvokeStatic(methodRef.NameAndType.Name);
                            
                            //Console.WriteLine("INVOKE STATIC: " + reader.ReadInt16());
                            // 
                        }
                        break;
                    case Opcode.invokespecial: reader.ReadInt16(); break;
                    case Opcode.@new:
                        /* Todo another classes */
                        {
                            reader.ReadInt16();
                            Object obj = CurrentClass.NewObject();
                            Heap.Add(obj);
                            Push(obj);
                        }
                        break;
                    case Opcode.newarray:
                        {
                            int array_len = Convert.ToInt32(Pop());
                            int array_type = reader.ReadByte();
                            var arr = new object[array_len];

                            Push(arr);
                            Heap.Add(arr);
                        }
                        break;
                    case Opcode.arraylength: Push(((object[])Pop()).Length); break;
                    #endregion

                    #region Control
                    case Opcode.@goto:
                        offsetPC(reader.ReadInt16() - 3);
                        break;
                    case Opcode.ireturn:
                    case Opcode.dreturn:
                    case Opcode.lreturn:
                        {
                            object retVal = Pop();
                            tracer.Return(currLine, retVal);

                            Console.WriteLine($"RET>> {retVal}");
                            Push(retVal);
                            var retPC = currFrame.ReturnPC;
                            frames.Pop();
                            reader = new BigEndianBinaryReader(new MemoryStream(currFrame.CurrentMethod.Code));
                            PC = retPC;
                        }
                        return;
                    case Opcode.@return:
                        {
                            tracer.Return(currLine);
                            var retPC = currFrame.ReturnPC;
                            reader = new BigEndianBinaryReader(new MemoryStream(currFrame.CurrentMethod.Code));
                            frames.Pop();
                            PC = retPC;
                        }
                        return;
                        
                    #endregion

                    default: throw new Exception("Unsupported Opcode");
                }
                foreach (var s in OperandStack.Reverse())
                    Debug.Write($"[{s}] ");

                Debug.WriteLine("");
            }
        }
    }
}
