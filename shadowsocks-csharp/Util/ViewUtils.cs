using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Shadowsocks.Util
{
    public static class ViewUtils
    {
        public static IEnumerable<TControl> GetChildControls<TControl>(this Control control) where TControl : Control
        {
            if (control.Controls.Count == 0)
            {
                return Enumerable.Empty<TControl>();
            }

            var children = control.Controls.OfType<TControl>().ToList();
            return children.SelectMany(GetChildControls<TControl>).Concat(children);
        }
    }
}
