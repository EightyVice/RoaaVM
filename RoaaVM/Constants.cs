using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoaaVirtualMachine
{
    internal abstract class Constant { }

    internal class UTF8Constant : Constant { 
        public string Value { get; }
        public UTF8Constant(string value) => Value = value;
    }

    internal class LocalVar
    {
        public int Start { get; }
        public int Length { get; }
        public int Slot { get; }
        public string Name { get; }
        public string Descriptor { get; }
        public LocalVar(int start, int length, int slot, string name, string descriptor)
        {
            Start = start;
            Length = length;
            Slot = slot;
            Name = name;
            Descriptor = descriptor;
        }
    }

    internal class ClassInfoConstant : Constant
    {
        public string Name { get; }
        public int NameIndex { get; }

        public ClassInfoConstant(string name, int nameIndex)
        {
            Name = name;
            NameIndex = nameIndex;
        }
    }

    internal class NameAndTypeConstant : Constant
    {
        public string Name { get; }
        public string Descriptor { get; }

        public NameAndTypeConstant(string name, string descriptor)
        {
            Name = name;
            Descriptor = descriptor;
        }
    }
    internal class FieldRefernceConstant : Constant
    {
        public ClassInfoConstant ClassInfo { get; }
        public NameAndTypeConstant NameAndType { get; }

        public FieldRefernceConstant(ClassInfoConstant classInfo, NameAndTypeConstant nameAndType)
        {
            ClassInfo = classInfo;
            NameAndType = nameAndType;
        }
    }
    internal class MethodReferenceConstant : Constant
    {
        public ClassInfoConstant ClassInfo { get; }
        public NameAndTypeConstant NameAndType { get; }

        public MethodReferenceConstant(ClassInfoConstant classInfo, NameAndTypeConstant nameAndType)
        {
            ClassInfo = classInfo;
            NameAndType = nameAndType;
        }
    }
    internal class Method
    {
        public int StackCount { get; }
        public int LocalsCount { get; }
        public Dictionary<int, int> LineNumberTable { get; } = new Dictionary<int, int>();

        // TODO: flags and descriptor
        public Dictionary<(int pc, int index), LocalVar> LocalVariablesTable { get; }
        public string Name { get; }
        public ushort MaxStack { get; }
        public ushort MaxLocals { get; }
        public byte[] Code { get; }
        public string[] ParametersNames { get; }

        public string DescriptorString { get; }
        public MethodDescriptor Descriptor { get; }
        public Method(string nameAsStr, string descriptor, ushort maxStack, ushort maxLocals, byte[] code, Dictionary<int, int> lnt, Dictionary<(int pc, int index), LocalVar> lctArr)
        {
            Name = nameAsStr;
            DescriptorString = descriptor;
            Descriptor = (MethodDescriptor)RoaaVirtualMachine.Descriptor.GetDescriptorFromString(DescriptorString);
            MaxStack = maxStack;
            MaxLocals = maxLocals;
            Code = code;
            LineNumberTable = lnt;
            LocalVariablesTable = lctArr;

            List<string> p = new List<string>();
            for (int i = 0; i < Descriptor.ParametersDescriptors.Length; i++)
                p.Add(LocalVariablesTable.Values.ElementAt(i).Name);

            ParametersNames = p.ToArray();
        }
    }

    internal class Field
    {
        public string Name { get; }
        public Descriptor TypeDescriptor { get; }

        public Field(string name, Descriptor typeDescriptor)
        {
            Name = name;
            TypeDescriptor = typeDescriptor;
        }
    }
    internal class Object
    {
        Dictionary<string, object> fields = new Dictionary<string, object>();
        public object this[string fieldName]
        {
            get => fields[fieldName];
            set => fields[fieldName] = value;
        }
    }
    internal class Class
    {
        public Dictionary<string, Method> Methods { get; } = new Dictionary<string, Method>();
        public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();


        public Object NewObject()
        {
            Object obj = new Object();

            foreach(var field in Fields.Keys)
            {
                obj[field] = null;
            }

            return obj;
        }

    }
    internal class StringConstant : Constant
    {
        public int StringIndex { get; }
        public string Value { get;}

        public StringConstant(int stringIndex, string value)
        {
            StringIndex = stringIndex;
            Value = value;
        }
        public override string ToString()
        {
            return Value;
        }
    }
    internal enum Opcode
    {
        aaload = 0x32,
        aastore = 0x53,
        aconst_null = 0x01,
        aload = 0x19,
        aload_0 = 0x2A,
        aload_1 = 0x2B,
        aload_2 = 0x2C,
        aload_3 = 0x2D,
        anewarray = 0xBD,
        areturn = 0xB0,
        arraylength = 0xBE,
        astore = 0x3A,
        astore_0 = 0x4B,
        astore_1 = 0x4C,
        astore_2 = 0x4D,
        astore_3 = 0x4E,
        athrow = 0xBF,
        baload = 0x33,
        bastore = 0x54,
        bipush = 0x10,
        caload = 0x34,
        castore = 0x55,
        checkcast = 0xC0,
        d2f = 0x90,
        d2i = 0x8E,
        d2l = 0x8F,
        dadd = 0x63,
        daload = 0x31,
        dastore = 0x52,
        dcmpg = 0x98,
        dcmpl = 0x97,
        dconst_0 = 0x0E,
        dconst_1 = 0x0F,
        ddiv = 0x6F,
        dload = 0x18,
        dload_0 = 0x26,
        dload_1 = 0x27,
        dload_2 = 0x28,
        dload_3 = 0x29,
        dmul = 0x6B,
        dneg = 0x77,
        drem = 0x73,
        dreturn = 0xAF,
        dstore = 0x39,
        dstore_0 = 0x47,
        dstore_1 = 0x48,
        dstore_2 = 0x49,
        dstore_3 = 0x4A,
        dsub = 0x67,
        dup = 0x59,
        dup_x1 = 0x5A,
        dup_x2 = 0x5B,
        dup2 = 0x5C,
        dup2_x1 = 0x5D,
        dup2_x2 = 0x5E,
        f2d = 0x8D,
        f2i = 0x8B,
        f2l = 0x8C,
        fadd = 0x62,
        faload = 0x30,
        fastore = 0x51,
        fcmpg = 0x96,
        fcmpl = 0x95,
        fconst_0 = 0x0B,
        fconst_1 = 0x0C,
        fconst_2 = 0x0D,
        fdiv = 0x6E,
        fload = 0x17,
        fload_0 = 0x22,
        fload_1 = 0x23,
        fload_2 = 0x24,
        fload_3 = 0x25,
        fmul = 0x6A,
        fneg = 0x76,
        frem = 0x72,
        freturn = 0xAE,
        fstore = 0x38,
        fstore_0 = 0x43,
        fstore_1 = 0x44,
        fstore_2 = 0x45,
        fstore_3 = 0x46,
        fsub = 0x66,
        getfield = 0xB4,
        getstatic = 0xB2,
        @goto = 0xA7,
        goto_w = 0xC8,
        i2b = 0x91,
        i2c = 0x92,
        i2d = 0x87,
        i2f = 0x86,
        i2l = 0x85,
        i2s = 0x93,
        iadd = 0x60,
        iaload = 0x2E,
        iand = 0x7E,
        iastore = 0x4F,
        iconst_m1 = 0x02,
        iconst_0 = 0x03,
        iconst_1 = 0x04,
        iconst_2 = 0x05,
        iconst_3 = 0x06,
        iconst_4 = 0x07,
        iconst_5 = 0x08,
        idiv = 0x6C,
        if_acmpeq = 0xA5,
        if_acmpne = 0xA6,
        if_icmpeq = 0x9F,
        if_icmpne = 0xA0,
        if_icmplt = 0xA1,
        if_icmpge = 0xA2,
        if_icmpgt = 0xA3,
        if_icmple = 0xA4,
        ifeq = 0x99,
        ifne = 0x9A,
        iflt = 0x9B,
        ifge = 0x9C,
        ifgt = 0x9D,
        ifle = 0x9E,
        ifnonnull = 0xC7,
        ifnull = 0xC6,
        iinc = 0x84,
        iload = 0x15,
        iload_0 = 0x1A,
        iload_1 = 0x1B,
        iload_2 = 0x1C,
        iload_3 = 0x1D,
        imul = 0x68,
        ineg = 0x74,
        instanceof = 0xC1,
        invokedynamic = 0xBA,
        invokeinterface = 0xB9,
        invokespecial = 0xB7,
        invokestatic = 0xB8,
        invokevirtual = 0xB6,
        ior = 0x80,
        irem = 0x70,
        ireturn = 0xAC,
        ishl = 0x78,
        ishr = 0x7A,
        istore = 0x36,
        istore_0 = 0x3B,
        istore_1 = 0x3C,
        istore_2 = 0x3D,
        istore_3 = 0x3E,
        isub = 0x64,
        iushr = 0x7C,
        ixor = 0x82,
        jsr = 0xA8,
        jsr_w = 0xC9,
        l2d = 0x8A,
        l2f = 0x89,
        l2i = 0x88,
        ladd = 0x61,
        laload = 0x2F,
        land = 0x7F,
        lastore = 0x50,
        lcmp = 0x94,
        lconst_0 = 0x09,
        lconst_1 = 0x0A,
        ldc = 0x12,
        ldc_w = 0x13,
        ldc2_w = 0x14,
        ldiv = 0x6D,
        lload = 0x16,
        lload_0 = 0x1E,
        lload_1 = 0x1F,
        lload_2 = 0x20,
        lload_3 = 0x21,
        lmul = 0x69,
        lneg = 0x75,
        lookupswitch = 0xAB,
        lor = 0x81,
        lrem = 0x71,
        lreturn = 0xAD,
        lshl = 0x79,
        lshr = 0x7B,
        lstore = 0x37,
        lstore_0 = 0x3F,
        lstore_1 = 0x40,
        lstore_2 = 0x41,
        lstore_3 = 0x42,
        lsub = 0x65,
        lushr = 0x7D,
        lxor = 0x83,
        monitorenter = 0xC2,
        monitorexit = 0xC3,
        multianewarray = 0xC5,
        @new = 0xBB,
        newarray = 0xBC,
        nop = 0x00,
        pop = 0x57,
        pop2 = 0x58,
        putfield = 0xB5,
        putstatic = 0xB3,
        ret = 0xA9,
        @return = 0xB1,
        saload = 0x35,
        sastore = 0x56,
        sipush = 0x11,
        swap = 0x5F,
        tableswitch = 0xAA,
        wide = 0xC4,
    }
}
