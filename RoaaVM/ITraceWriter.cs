using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoaaVirtualMachine
{
    internal interface ITraceWriter
    {

        /* Metadata */
        public void DefineFunction(string name, string returnType, params string[] parameters);

        /* Events */
        public void SetArrayElement(int line, int arr_id, int index, string oldVal, string newVal, string tag = null);
        public void SetField(int line, int object_id, string fieldName, string oldVal, string newVal, string tag = null);
        public void Assign(int line, string name, string oldVal, string newVal, string tag = null);
        public void Call(int line, string funcName, string tag, params string[] args);
        public void Return(int line, object returnValue = null);
        public void DefineClass(string name, string[] fields, string tag = null);

    }
}
