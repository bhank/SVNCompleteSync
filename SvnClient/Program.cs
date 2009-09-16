using System;
using System.Collections.Generic;
using System.Text;
using SharpSvn;
using System.Collections.ObjectModel;

namespace SvnClient
{
    class Program
    {
        static void Main(string[] args)
        {
            using (SharpSvn.SvnClient client = new SharpSvn.SvnClient())
            {
                // No args, nothing to do except show help.
                if (args.Length == 0)
                {
                    Console.WriteLine("SVNCompleteSync - add/remove changed files");
                    Console.WriteLine("Usage:");
                    Console.WriteLine(" SvnCompleteSync [path]");
                    Console.WriteLine("  -m        \"message\" (override default log message)");
                    Console.WriteLine("  -u        update directory before processing add/remove");
                    Console.WriteLine("  -user     username (optional)");
                    Console.WriteLine("  -password password (optional)");
                    return;
                }

                // first Argument : Path to folder that gets commited
                //-m Logmessage
                string path = args[0];
                string logMessage = "Added by SvnCompleteSync";
                bool isUpdate = false;
                string sUser = string.Empty;
                string sPassword = string.Empty;

                for (int i = 0; i <= args.Length - 1; i++)
                {
                    if (args[i] == "-m")
                    {
                        logMessage = args[i + 1];
                    }
                    if (args[i] == "-u")
                    {
                        isUpdate = true;
                    }
                    if (string.Compare(args[i], "-user", true) == 0)
                    {
                        sUser = args[i + 1];
                        i++; // advance past username
                    }
                    if (string.Compare(args[i], "-password", true) == 0)
                    {
                        sPassword = args[i + 1];
                        i++; // advance past password
                    }
                }

                // optionally, set authentication
                if ((sUser != string.Empty) && (sPassword != string.Empty))
                {
                    client.Authentication.Clear(); // prevents checking cached credentials
                    client.Authentication.DefaultCredentials = new System.Net.NetworkCredential(sUser, sPassword);
                }

                if (isUpdate)
                {
                    client.Update(path);
                }

                Collection<SvnStatusEventArgs> changedFiles = new Collection<SvnStatusEventArgs>();
                client.GetStatus(path, out changedFiles);

                //delete files from subversion that are not in filesystem
                //add files to subversion that are new in filesystem
                //modified files are automatically included as part of the commit

                //TODO: check remoteStatus
                foreach (SvnStatusEventArgs changedFile in changedFiles)
                {
                    if (changedFile.LocalContentStatus == SvnStatus.Missing)
                    {
                        // SVN thinks file is missing but it still exists hence
                        // a change in the case of the filename.
                        if (System.IO.File.Exists(changedFile.Path))
                        {
                            SvnDeleteArgs changed_args = new SvnDeleteArgs();
                            changed_args.KeepLocal = true;
                            client.Delete(changedFile.Path, changed_args);
                        }
                        else
                            client.Delete(changedFile.Path);
                    }
                    if (changedFile.LocalContentStatus == SvnStatus.NotVersioned)
                    {
                        client.Add(changedFile.Path);
                    }
                }

                SvnCommitArgs ca = new SvnCommitArgs();
                ca.LogMessage = logMessage;

                client.Commit(path, ca);

            }
        }
    }
}
