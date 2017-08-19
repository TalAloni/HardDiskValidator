/* Copyright (C) 2016 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace HardDiskValidator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            WindowsIdentity windowsIdentity = null;
            try
            {
                windowsIdentity = WindowsIdentity.GetCurrent();
            }
            catch
            {
                MessageBox.Show("This program requires execution privileges", "Hard Disk Validator");
                return;
            }
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
            bool isAdministrator = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdministrator)
            {
                MessageBox.Show("This program requires administrator privileges!", "Hard Disk Validator");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
            {
                Exception ex = (Exception)e.ExceptionObject;
                HandleUnhandledException(ex);
            }
        }

        private static void HandleUnhandledException(Exception ex)
        {
            string message = String.Format("Exception: {0}: {1} Source: {2} {3}", ex.GetType(), ex.Message, ex.Source, ex.StackTrace);
            MessageBox.Show(message, "Error");
            Application.Exit();
        }
    }
}