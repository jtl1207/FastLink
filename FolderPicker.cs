using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class FolderBrowser
{
    // C# representation of the IMalloc interface.
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000002-0000-0000-C000-000000000046")]
    private interface IMalloc
    {
        [PreserveSig]
        IntPtr Alloc([In] int cb);
        [PreserveSig]
        IntPtr Realloc([In] IntPtr pv, [In] int cb);
        [PreserveSig]
        void Free([In] IntPtr pv);
        [PreserveSig]
        int GetSize([In] IntPtr pv);
        [PreserveSig]
        int DidAlloc(IntPtr pv);
        [PreserveSig]
        void HeapMinimize();
    }

    // C# representation of struct containing scroll bar parameters
    [Serializable, StructLayout(LayoutKind.Sequential)]
    private struct SCROLLINFO
    {
        public uint cbSize;
        public uint fMask;
        public int nMin;
        public int nMax;
        public uint nPage;
        public int nPos;
        public int nTrackPos;
    }

    // Styles affecting the appearance and behaviour of the browser dialog. This is a subset of the styles 
    // available as we're not exposing the full list of options in this simple wrapper.
    // See http://msdn.microsoft.com/en-us/library/windows/desktop/bb773205%28v=vs.85%29.aspx for the complete
    // list.
    private enum BffStyles
    {
        ShowEditBox = 0x0010, // BIF_EDITBOX
        ValidateSelection = 0x0020, // BIF_VALIDATE
        NewDialogStyle = 0x0040, // BIF_NEWDIALOGSTYLE
        UsageHint = 0x0100, // BIF_UAHINT
        NoNewFolderButton = 0x0200, // BIF_NONEWFOLDERBUTTON
        IncludeFiles = 0x4000, // BIF_BROWSEINCLUDEFILES
    }

    // Delegate type used in BROWSEINFO.lpfn field.
    private delegate int BFFCALLBACK(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData);

    // Struct to pass parameters to the SHBrowseForFolder function.
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpszTitle;
        public int ulFlags;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public BFFCALLBACK lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    [DllImport("User32.DLL")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("Shell32.DLL")]
    private static extern int SHGetMalloc(out IMalloc ppMalloc);

    [DllImport("Shell32.DLL")]
    private static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, out IntPtr ppidl);

    [DllImport("Shell32.DLL")]
    private static extern int SHGetPathFromIDList(IntPtr pidl, StringBuilder Path);

    [DllImport("Shell32.DLL", CharSet = CharSet.Auto)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    private static readonly int MAX_PATH = 260;

    /// <summary>
    /// Helper function that returns the IMalloc interface used by the shell.
    /// </summary>
    private static IMalloc GetSHMalloc()
    {
        IMalloc malloc;
        SHGetMalloc(out malloc);
        return malloc;
    }

    /// <summary>
    /// Enum of CSIDLs identifying standard shell folders.
    /// </summary>
    private enum FolderID
    {
        Desktop = 0x0000,
        Printers = 0x0004,
        MyDocuments = 0x0005,
        Favorites = 0x0006,
        Recent = 0x0008,
        SendTo = 0x0009,
        StartMenu = 0x000b,
        MyComputer = 0x0011,
        NetworkNeighborhood = 0x0012,
        Templates = 0x0015,
        MyPictures = 0x0027,
        NetAndDialUpConnections = 0x0031,
    }

    // Constants for sending and receiving messages in BrowseCallBackProc.
    private const int WM_USER = 0x400;
    private const int BFFM_INITIALIZED = 1;
    private const int BFFM_SELCHANGED = 2;
    private const int BFFM_SETSELECTIONW = WM_USER + 103;
    private const int BFFM_SETEXPANDED = WM_USER + 106;

    // Constants for sending messages to a Tree-View Control.
    private const int TV_FIRST = 0x1100;
    private const int TVM_GETNEXTITEM = (TV_FIRST + 10);
    private const int TVGN_ROOT = 0x0;
    private const int TVGN_CHILD = 0x4;
    private const int TVGN_NEXTVISIBLE = 0x6;
    private const int TVGN_CARET = 0x9;

    // Constants defining scroll bar parameters to set or retrieve.
    private const int SIF_RANGE = 0x1;
    private const int SIF_PAGE = 0x2;
    private const int SIF_POS = 0x4;

    // Identifies Vertical Scrollbar.
    private const int SB_VERT = 0x1;

    // Used for vertical scroll bar message.
    private const int SB_LINEUP = 0;
    private const int SB_LINEDOWN = 1;
    private const int WM_VSCROLL = 0x115;

    // Root node of the tree view.
    private FolderID rootLocation = FolderID.Desktop;

    /// <summary>
    /// Gets or sets the descriptive text displayed above the tree view control in the dialog box.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the initial directory displayed by the dialog box.
    /// </summary>
    public string InitialDirectory { get; set; }

    /// <summary>
    /// Gets or sets the path selected by the user.
    /// </summary>
    public string SelectedPath { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog box will use the new style.
    /// </summary>
    public bool NewStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog box can be used to select files as well as folders.
    /// </summary>
    public bool IncludeFiles { get; set; }

    /// <summary>
    /// Gets or sets whether to include an edit control in the dialog box that allows the user to type the name of an item.
    /// </summary>
    public bool ShowEditBox { get; set; }

    /// <summary>
    /// Gets or sets whether to include the New Folder button in the dialog box.
    /// </summary>
    public bool ShowNewFolderButton { get; set; }

    public FolderBrowser()
    {
        Description = "Please select a folder below:";
        InitialDirectory = String.Empty;
        SelectedPath = String.Empty;
        NewStyle = true;
        IncludeFiles = false;
        ShowEditBox = false;
        ShowNewFolderButton = true;
    }

    /// <summary>
    /// Creates flags for BROWSEINFO.ulFlags based on the values of boolean member properties.
    /// </summary>
    private int GetFlags()
    {
        int ret_val = 0;
        if (NewStyle) ret_val |= (int)BffStyles.NewDialogStyle;
        if (IncludeFiles) ret_val |= (int)BffStyles.IncludeFiles;
        if (!ShowNewFolderButton) ret_val |= (int)BffStyles.NoNewFolderButton;
        if (ShowEditBox) ret_val |= (int)BffStyles.ShowEditBox;
        return ret_val;
    }

    /// <summary>
    /// Shows the folder browser dialog box.
    /// </summary>
    public DialogResult ShowDialog()
    {
        return ShowDialog(null);
    }

    /// <summary>
    /// Shows the folder browser dialog box with the specified owner window.
    /// </summary>
    public DialogResult ShowDialog(IWin32Window owner)
    {
        IntPtr pidlRoot = IntPtr.Zero;

        // Get/find an owner HWND for this dialog.
        IntPtr hWndOwner;

        if (owner != null)
        {
            hWndOwner = owner.Handle;
        }
        else
        {
            hWndOwner = GetActiveWindow();
        }

        // Get the IDL for the specific startLocation.
        SHGetSpecialFolderLocation(hWndOwner, (int)rootLocation, out pidlRoot);

        if (pidlRoot == IntPtr.Zero)
        {
            return DialogResult.Cancel;
        }

        int flags = GetFlags();

        if ((flags & (int)BffStyles.NewDialogStyle) != 0)
        {
            if (System.Threading.ApartmentState.MTA == Application.OleRequired())
                flags = flags & (~(int)BffStyles.NewDialogStyle);
        }

        IntPtr pidlRet = IntPtr.Zero;

        try
        {
            // Construct a BROWSEINFO.
            BROWSEINFO bi = new BROWSEINFO();
            IntPtr buffer = Marshal.AllocHGlobal(MAX_PATH);

            bi.pidlRoot = pidlRoot;
            bi.hwndOwner = hWndOwner;
            bi.pszDisplayName = buffer;
            bi.lpszTitle = Description;
            bi.ulFlags = flags;
            bi.lpfn = new BFFCALLBACK(FolderBrowserCallback);

            // Show the dialog.
            pidlRet = SHBrowseForFolder(ref bi);

            // Free the buffer you've allocated on the global heap.
            Marshal.FreeHGlobal(buffer);

            if (pidlRet == IntPtr.Zero)
            {
                // User clicked Cancel.
                return DialogResult.Cancel;
            }

            // Then retrieve the path from the IDList.
            StringBuilder sb = new StringBuilder(MAX_PATH);
            if (0 == SHGetPathFromIDList(pidlRet, sb))
            {
                return DialogResult.Cancel;
            }

            // Convert to a string.
            SelectedPath = sb.ToString();
        }
        finally
        {
            IMalloc malloc = GetSHMalloc();
            malloc.Free(pidlRoot);

            if (pidlRet != IntPtr.Zero)
            {
                malloc.Free(pidlRet);
            }
        }
        return DialogResult.OK;
    }

    private int FolderBrowserCallback(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData)
    {
        if (uMsg == BFFM_INITIALIZED && InitialDirectory != "")
        {
            // We get in here when the dialog is first displayed. If an initial directory
            // has been specified we will make this the selection now, and also make sure
            // that directory is expanded.
            HandleRef h = new HandleRef(null, hwnd);
            SendMessage(h, BFFM_SETSELECTIONW, 1, InitialDirectory);
            SendMessage(h, BFFM_SETEXPANDED, 1, InitialDirectory);
        }
        else if (uMsg == BFFM_SELCHANGED)
        {
            // We get in here whenever the selection in the dialog box changes. To cope with the bug in Win7 
            // (and above?) whereby the SHBrowseForFolder dialog won't always scroll the selection into view (see 
            // http://social.msdn.microsoft.com/Forums/en-US/vcgeneral/thread/a22b664e-cb30-44f4-bf77-b7a385de49f3/)
            // we follow the suggestion here: 
            // http://www.codeproject.com/Questions/179097/SHBrowseForFolder-and-SHGetPathFromIDList
            // to adjust the scroll position when the selection changes.
            IntPtr hbrowse = FindWindowEx(hwnd, IntPtr.Zero, "SHBrowseForFolder ShellNameSpace Control", null);
            IntPtr htree = FindWindowEx(hbrowse, IntPtr.Zero, "SysTreeView32", null);
            IntPtr htir = SendMessage(new HandleRef(null, htree), TVM_GETNEXTITEM, TVGN_ROOT, IntPtr.Zero);
            IntPtr htis = SendMessage(new HandleRef(null, htree), TVM_GETNEXTITEM, TVGN_CARET, IntPtr.Zero);
            IntPtr htic = SendMessage(new HandleRef(null, htree), TVM_GETNEXTITEM, TVGN_CHILD, htir);
            int count = 0;
            int pos = 0;
            for (; (int)htic != 0; htic = SendMessage(new HandleRef(null, htree), TVM_GETNEXTITEM, TVGN_NEXTVISIBLE, htic), count++)
            {
                if (htis == htic)
                    pos = count;
            }
            SCROLLINFO si = new SCROLLINFO();
            si.cbSize = (uint)Marshal.SizeOf(si);
            si.fMask = SIF_POS | SIF_RANGE | SIF_PAGE;
            GetScrollInfo(htree, SB_VERT, ref si);
            si.nPage /= 2;
            if ((pos > (int)(si.nMin + si.nPage)) && (pos <= (int)(si.nMax - si.nMin - si.nPage)))
            {
                si.nMax = si.nPos - si.nMin + (int)si.nPage;
                for (; pos < si.nMax; pos++) PostMessage(htree, WM_VSCROLL, SB_LINEUP, 0);
                for (; pos > si.nMax; pos--) PostMessage(htree, WM_VSCROLL, SB_LINEDOWN, 0);
            }
        }
        return 0;
    }

}
