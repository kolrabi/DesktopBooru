using System;
using System.Runtime.InteropServices;

namespace Booru
{
	public static class Native
	{
		[DllImport("libgdk-win32-3.0-0.dll")]
		static extern IntPtr gdk_win32_window_get_handle(IntPtr window);

		[DllImport("libgdk-3.so")]
		static extern IntPtr gdk_x11_window_get_xid(IntPtr window);

		public static IntPtr GetDrawableNativeId(Gdk.Window window)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				IntPtr id = gdk_x11_window_get_xid (window.Handle);
				return id;
			} else if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				return gdk_win32_window_get_handle (window.Handle);
			} else {
				return IntPtr.Zero;
			}
		}
	}
}

