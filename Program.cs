using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M1TE2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Command-line arguments (all optional):
            //   <path>          path to an .M1 session to open on startup
            //   -bg <view>      which BG view to open to: 1/2/3, all, or 312
            // e.g.  M1TE.exe level.M1 -bg 2
            string filePath = null;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string lower = a.ToLowerInvariant();

                if (lower == "-bg" || lower == "--bg")
                {
                    // value is the next argument
                    if (i + 1 < args.Length)
                    {
                        Form1.startup_bg = ParseBgView(args[++i]);
                    }
                }
                else if (lower.StartsWith("-bg=") || lower.StartsWith("--bg=") ||
                         lower.StartsWith("-bg:") || lower.StartsWith("--bg:"))
                {
                    // value is attached, e.g. -bg=2
                    int sep = a.IndexOfAny(new[] { '=', ':' });
                    Form1.startup_bg = ParseBgView(a.Substring(sep + 1));
                }
                else if (filePath == null && !a.StartsWith("-"))
                {
                    // first non-flag argument is the session file
                    filePath = a;
                }
            }

            if (filePath != null)
            {
                Form1.startup_file = filePath;
            }

            Application.Run(new Form1());
        }

        // Map a -bg value to an internal map_view index.
        // Returns -1 for unrecognised values (the default view is kept).
        static int ParseBgView(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "1": case "bg1": return 0; // BG1
                case "2": case "bg2": return 1; // BG2
                case "3": case "bg3": return 2; // BG3
                case "all": case "preview": case "composite": case "123": return 3; // toggle composite preview
                case "bg3hi": case "bg3high": case "312": return 4; // toggle BG3 high priority
                default: return -1;
            }
        }
    }
}
