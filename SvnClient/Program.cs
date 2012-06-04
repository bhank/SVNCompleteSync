using System;
using System.IO;
using System.Net;
using SharpSvn;
using System.Collections.ObjectModel;

namespace SvnClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // This doesn't affect SVN server certs
            //ServicePointManager.ServerCertificateValidationCallback += delegate { Console.WriteLine("ServerCertificateValidationCallback!"); return true; };

            Parameters parameters;
            if (!Parameters.TryParse(args, out parameters))
            {
                return;
            }

            switch (parameters.Command)
            {
                case Command.CompleteSync:
                    CompleteSync(parameters);
                    break;
                case Command.CheckoutUpdate:
                    CheckoutUpdate(parameters);
                    break;
            }
        }

        private static void CheckoutUpdate(Parameters parameters)
        {
            using (var client = new SharpSvn.SvnClient())
            {
                SetUpClient(parameters, client);

                var target = SvnTarget.FromString(parameters.Path);
                SvnInfoEventArgs svnInfoEventArgs;
                SvnUpdateResult svnUpdateResult;

                var nonExistentUrl = false;
                EventHandler<SvnErrorEventArgs> ignoreNonexistent = (o, eventArgs) =>
                                                                        {
                                                                            nonExistentUrl = false;
                                                                            //if (eventArgs.Exception.SubversionErrorCode == 170000)
                                                                            if(eventArgs.Exception.Message.Contains("non-existent in revision"))
                                                                            {
                                                                                nonExistentUrl = true;
                                                                                eventArgs.Cancel = true;
                                                                            }
                                                                        };

                if(client.GetWorkingCopyRoot(parameters.Path) == null)
                {
                    client.SvnError += ignoreNonexistent;
                    var getInfoSucceeded = client.GetInfo(SvnUriTarget.FromString(parameters.Url), out svnInfoEventArgs);
                    client.SvnError -= ignoreNonexistent;

                    if(!getInfoSucceeded)
                    {
                        if (nonExistentUrl)
                        {
                            Console.WriteLine("SVN info reported nonexistent URL; creating remote directory.");
                            if (!client.RemoteCreateDirectory(new Uri(parameters.Url), new SvnCreateDirectoryArgs {CreateParents = true, LogMessage = parameters.Message}))
                            {
                                throw new Exception("Create directory failed on " + parameters.Url);
                            }
                        }
                        else
                        {
                            throw new Exception("SVN info failed");
                        }
                    }

                    if (client.CheckOut(SvnUriTarget.FromString(parameters.Url), parameters.Path, out svnUpdateResult))
                    {
                        Console.WriteLine("Checked out r" + svnUpdateResult.Revision);
                        return;
                    }

                    throw new Exception("SVN checkout failed");
                }

                if(!client.GetInfo(target, out svnInfoEventArgs))
                {
                    throw new Exception("SVN info failed");
                }

                if(!UrlsMatch(svnInfoEventArgs.Uri.ToString(), parameters.Url))
                {
                    throw new Exception(string.Format("A different URL is already checked out ({0} != {1})", svnInfoEventArgs.Uri, parameters.Url));
                }
                
                if(parameters.Cleanup)
                {
                    client.CleanUp(parameters.Path);
                }

                if(parameters.Revert)
                {
                    client.Revert(parameters.Path);
                }

                if (parameters.DeleteUnversioned)
                {
                    Collection<SvnStatusEventArgs> changedFiles;
                    client.GetStatus(parameters.Path, out changedFiles);
                    foreach (var changedFile in changedFiles)
                    {
                        if(changedFile.LocalContentStatus == SvnStatus.NotVersioned)
                        {
                            File.Delete(changedFile.FullPath);
                        }
                    }
                }

                if(client.Update(parameters.Path, out svnUpdateResult))
                {
                    Console.WriteLine("Updated to r" + svnUpdateResult.Revision);
                    return;
                }

                throw new Exception("SVN update failed");
            }
        }

        private static bool UrlsMatch(string url1, string url2)
        {
            if (url1 == url2) return true;

            // Ignore trailing slash
            return url1.TrimEnd('/') == url2.TrimEnd('/');
        }

        // TODO: make trusting not the default
        private static void TrustUnsignedCertificates(SharpSvn.SvnClient client)
        {
            client.Authentication.SslServerTrustHandlers += (sender, e) =>
                                                                {
                                                                    Console.WriteLine("Certificate errors: " + e.Failures);
                                                                    e.AcceptedFailures = e.Failures;
                                                                    e.Save = true; // Save it for future use?

                                                                };
            //client.Authentication.SslServerTrustHandlers += Authentication_SslServerTrustHandlers;
        }

        public static void CompleteSync(Parameters parameters)
        {
            using (var client = new SharpSvn.SvnClient())
            {
                SetUpClient(parameters, client);

                if (parameters.UpdateBeforeCompleteSync)
                {
                    client.Update(parameters.Path);
                }

                Collection<SvnStatusEventArgs> changedFiles;
                client.GetStatus(parameters.Path, out changedFiles);
                if(changedFiles.Count == 0)
                {
                    Console.WriteLine("No changes to commit.");
                }

                //delete files from subversion that are not in filesystem
                //add files to subversion that are new in filesystem
                //modified files are automatically included as part of the commit

                //TODO: check remoteStatus
                foreach (var changedFile in changedFiles)
                {
                    if (changedFile.LocalContentStatus == SvnStatus.Missing)
                    {
                        // SVN thinks file is missing but it still exists hence
                        // a change in the case of the filename.
                        if (File.Exists(changedFile.Path))
                        {
                            var changedArgs = new SvnDeleteArgs {KeepLocal = true};
                            client.Delete(changedFile.Path, changedArgs);
                        }
                        else
                            client.Delete(changedFile.Path);
                    }
                    if (changedFile.LocalContentStatus == SvnStatus.NotVersioned)
                    {
                        client.Add(changedFile.Path);
                    }
                }

                var ca = new SvnCommitArgs {LogMessage = parameters.Message};
                SvnCommitResult result;
                client.Commit(parameters.Path, ca, out result);
                if(!string.IsNullOrEmpty(result.PostCommitError))
                {
                    Console.WriteLine("Post-commit hook error: " + result.PostCommitError);
                    return;
                }
                Console.WriteLine("Committed r" + result.Revision);
            }
        }

        private static void SetUpClient(Parameters parameters, SharpSvn.SvnClient client)
        {
            if (!string.IsNullOrEmpty(parameters.Username) && !string.IsNullOrEmpty(parameters.Password))
            {
                client.Authentication.Clear(); // prevents checking cached credentials
                client.Authentication.DefaultCredentials = new NetworkCredential(parameters.Username, parameters.Password);
            }

            if(parameters.TrustServerCert)
            {
                TrustUnsignedCertificates(client);
            }
        }
    }
}
