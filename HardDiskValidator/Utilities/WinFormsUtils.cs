using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Utilities
{
    public partial class WinFormsUtils
    {
        /// <summary>
        /// Use this method to prevent Windows themes from having a different inner-form size
        /// </summary>
        public static void SetFixedClientSize(Form form, int width, int height)
        {
            form.MaximumSize = Size.Empty; // Give some room for the form to expand
            form.MinimumSize = Size.Empty;
            form.ClientSize = new Size(width, height);
            form.MinimumSize = form.Size;
            form.MaximumSize = form.Size;
        }
    }
}
