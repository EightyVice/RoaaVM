public class TestClass {
    // Function that return average of an array.
    static double average(int a[], int n)
    {
         
        // Find sum of array element
        int sum = 0;
         
        for (int i = 0; i < n; i++)
            sum += a[i];
     
        return (double)sum / n;
    }
     
    // Driver code
    public static void Main(String[] args)
    {
         
        int arr[] = {10, 2, 3, 4, 5, 6, 7, 8, 9};
        int n = arr.length;
     
        System.out.println(average(arr, n));
    }
}



/*
    Object data;
    TestClass next;

    public TestClass(Object val) {
        data = val;
    }

    public static void main()
    {
        TestClass p = new TestClass("N");
        p.next = new TestClass("A");
        p.next.next = new TestClass("B");
        p.next.next.next = new TestClass("C");
    }
*/