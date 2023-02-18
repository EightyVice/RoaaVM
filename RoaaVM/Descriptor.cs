using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoaaVirtualMachine
{
    internal abstract class Descriptor
    {
        public static Descriptor GetDescriptorFromString(string descriptorString)
        {
            int i = 0;

            ClassDescriptor classDesc(){
                ClassDescriptor ret = null;
                ret = new ClassDescriptor(descriptorString.Substring(i).Split(';')[0]);
                i += ret.ClassName.Length + 1; // 1 for the L 
                return ret;
            }
            ArrayDescriptor arrayDesc()
            {
                ArrayDescriptor ret = null;
                ret = new ArrayDescriptor(fieldDesc());
                return ret;
            }
            Descriptor fieldDesc()
            {
                switch (descriptorString[i])
                {
                    case 'B': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Byte);
                    case 'C': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Char);
                    case 'D': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Double);
                    case 'F': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Float); 
                    case 'I': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Int);
                    case 'J': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Long);
                    case 'S': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Short); 
                    case 'Z': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Boolean);
                    case 'V': i++; return new BaseTypeDescriptor(BaseTypeDescriptor.BaseType.Void);
                    case 'L': i++; return classDesc();
                    case '[': i++; return arrayDesc();
                    default: return null;
                }
            }

            if (descriptorString.StartsWith("("))   // Method Descriptor
            {
                i++; // for (

                List<Descriptor> _params = new List<Descriptor>();
                while(i < descriptorString.IndexOf(')'))
                {
                    _params.Add(fieldDesc());
                }
                i++; // for )

                var ret_desc = fieldDesc();

                return new MethodDescriptor(_params.ToArray(), ret_desc);
            }
            else // Field Descriptor
            {
                return fieldDesc();
            }
            return null;
        }
    }
    internal class MethodDescriptor : Descriptor
    {
        public Descriptor[] ParametersDescriptors { get; }
        public Descriptor ReturnDescriptor { get; }

        public MethodDescriptor(Descriptor[] parametersDescriptors, Descriptor returnDescriptor)
        {
            ParametersDescriptors = parametersDescriptors;
            ReturnDescriptor = returnDescriptor;
        }
    }
    internal class ArrayDescriptor : Descriptor
    {
        public Descriptor ComponentDescriptor { get; }

        public ArrayDescriptor(Descriptor componentDescriptor)
        {
            ComponentDescriptor = componentDescriptor;
        }
    }
    internal class ClassDescriptor : Descriptor
    {
        public string ClassName { get; }

        public ClassDescriptor(string className)
        {
            ClassName = className;
        }
    }

    internal class BaseTypeDescriptor : Descriptor
    {
        internal enum BaseType
        {
            Byte,
            Char,
            Double,
            Float,
            Int,
            Long,
            Short,
            Boolean,
            Void,
        }

        public BaseType Type { get; }

        public BaseTypeDescriptor(BaseType type)
        {
            Type = type;
        }
    }
}
