public class TestClass {
   public static void main() {
        System.out.println("Hello World");
   }
}
/*
         0: iload_1
         1: iconst_1
         2: if_icmpne     9
         5: aload_0
         6: iconst_0
         7: iaload
         8: ireturn
         9: aload_0
        10: iload_1
        11: iconst_1
        12: isub
        13: iaload
        14: aload_0
        15: iload_1
        16: iconst_1
        17: isub
        18: invokestatic  #7                  // Method findMinimum:([II)I
        21: invokestatic  #13                 // Method java/lang/Math.min:(II)I
        24: ireturn
*/

