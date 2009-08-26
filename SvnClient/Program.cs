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
                //[Reflection.Assembly]::LoadFile("C:\Programme\SharpSvn\SharpSvn.dll")
                //$client = new-object SharpSvn.SvnClient
                //$outStatus = new-object Collection<SharpSvn.SvnStatusEventArgs> 

                //$ca = new-object  SharpSvn.SvnCommitArgs
                //$ca.LogMessage = "Moved from.txt to new.txt"
                //$client.Commit("d:\projekte\rc_trunk\b2bShop_www\Model",$ca ) 
                try
                {

                    if (args.Length == 0)
                    {
                        Console.WriteLine("SVNCompleteSync");
                        Console.WriteLine("Usage:");
                        Console.WriteLine("add/remove all changed files");
                        Console.WriteLine("SvnCompleteSync [path] -m Logmessage");
                        Console.WriteLine("update directory");
                        Console.WriteLine("SvnCompleteSync [path] -u");
                    }
                    // first Argument : Path to folder that gets commited
                    //-m Logmessage
                    string path = args[0];
                    string logMessage = "Added by SvnCompleteSync";
                    bool isUpdate = false;

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
                    }
                    if (isUpdate)
                    {
                        client.Update(path);
                        return;
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
                catch (System.Exception ex)
                {
                    throw ex;
                }
            
            }
        }
    }
}
