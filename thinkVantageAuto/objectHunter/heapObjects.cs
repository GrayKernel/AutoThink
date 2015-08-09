using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace thinkVantageAuto
{
    public static class heapObjects
    {
        #region init
        public static char clrVersion = '2';
        public static int clrSub = 1;
        public static int constant = 50000;//can be adjusted.. need better signature :( just till true?

        public delegate IntPtr getMethodTableDel(IntPtr objectIN);

        public static object[] getAddresses(object objectIN)
        {
            //see if clr is version 2 or 4
            List<object> objectList = new List<object>();
            clrVersion = Environment.Version.ToString().ElementAt(0);

            object thisObject = objectIN;
            object foundObject = null;
            IntPtr obj = IntPtr.Zero;
            IntPtr methodTable = IntPtr.Zero;
            List<IntPtr> matchedObjects = null;
            objectList.Add(thisObject);

            if (thisObject.GetType() == typeof(foundObject))
            {
                foundObject thisFoundObject = thisObject as foundObject;
                thisObject = thisFoundObject.targetObject;
            }

            obj = getObjectAddr64(thisObject);
            
            methodTable = getObjectMethodTable(obj, getMethodTablex64);
            Console.WriteLine(methodTable.ToString("X") + " is MT");
            matchedObjects = getAllObjects(obj, methodTable, getMethodTablex64, get3rdEntryx64);

            foreach (IntPtr actualObj in matchedObjects)
            {
                if (actualObj != null)
                {
                    foundObject = GetInstance64(actualObj);
                    foundObject objTarget = new foundObject();
                    objTarget.targetObject = foundObject;
                    objTarget.name = thisObject.ToString();
                    objTarget.addrOfObj = actualObj;
                    objectList.Add(objTarget);
                }
            }
            return objectList.ToArray();
        }
        #endregion init

        #region x64

        public static IntPtr getObjectAddr64(object wantedObject)
        {
            if (wantedObject == null)
                return IntPtr.Zero;

            IntPtr objectPointer = (IntPtr)4;
            object refer = wantedObject;
            IntPtr objectPointer2 = (IntPtr)8;

            unsafe
            {
                //System.Windows.Forms.MessageBox.Show("Address of objectPointer:" + (uint)(&objectPointer) + " address of objectPointer 2 " + (uint)(&objectPointer2));
                objectPointer = *(&objectPointer + clrSub);
            }

            return objectPointer;
        }

        static public byte[] getMethodTablex64 = new byte[] 
        {
            0x48, 0x8b, 0x01, //mov rax, [rcx]
            0xc3              //ret
        };

        //call once the location of an object is known to check against it's 3rd table entry :) 
        static public byte[] get3rdEntryx64 = new byte[] 
        {
            0x48, 0x8b, 0x41, 0x08, 0x48, 0x83, 0xf8, 0x00,
            0x74, 0x03, 0x48, 0x8b,
            0x00, 0xc3
        };


        public static object GetInstance64(IntPtr wantedObject)
        {
            if (wantedObject == null)
                return IntPtr.Zero;

            IntPtr objectPointer = wantedObject;
            object refer = wantedObject.GetType();
            IntPtr objectPointer2 = (IntPtr)8;

            unsafe
            {
                //System.Windows.Forms.MessageBox.Show("Address of objectPointer:" + (uint)(&objectPointer) + " address of objectPointer 2 " + (uint)(&objectPointer2));
                *(&objectPointer + clrSub) = *(&objectPointer);
            }
            //System.Windows.Forms.MessageBox.Show(refer.ToString());
            return refer;
        }

        #endregion x64

        #region generic
        public static IntPtr getObjectMethodTable(IntPtr objectIN, byte[] methodFinderIN)
        {
            IntPtr p = assemblyHelpers.VirtualAlloc(methodFinderIN);
            IntPtr methodTable = IntPtr.Zero;
            getMethodTableDel fireShellcode = (getMethodTableDel)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(p, typeof(getMethodTableDel));

            try
            {
                uint lpflOldProtect = 0;
                assemblyHelpers.VirtualProtect(objectIN, (uint)IntPtr.Size, (uint)0x40, out lpflOldProtect);
                methodTable = fireShellcode(objectIN);
            }
            catch (System.Exception ex)
            {
                assemblyHelpers.VirtualFree(p, 0, 0x8000);
                return IntPtr.Zero;
            }
            assemblyHelpers.VirtualFree(p, 0, 0x8000);
            return methodTable;
        }

        /*Scan through heap and compare first four bytes of all objects to the method table pointer...
        requires more or less a brute force approach :( (for now) */
        public static List<IntPtr> getAllObjects(IntPtr firstObjectPointer, IntPtr methodTable, byte[] typeOfASM, byte[] entryIN)
        {
            List<IntPtr> matchedObjects = new List<IntPtr>();

            int counter = 1;
            int i = 0;
            int err = 0;
            uint lpflOldProtect = 0;
            IntPtr testObjectLocation = IntPtr.Zero;
            IntPtr testMethodTable = IntPtr.Zero;
            IntPtr test3rdEntry = IntPtr.Zero;
            IntPtr size = IntPtr.Zero;
            object WORK = null;
            IntPtr getMethodTablefuncPtr = assemblyHelpers.VirtualAlloc(typeOfASM);
            getMethodTableDel fireShellcode = (getMethodTableDel)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(getMethodTablefuncPtr, typeof(getMethodTableDel));


            IntPtr get3rdEntry = assemblyHelpers.VirtualAlloc(entryIN);
            getMethodTableDel getSecondRef = (getMethodTableDel)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(get3rdEntry, typeof(getMethodTableDel));

            IntPtr thirdTable = getSecondRef(firstObjectPointer);

            //count down first until out of the heap 
            while (true)
            {
                try
                {
                    i = counter * IntPtr.Size;
                    counter++;
                    testObjectLocation = new IntPtr(firstObjectPointer.ToInt64() - i); //get a byte value to test on for an object 
                    //  assemblyHelpers.VirtualProtect(testObjectLocation, (uint)IntPtr.Size, (uint)0x04, out lpflOldProtect);
                    testMethodTable = fireShellcode(testObjectLocation);

                    if (testMethodTable == methodTable)
                    {
                        test3rdEntry = getSecondRef(testObjectLocation);
                        if (test3rdEntry == thirdTable)
                        {
                            WORK = GetInstance64(testObjectLocation);
                            matchedObjects.Add(testObjectLocation);
                            err = 0;
                        }
                    }

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Attempted to read or write protected memory") || ex.Message.Contains("AccessViolationException"))
                    {
                        err++;
                        if (err > 20)
                            break;
                    }
                }
            }

            counter = 1;
            err = 0;
            //count down first until out of the heap 
            while (true)
            {
                try
                {
                    i = counter * IntPtr.Size;
                    counter++;
                    testObjectLocation = new IntPtr(firstObjectPointer.ToInt64() + i); //get a byte value to test on for an object 
                    assemblyHelpers.VirtualProtect(testObjectLocation, (uint)IntPtr.Size, (uint)0x04, out lpflOldProtect);
                    testMethodTable = fireShellcode(testObjectLocation);

                    if (testMethodTable == methodTable)
                    {
                        test3rdEntry = getSecondRef(testObjectLocation);
                        if (test3rdEntry == thirdTable)
                        {
                            WORK = GetInstance64(testObjectLocation);
                            matchedObjects.Add(testObjectLocation);
                            err = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Attempted to read or write protected memory") || ex.Message.Contains("AccessViolationException"))
                    {
                        err++;
                        if (err > 20)
                            break;
                    }

                }
            }
            assemblyHelpers.VirtualFree(getMethodTablefuncPtr, 0, 0x8000);
            return matchedObjects;
        }
        #endregion generic
    }
}