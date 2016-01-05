using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// The Edge entry point.
public class Startup
{
    /**
     * Promotes all tray items created by the specified EXE from the toolbar customization area
     * to the toolbar itself if the user has not explicitly specified that they should never be
     * shown in the toolbar.
     *
     * The function accepts the name of the EXE as argument rather than attempt to determine it itself
     * simply because that was easier to determine Node-side.
     *
     * @param {string} The name of the EXE for which to promote tray items.
     *
     * @return {null}
     */
    public async Task<object> Invoke(string exeToPromote)
    {
        var trayFixer = new Squirrel.TrayStateChanger();
        trayFixer.PromoteTrayItems(exeToPromote);

        return null;
    }
}

/**
 * The below is copied from https://github.com/Squirrel/Squirrel.Windows/blob/a95b9853e650c2bc3e0239c6739d0363f55d20bc/src/Squirrel/TrayHelper.cs
 * except for that:
 *  - it has been pared down to only what was necessary to support `TrayStateChanger.PromoteTrayItem`
 *    (here renamed to "...Items" to more accurately reflect its effects), and
 *  - some comments have been added.
 */
namespace Squirrel
{
    public class TrayStateChanger
    {
        public void PromoteTrayItems(string exeToPromote)
        {
            var instance = new TrayNotify();

            try {
                var items = default(List<NOTIFYITEM>);
                var legacy = useLegacyInterface();

                if (legacy) {
                    items = getTrayItemsWin7(instance);
                } else {
                    items = getTrayItems(instance);
                }

                exeToPromote = exeToPromote.ToLowerInvariant();

                for (int i = 0; i < items.Count; i++) {
                    var item = items[i];
                    var exeName = item.exe_name.ToLowerInvariant();

                    // Ignore items not created by the specified EXE.
                    if (!exeName.Contains(exeToPromote)) continue;

                    // Ignore items that are not in the default state. We shouldn't overwrite the
                    // user's preference if it is to never show an item, and we don't need to overwrite
                    // the user's preference if it is to always show an item.
                    if (item.preference != NOTIFYITEM_PREFERENCE.PREFERENCE_SHOW_WHEN_ACTIVE) continue;
                    item.preference = NOTIFYITEM_PREFERENCE.PREFERENCE_SHOW_ALWAYS;

                    var writable = NOTIFYITEM_Writable.fromNotifyItem(item);
                    if (legacy) {
                        var notifier = (ITrayNotifyWin7)instance;
                        notifier.SetPreference(ref writable);
                    } else {
                        var notifier = (ITrayNotify)instance;
                        notifier.SetPreference(ref writable);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Failed to promote Tray icon: " + ex.ToString());
            } finally {
                Marshal.ReleaseComObject(instance);
            }
        }

        static List<NOTIFYITEM> getTrayItems(TrayNotify instance)
        {
            var notifier = (ITrayNotify)instance;
            var callback = new NotificationCb();
            var handle = default(ulong);

            notifier.RegisterCallback(callback, out handle);
            notifier.UnregisterCallback(handle);
            return callback.items;
        }

        static List<NOTIFYITEM> getTrayItemsWin7(TrayNotify instance)
        {
            var notifier = (ITrayNotifyWin7)instance;
            var callback = new NotificationCb();

            notifier.RegisterCallback(callback);
            notifier.RegisterCallback(null);
            return callback.items;
        }

        class NotificationCb : INotificationCb
        {
            public readonly List<NOTIFYITEM> items = new List<NOTIFYITEM>();

            public void Notify([In] uint nEvent, [In] ref NOTIFYITEM notifyItem)
            {
                items.Add(notifyItem);
            }
        }

        static bool useLegacyInterface()
        {
            var ver = Environment.OSVersion.Version;
            if (ver.Major < 6) return true;
            if (ver.Major > 6) return false;

            // Windows 6.2 and higher use new interface
            return ver.Minor <= 1;
        }
    }

    // The known values for NOTIFYITEM's dwPreference member.
    public enum NOTIFYITEM_PREFERENCE
    {
        // In Windows UI: "Only show notifications."
        PREFERENCE_SHOW_WHEN_ACTIVE = 0,
        // In Windows UI: "Hide icon and notifications."
        PREFERENCE_SHOW_NEVER = 1,
        // In Windows UI: "Show icon and notifications."
        PREFERENCE_SHOW_ALWAYS = 2
    };

    // NOTIFYITEM describes an entry in Explorer's registry of status icons.
    // Explorer keeps entries around for a process even after it exits.
    public struct NOTIFYITEM
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string exe_name;    // The file name of the creating executable.

        [MarshalAs(UnmanagedType.LPWStr)]
        public string tip;         // The last hover-text value associated with this status
                                   // item.

        public IntPtr icon;       // The icon associated with this status item.
        public IntPtr hwnd;       // The HWND associated with the status item.
        public NOTIFYITEM_PREFERENCE preference;  // Determines the behavior of the icon with respect to
                                                  // the taskbar
        public uint id;    // The ID specified by the application.  (hWnd, uID) is
                           // unique.
        public Guid guid;  // The GUID specified by the application, alternative to
                           // uID.
    };
    public struct NOTIFYITEM_Writable
    {
        public IntPtr exe_name;    // The file name of the creating executable.

        public IntPtr tip;         // The last hover-text value associated with this status
                                   // item.

        public IntPtr icon;       // The icon associated with this status item.
        public IntPtr hwnd;       // The HWND associated with the status item.
        public NOTIFYITEM_PREFERENCE preference;  // Determines the behavior of the icon with respect to
                                                  // the taskbar
        public uint id;    // The ID specified by the application.  (hWnd, uID) is
                           // unique.
        public Guid guid;  // The GUID specified by the application, alternative to
                           // uID.

        public static NOTIFYITEM_Writable fromNotifyItem(NOTIFYITEM item)
        {
            return new NOTIFYITEM_Writable {
                exe_name = Marshal.StringToCoTaskMemAuto(item.exe_name),
                tip = Marshal.StringToCoTaskMemAuto(item.tip),
                icon = item.icon,
                hwnd = item.hwnd,
                preference = item.preference,
                id = item.id,
                guid = item.guid
            };
        }
    };

    [ComImport]
    [Guid("D782CCBA-AFB0-43F1-94DB-FDA3779EACCB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INotificationCb
    {
        void Notify([In]uint nEvent, [In] ref NOTIFYITEM notifyItem);
    }

    [ComImport]
    [Guid("FB852B2C-6BAD-4605-9551-F15F87830935")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITrayNotifyWin7
    {
        void RegisterCallback([MarshalAs(UnmanagedType.Interface)]INotificationCb callback);
        void SetPreference([In] ref NOTIFYITEM_Writable notifyItem);
        void EnableAutoTray([In] bool enabled);
    }

    [ComImport]
    [Guid("D133CE13-3537-48BA-93A7-AFCD5D2053B4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITrayNotify
    {
        void RegisterCallback([MarshalAs(UnmanagedType.Interface)]INotificationCb callback, [Out] out ulong handle);
        void UnregisterCallback([In] ulong handle);
        void SetPreference([In] ref NOTIFYITEM_Writable notifyItem);
        void EnableAutoTray([In] bool enabled);
        void DoAction([In] bool enabled);
    }

    [ComImport, Guid("25DEAD04-1EAC-4911-9E3A-AD0A4AB560FD")]
    class TrayNotify { }
}
