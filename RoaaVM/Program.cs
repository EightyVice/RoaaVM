// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

using Kaitai;

using RoaaVirtualMachine;

var p = Process.Start(new ProcessStartInfo()
{
    FileName = "javac",
    Arguments = "-g -encoding UTF8 TestClass.java",
    UseShellExecute = false,
});


p.WaitForExit();

JavaClass javaClass = JavaClass.FromFile("TestClass.class");

JSONTraceWriter tracer = new JSONTraceWriter();
RoaaVM VM = new RoaaVM(javaClass, tracer);

VM.InvokeStatic("main");

Console.WriteLine("=== TRACE OUTPUT ===");
Console.WriteLine(tracer);

return;
foreach(var constant in javaClass.ConstantPool)
{
    if(constant.Tag == JavaClass.ConstantPoolEntry.TagEnum.Utf8)
        Console.WriteLine($"{constant.Tag}: {((JavaClass.Utf8CpInfo)constant.CpInfo).Value}");
}// ([[Ljava.Object;I[F)